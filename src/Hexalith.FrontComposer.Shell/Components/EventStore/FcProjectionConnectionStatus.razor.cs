using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.EventStore;

/// <summary>Inline EventStore projection connection status indicator.</summary>
public partial class FcProjectionConnectionStatus : ComponentBase, IDisposable, IAsyncDisposable {
    private const string AnnouncementSurface = "projection-connection";
    private const string ConnectivityModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-connectivity.js";
    private readonly string _episodeScope = Guid.NewGuid().ToString("N");
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
    private bool _offline;
    private long _offlineEpisode;
    private IJSObjectReference? _connectivityModule;
    private DotNetObjectReference<FcProjectionConnectionStatus>? _connectivityReference;
    private int _connectivityWatchId;
    private bool _connectivityReady;
    private readonly SemaphoreSlim _connectivityGate = new(1, 1);
    private ITimer? _connectivityRetryTimer;
    private long _onlineEpisode;
    private long _recoveryEpisode;
    private string? _recoveryGroup;

    [Inject]
    private IProjectionConnectionState ConnectionState { get; set; } = default!;

    [Inject]
    private IReconnectionReconciliationState ReconciliationState { get; set; } = default!;

    [Inject]
    private IOptionsMonitor<FcShellOptions> Options { get; set; } = default!;

    [Inject]
    private TimeProvider Time { get; set; } = default!;

    [Inject]
    private ISurfaceAnnouncementCoordinator Announcements { get; set; } = default!;

    [Inject]
    private IStringLocalizer<FcShellResources> AnnouncementLocalizer { get; set; } = default!;

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    [Inject]
    private IServiceProvider Services { get; set; } = default!;

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

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        await TryInitializeConnectivityAsync().ConfigureAwait(true);
    }

    private async Task TryInitializeConnectivityAsync() {
        if (_connectivityReady || _disposed != 0) {
            return;
        }

        bool gateEntered = false;
        try {
            await _connectivityGate.WaitAsync().ConfigureAwait(true);
            gateEntered = true;
            if (_disposed != 0) {
                return;
            }
            _connectivityModule ??= await JS.InvokeAsync<IJSObjectReference>("import", ConnectivityModulePath).ConfigureAwait(true);
            _connectivityReference ??= DotNetObjectReference.Create(this);
            if (_connectivityWatchId == 0) {
                _connectivityWatchId = await _connectivityModule.InvokeAsync<int>("watchConnectivity", _connectivityReference).ConfigureAwait(true);
            }
            bool online = await _connectivityModule.InvokeAsync<bool>("isOnline").ConfigureAwait(true);
            _connectivityReady = true;
            _connectivityRetryTimer?.Dispose();
            _connectivityRetryTimer = null;
            if (_disposed == 0) {
                await OnBrowserConnectivityChanged(online).ConfigureAwait(true);
            }
        }
        catch (JSDisconnectedException) {
            ScheduleConnectivityRetry();
        }
        catch (JSException) {
            ScheduleConnectivityRetry();
        }
        catch (InvalidOperationException) {
            ScheduleConnectivityRetry();
        }
        finally {
            if (gateEntered) {
                _connectivityGate.Release();
            }
        }
    }

    private void ScheduleConnectivityRetry() {
        if (_disposed != 0 || _connectivityReady || _connectivityRetryTimer is not null) {
            return;
        }
        _connectivityRetryTimer = Time.CreateTimer(_ => _ = InvokeAsync(async () => {
            _connectivityRetryTimer?.Dispose();
            _connectivityRetryTimer = null;
            await TryInitializeConnectivityAsync().ConfigureAwait(true);
        }), null, TimeSpan.FromSeconds(1), Timeout.InfiniteTimeSpan);
    }

    /// <summary>Receives actual browser online/offline evidence from the connectivity module.</summary>
    [JSInvokable]
    public async Task OnBrowserConnectivityChanged(bool online) {
        if (_disposed != 0 || (_offline == !online && ConnectionState.Current.BrowserOffline == !online)) {
            return;
        }

        _offline = !online;
        ConnectionState.SetBrowserOffline(_offline);
        if (_offline) {
            CancelClearTimer();
            _showReconnected = false;
            if (_recoveryGroup is { } recoveryGroup) {
                Announcements.Cancel(AnnouncementSurface, recoveryGroup);
                _recoveryGroup = null;
            }
            _offlineEpisode++;
            Announcements.Announce(
                AnnouncementSurface,
                ScopedEpisode("browser-offline", _offlineEpisode),
                "offline",
                AnnouncementLocalizer["Am30Offline"],
                immediate: true);
        }
        else {
            Announcements.Cancel(AnnouncementSurface, ScopedEpisode("browser-offline", _offlineEpisode));
            _onlineEpisode++;
            if (ConnectionState.Current.Status is not ProjectionConnectionStatus.Connected) {
                Announcements.Announce(
                    AnnouncementSurface,
                    ScopedEpisode("browser-online-disconnected", _onlineEpisode),
                    "fallback",
                    AnnouncementLocalizer["Am06Fallback"],
                    immediate: true);
            }
            if (Services.GetService(typeof(IReconnectionReconciliationCoordinator)) is IReconnectionReconciliationCoordinator coordinator) {
                _ = coordinator.ReconcileAsync();
            }
        }

        await InvokeAsync(StateHasChanged).ConfigureAwait(true);
    }

    private void OnConnectionChanged(ProjectionConnectionSnapshot snapshot) {
        if (_disposed != 0) {
            return;
        }

        _ = InvokeAsync(() => {
            if (_disposed != 0 || !ReferenceEquals(ConnectionState.Current, snapshot)) {
                return;
            }

            _snapshot = snapshot;
            if (snapshot.Status is ProjectionConnectionStatus.Connected) {
                Announcements.Cancel(AnnouncementSurface, "connection:" + snapshot.Epoch);
                Announcements.Cancel(AnnouncementSurface, ScopedEpisode("browser-online-disconnected", _onlineEpisode));
            }
            if (!_offline && snapshot.Status is (ProjectionConnectionStatus.Reconnecting or ProjectionConnectionStatus.Disconnected)) {
                Announcements.Announce(
                    AnnouncementSurface,
                    "connection:" + snapshot.Epoch,
                    snapshot.Status.ToString(),
                    AnnouncementLocalizer[snapshot.Status == ProjectionConnectionStatus.Disconnected
                        ? "Am06Fallback" : "Am05Reconnecting"]);
            }
            if (snapshot.IsDisconnected || _reconciliation.Status is ReconnectionReconciliationStatus.Reconciling) {
                CancelClearTimer();
                _showReconnected = false;
                CancelRecoveryAnnouncement();
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

            if (!ReferenceEquals(ReconciliationState.Current, snapshot)) {
                return;
            }

            // P31 — connection-status precedence wins. A late Refreshed snapshot from a
            // superseded reconnect epoch must never reopen a cleared status while we are
            // already disconnected/reconnecting. Stale-epoch snapshots are also ignored:
            // if a Refreshed lands for an older epoch than what we already saw, drop it.
            if (snapshot.Status is ReconnectionReconciliationStatus.Refreshed
                && (_offline
                    || _snapshot.IsDisconnected
                    || _snapshot.Status is ProjectionConnectionStatus.Reconnecting
                    || snapshot.Epoch < _reconciliation.Epoch)) {
                return;
            }

            _reconciliation = snapshot;
            CancelClearTimer();
            _showReconnected = snapshot.Status is ReconnectionReconciliationStatus.Refreshed;
            if (!_showReconnected) {
                CancelRecoveryAnnouncement();
            }
            if (_showReconnected) {
                _recoveryGroup = ScopedEpisode("recovery", ++_recoveryEpisode);
                Announcements.Announce(
                    AnnouncementSurface,
                    _recoveryGroup,
                    "recovered",
                    AnnouncementLocalizer[snapshot.DataRead ? "Am07Recovery" : "Am07ConnectionRestored"],
                    terminal: true);
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
                    if (_recoveryGroup is { } recoveryGroup) {
                        Announcements.Cancel(AnnouncementSurface, recoveryGroup);
                        _recoveryGroup = null;
                    }
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

    private void CancelRecoveryAnnouncement() {
        if (_recoveryGroup is { } recoveryGroup) {
            Announcements.Cancel(AnnouncementSurface, recoveryGroup);
            _recoveryGroup = null;
        }
    }

    private string ScopedEpisode(string kind, long episode) =>
        string.Concat(kind, ":", _episodeScope, ":", episode.ToString(System.Globalization.CultureInfo.InvariantCulture));

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        _subscription?.Dispose();
        _reconciliationSubscription?.Dispose();
        _connectivityRetryTimer?.Dispose();
        _connectivityRetryTimer = null;
        CancelClearTimer();
        Announcements.Clear(AnnouncementSurface);
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        Dispose();
        await _connectivityGate.WaitAsync().ConfigureAwait(true);
        try {
        if (_connectivityModule is not null) {
            try {
                await _connectivityModule.InvokeVoidAsync("unwatchConnectivity", _connectivityWatchId).ConfigureAwait(true);
                await _connectivityModule.DisposeAsync().ConfigureAwait(true);
            }
            catch (JSDisconnectedException) {
            }
            catch (JSException) {
            }
        }

        _connectivityReference?.Dispose();
        }
        finally {
            _connectivityGate.Release();
        }
        GC.SuppressFinalize(this);
    }
}
