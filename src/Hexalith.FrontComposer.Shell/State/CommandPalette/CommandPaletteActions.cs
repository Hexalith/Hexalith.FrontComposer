using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Dispatched when the palette opens (Ctrl+K, header icon click, or programmatic) (Story 3-4 D6 / D11 / D12).
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
public sealed record PaletteOpenedAction(string CorrelationId);

/// <summary>
/// Dispatched when the palette closes (Escape, X-button, backdrop click, activation, or
/// circuit-disconnect via <c>FcCommandPalette.DisposeAsync</c>) (Story 3-4 D11 / D20).
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
public sealed record PaletteClosedAction(string CorrelationId);

/// <summary>
/// Dispatched when the user changes the query in the palette search input (Story 3-4 D9).
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
/// <param name="Query">The new query string (raw user input).</param>
public sealed record PaletteQueryChangedAction(string CorrelationId, string Query);

/// <summary>
/// Dispatched when the per-user persistence scope (tenant or user) changes within a single
/// circuit (Story 3-4 DN2 / feedback_tenant_isolation_fail_closed). Clears
/// <see cref="FrontComposerCommandPaletteState.RecentRouteUrls"/> so the next hydrate repopulates
/// from the new scope's storage partition.
/// </summary>
public sealed record PaletteScopeChangedAction;

/// <summary>
/// Dispatched by <c>CommandPaletteEffects</c> after the debounce + scoring pass completes
/// (Story 3-4 D8 / D20). Reducer applies the D20 stale-result guard: refuses assignment when
/// the palette has closed between dispatch and reduce.
/// </summary>
/// <param name="Query">The user's original query (not the alias-canonicalised form).</param>
/// <param name="Results">The pre-computed ranked result set.</param>
public sealed record PaletteResultsComputedAction(string Query, ImmutableArray<PaletteResult> Results);

/// <summary>
/// Dispatched on Arrow Up / Arrow Down inside the palette dialog (Story 3-4 D11 / AC5).
/// </summary>
/// <param name="Delta">Selection delta — <c>+1</c> for ArrowDown, <c>-1</c> for ArrowUp.</param>
public sealed record PaletteSelectionMovedAction(int Delta);

/// <summary>
/// Dispatched when the user activates the currently-selected result (Enter or click) (Story 3-4 D13 / D23).
/// </summary>
/// <param name="SelectedIndex">The flat index into <see cref="FrontComposerCommandPaletteState.Results"/>.</param>
public sealed record PaletteResultActivatedAction(int SelectedIndex);

/// <summary>
/// Dispatched after a successful palette activation lands a navigation (Story 3-4 D10 / D13).
/// Triggers the recent-route ring-buffer persist effect.
/// </summary>
/// <param name="Url">The navigated route URL.</param>
public sealed record RecentRouteVisitedAction(string Url);

/// <summary>
/// Dispatched by <c>CommandPaletteEffects.HandleAppInitialized</c> after loading the persisted
/// recent-route ring buffer (Story 3-4 D10). Hydrate is read-only — does NOT trigger re-persistence.
/// </summary>
/// <param name="RecentRouteUrls">The persisted (and route-safety-filtered) recent-route list.</param>
public sealed record PaletteHydratedAction(ImmutableArray<string> RecentRouteUrls);
