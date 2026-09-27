namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>Keeps the palette open and exposes the existing denial heading for one activation attempt.</summary>
public sealed record PaletteActivationDeniedAction(PaletteActivationDenialKind Kind);
