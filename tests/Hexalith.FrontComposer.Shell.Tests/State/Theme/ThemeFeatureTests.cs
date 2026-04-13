namespace Hexalith.FrontComposer.Shell.Tests.State.Theme;

using Hexalith.FrontComposer.Shell.State.Theme;

using Shouldly;

using Xunit;

/// <summary>
/// Unit tests for <see cref="FrontComposerThemeFeature"/>.
/// </summary>
public class ThemeFeatureTests
{
    [Fact]
    public void GetInitialState_ReturnsLight()
    {
        // Arrange
        var feature = new TestableFrontComposerThemeFeature();

        // Act
        FrontComposerThemeState state = feature.ExposeInitialState();

        // Assert
        state.CurrentTheme.ShouldBe(ThemeValue.Light);
        feature.GetName().ShouldBe("FrontComposerTheme");
    }

    private sealed class TestableFrontComposerThemeFeature : FrontComposerThemeFeature
    {
        public FrontComposerThemeState ExposeInitialState() => GetInitialState();
    }
}
