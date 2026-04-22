using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerDensityState"/> (Story 3-3 D2 / Story 3-6 D19).
/// Initial state: no user preference, effective density = <see cref="DensityLevel.Comfortable"/>,
/// hydration = <see cref="DensityHydrationState.Idle"/>.
/// </summary>
public class FrontComposerDensityFeature : Feature<FrontComposerDensityState> {
    /// <inheritdoc/>
    public override string GetName() => "FrontComposerDensity";

    /// <inheritdoc/>
    protected override FrontComposerDensityState GetInitialState()
        => new(
            UserPreference: null,
            EffectiveDensity: DensityLevel.Comfortable,
            HydrationState: DensityHydrationState.Idle);
}
