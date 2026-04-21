namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Categorisation for a single <see cref="PaletteResult"/> row (Story 3-4 D14).
/// Ordinal-stable enum values — append-only per the cross-story contract table.
/// </summary>
public enum PaletteResultCategory
{
    /// <summary>Projection result (sourced from <see cref="Contracts.Registration.IFrontComposerRegistry"/>).</summary>
    Projection = 0,

    /// <summary>Command result (sourced from the registry; routes to the generated command form).</summary>
    Command = 1,

    /// <summary>Recently visited route — sourced from the per-tenant ring buffer (D10).</summary>
    Recent = 2,

    /// <summary>Keyboard-shortcut reference row (sourced from <see cref="Contracts.Shortcuts.IShortcutService.GetRegistrations"/>).</summary>
    Shortcut = 3,
}

/// <summary>
/// Load-state hint for the palette dialog. Drives the "Searching…" / "Ready" copy in the result
/// list (Story 3-4 D6 / D8).
/// </summary>
public enum PaletteLoadState
{
    /// <summary>No active query (first paint, or after close).</summary>
    Idle = 0,

    /// <summary>Debounce is in flight — the previous results are stale but still shown.</summary>
    Searching = 1,

    /// <summary>Latest results are live — safe to render.</summary>
    Ready = 2,
}

/// <summary>
/// Single row in the palette result list (Story 3-4 D6 / D14).
/// </summary>
/// <param name="Category">The result category.</param>
/// <param name="DisplayLabel">The human-readable label (already localised).</param>
/// <param name="BoundedContext">The bounded-context name when applicable; <see cref="string.Empty"/> for synthetic rows.</param>
/// <param name="RouteUrl">The navigation target URL; <see langword="null"/> for shortcut-reference rows or synthetic entries that refill the palette instead.</param>
/// <param name="CommandTypeName">The fully qualified command type name (or the <c>"@"</c>-prefixed sentinel for framework-synthetic entries per D23); <see langword="null"/> for non-command rows.</param>
/// <param name="Score">Pre-computed score from <see cref="PaletteScorer"/> + contextual bonus.</param>
/// <param name="IsInCurrentContext">Whether the result lives in the current bounded context (drives the inline hint).</param>
/// <param name="ProjectionType">The projection runtime type when <see cref="Category"/> is <see cref="PaletteResultCategory.Projection"/>; <see langword="null"/> otherwise.</param>
/// <param name="DescriptionKey">Localisation resource key for the description column (shortcut category); <see langword="null"/> when not applicable.</param>
public sealed record PaletteResult(
    PaletteResultCategory Category,
    string DisplayLabel,
    string BoundedContext,
    string? RouteUrl,
    string? CommandTypeName,
    int Score,
    bool IsInCurrentContext,
    Type? ProjectionType = null,
    string? DescriptionKey = null);
