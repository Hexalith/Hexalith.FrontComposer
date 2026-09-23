
using Hexalith.FrontComposer.Shell.State.Theme;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Theme;
/// <summary>
/// Unit tests for <see cref="ThemeReducers"/>.
/// </summary>
public class ThemeReducersTests {
    [Fact]
    public void ScopeChange_RejectsPriorHydrationActionAndCompletion() {
        FrontComposerThemeState a = new(ThemeValue.Dark, HydrationState.Hydrating);
        FrontComposerThemeState b = ThemeReducers.ReduceScopeChanged(a, new ScopeChangedAction());
        ThemeHydratingAction lateHydrating = new() { HydrationScopeVersion = a.ScopeVersion };
        ThemeChangedAction lateA = new("a", ThemeValue.Dark) { HydrationScopeVersion = a.ScopeVersion };
        ThemeHydratedCompletedAction lateCompletion = new() { HydrationScopeVersion = a.ScopeVersion };

        ThemeReducers.ReduceThemeHydrating(b, lateHydrating).ShouldBeSameAs(b);
        ThemeReducers.ReduceThemeChanged(b, lateA).ShouldBeSameAs(b);
        ThemeReducers.ReduceThemeHydratedCompleted(b, lateCompletion).ShouldBeSameAs(b);
        b.CurrentTheme.ShouldBe(ThemeValue.Light);
        b.HydrationState.ShouldBe(HydrationState.Idle);
        ThemeReducers.ReduceThemeHydrating(b,
            new ThemeHydratingAction { HydrationScopeVersion = b.ScopeVersion })
            .HydrationState.ShouldBe(HydrationState.Hydrating);
    }
    [Theory]
    [InlineData(ThemeValue.Light)]
    [InlineData(ThemeValue.Dark)]
    [InlineData(ThemeValue.System)]
    public void ReduceThemeChanged_AllThemeValues_UpdatesState(ThemeValue newTheme) {
        // Arrange
        var state = new FrontComposerThemeState(ThemeValue.Light);
        var action = new ThemeChangedAction("corr-1", newTheme);

        // Act
        FrontComposerThemeState result = ThemeReducers.ReduceThemeChanged(state, action);

        // Assert
        result.CurrentTheme.ShouldBe(newTheme);
    }
}
