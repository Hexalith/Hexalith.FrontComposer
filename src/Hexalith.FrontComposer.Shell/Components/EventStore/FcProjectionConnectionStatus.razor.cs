using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Components.EventStore;

/// <summary>Inline EventStore projection connection status indicator.</summary>
public partial class FcProjectionConnectionStatus : ComponentBase, IDisposable {
    private IDisposable? _subscription;
    private ITimer? _clearTimer;
    private long _clearTimerGeneration;
    private ProjectionConnectionSnapshot _snapshot = new(
        ProjectionConnectionStatus.Connected,
        DateTimeOffset.MinValue,
        ReconnectAttempt: 0,
        LastFailureCategory: null);
    private bool _showReconnected;
    private int _disposed;

    [Inject]
    private IProjectionConnectionState ConnectionState { get; set; } = default!;

    [Inject]
    private IOptionsMonitor<FcShellOptions> Options { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnInitialized()
        => _subscription = ConnectionState.Subscribe(OnConnectionChanged);

    private void OnConnectionChanged(ProjectionConnectionSnapshot snapshot) {
        if (_disposed != 0) {
            return;
        }

        _ = InvokeAsync(() => {
            if (_disposed != 0) {
                return;
            }

            bool wasUnavailable = _snapshot.IsDisconnected;
            _snapshot = snapshot;
            CancelClearTimer();
            _showReconnected = wasUnavailable && snapshot.Status is ProjectionConnectionStatus.Connected;
            if (_showReconnected) {
                // P8 — capture the generation that owns this timer. The timer callback only
                // mutates state when the generation it captured still matches the current
                // generation; stale callbacks from prior reconnect cycles are no-ops.
                long generation = Interlocked.Increment(ref _clearTimerGeneration);
                _clearTimer = Time.CreateTimer(
                    _ => {
                        if (_disposed != 0) {
                            return;
                        }

                        _ = InvokeAsync(() => {
                            if (_disposed != 0 || Interlocked.Read(ref _clearTimerGeneration) != generation) {
                                return;
                            }

                            _showReconnected = false;
                            StateHasChanged();
                        });
                    },
                    state: null,
                    dueTime: TimeSpan.FromMilliseconds(Options.CurrentValue.ProjectionReconnectedNoticeDurationMs),
                    period: Timeout.InfiniteTimeSpan);
            }

            StateHasChanged();
        });
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
        CancelClearTimer();
        GC.SuppressFinalize(this);
    }
}
