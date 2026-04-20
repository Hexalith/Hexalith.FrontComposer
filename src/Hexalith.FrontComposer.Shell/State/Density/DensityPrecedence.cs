using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Navigation;

namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Pure static resolver that codifies the four-tier density precedence rule (Story 3-3 D1 / D5 / D6;
/// UX spec §197-204; ADR-039; ADR-040).
/// </summary>
/// <remarks>
/// <para>
/// Tier order (first applicable wins):
/// </para>
/// <list type="number">
/// <item><description><b>Viewport tier-force override</b> — <c>tier ≤ ViewportTier.Tablet</c> returns
/// <see cref="DensityLevel.Comfortable"/> regardless of user preference and deployment default.
/// Accessibility floor for 44 px touch targets (ADR-040).</description></item>
/// <item><description><b>User preference</b> — when <paramref name="userPreference"/> is non-null.</description></item>
/// <item><description><b>Deployment default</b> — when <paramref name="deploymentDefault"/> is non-null
/// (bound from <c>FcShellOptions.DefaultDensity</c>).</description></item>
/// <item><description><b>Factory hybrid</b> — per-surface recommendation from UX spec §208-214
/// via <see cref="GetFactoryDefault(DensitySurface)"/>.</description></item>
/// </list>
/// <para>
/// Tier 5 (per-component default) is <b>not reachable</b> through this resolver — custom components
/// that want a locked density hardcode the density attribute on their root DOM element and do not
/// consult <c>EffectiveDensity</c>. The resolver's exit.
/// </para>
/// <para>
/// <b>ADR-039 purity invariant:</b> this function is called by action producers (effects + components
/// that dispatch) BEFORE dispatching state changes. Reducers never call the resolver; they assign the
/// pre-resolved value from the action payload. <c>DensityReducerPurityTest</c> enforces this at CI.
/// </para>
/// </remarks>
public static class DensityPrecedence {
    /// <summary>
    /// Resolves the effective <see cref="DensityLevel"/> for a given surface at the current viewport.
    /// See type-level remarks for the tier ordering and the ADR-039 reducer-purity invariant.
    /// </summary>
    /// <param name="userPreference">Explicit user choice from <c>FrontComposerDensityState.UserPreference</c>, or <see langword="null"/> when the user has not chosen.</param>
    /// <param name="deploymentDefault">Adopter-configured default from <c>FcShellOptions.DefaultDensity</c>, or <see langword="null"/> when the deployment leaves it unset.</param>
    /// <param name="surface">The rendering surface requesting the density. Story 3-3 wires only <see cref="DensitySurface.Default"/>.</param>
    /// <param name="tier">The current <see cref="ViewportTier"/> observed by <c>FcLayoutBreakpointWatcher</c>.</param>
    /// <returns>The resolved <see cref="DensityLevel"/>.</returns>
    public static DensityLevel Resolve(
        DensityLevel? userPreference,
        DensityLevel? deploymentDefault,
        DensitySurface surface,
        ViewportTier tier) {
        // Tier 1 — viewport tier-force override (ADR-040). Tablet + Phone → Comfortable for 44 px touch targets.
        if (tier <= ViewportTier.Tablet) {
            return DensityLevel.Comfortable;
        }

        // Tier 2 — explicit user preference.
        if (userPreference is DensityLevel userValue) {
            return userValue;
        }

        // Tier 3 — deployment default.
        if (deploymentDefault is DensityLevel deploymentValue) {
            return deploymentValue;
        }

        // Tier 4 — factory hybrid by surface.
        return GetFactoryDefault(surface);
    }

    private static DensityLevel GetFactoryDefault(DensitySurface surface) => surface switch {
        DensitySurface.DataGrid => DensityLevel.Compact,
        DensitySurface.DevModeOverlay => DensityLevel.Compact,
        _ => DensityLevel.Comfortable,
    };
}
