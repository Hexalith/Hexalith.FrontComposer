namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>A stored recent route and its palette label.</summary>
/// <param name="Url">The internal destination.</param>
/// <param name="Label">The label seen when the destination was activated, when available.</param>
internal sealed record PaletteRecentRoute(string Url, string? Label);
