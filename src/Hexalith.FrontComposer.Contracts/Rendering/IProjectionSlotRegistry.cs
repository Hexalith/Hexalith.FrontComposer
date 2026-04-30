using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Resolves Level 3 field-slot override descriptors at render time.
/// </summary>
/// <remarks>
/// Implementations must be safe for concurrent reads from multiple Blazor render circuits.
/// Per binding decisions D3 / D16, the registry caches descriptors only — never
/// <see cref="FieldSlotContext{TProjection,TField}"/>, parent items, <see cref="RenderContext"/>,
/// rendered fragments, or service-provider scoped values — and <see cref="Descriptors"/> must
/// expose an immutable snapshot so concurrent renders observe a stable view.
/// </remarks>
public interface IProjectionSlotRegistry {
    /// <summary>
    /// Resolves a slot descriptor for a projection role and field. Exact role matches win,
    /// then role-agnostic descriptors, then default generated rendering.
    /// </summary>
    /// <param name="projectionType">Projection CLR type.</param>
    /// <param name="role">Optional projection role.</param>
    /// <param name="fieldName">Canonical projection property name.</param>
    /// <returns>The selected descriptor, or <see langword="null"/> when no valid slot exists
    /// or when ambiguous/incompatible registrations have been suppressed (the Shell
    /// implementation logs HFC1040/HFC1041 in that case).</returns>
    ProjectionSlotDescriptor? Resolve(Type projectionType, ProjectionRole? role, string fieldName);

    /// <summary>
    /// Gets a frozen snapshot of valid, non-ambiguous descriptors for diagnostics and dev tooling.
    /// Implementations must return an immutable view that is safe to enumerate concurrently.
    /// </summary>
    IReadOnlyCollection<ProjectionSlotDescriptor> Descriptors { get; }
}
