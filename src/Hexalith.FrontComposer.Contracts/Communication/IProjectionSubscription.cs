namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Provisional subscription contract for real-time projection updates.
/// Story 1.3 may extend subscription behavior through companion abstractions while
/// keeping this interface stable for existing implementers.
/// </summary>
public interface IProjectionSubscription {
    /// <summary>
    /// Subscribes to change notifications for a projection type within a tenant.
    /// </summary>
    /// <param name="projectionType">The projection type name to subscribe to.</param>
    /// <param name="tenantId">The tenant context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from change notifications for a projection type within a tenant.
    /// </summary>
    /// <param name="projectionType">The projection type name to unsubscribe from.</param>
    /// <param name="tenantId">The tenant context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Companion subscription surface that additionally accepts a sub-tenant group scope (for example,
/// a conversation id). Kept separate from <see cref="IProjectionSubscription"/> so existing
/// implementers stay stable (netstandard2.0 has no default interface methods). A null/empty scope
/// is equivalent to the tenant-wide <see cref="IProjectionSubscription"/> overloads. The scope
/// survives reconnect rejoin.
/// </summary>
public interface IProjectionScopedSubscription : IProjectionSubscription {
    /// <summary>
    /// Subscribes to change notifications for a projection type within a tenant, optionally scoped
    /// below tenant level.
    /// </summary>
    /// <param name="projectionType">The projection type name to subscribe to.</param>
    /// <param name="tenantId">The tenant context.</param>
    /// <param name="scope">Optional sub-tenant group scope (for example, a conversation id).</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SubscribeAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from change notifications for a scoped projection subscription.
    /// </summary>
    /// <param name="projectionType">The projection type name to unsubscribe from.</param>
    /// <param name="tenantId">The tenant context.</param>
    /// <param name="scope">Optional sub-tenant group scope (for example, a conversation id).</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UnsubscribeAsync(string projectionType, string tenantId, string? scope, CancellationToken cancellationToken = default);
}
