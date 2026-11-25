namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>Optional rejection diagnostics exposed by a Level 4 view override registry.</summary>
public interface IProjectionViewOverrideDiagnostics : IProjectionViewOverrideRegistry
{
    /// <summary>Gets the read-only list of descriptors rejected during startup hydration.</summary>
    /// <remarks>
    /// Conflicting valid registrations fail registry construction and are reported by its exception.
    /// </remarks>
    IReadOnlyList<ProjectionViewOverrideRejection> RejectedDescriptors { get; }
}
