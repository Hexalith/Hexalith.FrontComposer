using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

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
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<GroupKey, GroupHealth> _activeGroups = new();
    private readonly IDisposable _projectionChangedRegistration;
    private readonly IDisposable _connectionStateRegistration;
    private readonly CancellationTokenSource _disposalCts = new();
    private readonly ProjectionFallbackPollingDriver? _fallbackDriver;
    private bool _disposed;

    public ProjectionSubscriptionService(
        IOptions<EventStoreOptions> options,
        IProjectionHubConnectionFactory connectionFactory,
        IProjectionConnectionState connectionState,
        IProjectionFallbackRefreshScheduler refreshScheduler,
        IProjectionChangeNotifier notifier,
        ILogger<ProjectionSubscriptionService> logger,
        ProjectionFallbackPollingDriver? fallbackDriver = null) {
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
        EventStoreOptions current = options.Value;
        Uri hubUri = BuildHubUri(current.BaseAddress ?? throw new InvalidOperationException("EventStore BaseAddress is required."), current.ProjectionChangesHubPath);
        _connection = connectionFactory.Create(hubUri, current.AccessTokenProvider);
        _projectionChangedRegistration = _connection.OnProjectionChanged(OnProjectionChangedAsync);
        _connectionStateRegistration = _connection.OnConnectionStateChanged(OnConnectionStateChangedAsync);
        // DN1 — wire the bounded fallback polling driver. The driver subscribes to connection
        // state and only runs while disconnected; injection is optional so test harnesses without
        // a driver still construct cleanly.
        _fallbackDriver?.Start();
    }

    public async Task SubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
        GroupKey key = ValidateGroup(projectionType, tenantId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            ThrowIfDisposed();
            if (_activeGroups.ContainsKey(key)) {
                return;
            }

            if (!_connection.IsConnected) {
                try {
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
            _ = _activeGroups.TryAdd(key, GroupHealth.Active);
        }
        finally {
            _ = _gate.Release();
        }
    }

    public async Task UnsubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
        GroupKey key = ValidateGroup(projectionType, tenantId);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            if (!_activeGroups.ContainsKey(key) || _disposed) {
                return;
            }

            await _connection.LeaveGroupAsync(key.ProjectionType, key.TenantId, cancellationToken).ConfigureAwait(false);
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
        if (_activeGroups.TryGetValue(key, out GroupHealth health) && health == GroupHealth.Active) {
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
            }
            catch (OperationCanceledException) when (_disposalCts.IsCancellationRequested) {
                // Expected on disposal; swallow.
            }
            catch (Exception ex) when (ex is not OutOfMemoryException) {
                // A buggy subscriber must not kill the SignalR callback dispatcher.
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
                _connectionState.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
                // DN3 — rejoin runs in the handler chain (so tests and adopters can observe
                // completion deterministically) but takes the gate with a bounded timeout and the
                // service disposal token. A blocked subscribe/unsubscribe on the same gate cannot
                // hang rejoin indefinitely; disposal cancels the sweep promptly.
                await RejoinActiveGroupsAsync(_disposalCts.Token).ConfigureAwait(false);
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

                try {
                    await _connection.JoinGroupAsync(key.ProjectionType, key.TenantId, cancellationToken).ConfigureAwait(false);
                    _activeGroups[key] = GroupHealth.Active;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                    return;
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) {
                    // DN2 — mark degraded; nudges skip until the next successful rejoin.
                    _activeGroups[key] = GroupHealth.Degraded;
                    // P5 — exception type only. Raw exception messages can carry group/tenant
                    // arguments embedded in stack frames or framework-formatted text.
                    _logger.LogWarning(
                        "EventStore projection group rejoin failed. FailureCategory={FailureCategory}",
                        ex.GetType().Name);
                }
            }
        }
        finally {
            _ = _gate.Release();
        }
    }

    private void ThrowIfDisposed() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(ProjectionSubscriptionService));
        }
    }

    private static GroupKey ValidateGroup(string projectionType, string tenantId)
        => new(
            EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType)),
            EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId)));

    private static Uri BuildHubUri(Uri baseAddress, string path)
        => new(baseAddress, path);

    private readonly record struct GroupKey(string ProjectionType, string TenantId);

    private enum GroupHealth : byte {
        Active,
        Degraded,
    }
}
