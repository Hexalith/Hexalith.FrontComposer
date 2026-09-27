namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>Classifies a palette activation that no longer has access to its result.</summary>
public enum PaletteActivationDenialKind
{
    /// <summary>The current user no longer has permission for the command.</summary>
    Permission,

    /// <summary>The selected result is stale or no longer available.</summary>
    Unavailable,
}
