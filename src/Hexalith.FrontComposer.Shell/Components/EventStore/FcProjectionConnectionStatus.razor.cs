using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Components.EventStore;

/// <summary>Inline EventStore projection connection status indicator.</summary>
public partial class FcProjectionConnectionStatus : ComponentBase, IDisposable {
    private IDisposable? _subscription;
    private IDisposable? _reconciliationSubscription;
    private ITimer? _clearTimer;
    private long _clearTimerGeneration;
    private ProjectionConnectionSnapshot _snapshot = new(
        ProjectionConnectionStatus.Connected,
        DateTimeOffset.MinValue,
        ReconnectAttempt: 0,
        LastFailureCategory: null);
    private bool _showReconnected;
    private ReconnectionReconciliationSnapshot _reconciliation = new(
        ReconnectionReconciliationStatus.Idle,
        Epoch: 0,
        Changed: false,
        LastTransitionAt: DateTimeOffset.MinValue);
    private int _disposed;

    [Inject]
    private IProjectionConnectionState ConnectionState { get; set; } = default!;

    [Inject]
    private IReconnectionReconciliationState ReconciliationState { get; set; } = default!;

    [Inject]
    private IOptionsMonitor<FcShellOptions> Options { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized() {
        // P30 — if the second Subscribe throws, ensure the first subscription does not leak.
        _subscription = ConnectionState.Subscribe(OnConnectionChanged);
        try {
            _reconciliationSubscription = ReconciliationState.Subscribe(OnReconciliationChanged);
        }
        catch {
            _subscription?.Dispose();
            _subscription = null;
            throw;
        }
    }

    private void OnConnectionChanged(ProjectionConnectionSnapshot snapshot) {
        if (_disposed != 0) {
            return;
        }

        _ = InvokeAsync(() => {
            if (_disposed != 0) {
                return;
            }

            _snapshot = snapshot;
            if (snapshot.IsDisconnected || _reconciliation.Status is ReconnectionReconciliationStatus.Reconciling) {
                CancelClearTimer();
                _showReconnected = false;
            }

            StateHasChanged();
        });
    }

    private void OnReconciliationChanged(ReconnectionReconciliationSnapshot snapshot) {
        if (_disposed != 0) {
            return;
        }

        _ = InvokeAsync(() => {
            if (_disposed != 0) {
                return;
            }

            // P31 — connection-status precedence wins. A late Refreshed snapshot from a
            // superseded reconnect epoch must never reopen a cleared status while we are
            // already disconnected/reconnecting. Stale-epoch snapshots are also ignored:
            // if a Refreshed lands for an older epoch than what we already saw, drop it.
            if (snapshot.Status is ReconnectionReconciliationStatus.Refreshed
                && (_snapshot.IsDisconnected
                    || _snapshot.Status is ProjectionConnectionStatus.Reconnecting
                    || snapshot.Epoch < _reconciliation.Epoch)) {
                return;
            }

            _reconciliation = snapshot;
            CancelClearTimer();
            _showReconnected = snapshot.Status is ReconnectionReconciliationStatus.Refreshed && snapshot.Changed;
            if (_showReconnected) {
                StartClearTimer();
            }

            StateHasChanged();
        });
    }

    private void StartClearTimer() {
        // P32 — clamp the configured duration to a sane window. ITimer.CreateTimer with a
        // negative TimeSpan throws ArgumentOutOfRangeException, and a one-hour ceiling caps
        // any pathological misconfiguration.
        long configuredMs = Options.CurrentValue.ProjectionReconnectedNoticeDurationMs;
        long boundedMs = Math.Clamp(configuredMs, 1, 60_000);
        long generation = Interlocked.Increment(ref _clearTimerGeneration);

        // P33 — atomically replace the timer reference so a second StartClearTimer call cannot
        // leak a previously-allocated timer between the assignment and CancelClearTimer.
        ITimer newTimer = Time.CreateTimer(
            _ => {
                if (_disposed != 0) {
                    return;
                }

                _ = InvokeAsync(() => {
                    if (_disposed != 0 || Interlocked.Read(ref _clearTimerGeneration) != generation) {
                        return;
                    }

                    _showReconnected = false;
                    // P34 — generation counter breaks the loop. ReconciliationState.Reset()
                    // synchronously notifies subscribers; OnReconciliationChanged queues a
                    // follow-up InvokeAsync but its check sees the bumped generation as stale
                    // (CancelClearTimer increments it before the second continuation runs) so
                    // no new timer is started.
                    ReconciliationState.Reset(_reconciliation.Epoch);
                    StateHasChanged();
                });
            },
            state: null,
            dueTime: TimeSpan.FromMilliseconds(boundedMs),
            period: Timeout.InfiniteTimeSpan);

        ITimer? previous = Interlocked.Exchange(ref _clearTimer, newTimer);
        previous?.Dispose();
    }

    private void CancelClearTimer() {
        // Bumping the generation invalidates any in-flight callback from the previous timer
        // before disposing it.
        _ = Interlocked.Increment(ref _clearTimerGeneration);
        ITimer? timer = Interlocked.Exchange(ref _clearTimer, null);
        timer?.Dispose();
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        _subscription?.Dispose();
        _reconciliationSubscription?.Dispose();
        CancelClearTimer();
        GC.SuppressFinalize(this);
    }
}
