using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

/// <summary>
/// Fluxor feature registration for <see cref="FrontComposerCapabilityDiscoveryState"/>
/// (Story 3-5 D8 / ADR-046).
/// </summary>
public sealed class FrontComposerCapabilityDiscoveryFeature : Feature<FrontComposerCapabilityDiscoveryState> {
    /// <inheritdoc/>
    public override string GetName() => "FrontComposerCapabilityDiscovery";

    /// <inheritdoc/>
    protected override FrontComposerCapabilityDiscoveryState GetInitialState()
        => FrontComposerCapabilityDiscoveryState.Empty;
}
