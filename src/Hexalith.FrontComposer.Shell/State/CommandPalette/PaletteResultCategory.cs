namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Categorisation for a single <see cref="PaletteResult"/> row (Story 3-4 D14).
/// Ordinal-stable enum values — append-only per the cross-story contract table.
/// </summary>
public enum PaletteResultCategory {
    /// <summary>Projection result (sourced from <see cref="Contracts.Registration.IFrontComposerRegistry"/>).</summary>
    Projection = 0,

    /// <summary>Command result (sourced from the registry; routes to the generated command form).</summary>
    Command = 1,

    /// <summary>Recently visited route — sourced from the per-tenant ring buffer (D10).</summary>
    Recent = 2,

    /// <summary>Keyboard-shortcut reference row (sourced from <see cref="Contracts.Shortcuts.IShortcutService.GetRegistrations"/>).</summary>
    Shortcut = 3,
}
