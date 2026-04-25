using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.ExpandedRow;

/// <summary>
/// Story 4-5 D4 / D18 — PURE reducers for <see cref="ExpandedRowState"/>. Reducers NEVER chain
/// dispatches (Story 4-4 D4 / D7 Fluxor-purity correction inherited). The single-expand
/// invariant (UX-DR17) is enforced at the reducer level: <see cref="ExpandRowAction"/> for an
/// existing view-key REPLACES the entry; the view does NOT need to dispatch
/// <see cref="CollapseRowAction"/> first.
/// </summary>
public static class ExpandedRowReducers {
    /// <summary>
    /// Writes (or replaces) the entry for <see cref="ExpandRowAction.ViewKey"/>. The
    /// REPLACE-on-existing semantic is the load-bearing single-expand invariant — accumulating
    /// multiple expansions per view-key is impossible regardless of view-side dispatch ordering.
    /// </summary>
    [ReducerMethod]
    public static ExpandedRowState ReduceExpandRow(ExpandedRowState state, ExpandRowAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        ExpandedRowEntry entry = new(action.ItemKey, DateTimeOffset.UtcNow);
        return state with {
            ExpandedByViewKey = state.ExpandedByViewKey.SetItem(action.ViewKey, entry),
        };
    }

    /// <summary>
    /// Removes the entry for <see cref="CollapseRowAction.ViewKey"/>. Idempotent — a no-op when
    /// no entry exists, supporting the unconditional <c>DisposeAsync</c> dispatch contract (D18).
    /// </summary>
    [ReducerMethod]
    public static ExpandedRowState ReduceCollapseRow(ExpandedRowState state, CollapseRowAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        if (!state.ExpandedByViewKey.ContainsKey(action.ViewKey)) {
            return state;
        }

        return state with {
            ExpandedByViewKey = state.ExpandedByViewKey.Remove(action.ViewKey),
        };
    }
}
