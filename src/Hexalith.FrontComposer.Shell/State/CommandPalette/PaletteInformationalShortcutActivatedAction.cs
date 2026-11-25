namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>Requests feedback for a reference-only shortcut while the palette remains open.</summary>
/// <param name="Query">The query whose shortcut result was activated.</param>
public sealed record PaletteInformationalShortcutActivatedAction(string Query)
{
    internal long? OriginScopeGeneration { get; init; }
}
