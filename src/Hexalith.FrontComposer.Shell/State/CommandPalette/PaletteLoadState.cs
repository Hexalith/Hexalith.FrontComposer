namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Load-state hint for the palette dialog. Drives the "Searching…" / "Ready" copy in the result
/// list (Story 3-4 D6 / D8).
/// </summary>
public enum PaletteLoadState {
    /// <summary>No active query (first paint, or after close).</summary>
    Idle = 0,

    /// <summary>Debounce is in flight — the previous results are stale but still shown.</summary>
    Searching = 1,

    /// <summary>Latest results are live — safe to render.</summary>
    Ready = 2,
}
