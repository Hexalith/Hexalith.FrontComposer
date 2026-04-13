namespace Hexalith.FrontComposer.Shell.State.Theme;

/// <summary>
/// Fluxor state record for the application color theme.
/// Positional syntax enables <c>state with { CurrentTheme = action.NewTheme }</c> in reducers.
/// </summary>
/// <param name="CurrentTheme">The currently active theme value.</param>
public record FrontComposerThemeState(ThemeValue CurrentTheme);
