namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Provisional notifier contract for projection data changes received via SignalR.
/// Story 1.3 may extend notification behavior through companion abstractions while
/// keeping this interface stable for existing implementers.
/// </summary>
public interface IProjectionChangeNotifier
{
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
