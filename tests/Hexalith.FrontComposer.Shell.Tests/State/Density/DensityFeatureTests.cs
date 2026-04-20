// ATDD RED PHASE — Story 3-3 Task 10.4 (D2; AC1)
// Fails at compile until Task 2.1 / 2.2 land the rewindowed state record and initial state.

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Density;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

/// <summary>
/// Unit tests for <see cref="FrontComposerDensityFeature"/> — Story 3-3 D2 rewindow:
/// initial state is <c>(UserPreference: null, EffectiveDensity: Comfortable)</c>.
/// </summary>
public class DensityFeatureTests
{
    [Fact]
    public void GetInitialState_ReturnsNullPreferenceAndComfortableEffective()
    {
        TestableFrontComposerDensityFeature feature = new();

        FrontComposerDensityState state = feature.ExposeInitialState();

        state.UserPreference.ShouldBeNull("D2 — initial state has no user preference.");
        state.EffectiveDensity.ShouldBe(DensityLevel.Comfortable, "D2 — feature default EffectiveDensity is Comfortable.");
        feature.GetName().ShouldBe("FrontComposerDensity");
    }

    private sealed class TestableFrontComposerDensityFeature : FrontComposerDensityFeature
    {
        public FrontComposerDensityState ExposeInitialState() => GetInitialState();
    }
}
