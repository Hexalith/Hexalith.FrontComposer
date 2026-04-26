namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Raised when a projection payload or cached projection entry cannot be consumed safely by
/// the current Shell runtime.
/// </summary>
public sealed class ProjectionSchemaMismatchException : Exception {
    public ProjectionSchemaMismatchException(string projectionType, Exception? innerException = null)
        : base("Projection payload schema is incompatible with the current client.", innerException) {
        // P5 — fail-closed on null/empty projection type so structured diagnostics never log
        // a literal "null" and downstream consumers cannot deref a missing field.
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionType);
        ProjectionType = projectionType;
    }

    public string ProjectionType { get; }
}
