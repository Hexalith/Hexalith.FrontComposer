namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Immutable context passed to renderers for each render operation.
/// Carries tenant, user, mode, density, read-only state, and dev-mode flag.
/// </summary>
/// <param name="TenantId">The current tenant identifier.</param>
/// <param name="UserId">The current user identifier.</param>
/// <param name="Mode">The active Blazor render mode.</param>
/// <param name="DensityLevel">The display density level.</param>
/// <param name="IsReadOnly">Whether the rendered output should be read-only.</param>
/// <param name="IsDevMode">Whether developer-diagnostics affordances (e.g., red-dashed
/// FcFieldPlaceholder border) should render. Orthogonal to <paramref name="Mode"/> so dev-mode
/// can be enabled in any host (Server, WebAssembly, or Auto).</param>
public record RenderContext(
    string TenantId,
    string UserId,
    FcRenderMode Mode,
    DensityLevel DensityLevel,
    bool IsReadOnly,
    bool IsDevMode = false);
