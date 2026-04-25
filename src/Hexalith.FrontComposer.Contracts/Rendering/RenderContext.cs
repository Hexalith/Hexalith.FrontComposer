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
/// <remarks>
/// P-7 (Pass-3 review fix): <see cref="IsDevMode"/> is intentionally a non-positional <c>init</c>
/// property rather than a 6th positional parameter so adopter code that destructures
/// <see cref="RenderContext"/> via positional patterns (<c>is RenderContext(_,_,_,_,_)</c>) keeps
/// working AND record equality stays anchored on the original 5-tuple (avoids surprising Fluxor
/// memo invalidation when only the dev-mode flag flips). Callers set the flag with
/// <c>new RenderContext(...) { IsDevMode = true }</c>.
/// </remarks>
public record RenderContext(
    string TenantId,
    string UserId,
    FcRenderMode Mode,
    DensityLevel DensityLevel,
    bool IsReadOnly) {
    /// <summary>Gets a value indicating whether developer-diagnostics affordances (e.g.,
    /// red-dashed FcFieldPlaceholder border) should render. Orthogonal to <see cref="Mode"/>
    /// so dev-mode can be enabled in any host (Server, WebAssembly, or Auto).</summary>
    public bool IsDevMode { get; init; }
}
