namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Raised when a projection payload or cached projection entry cannot be consumed safely by
/// the current Shell runtime.
/// </summary>
public sealed class ProjectionSchemaMismatchException : Exception {
    public ProjectionSchemaMismatchException(string projectionType, Exception? innerException = null)
        : base("Projection payload schema is incompatible with the current client.", innerException)
        => ProjectionType = projectionType;

    public string ProjectionType { get; }
}
