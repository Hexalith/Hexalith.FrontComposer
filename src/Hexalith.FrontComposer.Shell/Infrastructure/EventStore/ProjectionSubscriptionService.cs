using System.Collections.Concurrent;
using System.Diagnostics;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.PendingCommands;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Default SignalR-backed projection subscription service.
/// </summary>
internal sealed class ProjectionSubscriptionService : IProjectionScopedSubscription, IAsyncDisposable {
    private static readonly TimeSpan GateWaitTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan DisposalWaitTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ClosedRestartTimeout = TimeSpan.FromSeconds(10);

    private readonly IProjectionHubConnection _connection;
    private readonly IProjectionConnectionState _connectionState;
    private readonly IProjectionFallbackRefreshScheduler _refreshScheduler;
    private readonly IProjectionChangeNotifier _notifier;
    private readonly ILogger<ProjectionSubscriptionService> _logger;
    private readonly Func<CancellationToken, ValueTask<string?>>? _configuredAccessTokenProvider;
    private readonly FrontComposerAccessTokenProvider? _frontComposerAccessTokenProvider;
    private readonly bool _requireAccessToken;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<GroupKey, GroupState> _activeGroups = new();
    private readonly IDisposable _projectionChangedRegistration;
    private readonly IDisposable _projectionChangedDetailRegistration;
    private readonly IDisposable _connectionStateRegistration;
    private readonly CancellationTokenSource _disposalCts = new();
    private readonly ProjectionFallbackPollingDriver? _fallbackDriver;
    private readonly PendingCommandPollingDriver? _commandPollingDriver;
    private readonly IReconnectionReconciliationCoordinator? _reconciliationCoordinator;
    private readonly IPendingCommandPollingCoordinator? _pendingCommandPolling;
    private Func<CancellationToken, ValueTask<string?>>? _connectionAccessTokenProvider;
    /// <summary>P2-P19 — debounces concurrent live-nudge invocations so a burst of N nudges produces at most one in-flight `PollOnceAsync`.</summary>
    private int _pendingPollInFlight;
    private int _closedRestartInFlight;
    private int _restartConnectedStateSuppression;
    private int _disposeStarted;
    private bool _disposed;

    public ProjectionSubscriptionService(
        IOptions<EventStoreOptions> options,
        IProjectionHubConnectionFactory connectionFactory,
        IProjectionConnectionState connectionState,
        IProjectionFallbackRefreshScheduler refreshScheduler,
        IProjectionChangeNotifier notifier,
        ILogger<ProjectionSubscriptionService> logger,
        ProjectionFallbackPollingDriver? fallbackDriver = null,
        IReconnectionReconciliationCoordinator? reconciliationCoordinator = null,
        IPendingCommandPollingCoordinator? pendingCommandPolling = null,
        IUserContextAccessor? userContextAccessor = null,
        IOptions<FcShellOptions>? shellOptions = null,
        PendingCommandPollingDriver? commandPollingDriver = null,
        FrontComposerAccessTokenProvider? frontComposerAccessTokenProvider = null,
        IOptionsMonitor<FcShellOptions>? shellOptionsMonitor = null) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(connectionState);
        ArgumentNullException.ThrowIfNull(refreshScheduler);
        ArgumentNullException.ThrowIfNull(notifier);

        _connectionState = connectionState;
        _refreshScheduler = refreshScheduler;
        _notifier = notifier;
        _logger = logger;
        _fallbackDriver = fallbackDriver;
        _commandPollingDriver = commandPollingDriver;
        _reconciliationCoordinator = reconciliationCoordinator;
        _pendingCommandPolling = pendingCommandPolling;
        _frontComposerAccessTokenProvider = frontComposerAccessTokenProvider;
        _userContextAccessor = userContextAccessor;
        _shellOptions = shellOptions;
        _shellOptionsMonitor = shellOptionsMonitor;
        EventStoreOptions current = options.Value;
        _requireAccessToken = current.RequireAccessToken;
        _configuredAccessTokenProvider = current.AccessTokenProvider;
        Uri hubUri = BuildHubUri(current.BaseAddress ?? throw new InvalidOperationException("EventStore BaseAddress is required."), current.ProjectionChangesHubPath);
        _connection = connectionFactory.Create(
            hubUri,
            _configuredAccessTokenProvider is null ? null : GetConnectionAccessTokenAsync);
        _projectionChangedRegistration = _connection.OnProjectionChanged(OnProjectionChangedAsync);
        _projectionChangedDetailRegistration = _connection.OnProjectionChangedDetail(OnProjectionChangedDetailAsync);
        _connectionStateRegistration = _connection.OnConnectionStateChanged(OnConnectionStateChangedAsync);
        // DN1 — wire the bounded fallback polling driver. The driver subscribes to connection
        // state and only runs while disconnected; injection is optional so test harnesses without
        // a driver still construct cleanly.
        _fallbackDriver?.Start();
        _commandPollingDriver?.Start();
    }

    private readonly IUserContextAccessor? _userContextAccessor;
    private readonly IOptions<FcShellOptions>? _shellOptions;
    private readonly IOptionsMonitor<FcShellOptions>? _shellOptionsMonitor;

    public Task SubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
        => SubscribeAsync(projectionType, tenantId, scope: null, cancellationToken);

    public async Task SubscribeAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken = default) {
        TenantContextSnapshot? context = ResolveTenantContext(tenantId, "projection-subscribe");
        GroupKey key = ValidateGroup(projectionType, context?.TenantId ?? tenantId, scope);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            ThrowIfDisposed();
            if (_activeGroups.ContainsKey(key)) {
                return;
            }

            if (!_connection.IsConnected) {
                try {
                    await EnsureRequiredAccessTokenAvailableAsync(cancellationToken).ConfigureAwait(false);
                    await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
                }
                catch {
                    _connectionState.Apply(new ProjectionConnectionTransition(
                        ProjectionConnectionStatus.Disconnected,
                        FailureCategory: "InitialStartFailed"));
                    throw;
                }
            }

            ThrowIfDisposed();
            await _connection.JoinGroupAsync(key.ProjectionType, key.TenantId, key.Scope, cancellationToken).ConfigureAwait(false);
            ThrowIfDisposed();
            _ = _activeGroups.TryAdd(key, new GroupState(GroupHealth.Active, context));
        }
        finally {
            _ = _gate.Release();
        }
    }

    public Task UnsubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
        => UnsubscribeAsync(projectionType, tenantId, scope: null, cancellationToken);

    public async Task UnsubscribeAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken = default) {
        // P2 — unsubscribe must be non-throwing on missing/stale tenant context. Sign-out
        // makes TenantId null on the accessor, and the previous code threw TenantContextException,
        // leaving the group permanently in _activeGroups (no LeaveGroupAsync, no removal). A
        // subsequent re-sign-in short-circuited at `_activeGroups.ContainsKey(key) → return`,
        // silently keeping a stale subscription.
        GroupKey key = ValidateGroup(projectionType, tenantId, scope);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (!_activeGroups.ContainsKey(key) || _disposed) {
                return;
            }

            // Transport leave failures propagate to callers (a throw-on-leave fixture pins this);
            // on failure the group intentionally stays in _activeGroups so a retry can leave again.
            await _connection.LeaveGroupAsync(key.ProjectionType, key.TenantId, key.Scope, cancellationToken).ConfigureAwait(false);
            _ = _activeGroups.TryRemove(key, out _);
        }
        finally {
            _ = _gate.Release();
        }
    }

    public async ValueTask DisposeAsync() {
        if (Interlocked.Exchange(ref _disposeStarted, 1) != 0) {
            return;
        }

        // Signal disposal to background rejoin/nudge tasks before taking the gate so they can
        // observe cancellation while we wait for the gate.
        try {
            _disposalCts.Cancel();
        }
        catch (ObjectDisposedException) {
            // Already disposed; nothing to do.
        }

        bool gateAcquired = await _gate.WaitAsync(GateWaitTimeout).ConfigureAwait(false);
        if (!gateAcquired) {
            _logger.LogWarning(
                "EventStore projection subscription disposal timed out waiting for the operation gate. FailureCategory={FailureCategory}",
                "Timeout");
        }

        try {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _projectionChangedRegistration.Dispose();
            _projectionChangedDetailRegistration.Dispose();
            _connectionStateRegistration.Dispose();
            if (_fallbackDriver is not null) {
                await DisposeBoundedAsync(
                    static driver => driver.DisposeAsync(),
                    _fallbackDriver,
                    nameof(ProjectionFallbackPollingDriver)).ConfigureAwait(false);
            }
            if (_commandPollingDriver is not null) {
                await DisposeBoundedAsync(
                    static driver => driver.DisposeAsync(),
                    _commandPollingDriver,
                    nameof(PendingCommandPollingDriver)).ConfigureAwait(false);
            }

            if (gateAcquired) {
                _activeGroups.Clear();
                await RunBoundedDisposalOperationAsync(
                    nameof(IProjectionHubConnection.StopAsync),
                    token => _connection.StopAsync(token)).ConfigureAwait(false);
                await DisposeBoundedAsync(
                    static connection => connection.DisposeAsync(),
                    _connection,
                    nameof(IProjectionHubConnection)).ConfigureAwait(false);
            }
        }
        catch (Exception ex) {
            _logger.LogWarning("EventStore projection subscription disposal failed. FailureCategory={FailureCategory}", ex.GetType().Name);
        }
        finally {
            if (gateAcquired) {
                _ = _gate.Release();
            }

            if (gateAcquired) {
                _disposalCts.Dispose();
            }
        }
    }

    private async Task OnProjectionChangedAsync(string projectionType, string tenantId) {
        if (_disposed) {
            return;
        }

        GroupKey key;
        try {
            key = ValidateGroup(projectionType, tenantId);
        }
        catch (ArgumentException) {
            return;
        }

        // DN2 — only Active groups receive nudge refresh. Degraded groups (failed rejoin)
        // wait for the next reconnect cycle to attempt recovery.
        // P3 — Blocked groups may recover on a subsequent nudge if the authenticated context
        // now matches the captured snapshot (e.g., after a logout+relogin to the same identity
        // without a connection drop). IsGroupContextCurrent transitions Blocked→Active when
        // the validation succeeds.
        if (_activeGroups.TryGetValue(key, out GroupState state)
            && (state.Health == GroupHealth.Active || state.Health == GroupHealth.Blocked)) {
            if (!IsGroupContextCurrent(key, state, "projection-nudge")) {
                return;
            }

            // Re-read state in case IsGroupContextCurrent flipped Health back to Active.
            if (!_activeGroups.TryGetValue(key, out state) || state.Health != GroupHealth.Active) {
                return;
            }

            // F03 — use the receive-seam span name so it does not double-count under the
            // `frontcomposer.projection.nudge` operation alongside the scheduler's per-refresh
            // span. Operators see "received" and "refreshed" as distinct measurements.
            using Activity? activity = FrontComposerTelemetry.StartProjectionNudgeReceived(
                key.ProjectionType,
                FrontComposerTelemetry.TenantMarker(key.TenantId));
            try {
                if (_notifier is IProjectionChangeNotifierWithTenant tenantAware) {
                    tenantAware.NotifyChanged(key.ProjectionType, key.TenantId);
                }
                else {
                    _notifier.NotifyChanged(key.ProjectionType);
                }

                // P11 — link nudge refresh to the service's disposal CTS so disposal cancels
                // an in-flight refresh promptly.
                _ = await _refreshScheduler.TriggerNudgeRefreshAsync(
                    key.ProjectionType,
                    key.TenantId,
                    _disposalCts.Token).ConfigureAwait(false);

                // Story 5-5 DN1 — feed pending-command resolution from the live nudge path so
                // commands resolve through the SAME shared resolver/state path that the polling
                // and reconnect paths use. PollOnceAsync is bounded by FcShellOptions caps and
                // returns quickly when there are no pending commands.
                // P2-P19 — coalesce bursty live nudges. Each nudge would otherwise trigger its
                // own PollOnceAsync; with default budget=25 and 50 concurrent nudges this fans
                // out to ~1,250 status queries. The in-flight flag swallows concurrent invocations
                // so a burst produces at most one running poll.
                if (_pendingCommandPolling is not null
                    && Interlocked.CompareExchange(ref _pendingPollInFlight, 1, 0) == 0) {
                    try {
                        _ = await _pendingCommandPolling.PollOnceAsync(_disposalCts.Token).ConfigureAwait(false);
                    }
                    finally {
                        _ = Interlocked.Exchange(ref _pendingPollInFlight, 0);
                    }
                }
                FrontComposerTelemetry.SetOutcome(activity, "handled");
            }
            catch (OperationCanceledException) when (_disposalCts.IsCancellationRequested) {
                // Expected on disposal; swallow.
                FrontComposerTelemetry.SetOutcome(activity, "canceled");
            }
            catch (Exception ex) when (ex is not OutOfMemoryException) {
                // A buggy subscriber must not kill the SignalR callback dispatcher.
                FrontComposerTelemetry.SetFailure(activity, ex.GetType().Name);
                _logger.LogWarning(
                    "Projection change subscriber threw while handling nudge. FailureCategory={FailureCategory}",
                    ex.GetType().Name);
            }
        }
    }

    private async Task OnProjectionChangedDetailAsync(ProjectionChangedDetail detail) {
        if (_disposed || detail is null) {
            return;
        }

        // Validate the routing shape defensively; ignore malformed messages. The SignalR server
        // only delivers detail messages to groups this client actually joined, so the payload is
        // surfaced opaquely — FrontComposer adds no AI/domain interpretation (it does NOT trigger
        // the scheduler refresh or pending-command poll; the detail subscriber owns that decision).
        try {
            _ = ValidateGroup(detail.ProjectionType, detail.TenantId, detail.GroupScope);
        }
        catch (ArgumentException) {
            return;
        }

        if (_notifier is not IProjectionChangeDetailNotifier detailNotifier) {
            return;
        }

        using Activity? activity = FrontComposerTelemetry.StartProjectionNudgeReceived(
            detail.ProjectionType,
            FrontComposerTelemetry.TenantMarker(detail.TenantId));
        try {
            await detailNotifier.NotifyDetailAsync(detail, _disposalCts.Token).ConfigureAwait(false);
            FrontComposerTelemetry.SetOutcome(activity, "handled");
        }
        catch (OperationCanceledException) when (_disposalCts.IsCancellationRequested) {
            FrontComposerTelemetry.SetOutcome(activity, "canceled");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            // A buggy detail subscriber must not kill the SignalR callback dispatcher.
            FrontComposerTelemetry.SetFailure(activity, ex.GetType().Name);
            _logger.LogWarning(
                "Projection change detail subscriber threw while handling nudge. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
    }

    private async Task OnConnectionStateChangedAsync(ProjectionHubConnectionStateChanged change) {
        if (_disposed) {
            return;
        }

        switch (change.State) {
            case ProjectionHubConnectionState.Connected:
                if (Interlocked.CompareExchange(ref _restartConnectedStateSuppression, 0, 1) == 1) {
                    break;
                }

                _connectionState.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
                break;

            case ProjectionHubConnectionState.Reconnecting:
                _connectionState.Apply(new ProjectionConnectionTransition(
                    ProjectionConnectionStatus.Reconnecting,
                    FailureCategory: change.Exception?.GetType().Name ?? "Reconnecting",
                    ReconnectAttempt: 1));
                break;

            case ProjectionHubConnectionState.Reconnected:
                await HandleReconnectedEpochAsync(_disposalCts.Token).ConfigureAwait(false);
                break;

            case ProjectionHubConnectionState.Closed:
                // P19 — preserve sticky InitialStartFailed category set inside SubscribeAsync,
                // but do not suppress the Story 11.2 recovery attempt. A later Closed event after
                // RestartFailed/RejoinSkipped is exactly the next recovery signal.
                ProjectionConnectionSnapshot currentSnapshot = _connectionState.Current;
                if (currentSnapshot.Status is not ProjectionConnectionStatus.Disconnected
                    || string.IsNullOrEmpty(currentSnapshot.LastFailureCategory)) {
                    _connectionState.Apply(new ProjectionConnectionTransition(
                        ProjectionConnectionStatus.Disconnected,
                        FailureCategory: change.Exception?.GetType().Name ?? "Closed"));
                }

                await RestartClosedConnectionAsync().ConfigureAwait(false);
                break;
        }
    }

    private async Task HandleReconnectedEpochAsync(CancellationToken cancellationToken) {
        // DN3 — rejoin runs in the handler chain (so tests and adopters can observe
        // completion deterministically) but takes the gate with a bounded timeout and the
        // service disposal token. A blocked subscribe/unsubscribe on the same gate cannot
        // hang rejoin indefinitely; disposal cancels the sweep promptly.
        bool rejoined = await RejoinActiveGroupsAsync(cancellationToken).ConfigureAwait(false);
        if (!rejoined) {
            if (!_disposed && !cancellationToken.IsCancellationRequested) {
                _connectionState.Apply(new ProjectionConnectionTransition(
                    ProjectionConnectionStatus.Disconnected,
                    FailureCategory: "RejoinSkipped"));
            }

            return;
        }

        await CompleteReconnectedEpochAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task CompleteReconnectedEpochAsync(CancellationToken cancellationToken) {
        // H-F6 — do not apply a Connected transition or reconcile once disposal has begun; the
        // gate-free rejoin -> epoch handoff can race a concurrent DisposeAsync that already tore
        // the service down, leaving the shared connection state stuck reporting Connected.
        if (_disposed) {
            return;
        }

        _refreshScheduler.SetReconciliationGroupHealth(SnapshotGroupHealth());
        // DN5=a — Apply Connected unconditionally; per-group degradation surfaces through
        // GroupHealth.Degraded (Story 5-3 P9) and per-lane reconciliation failures.
        _connectionState.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        // P7 — re-check disposal/cancellation between rejoin completion and reconcile.
        if (_reconciliationCoordinator is null) {
            // P8 — log once when DI did not provide the coordinator so a regression is
            // visible instead of silently no-op'ing reconciliation.
            _logger.LogInformation(
                "Projection reconciliation coordinator is not registered. Reconnect catch-up will not run.");
            return;
        }

        if (_disposed || cancellationToken.IsCancellationRequested) {
            return;
        }

        try {
            // P6 — wrap the coordinator call so a buggy reconciliation cannot escape into
            // the SignalR hub state-changed dispatcher (which would terminate the callback
            // chain and prevent further state transitions from propagating).
            _ = await _reconciliationCoordinator.ReconcileAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            // Expected on disposal.
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            _logger.LogWarning(
                "Reconnection reconciliation threw out of the hub callback. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
    }

    private async Task RestartClosedConnectionAsync() {
        if (!CanRestartClosedConnection()) {
            return;
        }

        if (Interlocked.CompareExchange(ref _closedRestartInFlight, 1, 0) != 0) {
            return;
        }

        try {
            while (CanRestartClosedConnection()) {
                using CancellationTokenSource restartCts = CancellationTokenSource.CreateLinkedTokenSource(_disposalCts.Token);
                restartCts.CancelAfter(ClosedRestartTimeout);
                CancellationToken token = restartCts.Token;

                bool gateAcquired = await _gate.WaitAsync(GateWaitTimeout, token).ConfigureAwait(false);
                if (!gateAcquired) {
                    _logger.LogWarning(
                        "EventStore projection hub closed-restart skipped because the operation gate was unavailable. FailureCategory={FailureCategory}",
                        "Timeout");
                    await DelayClosedRestartRetryAsync(token).ConfigureAwait(false);
                    continue;
                }

                bool shouldRetry = false;
                bool completeEpoch = false;
                try {
                    if (!CanRestartClosedConnection()) {
                        return;
                    }

                    await EnsureRequiredAccessTokenAvailableAsync(token).ConfigureAwait(false);
                    if (!_connection.IsConnected) {
                        _ = Interlocked.Exchange(ref _restartConnectedStateSuppression, 1);
                        try {
                            await _connection.StartAsync(token).ConfigureAwait(false);
                        }
                        catch {
                            _ = Interlocked.Exchange(ref _restartConnectedStateSuppression, 0);
                            throw;
                        }

                        _ = Interlocked.Exchange(ref _restartConnectedStateSuppression, 0);
                    }

                    if (await RejoinActiveGroupsCoreAsync(token).ConfigureAwait(false)) {
                        // H-F4 — run the reconcile epoch only after releasing the gate so a
                        // closed-restart recovery does not block concurrent Subscribe/Unsubscribe
                        // for the reconcile duration (mirrors the Reconnected path).
                        completeEpoch = true;
                    }
                    else {
                        _connectionState.Apply(new ProjectionConnectionTransition(
                            ProjectionConnectionStatus.Disconnected,
                            FailureCategory: "RejoinSkipped"));
                        shouldRetry = true;
                    }
                }
                catch (OperationCanceledException) when (_disposalCts.IsCancellationRequested) {
                    throw;
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) {
                    // H-F1 — a single restart attempt hit the per-attempt timeout (not disposal);
                    // keep retrying instead of abandoning the unbounded restart loop, otherwise a
                    // slow server permanently disables realtime recovery.
                    _connectionState.Apply(new ProjectionConnectionTransition(
                        ProjectionConnectionStatus.Disconnected,
                        FailureCategory: "RestartTimeout"));
                    _logger.LogWarning(
                        "EventStore projection hub closed-restart attempt timed out. FailureCategory={FailureCategory}",
                        "Timeout");
                    shouldRetry = true;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) {
                    _connectionState.Apply(new ProjectionConnectionTransition(
                        ProjectionConnectionStatus.Disconnected,
                        FailureCategory: "RestartFailed"));
                    _logger.LogWarning(
                        "EventStore projection hub closed-restart failed. FailureCategory={FailureCategory}",
                        ex.GetType().Name);
                    shouldRetry = true;
                }
                finally {
                    _ = _gate.Release();
                }

                if (completeEpoch) {
                    await CompleteReconnectedEpochAsync(token).ConfigureAwait(false);
                    return;
                }

                // H-F1 — after a per-attempt timeout the 10s wait already provided backoff and
                // `token` is cancelled, so skip the extra retry delay and loop again immediately.
                if (shouldRetry && !token.IsCancellationRequested) {
                    await DelayClosedRestartRetryAsync(token).ConfigureAwait(false);
                }
            }
        }
        catch (OperationCanceledException ex) when (_disposalCts.IsCancellationRequested) {
            _logger.LogWarning(
                "EventStore projection hub closed-restart canceled during disposal. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
        catch (OperationCanceledException ex) {
            _connectionState.Apply(new ProjectionConnectionTransition(
                ProjectionConnectionStatus.Disconnected,
                FailureCategory: "RestartCanceled"));
            _logger.LogWarning(
                "EventStore projection hub closed-restart timed out or was canceled. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
        catch (ObjectDisposedException ex) {
            // H-F6 — concurrent disposal disposed the linked disposal CancellationTokenSource
            // mid-restart (the loop re-entered the body on a stale not-disposed read, then
            // CreateLinkedTokenSource(_disposalCts.Token) observed the disposed source). Treat it
            // as a clean teardown so the "no exception escapes the callback" invariant is upheld
            // here rather than only by the factory's outer per-handler guard.
            _logger.LogWarning(
                "EventStore projection hub closed-restart canceled during disposal. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
        finally {
            _ = Interlocked.Exchange(ref _closedRestartInFlight, 0);
        }
    }

    private bool CanRestartClosedConnection()
        => !_disposed
            && _fallbackDriver is not null
            && CurrentShellOptions().ProjectionFallbackPollingIntervalSeconds > 0
            && !_activeGroups.IsEmpty;

    private async Task<bool> RejoinActiveGroupsAsync(CancellationToken cancellationToken) {
        bool gateAcquired;
        try {
            gateAcquired = await _gate.WaitAsync(GateWaitTimeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return false;
        }

        if (!gateAcquired) {
            _logger.LogWarning(
                "Projection reconnect rejoin skipped because the operation gate was unavailable. FailureCategory={FailureCategory}",
                "Timeout");
            return false;
        }

        try {
            if (_disposed) {
                return false;
            }

            return await RejoinActiveGroupsCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally {
            _ = _gate.Release();
        }
    }

    private async Task<bool> RejoinActiveGroupsCoreAsync(CancellationToken cancellationToken) {
        if (_disposed) {
            return false;
        }

        foreach (GroupKey key in _activeGroups.Keys.OrderBy(static key => key.ProjectionType, StringComparer.Ordinal)) {
                if (cancellationToken.IsCancellationRequested) {
                    return false;
                }

                // P13 — re-check connection per-Join so a mid-loop disconnect stops the sweep
                // instead of flooding logs with RejoinFailed for every remaining group.
                if (!_connection.IsConnected) {
                    return false;
                }

                using Activity? activity = FrontComposerTelemetry.StartProjectionRejoin(
                    key.ProjectionType,
                    FrontComposerTelemetry.TenantMarker(key.TenantId));
                try {
                    if (!_activeGroups.TryGetValue(key, out GroupState state)) {
                        // P4 — group was concurrently unsubscribed; skip without writing a
                        // fabricated entry back. The previous code wrote `state with {...}`
                        // using `default(GroupState)`, creating a Blocked entry with null
                        // TenantContext for a key the caller already removed.
                        continue;
                    }

                    if (!IsGroupContextCurrent(key, state, "projection-rejoin")) {
                        // P3 — Blocked groups *can* recover. IsGroupContextCurrent already wrote
                        // the Blocked state with a TryUpdate guard; we just skip the rejoin.
                        // The Active vs Blocked transition here is owned by IsGroupContextCurrent.
                        continue;
                    }

                    await _connection.JoinGroupAsync(key.ProjectionType, key.TenantId, key.Scope, cancellationToken).ConfigureAwait(false);
                    // P3/P4 — TryUpdate so a concurrent unsubscribe/reconnect does not
                    // resurrect a removed key. If the entry is gone, the rejoin is moot.
                    _ = _activeGroups.TryUpdate(key, state with { Health = GroupHealth.Active }, state);
                    FrontComposerTelemetry.SetOutcome(activity, "active");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return false;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) {
                    // DN2 — mark degraded; nudges skip until the next successful rejoin.
                    // P4 — TryUpdate so a concurrent unsubscribe doesn't resurrect a removed key.
                    if (_activeGroups.TryGetValue(key, out GroupState current)) {
                        _ = _activeGroups.TryUpdate(key, current with { Health = GroupHealth.Degraded }, current);
                    }
                    // P5 — exception type only. Raw exception messages can carry group/tenant
                    // arguments embedded in stack frames or framework-formatted text.
                    FrontComposerTelemetry.SetFailure(activity, ex.GetType().Name);
                    FrontComposerLog.ProjectionRejoinFailed(_logger, key.ProjectionType, ex.GetType().Name);
                }
            }

        return true;
    }

    private IReadOnlyDictionary<ProjectionFallbackGroupKey, bool> SnapshotGroupHealth() {
        Dictionary<ProjectionFallbackGroupKey, bool> snapshot = [];
        foreach (KeyValuePair<GroupKey, GroupState> group in _activeGroups) {
            snapshot[new ProjectionFallbackGroupKey(group.Key.ProjectionType, group.Key.TenantId)] = group.Value.Health == GroupHealth.Active;
        }

        return snapshot;
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(ProjectionSubscriptionService));
        }
    }

    private async ValueTask EnsureRequiredAccessTokenAvailableAsync(CancellationToken cancellationToken) {
        if (_configuredAccessTokenProvider is null) {
            if (_requireAccessToken) {
                throw new InvalidOperationException("EventStore access token provider is required.");
            }

            return;
        }

        await EnsureConnectionAccessTokenProviderCapturedAsync(cancellationToken).ConfigureAwait(false);

        if (_requireAccessToken) {
            _ = await GetConnectionAccessTokenAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async ValueTask EnsureConnectionAccessTokenProviderCapturedAsync(CancellationToken cancellationToken) {
        if (_connectionAccessTokenProvider is not null) {
            return;
        }

        if (_frontComposerAccessTokenProvider is not null) {
            Func<CancellationToken, ValueTask<string?>>? captured =
                await _frontComposerAccessTokenProvider.CaptureCurrentUserAccessTokenProviderAsync(cancellationToken).ConfigureAwait(false);
            if (captured is not null) {
                _connectionAccessTokenProvider = captured;
                return;
            }
        }

        _connectionAccessTokenProvider = _configuredAccessTokenProvider;
    }

    private async ValueTask<string?> GetConnectionAccessTokenAsync(CancellationToken cancellationToken) {
        Func<CancellationToken, ValueTask<string?>>? provider = _connectionAccessTokenProvider ?? _configuredAccessTokenProvider;
        return provider is null
            ? null
            : await EventStoreAccessTokenGuard.GetRequiredTokenAsync(provider, _requireAccessToken, cancellationToken).ConfigureAwait(false);
    }

    private static GroupKey ValidateGroup(string projectionType, string tenantId, string? scope = null)
        => new(
            EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType)),
            EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId)),
            NormalizeScope(scope));

    private static string? NormalizeScope(string? scope)
        => string.IsNullOrWhiteSpace(scope)
            ? null
            : EventStoreValidation.RequireNonColonSegment(scope, nameof(scope));

    private TenantContextSnapshot? ResolveTenantContext(string? requestedTenant, string operationKind) {
        if (_userContextAccessor is null) {
            return null;
        }

        return FrontComposerTenantContextAccessor
            .Resolve(
                _userContextAccessor,
                CurrentShellOptions(),
                _logger,
                requestedTenant,
                operationKind)
            .EnsureSuccess();
    }

    private bool IsGroupContextCurrent(GroupKey key, GroupState state, string operationKind) {
        if (_userContextAccessor is null || state.TenantContext is null) {
            return true;
        }

        // P5 — explicitly compare BOTH tenant and user against the original snapshot, do NOT
        // pass key.TenantId as requestedTenant (which would categorize a tenant change as
        // TenantMismatch (HFC2017) instead of StaleTenantContext (HFC2019)).
        TenantContextResult current = FrontComposerTenantContextAccessor.Resolve(
            _userContextAccessor,
            CurrentShellOptions(),
            _logger,
            requestedTenant: null,
            operationKind);
        if (!current.Succeeded || current.Context is null) {
            // P4 — TryUpdate so we don't fabricate a default GroupState entry for a key
            // that was concurrently unsubscribed.
            _ = _activeGroups.TryUpdate(key, state with { Health = GroupHealth.Blocked }, state);
            return false;
        }

        bool matches = string.Equals(current.Context.TenantId, state.TenantContext.TenantId, StringComparison.Ordinal)
            && string.Equals(current.Context.UserId, state.TenantContext.UserId, StringComparison.Ordinal);
        if (!matches) {
            // P3/P4 — flip to Blocked atomically; recovery happens when the user re-authenticates
            // with the matching context and a subsequent rejoin loop re-evaluates.
            _ = _activeGroups.TryUpdate(key, state with { Health = GroupHealth.Blocked }, state);
        }
        else if (state.Health != GroupHealth.Active) {
            // P3 — context still matches and the group is currently Blocked/Degraded; restore
            // Active so live nudges resume after a transient validation failure.
            _ = _activeGroups.TryUpdate(key, state with { Health = GroupHealth.Active }, state);
        }

        return matches;
    }

    private static Uri BuildHubUri(Uri baseAddress, string path)
        => new(baseAddress, path);

    private FcShellOptions CurrentShellOptions()
        => _shellOptionsMonitor?.CurrentValue ?? _shellOptions?.Value ?? new FcShellOptions();

    private async Task DelayClosedRestartRetryAsync(CancellationToken cancellationToken) {
        try {
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!_disposalCts.IsCancellationRequested) {
            // H-F5 — the per-attempt restart timeout (ClosedRestartTimeout, not disposal) fired
            // while backing off. Swallow it so the unbounded restart loop re-evaluates and retries
            // on the next iteration instead of the OperationCanceledException escaping to the outer
            // handler, which would apply RestartCanceled and permanently abandon realtime recovery.
            // This closes the H-F1 failure class reached through the retry-delay path (both the
            // gate-unavailable and rejoin-skip callers) rather than only through StartAsync/rejoin.
        }
    }

    private async Task RunBoundedDisposalOperationAsync(
        string operationName,
        Func<CancellationToken, Task> operation) {
        using CancellationTokenSource timeout = new(DisposalWaitTimeout);
        try {
            await operation(timeout.Token).WaitAsync(DisposalWaitTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException) {
            _logger.LogWarning(
                "EventStore projection subscription disposal operation timed out. Operation={Operation}, FailureCategory={FailureCategory}",
                operationName,
                nameof(TimeoutException));
        }
        catch (OperationCanceledException) {
            // H-F7 — the bounded-disposal timeout token cancelled the operation itself (StopAsync
            // observed timeout.Token before the WaitAsync deadline), so this is the timeout path,
            // not a generic failure. Log it as a timeout to keep disposal diagnostics accurate.
            _logger.LogWarning(
                "EventStore projection subscription disposal operation timed out. Operation={Operation}, FailureCategory={FailureCategory}",
                operationName,
                nameof(TimeoutException));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            _logger.LogWarning(
                "EventStore projection subscription disposal operation failed. Operation={Operation}, FailureCategory={FailureCategory}",
                operationName,
                ex.GetType().Name);
        }
    }

    private async Task DisposeBoundedAsync<T>(
        Func<T, ValueTask> disposeAsync,
        T instance,
        string operationName) {
        try {
            ValueTask dispose = disposeAsync(instance);
            if (dispose.IsCompletedSuccessfully) {
                await dispose.ConfigureAwait(false);
                return;
            }

            await dispose.AsTask().WaitAsync(DisposalWaitTimeout).ConfigureAwait(false);
        }
        catch (TimeoutException) {
            _logger.LogWarning(
                "EventStore projection subscription disposal operation timed out. Operation={Operation}, FailureCategory={FailureCategory}",
                operationName,
                nameof(TimeoutException));
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            _logger.LogWarning(
                "EventStore projection subscription disposal operation failed. Operation={Operation}, FailureCategory={FailureCategory}",
                operationName,
                ex.GetType().Name);
        }
    }

    private readonly record struct GroupKey(string ProjectionType, string TenantId, string? Scope);

    private readonly record struct GroupState(GroupHealth Health, TenantContextSnapshot? TenantContext);

    private enum GroupHealth : byte {
        Active,
        Degraded,
        Blocked,
    }
}
