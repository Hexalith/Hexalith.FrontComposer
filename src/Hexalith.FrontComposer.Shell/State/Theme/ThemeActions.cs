namespace Hexalith.FrontComposer.Shell.State.Theme;

/// <summary>
/// Dispatched when the application theme changes.
/// </summary>
/// <param name="CorrelationId">Correlation identifier for tracing.</param>
/// <param name="NewTheme">The new theme value to apply.</param>
public record ThemeChangedAction(string CorrelationId, ThemeValue NewTheme);
