namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 6-2 T4 — typed runtime resolver for Level 2 projection-template descriptors.
/// </summary>
/// <remarks>
/// <para>
/// <b>Public resolver only for Story 6-2.</b> The adopter customization surface remains the
/// <c>[ProjectionTemplate]</c> marker plus the typed Razor template. Registration is Shell-owned
/// startup plumbing fed by SourceTools-emitted descriptor manifests, not a public mutation API.
/// </para>
/// <para>
/// <b>Cache-safety boundary.</b> Implementations may cache descriptor lookups by
/// (projection type, role) but MUST NOT cache <c>ProjectionTemplateContext{TProjection}</c>,
/// <c>RenderContext</c>, item lists, culture-specific resolved strings, tenant/user state,
/// or rendered <c>RenderFragment</c> output (Story 6-2 D15 / AC15).
/// </para>
/// <para>
/// <b>No assembly reflection.</b> Implementations MUST NOT scan loaded assemblies for
/// <c>[ProjectionTemplate]</c> markers. Discovery happens exclusively through
/// SourceTools-emitted manifest registration at startup time (Story 6-2 D2 / AC11).
/// </para>
/// </remarks>
public interface IProjectionTemplateRegistry {
    /// <summary>
    /// Resolves the template descriptor for the supplied projection type and role.
    /// Returns <see langword="null"/> when no matching template was registered, when
    /// the descriptor is contract-incompatible, or when the projection-and-role tuple
    /// has duplicate registrations (Story 6-2 D10 / AC11).
    /// </summary>
    /// <param name="projectionType">The projection type whose template is being resolved.</param>
    /// <param name="role">The active projection role; <see langword="null"/> for the Default role.</param>
    /// <returns>The matching descriptor, or <see langword="null"/> when no template applies.</returns>
    ProjectionTemplateDescriptor? Resolve(Type projectionType, Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole? role);
}
