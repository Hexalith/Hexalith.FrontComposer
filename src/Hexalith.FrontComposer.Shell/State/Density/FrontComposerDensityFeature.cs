namespace Hexalith.FrontComposer.Shell.State.Density;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerDensityState"/>.
/// </summary>
public class FrontComposerDensityFeature : Feature<FrontComposerDensityState>
{
    /// <inheritdoc/>
    public override string GetName() => "FrontComposerDensity";

    /// <inheritdoc/>
    protected override FrontComposerDensityState GetInitialState()
        => new(DensityLevel.Comfortable);
}
