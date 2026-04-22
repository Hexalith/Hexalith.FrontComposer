using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Fluxor state record for the framework-owned command palette (Story 3-4 D6 / D11 / D12 / D20
/// and Story 3-6 D19 deviation).
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IsOpen"/> shadows <c>IDialogService</c>'s internal state for the shortcut-arbitration
/// path (D12 idempotent open). Every dismiss path eventually flips this back to
/// <see langword="false"/> via <c>PaletteClosedAction</c> (D11 dismiss-path coherence).
/// </para>
/// <para>
/// <see cref="Results"/> is NEVER persisted; <see cref="RecentRouteUrls"/> IS persisted under
/// <c>{tenantId}:{userId}:palette-recent</c> (D10).
/// </para>
/// </remarks>
/// <param name="IsOpen">Whether the palette dialog is currently open.</param>
/// <param name="Query">The current search query (raw user input, not the alias-canonicalised form).</param>
/// <param name="Results">The current ranked result set.</param>
/// <param name="RecentRouteUrls">Ring buffer of recently-visited routes (max 5, most-recent-first).</param>
/// <param name="SelectedIndex">The flat index of the currently-selected result (clamped to <c>[0, Results.Length - 1]</c>).</param>
/// <param name="LoadState">Whether a debounce is in flight.</param>
/// <param name="HydrationState">
/// Transient three-state hydration marker (Story 3-6 D19). Initial value <see cref="CommandPaletteHydrationState.Idle"/>;
/// flips <c>Idle → Hydrating → Hydrated</c> via dedicated reducers. NEVER persisted. Re-hydrate
/// via <c>StorageReadyAction</c> only runs when this is <see cref="CommandPaletteHydrationState.Idle"/>.
/// </param>
public sealed record FrontComposerCommandPaletteState(
    bool IsOpen,
    string Query,
    ImmutableArray<PaletteResult> Results,
    ImmutableArray<string> RecentRouteUrls,
    int SelectedIndex,
    PaletteLoadState LoadState,
    CommandPaletteHydrationState HydrationState = CommandPaletteHydrationState.Idle)
{
    /// <summary>Maximum size of the recent-route ring buffer (Story 3-4 D10).</summary>
    public const int RingBufferCap = 5;
}
