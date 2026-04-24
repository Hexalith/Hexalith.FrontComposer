using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-3 T3 / D1 / D3 / D8 / D17 / D21 — effects that translate filter-surface actions
/// (<see cref="ColumnFilterChangedAction"/>, <see cref="StatusFilterToggledAction"/>,
/// <see cref="GlobalSearchChangedAction"/>, <see cref="SortChangedAction"/>,
/// <see cref="FiltersResetAction"/>) into Story 2-2 <see cref="CaptureGridStateAction"/> /
/// <see cref="ClearGridStateAction"/> dispatches.
/// </summary>
/// <remarks>
/// <para>
/// <b>Single write path (D8):</b> every filter action handler reads the current snapshot,
/// computes the next snapshot with reserved-key packing for status / search (D3), and dispatches
/// <see cref="CaptureGridStateAction"/>. Story 2-2's reducer applies the snapshot to ViewStates,
/// Story 3-6's effect persists it — no parallel write path.
/// </para>
/// <para>
/// <b>Reset (D17):</b> <see cref="FiltersResetAction"/> dispatches <see cref="ClearGridStateAction"/>
/// so the Story 2-2 reducer removes the entry from ViewStates AND Story 3-6's effect removes
/// the LocalStorage blob.
/// </para>
/// <para>
/// <b>Reserved-key invariant:</b> filter column keys starting with <c>__</c> are rejected at
/// the <see cref="ColumnFilterChangedAction"/> entry point — the generator emits only
/// C#-identifier-legal property names, so this path is defensive and unit-testable.
/// </para>
/// </remarks>
public sealed class FilterEffects {
    private static readonly char[] StatusCsvSeparator = [','];
    private readonly IState<DataGridNavigationState> _state;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="FilterEffects"/> class.</summary>
    /// <param name="state">Read-only access to the DataGrid navigation state.</param>
    /// <param name="timeProvider">Time source used for the <c>CapturedAt</c> timestamp (<see cref="TimeProvider.System"/> in production).</param>
    public FilterEffects(IState<DataGridNavigationState> state, TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(state);
        _state = state;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>T3.5 / D21 — write or clear a column filter value.</summary>
    [EffectMethod]
    public Task HandleColumnFilterChanged(ColumnFilterChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        if (action.ColumnKey.StartsWith("__", StringComparison.Ordinal)) {
            throw new InvalidOperationException(
                $"Column key '{action.ColumnKey}' collides with a reserved filter key. " +
                "Reserved keys ('__status', '__search') must never be written by a column-filter action.");
        }

        GridViewSnapshot current = GetOrEmptySnapshot(action.ViewKey);
        IImmutableDictionary<string, string> filters = current.Filters;
        string? trimmed = NormalizeFilterValue(action.FilterValue);

        filters = trimmed is null
            ? filters.Remove(action.ColumnKey)
            : filters.SetItem(action.ColumnKey, trimmed);

        GridViewSnapshot next = current with {
            Filters = filters,
            CapturedAt = UtcNow(),
        };
        dispatcher.Dispatch(new CaptureGridStateAction(action.ViewKey, next));
        return Task.CompletedTask;
    }

    /// <summary>T3.3 — toggle a slot name inside the reserved <c>__status</c> CSV.</summary>
    [EffectMethod]
    public Task HandleStatusFilterToggled(StatusFilterToggledAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        GridViewSnapshot current = GetOrEmptySnapshot(action.ViewKey);
        HashSet<string> slots = ParseStatusCsv(current.Filters);
        if (!slots.Add(action.SlotName)) {
            slots.Remove(action.SlotName);
        }

        IImmutableDictionary<string, string> filters = slots.Count == 0
            ? current.Filters.Remove(ReservedFilterKeys.StatusKey)
            : current.Filters.SetItem(ReservedFilterKeys.StatusKey, JoinStatusCsv(slots));

        GridViewSnapshot next = current with {
            Filters = filters,
            CapturedAt = UtcNow(),
        };
        dispatcher.Dispatch(new CaptureGridStateAction(action.ViewKey, next));
        return Task.CompletedTask;
    }

    /// <summary>T3.6 — write or clear the reserved <c>__search</c> query.</summary>
    [EffectMethod]
    public Task HandleGlobalSearchChanged(GlobalSearchChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        GridViewSnapshot current = GetOrEmptySnapshot(action.ViewKey);
        string? trimmed = NormalizeFilterValue(action.Query);

        IImmutableDictionary<string, string> filters = trimmed is null
            ? current.Filters.Remove(ReservedFilterKeys.SearchKey)
            : current.Filters.SetItem(ReservedFilterKeys.SearchKey, trimmed);

        GridViewSnapshot next = current with {
            Filters = filters,
            CapturedAt = UtcNow(),
        };
        dispatcher.Dispatch(new CaptureGridStateAction(action.ViewKey, next));
        return Task.CompletedTask;
    }

    /// <summary>T3.4 — write sort column + direction onto the snapshot.</summary>
    [EffectMethod]
    public Task HandleSortChanged(SortChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        GridViewSnapshot current = GetOrEmptySnapshot(action.ViewKey);
        GridViewSnapshot next = current with {
            SortColumn = action.SortColumn,
            SortDescending = action.SortDescending,
            CapturedAt = UtcNow(),
        };
        dispatcher.Dispatch(new CaptureGridStateAction(action.ViewKey, next));
        return Task.CompletedTask;
    }

    /// <summary>T3.2 / D17 — reset chains into <see cref="ClearGridStateAction"/>.</summary>
#pragma warning disable CA1822 // Fluxor discovers effects via reflection on instance methods.
    [EffectMethod]
    public Task HandleFiltersReset(FiltersResetAction action, IDispatcher dispatcher) {
#pragma warning restore CA1822
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        dispatcher.Dispatch(new ClearGridStateAction(action.ViewKey));
        return Task.CompletedTask;
    }

    private GridViewSnapshot GetOrEmptySnapshot(string viewKey) {
        if (_state.Value.ViewStates.TryGetValue(viewKey, out GridViewSnapshot? existing)) {
            return existing;
        }

        return new GridViewSnapshot(
            scrollTop: 0,
            filters: ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal),
            sortColumn: null,
            sortDescending: false,
            expandedRowId: null,
            selectedRowId: null,
            capturedAt: UtcNow());
    }

    private DateTimeOffset UtcNow() => _timeProvider.GetUtcNow().ToUniversalTime();

    private static HashSet<string> ParseStatusCsv(IImmutableDictionary<string, string> filters) {
        if (!filters.TryGetValue(ReservedFilterKeys.StatusKey, out string? csv) || string.IsNullOrWhiteSpace(csv)) {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        string[] tokens = csv.Split(StatusCsvSeparator, StringSplitOptions.RemoveEmptyEntries);
        HashSet<string> result = new(StringComparer.Ordinal);
        foreach (string token in tokens) {
            string trimmed = token.Trim();
            if (trimmed.Length > 0) {
                _ = result.Add(trimmed);
            }
        }

        return result;
    }

    private static string JoinStatusCsv(IEnumerable<string> slots)
        => string.Join(",", slots.OrderBy(static slot => slot, StringComparer.Ordinal));

    private static string? NormalizeFilterValue(string? value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        return value;
    }
}
