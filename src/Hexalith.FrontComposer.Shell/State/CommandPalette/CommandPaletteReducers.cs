using System.Collections.Immutable;

using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Pure static reducers for <see cref="FrontComposerCommandPaletteState"/> (Story 3-4 D6 / D8 / D11 / D20).
/// </summary>
/// <remarks>
/// Reducers do NOT compute scores — the producer effect pre-resolves
/// <see cref="PaletteResultsComputedAction.Results"/> before dispatching (D8 — mirrors Story 3-3
/// ADR-039 purity invariant).
/// </remarks>
public static class CommandPaletteReducers {
    /// <summary>
    /// Sets <see cref="FrontComposerCommandPaletteState.IsOpen"/> = <see langword="true"/> while
    /// preserving every other field (queries / results pre-population happen via the effect).
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The palette-opened action.</param>
    /// <returns>A new state with the dialog open.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReducePaletteOpened(
        FrontComposerCommandPaletteState state,
        PaletteOpenedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { IsOpen = true, LoadState = PaletteLoadState.Idle };
    }

    /// <summary>
    /// Resets <see cref="FrontComposerCommandPaletteState.IsOpen"/> to <see langword="false"/> AND
    /// clears the in-flight query / result set so a subsequent open starts fresh.
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The palette-closed action.</param>
    /// <returns>A new state with the dialog closed.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReducePaletteClosed(
        FrontComposerCommandPaletteState state,
        PaletteClosedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with {
            IsOpen = false,
            Query = string.Empty,
            Results = ImmutableArray<PaletteResult>.Empty,
            SelectedIndex = 0,
            LoadState = PaletteLoadState.Idle,
        };
    }

    /// <summary>
    /// Records the new query and flips <see cref="FrontComposerCommandPaletteState.LoadState"/> to
    /// <see cref="PaletteLoadState.Searching"/>.
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The query-changed action.</param>
    /// <returns>A new state with the query and a Searching load flag.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReducePaletteQueryChanged(
        FrontComposerCommandPaletteState state,
        PaletteQueryChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { Query = action.Query, LoadState = PaletteLoadState.Searching };
    }

    /// <summary>
    /// Assigns the pre-computed <see cref="PaletteResultsComputedAction.Results"/>. The D20
    /// stale-result guard refuses assignments that arrive after the palette closed
    /// (<see cref="FrontComposerCommandPaletteState.IsOpen"/> is <see langword="false"/>).
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The results-computed action.</param>
    /// <returns>The new state with results assigned, or the unchanged state when D20 fires.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReducePaletteResultsComputed(
        FrontComposerCommandPaletteState state,
        PaletteResultsComputedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if (!state.IsOpen) {
            // D20 — refuse assignments after close so a re-open does not flash stale results.
            return state;
        }

        // Stale-computation guard: a late-arriving scoring pass for an earlier query must not
        // overwrite the result set now bound to a newer query (debounce CTS race).
        if (!string.Equals(state.Query, action.Query, StringComparison.Ordinal)) {
            return state;
        }

        // DN4 — preserve the user's arrow-selection when the new Results still spans it,
        // instead of hard-resetting to 0 on every query refinement.
        int preservedIndex = state.Results.IsEmpty
            ? 0
            : Math.Clamp(state.SelectedIndex, 0, Math.Max(0, action.Results.Length - 1));
        if (action.Results.IsEmpty) {
            preservedIndex = 0;
        }

        return state with {
            Results = action.Results,
            SelectedIndex = preservedIndex,
            LoadState = PaletteLoadState.Ready,
        };
    }

    /// <summary>
    /// Clamps the selected index to <c>[0, Results.Length - 1]</c>. No-op on empty result set.
    /// No wrap-around in v1.
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The selection-moved action.</param>
    /// <returns>A new state with the clamped selected index.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReducePaletteSelectionMoved(
        FrontComposerCommandPaletteState state,
        PaletteSelectionMovedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if (state.Results.IsEmpty) {
            return state;
        }

        int candidate = state.SelectedIndex + action.Delta;
        int clamped = Math.Clamp(candidate, 0, state.Results.Length - 1);
        return clamped == state.SelectedIndex
            ? state
            : state with { SelectedIndex = clamped };
    }

    /// <summary>
    /// Marks an activation (no state mutation today — the effect handles navigation + recent-route
    /// dispatch). The reducer exists so Fluxor's pipeline observes the action; future state additions
    /// (e.g., a "last-activated" field) layer here.
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The result-activated action.</param>
    /// <returns>The state unchanged.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReducePaletteResultActivated(
        FrontComposerCommandPaletteState state,
        PaletteResultActivatedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state;
    }

    /// <summary>
    /// Prepends the visited route to the ring buffer (max 5, de-dup on exact string match)
    /// (Story 3-4 D10).
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The recent-route-visited action.</param>
    /// <returns>A new state with the ring buffer updated.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReduceRecentRouteVisited(
        FrontComposerCommandPaletteState state,
        RecentRouteVisitedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        // P14 (2026-04-21 pass-3): OrdinalIgnoreCase dedupe — Blazor's default routing is
        // case-insensitive, so `/Counter` and `/counter` resolve to the same view. Using Ordinal
        // here allowed the ring buffer to fill with near-duplicates differing only in case.
        ImmutableArray<string> updated = state.RecentRouteUrls.Remove(action.Url, StringComparer.OrdinalIgnoreCase).Insert(0, action.Url);
        if (updated.Length > FrontComposerCommandPaletteState.RingBufferCap) {
            updated = [.. updated.Take(FrontComposerCommandPaletteState.RingBufferCap)];
        }

        return state with { RecentRouteUrls = updated };
    }

    /// <summary>
    /// Replaces <see cref="FrontComposerCommandPaletteState.RecentRouteUrls"/> wholesale from the
    /// hydrate payload. Does NOT trigger re-persistence (ADR-038 mirror).
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The hydrated action.</param>
    /// <returns>A new state with the hydrated ring buffer assigned.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReducePaletteHydrated(
        FrontComposerCommandPaletteState state,
        PaletteHydratedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        // Hydrate-vs-first-visit race: if the user activated a palette result before storage
        // completed, that route is already in state. Do NOT overwrite with the (now-stale)
        // pre-visit hydrate payload.
        if (!state.RecentRouteUrls.IsEmpty) {
            return state;
        }

        // Cap tampered / schema-drifted blobs at RingBufferCap so the ring buffer invariant
        // survives untrusted storage contents.
        ImmutableArray<string> capped = action.RecentRouteUrls.Length > FrontComposerCommandPaletteState.RingBufferCap
            ? [.. action.RecentRouteUrls.Take(FrontComposerCommandPaletteState.RingBufferCap)]
            : action.RecentRouteUrls;
        return state with { RecentRouteUrls = capped };
    }

    /// <summary>
    /// Clears per-user state when the persistence scope (tenant/user) changes mid-circuit (DN2).
    /// The hydrate effect then repopulates from the new scope's storage partition.
    /// </summary>
    /// <param name="state">The current palette state.</param>
    /// <param name="action">The scope-changed action.</param>
    /// <returns>A new state with the ring buffer cleared.</returns>
    [ReducerMethod]
    public static FrontComposerCommandPaletteState ReducePaletteScopeChanged(
        FrontComposerCommandPaletteState state,
        PaletteScopeChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { RecentRouteUrls = ImmutableArray<string>.Empty };
    }
}
