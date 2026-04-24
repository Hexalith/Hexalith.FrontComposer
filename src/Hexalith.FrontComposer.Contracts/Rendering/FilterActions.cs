namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 4-3 D1 / AC1 — column-filter change dispatched by <c>FcColumnFilterCell</c>
/// after its 300 ms debounce. Reducer chains into <see cref="CaptureGridStateAction"/>
/// so Story 3-6's effect persists without a parallel write path.
/// </summary>
public sealed record ColumnFilterChangedAction {
    private readonly string _viewKey = string.Empty;
    private readonly string _columnKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ColumnFilterChangedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key (<c>{boundedContext}:{projectionTypeFqn}</c>).</param>
    /// <param name="columnKey">Declared property name of the filtered column.</param>
    /// <param name="filterValue">The new filter value, or <see langword="null"/> to clear the filter.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> or <paramref name="columnKey"/> is null, empty, or whitespace.</exception>
    public ColumnFilterChangedAction(string viewKey, string columnKey, string? filterValue) {
        ViewKey = viewKey;
        ColumnKey = columnKey;
        FilterValue = filterValue;
    }

    /// <summary>Gets the stable per-view key.</summary>
    public string ViewKey {
        get => _viewKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            _viewKey = value;
        }
    }

    /// <summary>Gets the declared property name of the filtered column.</summary>
    /// <remarks>
    /// Review pass 2 — dispatching with a key starting with <c>__</c> is rejected at the record
    /// boundary so reserved-key packing (<see cref="ReservedFilterKeys"/>) cannot be spoofed via a
    /// direct action dispatch that bypasses the reducer/effect guard.
    /// </remarks>
    public string ColumnKey {
        get => _columnKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("Column key cannot be null, empty, or whitespace.", nameof(value));
            }

            if (value.StartsWith("__", System.StringComparison.Ordinal)) {
                throw new System.ArgumentException(
                    "Column key must not start with '__' — that prefix is reserved for framework-managed filter keys (see ReservedFilterKeys).",
                    nameof(value));
            }

            _columnKey = value;
        }
    }

    /// <summary>Gets the filter value, or <see langword="null"/> when the user cleared the input.</summary>
    public string? FilterValue { get; init; }
}

/// <summary>
/// Story 4-3 D1 / AC2 — status-chip toggle dispatched by <c>FcStatusFilterChips</c>.
/// Reducer reads the CSV at <see cref="ReservedFilterKeys.StatusKey"/>, toggles the
/// slot name, writes back, and chains <see cref="CaptureGridStateAction"/>.
/// </summary>
public sealed record StatusFilterToggledAction {
    private readonly string _viewKey = string.Empty;
    private readonly string _slotName = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="StatusFilterToggledAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="slotName">Slot name (enum member name, e.g. <c>Success</c>).</param>
    /// <exception cref="System.ArgumentException">Thrown when either argument is null, empty, or whitespace.</exception>
    public StatusFilterToggledAction(string viewKey, string slotName) {
        ViewKey = viewKey;
        SlotName = slotName;
    }

    /// <summary>Gets the stable per-view key.</summary>
    public string ViewKey {
        get => _viewKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            _viewKey = value;
        }
    }

    /// <summary>Gets the slot name to toggle.</summary>
    public string SlotName {
        get => _slotName;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("Slot name cannot be null, empty, or whitespace.", nameof(value));
            }

            _slotName = value;
        }
    }
}

/// <summary>
/// Story 4-3 D1 / AC4 — global search change dispatched by <c>FcProjectionGlobalSearch</c>.
/// Reducer writes the query under <see cref="ReservedFilterKeys.SearchKey"/>.
/// </summary>
public sealed record GlobalSearchChangedAction {
    private readonly string _viewKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="GlobalSearchChangedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="query">The new search query, or <see langword="null"/> to clear.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public GlobalSearchChangedAction(string viewKey, string? query) {
        ViewKey = viewKey;
        Query = query;
    }

    /// <summary>Gets the stable per-view key.</summary>
    public string ViewKey {
        get => _viewKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            _viewKey = value;
        }
    }

    /// <summary>Gets the new search query, or <see langword="null"/> when the user cleared the input.</summary>
    public string? Query { get; init; }
}

/// <summary>
/// Story 4-3 D1 / D12 / AC1 — sort change dispatched by the generated view's
/// <c>FluentDataGrid</c> sort event. Reducer updates <c>GridViewSnapshot.SortColumn</c>
/// / <c>SortDescending</c> and chains <see cref="CaptureGridStateAction"/>.
/// </summary>
public sealed record SortChangedAction {
    private readonly string _viewKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="SortChangedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="sortColumn">Declared property name of the sort column, or <see langword="null"/> to clear the sort.</param>
    /// <param name="sortDescending">Whether the sort is descending.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public SortChangedAction(string viewKey, string? sortColumn, bool sortDescending) {
        ViewKey = viewKey;
        SortColumn = sortColumn;
        SortDescending = sortDescending;
    }

    /// <summary>Gets the stable per-view key.</summary>
    public string ViewKey {
        get => _viewKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            _viewKey = value;
        }
    }

    /// <summary>Gets the declared property name of the sort column, or <see langword="null"/> when the sort is cleared.</summary>
    public string? SortColumn { get; init; }

    /// <summary>Gets a value indicating whether the sort is descending.</summary>
    public bool SortDescending { get; init; }
}

/// <summary>
/// Story 4-3 D1 / D17 / AC3 — reset dispatched by <c>FcFilterResetButton</c>.
/// Reducer empties every filter / sort field on the snapshot and chains
/// <see cref="ClearGridStateAction"/> so Story 3-6's effect removes the blob.
/// </summary>
public sealed record FiltersResetAction {
    private readonly string _viewKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="FiltersResetAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public FiltersResetAction(string viewKey) {
        ViewKey = viewKey;
    }

    /// <summary>Gets the stable per-view key.</summary>
    public string ViewKey {
        get => _viewKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            _viewKey = value;
        }
    }
}
