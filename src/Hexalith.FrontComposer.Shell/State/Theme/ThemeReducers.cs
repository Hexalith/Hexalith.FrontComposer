namespace Hexalith.FrontComposer.Shell.State.Theme;

using Fluxor;

/// <summary>
/// Pure reducers for <see cref="FrontComposerThemeState"/>.
/// </summary>
public static class ThemeReducers
{
    /// <summary>
    /// Applies a theme change to the state.
    /// </summary>
    /// <param name="state">The current theme state.</param>
    /// <param name="action">The theme changed action.</param>
    /// <returns>A new state with the updated theme.</returns>
    [ReducerMethod]
    public static FrontComposerThemeState ReduceThemeChanged(FrontComposerThemeState state, ThemeChangedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { CurrentTheme = action.NewTheme };
    }
}
