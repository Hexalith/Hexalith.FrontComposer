using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Resolves Level 4 full projection-view replacement descriptors at render time.
/// </summary>
/// <remarks>
/// Implementations may cache descriptor metadata only. They must not cache
/// <see cref="ProjectionViewContext{TProjection}"/>, item lists, <see cref="RenderContext"/>,
/// rendered fragments, scoped services, tenant/user identifiers, or localized strings.
/// </remarks>
public interface IProjectionViewOverrideRegistry {
    /// <summary>
    /// Resolves a replacement descriptor for a projection role. Exact role matches win, then a
    /// role-agnostic descriptor, then the generated customization pipeline.
    /// </summary>
    /// <param name="projectionType">Projection CLR type.</param>
    /// <param name="role">Optional projection role; <see langword="null"/> is the default role.</param>
    /// <returns>The selected descriptor, or <see langword="null"/> when no valid descriptor exists
    /// or when an exact tuple is ambiguous/incompatible.</returns>
    ProjectionViewOverrideDescriptor? Resolve(Type projectionType, ProjectionRole? role);

    /// <summary>
    /// Gets a frozen snapshot of valid, non-ambiguous descriptors for diagnostics and dev tooling.
    /// </summary>
    IReadOnlyCollection<ProjectionViewOverrideDescriptor> Descriptors { get; }
}
