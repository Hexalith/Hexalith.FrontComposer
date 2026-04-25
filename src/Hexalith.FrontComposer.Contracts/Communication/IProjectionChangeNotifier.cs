namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Provisional notifier contract for projection data changes received via SignalR.
/// Story 1.3 may extend notification behavior through companion abstractions while
/// keeping this interface stable for existing implementers.
/// </summary>
public interface IProjectionChangeNotifier {
    /// <summary>
    /// Raised when a projection type has been updated.
    /// The string parameter is the projection type name.
    /// </summary>
    event Action<string>? ProjectionChanged;

    /// <summary>
    /// Signals that the given projection type has changed.
    /// </summary>
    /// <param name="projectionType">The projection type name that changed.</param>
    void NotifyChanged(string projectionType);
}

/// <summary>
/// Companion notifier surface that carries the originating tenant alongside the projection
/// type. Stories 5-3 and 5-4 consume this overload to route nudges per tenant when a single
/// circuit observes more than one tenant. Implementations that ship the tenant-aware path
/// also raise the legacy <see cref="IProjectionChangeNotifier.ProjectionChanged"/> event for
/// backwards compatibility.
/// </summary>
public interface IProjectionChangeNotifierWithTenant : IProjectionChangeNotifier {
    /// <summary>
    /// Raised when a projection type has been updated for a specific tenant.
    /// </summary>
    event Action<string, string>? ProjectionChangedForTenant;

    /// <summary>
    /// Signals that the given projection type has changed for a specific tenant.
    /// </summary>
    /// <param name="projectionType">The projection type name that changed.</param>
    /// <param name="tenantId">The tenant context that observed the change.</param>
    void NotifyChanged(string projectionType, string tenantId);
}
