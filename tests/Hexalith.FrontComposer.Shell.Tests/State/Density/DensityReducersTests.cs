namespace Hexalith.FrontComposer.Shell.Tests.State.Density;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Density;

using Shouldly;

using Xunit;

/// <summary>
/// Unit tests for <see cref="DensityReducers"/>.
/// </summary>
public class DensityReducersTests
{
    [Theory]
    [InlineData(DensityLevel.Compact)]
    [InlineData(DensityLevel.Comfortable)]
    [InlineData(DensityLevel.Roomy)]
    public void ReduceDensityChanged_AllDensityLevels_UpdatesState(DensityLevel newDensity)
    {
        // Arrange
        var state = new FrontComposerDensityState(DensityLevel.Comfortable);
        var action = new DensityChangedAction("corr-1", newDensity);

        // Act
        FrontComposerDensityState result = DensityReducers.ReduceDensityChanged(state, action);

        // Assert
        result.CurrentDensity.ShouldBe(newDensity);
    }
}
