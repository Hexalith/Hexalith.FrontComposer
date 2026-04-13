namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Immutable context passed to renderers for each render operation.
/// Carries tenant, user, mode, density, and read-only state.
/// </summary>
/// <param name="TenantId">The current tenant identifier.</param>
/// <param name="UserId">The current user identifier.</param>
/// <param name="Mode">The active Blazor render mode.</param>
/// <param name="DensityLevel">The display density level.</param>
/// <param name="IsReadOnly">Whether the rendered output should be read-only.</param>
public record RenderContext(
    string TenantId,
    string UserId,
    FcRenderMode Mode,
    DensityLevel DensityLevel,
    bool IsReadOnly);
