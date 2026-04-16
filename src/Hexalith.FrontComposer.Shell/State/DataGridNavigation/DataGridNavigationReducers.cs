using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Pure reducers for <see cref="DataGridNavigationState"/> (Story 2-2 AC7, Decision D30).
/// LRU cap applied on <see cref="CaptureGridStateAction"/>: when <c>ViewStates.Count</c> exceeds
/// <see cref="FcShellOptions.DataGridNavCap"/>, the entry with the oldest <c>CapturedAt</c>
/// is evicted (Decision D33).
/// </summary>
public static class DataGridNavigationReducers {
    /// <summary>
    /// Mutable ambient cap read by <see cref="ReduceCapture"/>. Wired by
    /// <c>AddHexalithFrontComposer()</c> from <see cref="IOptions{FcShellOptions}"/>. Default 50.
    /// </summary>
    public static int Cap { get; set; } = 50;

    [ReducerMethod]
    public static DataGridNavigationState ReduceCapture(DataGridNavigationState state, CaptureGridStateAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        ImmutableDictionary<string, GridViewSnapshot> next = state.ViewStates.SetItem(action.ViewKey, action.Snapshot);

        // LRU eviction (Decision D33) — capped per FcShellOptions.DataGridNavCap.
        int cap = Cap;
        while (next.Count > cap) {
            string? oldestKey = null;
            DateTimeOffset oldestAt = DateTimeOffset.MaxValue;
            foreach (KeyValuePair<string, GridViewSnapshot> kvp in next) {
                if (kvp.Value.CapturedAt < oldestAt) {
                    oldestAt = kvp.Value.CapturedAt;
                    oldestKey = kvp.Key;
                }
            }

            if (oldestKey is null) {
                break;
            }

            next = next.Remove(oldestKey);
        }

        return new DataGridNavigationState(next);
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
            ? new DataGridNavigationState(state.ViewStates.Remove(action.ViewKey))
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

        return new DataGridNavigationState(next);
    }
}
