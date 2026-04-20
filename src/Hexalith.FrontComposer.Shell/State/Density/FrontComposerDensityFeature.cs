using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerDensityState"/>.
/// Initial state (Story 3-3 D2): no user preference, effective density = <see cref="DensityLevel.Comfortable"/>.
/// </summary>
public class FrontComposerDensityFeature : Feature<FrontComposerDensityState> {
    /// <inheritdoc/>
    public override string GetName() => "FrontComposerDensity";

    /// <inheritdoc/>
    protected override FrontComposerDensityState GetInitialState()
        => new(UserPreference: null, EffectiveDensity: DensityLevel.Comfortable);
}
