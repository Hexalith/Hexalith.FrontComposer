using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Pure reducers for <see cref="DataGridNavigationState"/> (Story 2-2 AC7, Decision D30).
/// LRU cap is carried in <see cref="DataGridNavigationState.Cap"/> (seeded by
/// <see cref="DataGridNavigationFeature"/> from <c>FcShellOptions.DataGridNavCap</c>) so
/// reducers remain pure — no mutable process-static (Group D code review W1 resolution).
/// </summary>
public static class DataGridNavigationReducers {
    [ReducerMethod]
    public static DataGridNavigationState ReduceCapture(DataGridNavigationState state, CaptureGridStateAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        ImmutableDictionary<string, GridViewSnapshot> next = state.ViewStates.SetItem(action.ViewKey, action.Snapshot);

        // LRU eviction (Decision D33) — capped per state.Cap. Clamp to 1 so a bad
        // upstream configuration never silently drains state.
        int cap = Math.Max(1, state.Cap);
        while (next.Count > cap) {
            string? oldestKey = null;
            DateTimeOffset oldestAt = DateTimeOffset.MaxValue;
            foreach (KeyValuePair<string, GridViewSnapshot> kvp in next) {
                // Strict less-than keeps the first-seen key on equality; the ordinal tie-break
                // below makes "first-seen" deterministic across ImmutableDictionary iterations.
                int compare = kvp.Value.CapturedAt < oldestAt ? -1
                    : kvp.Value.CapturedAt > oldestAt ? 1
                    : oldestKey is null ? -1
                    : StringComparer.Ordinal.Compare(kvp.Key, oldestKey);
                if (compare < 0) {
                    oldestAt = kvp.Value.CapturedAt;
                    oldestKey = kvp.Key;
                }
            }

            if (oldestKey is null) {
                break;
            }

            next = next.Remove(oldestKey);
        }

        return state with { ViewStates = next };
    }

    // Restore is read-side — no state mutation (Decision D30). Story 4.3 effects will read+dispatch downstream.
    [ReducerMethod]
    public static DataGridNavigationState ReduceRestore(DataGridNavigationState state, RestoreGridStateAction _) {
        ArgumentNullException.ThrowIfNull(state);
        return state;
    }

    [ReducerMethod]
    public static DataGridNavigationState ReduceClear(DataGridNavigationState state, ClearGridStateAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.ViewStates.ContainsKey(action.ViewKey)
            ? state with { ViewStates = state.ViewStates.Remove(action.ViewKey) }
            : state;
    }

    [ReducerMethod]
    public static DataGridNavigationState ReducePruneExpired(DataGridNavigationState state, PruneExpiredAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        List<string>? toRemove = null;
        foreach (KeyValuePair<string, GridViewSnapshot> kvp in state.ViewStates) {
            if (kvp.Value.CapturedAt < action.Threshold) {
                (toRemove ??= []).Add(kvp.Key);
            }
        }

        if (toRemove is null) {
            return state;
        }

        ImmutableDictionary<string, GridViewSnapshot> next = state.ViewStates;
        foreach (string key in toRemove) {
            next = next.Remove(key);
        }

        return state with { ViewStates = next };
    }

    /// <summary>
    /// Adds the hydrated snapshot into <see cref="DataGridNavigationState.ViewStates"/> iff the
    /// view-key is absent (Story 3-6 D8 / ADR-050). Present-key = no-op — in-memory state wins
    /// over storage so a fresher in-circuit capture isn't stomped by a stale cross-tab blob.
    /// </summary>
    /// <param name="state">The current DataGrid navigation state.</param>
    /// <param name="action">The grid-view-hydrated action.</param>
    /// <returns>A new state with the snapshot inserted when the key was absent; the same state otherwise.</returns>
    [ReducerMethod]
    public static DataGridNavigationState ReduceGridViewHydrated(
        DataGridNavigationState state,
        GridViewHydratedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.ViewStates.ContainsKey(action.ViewKey)
            ? state
            : state with { ViewStates = state.ViewStates.SetItem(action.ViewKey, action.Snapshot) };
    }

    /// <summary>
    /// Flips <see cref="DataGridNavigationState.HydrationState"/> from <see cref="DataGridNavigationHydrationState.Idle"/>
    /// to <see cref="DataGridNavigationHydrationState.Hydrating"/> at the start of the hydrate path
    /// (Story 3-6 D19 / A7). No-op when already <c>Hydrated</c>.
    /// </summary>
    /// <param name="state">The current DataGrid navigation state.</param>
    /// <param name="action">The hydrating action.</param>
    /// <returns>A new state with <c>HydrationState = Hydrating</c> when transitioning from <c>Idle</c>.</returns>
    [ReducerMethod]
    public static DataGridNavigationState ReduceDataGridNavigationHydrating(
        DataGridNavigationState state,
        DataGridNavigationHydratingAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.HydrationState == DataGridNavigationHydrationState.Hydrated
            ? state
            : state with { HydrationState = DataGridNavigationHydrationState.Hydrating };
    }

    /// <summary>
    /// Flips <see cref="DataGridNavigationState.HydrationState"/> to <see cref="DataGridNavigationHydrationState.Hydrated"/>
    /// at the end of the hydrate path (Story 3-6 D19 / A7). Called on BOTH happy path AND
    /// fail-closed path.
    /// </summary>
    /// <param name="state">The current DataGrid navigation state.</param>
    /// <param name="action">The hydrated-completed action.</param>
    /// <returns>A new state with <c>HydrationState = Hydrated</c>.</returns>
    [ReducerMethod]
    public static DataGridNavigationState ReduceDataGridNavigationHydratedCompleted(
        DataGridNavigationState state,
        DataGridNavigationHydratedCompletedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.HydrationState == DataGridNavigationHydrationState.Hydrated
            ? state
            : state with { HydrationState = DataGridNavigationHydrationState.Hydrated };
    }
}
