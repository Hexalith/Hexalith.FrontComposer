namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Raised when a projection payload or cached projection entry cannot be consumed safely by
/// the current Shell runtime.
/// </summary>
public sealed class ProjectionSchemaMismatchException : Exception {
    /// <summary>Initializes a schema mismatch with a payload-independent message.</summary>
    /// <param name="projectionType">The projection type that could not be read.</param>
    /// <param name="innerException">Underlying parser failure. Only its type name is retained.</param>
    public ProjectionSchemaMismatchException(string projectionType, Exception? innerException = null)
        : base("Projection payload schema is incompatible with the current client.") {
        // P5 — fail-closed on null/empty projection type so structured diagnostics never log
        // a literal "null" and downstream consumers cannot deref a missing field.
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionType);
        ProjectionType = projectionType;
        FailureCategory = innerException?.GetType().Name;
    }

    /// <summary>Gets the redacted type name of the underlying failure.</summary>
    internal string? FailureCategory { get; }

    /// <summary>Gets the projection type that could not be read.</summary>
    public string ProjectionType { get; }
}
