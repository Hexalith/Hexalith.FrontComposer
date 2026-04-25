using System.Collections.Immutable;
using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// PURE reducers for <see cref="LoadedPageState"/> (Story 4-4 D3 / D4 / D6 / D7 / D10 / D16).
/// Reducers NEVER chain dispatches — persistence effects listen separately. TCS resolution
/// always goes through <c>TrySet*</c> variants so rapid-scroll races are absorbed silently.
/// </summary>
/// <remarks>
/// The reducer class is non-static because <see cref="FcShellOptions.MaxCachedPages"/> and the
/// logger must be injected — Fluxor supports DI-backed reducer classes via its reducer discovery.
/// </remarks>
public sealed class LoadedPageReducers {
    private readonly IOptionsMonitor<FcShellOptions> _options;
    private readonly ILogger<LoadedPageReducers> _logger;

    /// <summary>Initializes a new instance of the <see cref="LoadedPageReducers"/> class.</summary>
    /// <param name="options">Shell options monitor; <c>MaxCachedPages</c> governs FIFO eviction (Story 4-4 D10).</param>
    /// <param name="logger">Logger for the Information-level eviction breadcrumb.</param>
    public LoadedPageReducers(
        IOptionsMonitor<FcShellOptions> options,
        ILogger<LoadedPageReducers> logger) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Registers the provider-supplied TCS in <see cref="LoadedPageState.PendingCompletionsByKey"/>
    /// and latches the server-side virtualization lane for the view key on first dispatch
    /// (Story 4-4 D2 / D3 double-registration idempotency).
    /// </summary>
    /// <remarks>
    /// Double-registration idempotency: if an entry already exists for <c>(viewKey, skip)</c>,
    /// <c>TrySetCanceled</c> is called on the existing TCS before it is replaced — preventing
    /// silent orphan of the first TCS under rapid-scroll-and-bounce.
    /// </remarks>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPage(LoadedPageState state, LoadPageAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> nextPending = state.PendingCompletionsByKey;

        if (nextPending.TryGetValue(key, out TaskCompletionSource<object>? existing)
            && !ReferenceEquals(existing, action.Completion)) {
            existing.TrySetCanceled();
            nextPending = nextPending.SetItem(key, action.Completion);
        }
        else if (!nextPending.ContainsKey(key)) {
            nextPending = nextPending.SetItem(key, action.Completion);
        }

        ImmutableDictionary<string, VirtualizationLane> nextLane = state.LaneByKey.ContainsKey(action.ViewKey)
            ? state.LaneByKey
            : state.LaneByKey.SetItem(action.ViewKey, VirtualizationLane.ServerSide);

        if (ReferenceEquals(nextPending, state.PendingCompletionsByKey)
            && ReferenceEquals(nextLane, state.LaneByKey)) {
            return state;
        }

        return state with {
            PendingCompletionsByKey = nextPending,
            LaneByKey = nextLane,
        };
    }

    /// <summary>
    /// Writes the loaded page into <see cref="LoadedPageState.PagesByKey"/>, updates
    /// <see cref="LoadedPageState.TotalCountByKey"/> + <see cref="LoadedPageState.LastElapsedMsByKey"/>,
    /// enqueues the insertion-order token, resolves the matching TCS via <c>TrySetResult</c>,
    /// and evicts the oldest entry when <c>MaxCachedPages</c> is exceeded.
    /// </summary>
    /// <remarks>
    /// <b>Null-items guard (D3 chaos-monkey):</b> a null <c>Items</c> payload is treated as a
    /// failure — the TCS receives <c>TrySetException</c>, the entry is removed, and the log
    /// warning is emitted. <see cref="LoadedPageState.PagesByKey"/> is untouched.
    /// </remarks>
    [ReducerMethod]
    public LoadedPageState ReduceLoadPageSucceeded(LoadedPageState state, LoadPageSucceededAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        if (action.Completion is not null
            && state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? pending)
            && !ReferenceEquals(pending, action.Completion)) {
            return state;
        }

        if (action.Items is null) {
            _logger.LogWarning(
                "LoadPageSucceededAction received null Items payload. ViewKey={ViewKey}, Skip={Skip}.",
                action.ViewKey,
                action.Skip);

            if (state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? nullTcs)) {
                nullTcs.TrySetException(new InvalidOperationException(
                    $"LoadPageSucceededAction received null Items payload for (ViewKey={action.ViewKey}, Skip={action.Skip})."));
                return state with {
                    PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
                };
            }

            return state;
        }

        bool refreshedExistingPage = state.PagesByKey.ContainsKey(key);
        ImmutableDictionary<(string, int), IReadOnlyList<object>> nextPages =
            state.PagesByKey.SetItem(key, action.Items);
        ImmutableDictionary<string, int> nextTotal = state.TotalCountByKey.SetItem(action.ViewKey, action.TotalCount);
        ImmutableDictionary<string, long> nextElapsed = state.LastElapsedMsByKey.SetItem(action.ViewKey, action.ElapsedMs);
        ImmutableQueue<(string ViewKey, int Skip)> nextOrder = refreshedExistingPage
            ? state.PageInsertionOrder
            : state.PageInsertionOrder.Enqueue(key);
        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> nextPending = state.PendingCompletionsByKey;

        if (nextPending.TryGetValue(key, out TaskCompletionSource<object>? tcs)) {
            tcs.TrySetResult(action.Items);
            nextPending = nextPending.Remove(key);
        }

        int cap = _options.CurrentValue.MaxCachedPages;
        while (nextPages.Count > cap && !nextOrder.IsEmpty) {
            nextOrder = nextOrder.Dequeue(out (string ViewKey, int Skip) evicted);
            if (!nextPages.ContainsKey(evicted)) {
                continue;
            }

            nextPages = nextPages.Remove(evicted);
            _logger.LogInformation(
                "LoadedPageState eviction — MaxCachedPages={Cap} reached; evicted (viewKey={ViewKey}, skip={Skip})",
                cap,
                evicted.ViewKey,
                evicted.Skip);
        }

        return state with {
            PagesByKey = nextPages,
            TotalCountByKey = nextTotal,
            LastElapsedMsByKey = nextElapsed,
            PageInsertionOrder = nextOrder,
            PendingCompletionsByKey = nextPending,
        };
    }

    /// <summary>Resolves the matching TCS via <c>TrySetException</c> and removes the entry.</summary>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPageFailed(LoadedPageState state, LoadPageFailedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        if (!state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? tcs)) {
            return state;
        }

        if (action.Completion is not null && !ReferenceEquals(tcs, action.Completion)) {
            return state;
        }

        tcs.TrySetException(new InvalidOperationException(action.ErrorMessage));
        return state with {
            PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
        };
    }

    /// <summary>Resolves the matching TCS via <c>TrySetCanceled</c> and removes the entry.</summary>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPageCancelled(LoadedPageState state, LoadPageCancelledAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        if (!state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? tcs)) {
            return state;
        }

        if (action.Completion is not null && !ReferenceEquals(tcs, action.Completion)) {
            return state;
        }

        tcs.TrySetCanceled();
        return state with {
            PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
        };
    }

    /// <summary>
    /// Sweeps every <see cref="LoadedPageState.PendingCompletionsByKey"/> entry whose
    /// view-key component matches — invoked from the generated view's <c>DisposeAsync</c>.
    /// </summary>
    [ReducerMethod]
    public static LoadedPageState ReduceClearPendingPages(LoadedPageState state, ClearPendingPagesAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> next = state.PendingCompletionsByKey;
        foreach (KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>> kvp in state.PendingCompletionsByKey) {
            if (string.Equals(kvp.Key.ViewKey, action.ViewKey, StringComparison.Ordinal)) {
                kvp.Value.TrySetCanceled();
                next = next.Remove(kvp.Key);
            }
        }

        return ReferenceEquals(next, state.PendingCompletionsByKey)
            ? state
            : state with { PendingCompletionsByKey = next };
    }
}

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
            hidden.Remove(action.ColumnKey);
        } else {
            hidden.Add(action.ColumnKey);
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
            result.Add(part);
        }

        return result;
    }
}
