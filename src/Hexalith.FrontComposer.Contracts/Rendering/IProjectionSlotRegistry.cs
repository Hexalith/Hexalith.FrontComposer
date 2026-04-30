using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Resolves Level 3 field-slot override descriptors at render time.
/// </summary>
public interface IProjectionSlotRegistry {
    /// <summary>
    /// Resolves a slot descriptor for a projection role and field. Exact role matches win,
    /// then role-agnostic descriptors, then default generated rendering.
    /// </summary>
    /// <param name="projectionType">Projection CLR type.</param>
    /// <param name="role">Optional projection role.</param>
    /// <param name="fieldName">Canonical projection property name.</param>
    /// <returns>The selected descriptor, or <see langword="null"/> when no valid slot exists.</returns>
    ProjectionSlotDescriptor? Resolve(Type projectionType, ProjectionRole? role, string fieldName);

    /// <summary>Gets valid, non-ambiguous descriptors for diagnostics and dev tooling.</summary>
    IReadOnlyCollection<ProjectionSlotDescriptor> Descriptors { get; }
}
