using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Default SignalR-backed projection subscription service.
/// </summary>
internal sealed class ProjectionSubscriptionService : IProjectionSubscription, IAsyncDisposable {
    private readonly IProjectionHubConnection _connection;
    private readonly IProjectionConnectionState _connectionState;
    private readonly IProjectionFallbackRefreshScheduler _refreshScheduler;
    private readonly IProjectionChangeNotifier _notifier;
    private readonly ILogger<ProjectionSubscriptionService> _logger;
    private readonly Func<CancellationToken, ValueTask<string?>>? _accessTokenProvider;
    private readonly bool _requireAccessToken;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<GroupKey, GroupState> _activeGroups = new();
    private readonly IDisposable _projectionChangedRegistration;
    private readonly IDisposable _connectionStateRegistration;
    private readonly CancellationTokenSource _disposalCts = new();
    private readonly ProjectionFallbackPollingDriver? _fallbackDriver;
    private readonly IReconnectionReconciliationCoordinator? _reconciliationCoordinator;
    private readonly IPendingCommandPollingCoordinator? _pendingCommandPolling;
    /// <summary>P2-P19 — debounces concurrent live-nudge invocations so a burst of N nudges produces at most one in-flight `PollOnceAsync`.</summary>
    private int _pendingPollInFlight;
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
        IOptions<FcShellOptions>? shellOptions = null) {
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
        _reconciliationCoordinator = reconciliationCoordinator;
        _pendingCommandPolling = pendingCommandPolling;
        _userContextAccessor = userContextAccessor;
        _shellOptions = shellOptions;
        EventStoreOptions current = options.Value;
        _requireAccessToken = current.RequireAccessToken;
        _accessTokenProvider = current.AccessTokenProvider is null
            ? null
            : cancellationToken => EventStoreAccessTokenGuard.GetRequiredTokenAsync(
                current.AccessTokenProvider,
                current.RequireAccessToken,
                cancellationToken);
        Uri hubUri = BuildHubUri(current.BaseAddress ?? throw new InvalidOperationException("EventStore BaseAddress is required."), current.ProjectionChangesHubPath);
        _connection = connectionFactory.Create(hubUri, _accessTokenProvider);
        _projectionChangedRegistration = _connection.OnProjectionChanged(OnProjectionChangedAsync);
        _connectionStateRegistration = _connection.OnConnectionStateChanged(OnConnectionStateChangedAsync);
        // DN1 — wire the bounded fallback polling driver. The driver subscribes to connection
        // state and only runs while disconnected; injection is optional so test harnesses without
        // a driver still construct cleanly.
        _fallbackDriver?.Start();
    }

    private readonly IUserContextAccessor? _userContextAccessor;
    private readonly IOptions<FcShellOptions>? _shellOptions;

    public async Task SubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
        TenantContextSnapshot? context = ResolveTenantContext(tenantId, "projection-subscribe");
        GroupKey key = ValidateGroup(projectionType, context?.TenantId ?? tenantId);
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

            await _connection.JoinGroupAsync(key.ProjectionType, key.TenantId, cancellationToken).ConfigureAwait(false);
            _ = _activeGroups.TryAdd(key, new GroupState(GroupHealth.Active, context));
        }
        finally {
            _ = _gate.Release();
        }
    }

    public async Task UnsubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
        // P2 — unsubscribe must be non-throwing on missing/stale tenant context. Sign-out
        // makes TenantId null on the accessor, and the previous code threw TenantContextException,
        // leaving the group permanently in _activeGroups (no LeaveGroupAsync, no removal). A
        // subsequent re-sign-in short-circuited at `_activeGroups.ContainsKey(key) → return`,
        // silently keeping a stale subscription.
        GroupKey key = ValidateGroup(projectionType, tenantId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (!_activeGroups.ContainsKey(key) || _disposed) {
                return;
            }

            try {
                await _connection.LeaveGroupAsync(key.ProjectionType, key.TenantId, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            }
            catch (Exception) {
                // Best-effort transport leave; the test suite already exercises a
                // throw-on-leave fixture. Re-throw to preserve existing semantics for callers
                // that observe transport failure.
                throw;
            }
            _ = _activeGroups.TryRemove(key, out _);
        }
        finally {
            _ = _gate.Release();
        }
    }

    public async ValueTask DisposeAsync() {
        // Signal disposal to background rejoin/nudge tasks before taking the gate so they can
        // observe cancellation while we wait for the gate.
        try {
            _disposalCts.Cancel();
        }
        catch (ObjectDisposedException) {
            // Already disposed; nothing to do.
        }

        await _gate.WaitAsync().ConfigureAwait(false);
        try {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _activeGroups.Clear();
            _projectionChangedRegistration.Dispose();
            _connectionStateRegistration.Dispose();
            if (_fallbackDriver is not null) {
                await _fallbackDriver.DisposeAsync().ConfigureAwait(false);
            }

            await _connection.StopAsync(CancellationToken.None).ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning("EventStore projection subscription disposal failed. FailureCategory={FailureCategory}", ex.GetType().Name);
        }
        finally {
            _ = _gate.Release();
            _disposalCts.Dispose();
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

    private async Task OnConnectionStateChangedAsync(ProjectionHubConnectionStateChanged change) {
        if (_disposed) {
            return;
        }

        switch (change.State) {
            case ProjectionHubConnectionState.Connected:
                _connectionState.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
                break;

            case ProjectionHubConnectionState.Reconnecting:
                _connectionState.Apply(new ProjectionConnectionTransition(
                    ProjectionConnectionStatus.Reconnecting,
                    FailureCategory: change.Exception?.GetType().Name ?? "Reconnecting",
                    ReconnectAttempt: 1));
                break;

            case ProjectionHubConnectionState.Reconnected:
                // DN3 — rejoin runs in the handler chain (so tests and adopters can observe
                // completion deterministically) but takes the gate with a bounded timeout and the
                // service disposal token. A blocked subscribe/unsubscribe on the same gate cannot
                // hang rejoin indefinitely; disposal cancels the sweep promptly.
                await RejoinActiveGroupsAsync(_disposalCts.Token).ConfigureAwait(false);
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
                    break;
                }

                if (_disposed || _disposalCts.IsCancellationRequested) {
                    break;
                }

                try {
                    // P6 — wrap the coordinator call so a buggy reconciliation cannot escape into
                    // the SignalR hub state-changed dispatcher (which would terminate the callback
                    // chain and prevent further state transitions from propagating).
                    _ = await _reconciliationCoordinator.ReconcileAsync(_disposalCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (_disposalCts.IsCancellationRequested) {
                    // Expected on disposal.
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) {
                    _logger.LogWarning(
                        "Reconnection reconciliation threw out of the hub callback. FailureCategory={FailureCategory}",
                        ex.GetType().Name);
                }

                break;

            case ProjectionHubConnectionState.Closed:
                // P19 — preserve sticky InitialStartFailed category set inside SubscribeAsync.
                // Once a non-null failure category exists in the current Disconnected state, a
                // follow-up Closed event must not overwrite it with a less-specific category.
                ProjectionConnectionSnapshot currentSnapshot = _connectionState.Current;
                if (currentSnapshot.Status is ProjectionConnectionStatus.Disconnected
                    && !string.IsNullOrEmpty(currentSnapshot.LastFailureCategory)) {
                    break;
                }

                _connectionState.Apply(new ProjectionConnectionTransition(
                    ProjectionConnectionStatus.Disconnected,
                    FailureCategory: change.Exception?.GetType().Name ?? "Closed"));
                break;
        }
    }

    private async Task RejoinActiveGroupsAsync(CancellationToken cancellationToken) {
        try {
            await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            return;
        }

        try {
            if (_disposed) {
                return;
            }

            foreach (GroupKey key in _activeGroups.Keys.OrderBy(static key => key.ProjectionType, StringComparer.Ordinal)) {
                if (cancellationToken.IsCancellationRequested) {
                    return;
                }

                // P13 — re-check connection per-Join so a mid-loop disconnect stops the sweep
                // instead of flooding logs with RejoinFailed for every remaining group.
                if (!_connection.IsConnected) {
                    break;
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

                    await _connection.JoinGroupAsync(key.ProjectionType, key.TenantId, cancellationToken).ConfigureAwait(false);
                    // P3/P4 — TryUpdate so a concurrent unsubscribe/reconnect does not
                    // resurrect a removed key. If the entry is gone, the rejoin is moot.
                    _ = _activeGroups.TryUpdate(key, state with { Health = GroupHealth.Active }, state);
                    FrontComposerTelemetry.SetOutcome(activity, "active");
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return;
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
        }
        finally {
            _ = _gate.Release();
        }
    }

    private IReadOnlyDictionary<ProjectionFallbackGroupKey, bool> SnapshotGroupHealth() {
        Dictionary<ProjectionFallbackGroupKey, bool> snapshot = new();
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
        if (!_requireAccessToken) {
            return;
        }

        if (_accessTokenProvider is null) {
            throw new InvalidOperationException("EventStore access token provider is required.");
        }

        _ = await _accessTokenProvider(cancellationToken).ConfigureAwait(false);
    }

    private static GroupKey ValidateGroup(string projectionType, string tenantId)
        => new(
            EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType)),
            EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId)));

    private TenantContextSnapshot? ResolveTenantContext(string? requestedTenant, string operationKind) {
        if (_userContextAccessor is null) {
            return null;
        }

        return FrontComposerTenantContextAccessor
            .Resolve(
                _userContextAccessor,
                _shellOptions?.Value ?? new FcShellOptions(),
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
            _shellOptions?.Value ?? new FcShellOptions(),
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

    private readonly record struct GroupKey(string ProjectionType, string TenantId);

    private readonly record struct GroupState(GroupHealth Health, TenantContextSnapshot? TenantContext);

    private enum GroupHealth : byte {
        Active,
        Degraded,
        Blocked,
    }
}
