
using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.Density;
/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerDensityState"/>.
/// </summary>
public class FrontComposerDensityFeature : Feature<FrontComposerDensityState> {
    /// <inheritdoc/>
    public override string GetName() => "FrontComposerDensity";

    /// <inheritdoc/>
    protected override FrontComposerDensityState GetInitialState()
        => new(DensityLevel.Comfortable);
}
