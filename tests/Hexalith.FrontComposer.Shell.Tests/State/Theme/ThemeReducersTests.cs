namespace Hexalith.FrontComposer.Shell.Tests.State.Theme;

using Hexalith.FrontComposer.Shell.State.Theme;

using Shouldly;

using Xunit;

/// <summary>
/// Unit tests for <see cref="ThemeReducers"/>.
/// </summary>
public class ThemeReducersTests
{
    [Theory]
    [InlineData(ThemeValue.Light)]
    [InlineData(ThemeValue.Dark)]
    [InlineData(ThemeValue.System)]
    public void ReduceThemeChanged_AllThemeValues_UpdatesState(ThemeValue newTheme)
    {
        // Arrange
        var state = new FrontComposerThemeState(ThemeValue.Light);
        var action = new ThemeChangedAction("corr-1", newTheme);

        // Act
        FrontComposerThemeState result = ThemeReducers.ReduceThemeChanged(state, action);

        // Assert
        result.CurrentTheme.ShouldBe(newTheme);
    }
}
