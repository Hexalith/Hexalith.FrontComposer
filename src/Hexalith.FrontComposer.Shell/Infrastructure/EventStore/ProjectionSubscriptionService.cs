using Hexalith.FrontComposer.Contracts.Communication;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Default SignalR-backed projection subscription service.
/// </summary>
internal sealed class ProjectionSubscriptionService : IProjectionSubscription, IAsyncDisposable {
    private readonly IProjectionHubConnection _connection;
    private readonly IProjectionChangeNotifier _notifier;
    private readonly ILogger<ProjectionSubscriptionService> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly ConcurrentDictionary<GroupKey, byte> _activeGroups = new();
    private readonly IDisposable _projectionChangedRegistration;
    private bool _disposed;

    public ProjectionSubscriptionService(
        IOptions<EventStoreOptions> options,
        IProjectionHubConnectionFactory connectionFactory,
        IProjectionChangeNotifier notifier,
        ILogger<ProjectionSubscriptionService> logger) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(connectionFactory);
        ArgumentNullException.ThrowIfNull(notifier);

        _notifier = notifier;
        _logger = logger;
        EventStoreOptions current = options.Value;
        Uri hubUri = BuildHubUri(current.BaseAddress ?? throw new InvalidOperationException("EventStore BaseAddress is required."), current.ProjectionChangesHubPath);
        _connection = connectionFactory.Create(hubUri, current.AccessTokenProvider);
        _projectionChangedRegistration = _connection.OnProjectionChanged(OnProjectionChangedAsync);
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
                await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
            }

            await _connection.JoinGroupAsync(key.ProjectionType, key.TenantId, cancellationToken).ConfigureAwait(false);
            _ = _activeGroups.TryAdd(key, 0);
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
        await _gate.WaitAsync().ConfigureAwait(false);
        try {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _activeGroups.Clear();
            _projectionChangedRegistration.Dispose();
            await _connection.StopAsync(CancellationToken.None).ConfigureAwait(false);
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "EventStore projection subscription disposal failed.");
        }
        finally {
            _ = _gate.Release();
        }
    }

    private Task OnProjectionChangedAsync(string projectionType, string tenantId) {
        if (_disposed) {
            return Task.CompletedTask;
        }

        GroupKey key;
        try {
            key = ValidateGroup(projectionType, tenantId);
        }
        catch (ArgumentException) {
            return Task.CompletedTask;
        }

        if (_activeGroups.ContainsKey(key)) {
            try {
                if (_notifier is IProjectionChangeNotifierWithTenant tenantAware) {
                    tenantAware.NotifyChanged(key.ProjectionType, key.TenantId);
                }
                else {
                    _notifier.NotifyChanged(key.ProjectionType);
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException) {
                // A buggy subscriber must not kill the SignalR callback dispatcher.
                _logger.LogWarning(ex, "Projection change subscriber threw while handling nudge.");
            }
        }

        return Task.CompletedTask;
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
}
