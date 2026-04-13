namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Density;

using Shouldly;

using Xunit;

/// <summary>
/// Unit tests for <see cref="FrontComposerDensityFeature"/>.
/// </summary>
public class DensityFeatureTests
{
    [Fact]
    public void GetInitialState_ReturnsComfortable()
    {
        // Arrange
        var feature = new TestableFrontComposerDensityFeature();

        // Act
        FrontComposerDensityState state = feature.ExposeInitialState();

        // Assert
        state.CurrentDensity.ShouldBe(DensityLevel.Comfortable);
        feature.GetName().ShouldBe("FrontComposerDensity");
    }

    private sealed class TestableFrontComposerDensityFeature : FrontComposerDensityFeature
    {
        public FrontComposerDensityState ExposeInitialState() => GetInitialState();
    }
}
