using System.Collections.Immutable;

using Fluxor;
using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// PURE reducers mutating <see cref="DataGridNavigationState"/> for visibility + scroll actions
/// (Story 4-4 D4 / D7). Reducers update the snapshot and return; effects chain
/// <see cref="CaptureGridStateAction"/> separately.
/// </summary>
public static class VirtualizationViewStateReducers {
    /// <summary>
    /// Updates <see cref="GridViewSnapshot.Filters"/>[<c>"__hidden"</c>] CSV by adding or removing
    /// the toggled column key. Removes the entry entirely when the CSV becomes empty.
    /// Creates the view snapshot on demand when the key is missing.
    /// </summary>
    [ReducerMethod]
    public static DataGridNavigationState ReduceColumnVisibilityChanged(
        DataGridNavigationState state,
        ColumnVisibilityChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        GridViewSnapshot current = state.ViewStates.TryGetValue(action.ViewKey, out GridViewSnapshot? existing)
            ? existing
            : new GridViewSnapshot(
                scrollTop: 0,
                filters: ImmutableDictionary<string, string>.Empty,
                sortColumn: null,
                sortDescending: false,
                expandedRowId: null,
                selectedRowId: null,
                capturedAt: DateTimeOffset.UtcNow);

        HashSet<string> hidden = ParseHiddenCsv(current.Filters);
        if (action.IsVisible) {
            _ = hidden.Remove(action.ColumnKey);
        }
        else {
            _ = hidden.Add(action.ColumnKey);
        }

        IImmutableDictionary<string, string> nextFilters = current.Filters.SetItem(
            VirtualizationReservedKeys.HiddenColumnsKey,
            string.Join(",", hidden.OrderBy(k => k, StringComparer.Ordinal)));

        if (ReferenceEquals(nextFilters, current.Filters)) {
            return state;
        }

        GridViewSnapshot next = current with { Filters = nextFilters };
        return state with { ViewStates = state.ViewStates.SetItem(action.ViewKey, next) };
    }

    /// <summary>Removes the <c>"__hidden"</c> reserved key entirely (Story 4-4 D6 / AC5 "Reset to defaults").</summary>
    [ReducerMethod]
    public static DataGridNavigationState ReduceResetColumnVisibility(
        DataGridNavigationState state,
        ResetColumnVisibilityAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        if (!state.ViewStates.TryGetValue(action.ViewKey, out GridViewSnapshot? current)) {
            return state;
        }

        if (!current.Filters.ContainsKey(VirtualizationReservedKeys.HiddenColumnsKey)) {
            return state;
        }

        GridViewSnapshot next = current with {
            Filters = current.Filters.Remove(VirtualizationReservedKeys.HiddenColumnsKey),
        };
        return state with { ViewStates = state.ViewStates.SetItem(action.ViewKey, next) };
    }

    /// <summary>
    /// Updates <see cref="GridViewSnapshot.ScrollTop"/> with defensive revalidation. Creates the
    /// snapshot on demand. No chained dispatch — <c>ScrollPersistenceEffect</c> handles the
    /// debounced <see cref="CaptureGridStateAction"/>.
    /// </summary>
    [ReducerMethod]
    public static DataGridNavigationState ReduceScrollCaptured(
        DataGridNavigationState state,
        ScrollCapturedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        // The record validates ScrollTop at construction; the reducer revalidates for
        // defence-in-depth against adopter-side custom dispatchers bypassing the record guard.
        if (double.IsNaN(action.ScrollTop) || double.IsInfinity(action.ScrollTop) || action.ScrollTop < 0) {
            return state;
        }

        GridViewSnapshot current = state.ViewStates.TryGetValue(action.ViewKey, out GridViewSnapshot? existing)
            ? existing
            : new GridViewSnapshot(
                scrollTop: 0,
                filters: ImmutableDictionary<string, string>.Empty,
                sortColumn: null,
                sortDescending: false,
                expandedRowId: null,
                selectedRowId: null,
                capturedAt: DateTimeOffset.UtcNow);

        if (current.ScrollTop.Equals(action.ScrollTop)) {
            return state;
        }

        GridViewSnapshot next = current with { ScrollTop = action.ScrollTop };
        return state with { ViewStates = state.ViewStates.SetItem(action.ViewKey, next) };
    }

    private static HashSet<string> ParseHiddenCsv(IImmutableDictionary<string, string> filters) {
        if (!filters.TryGetValue(VirtualizationReservedKeys.HiddenColumnsKey, out string? csv)
            || string.IsNullOrEmpty(csv)) {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (string part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
            _ = result.Add(part);
        }

        return result;
    }
}
