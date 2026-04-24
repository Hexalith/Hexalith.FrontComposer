using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Story 4-4 D3 / AC2 — dispatched by the generated view's <c>LoadPageAsync</c> provider
/// callback when Fluent requests a page range. <c>LoadPageEffects</c> registers a
/// <c>TaskCompletionSource</c> keyed by <c>(ViewKey, Skip)</c> in
/// <c>LoadedPageState.PendingCompletionsByKey</c> before awaiting
/// <c>IQueryService.QueryAsync</c>; the provider callback awaits that TCS.
/// </summary>
public sealed record LoadPageAction {
    private readonly string _viewKey = string.Empty;
    private readonly int _skip;
    private readonly int _take;

    /// <summary>Initializes a new instance of the <see cref="LoadPageAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key (<c>{boundedContext}:{projectionTypeFqn}</c>).</param>
    /// <param name="skip">Non-negative skip offset.</param>
    /// <param name="take">Positive page size.</param>
    /// <param name="filters">Resolved filter dictionary from the current <see cref="GridViewSnapshot"/>; never null — pass <see cref="ImmutableDictionary{TKey, TValue}.Empty"/> when empty.</param>
    /// <param name="sortColumn">Declared property name to sort by, or <see langword="null"/> when unsorted.</param>
    /// <param name="sortDescending">Whether the sort is descending.</param>
    /// <param name="searchQuery">Global search query, or <see langword="null"/> when the search box is empty.</param>
    /// <param name="completion">TCS awaited by the provider callback; the reducer registers it in <c>PendingCompletionsByKey</c> and the downstream <c>LoadPageSucceededAction</c> / <c>LoadPageFailedAction</c> / <c>LoadPageCancelledAction</c> reducers resolve it via <c>TrySet*</c>.</param>
    /// <param name="cancellationToken">Cancellation flowing from <c>GridItemsProviderRequest&lt;T&gt;.CancellationToken</c>.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="skip"/> is negative or <paramref name="take"/> is not positive.</exception>
    /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="filters"/> or <paramref name="completion"/> is null.</exception>
    public LoadPageAction(
        string viewKey,
        int skip,
        int take,
        IImmutableDictionary<string, string> filters,
        string? sortColumn,
        bool sortDescending,
        string? searchQuery,
        TaskCompletionSource<object> completion,
        CancellationToken cancellationToken) {
        ViewKey = viewKey;
        Skip = skip;
        Take = take;
        Filters = filters ?? throw new System.ArgumentNullException(nameof(filters));
        SortColumn = sortColumn;
        SortDescending = sortDescending;
        SearchQuery = searchQuery;
        Completion = completion ?? throw new System.ArgumentNullException(nameof(completion));
        CancellationToken = cancellationToken;
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

    /// <summary>Gets the non-negative skip offset.</summary>
    public int Skip {
        get => _skip;
        init {
            if (value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Skip must be non-negative.");
            }

            _skip = value;
        }
    }

    /// <summary>Gets the positive page size.</summary>
    public int Take {
        get => _take;
        init {
            if (value <= 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Take must be strictly positive.");
            }

            _take = value;
        }
    }

    /// <summary>Gets the resolved filter dictionary (never null).</summary>
    public IImmutableDictionary<string, string> Filters { get; init; } = ImmutableDictionary<string, string>.Empty;

    /// <summary>Gets the sort column, or <see langword="null"/> when unsorted.</summary>
    public string? SortColumn { get; init; }

    /// <summary>Gets a value indicating whether the sort is descending.</summary>
    public bool SortDescending { get; init; }

    /// <summary>Gets the global search query, or <see langword="null"/> when empty.</summary>
    public string? SearchQuery { get; init; }

    /// <summary>
    /// Gets the TCS awaited by the provider callback. The reducer registers it in
    /// <c>PendingCompletionsByKey</c>; success/failure/cancel reducers resolve via <c>TrySet*</c>.
    /// </summary>
    public TaskCompletionSource<object> Completion { get; init; } = new();

    /// <summary>Gets the cancellation token flowing from the provider request.</summary>
    public CancellationToken CancellationToken { get; init; }
}

/// <summary>
/// Story 4-4 D3 — dispatched by <c>LoadPageEffects.HandleLoadPageAsync</c> on
/// <c>IQueryService.QueryAsync</c> success. The reducer writes the page, updates
/// <c>TotalCountByKey</c> / <c>LastElapsedMsByKey</c>, enqueues the insertion-order token,
/// and resolves the matching TCS via <c>TrySetResult</c>.
/// </summary>
public sealed record LoadPageSucceededAction {
    private readonly string _viewKey = string.Empty;
    private readonly int _skip;

    /// <summary>Initializes a new instance of the <see cref="LoadPageSucceededAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="skip">Non-negative skip offset matching the triggering <see cref="LoadPageAction"/>.</param>
    /// <param name="items">Row payload — MAY be null; the reducer treats null as a failure signal per D3 null-items guard.</param>
    /// <param name="totalCount">Server-reported total row count across all pages (non-negative).</param>
    /// <param name="elapsedMs">Measured elapsed milliseconds (non-negative).</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="skip"/>, <paramref name="totalCount"/>, or <paramref name="elapsedMs"/> is negative.</exception>
    public LoadPageSucceededAction(
        string viewKey,
        int skip,
        IReadOnlyList<object>? items,
        int totalCount,
        long elapsedMs) {
        ViewKey = viewKey;
        Skip = skip;
        Items = items;
        if (totalCount < 0) {
            throw new System.ArgumentOutOfRangeException(nameof(totalCount), totalCount, "TotalCount must be non-negative.");
        }

        if (elapsedMs < 0) {
            throw new System.ArgumentOutOfRangeException(nameof(elapsedMs), elapsedMs, "ElapsedMs must be non-negative.");
        }

        TotalCount = totalCount;
        ElapsedMs = elapsedMs;
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

    /// <summary>Gets the skip offset matching the triggering <see cref="LoadPageAction"/>.</summary>
    public int Skip {
        get => _skip;
        init {
            if (value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Skip must be non-negative.");
            }

            _skip = value;
        }
    }

    /// <summary>Gets the row payload, or <see langword="null"/> (treated as failure by the reducer's D3 guard).</summary>
    public IReadOnlyList<object>? Items { get; init; }

    /// <summary>Gets the server-reported total row count across pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>Gets the measured elapsed milliseconds.</summary>
    public long ElapsedMs { get; init; }
}

/// <summary>
/// Story 4-4 D3 — dispatched by <c>LoadPageEffects</c> (or its defensive finally) when
/// <c>IQueryService.QueryAsync</c> throws. Reducer resolves the matching TCS via
/// <c>TrySetException</c> and removes the entry.
/// </summary>
public sealed record LoadPageFailedAction {
    private readonly string _viewKey = string.Empty;
    private readonly int _skip;
    private readonly string _errorMessage = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="LoadPageFailedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="skip">Non-negative skip offset.</param>
    /// <param name="errorMessage">Human-readable error message (never null or whitespace).</param>
    /// <exception cref="System.ArgumentException">Thrown on invalid <paramref name="viewKey"/> or <paramref name="errorMessage"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="skip"/> is negative.</exception>
    public LoadPageFailedAction(string viewKey, int skip, string errorMessage) {
        ViewKey = viewKey;
        Skip = skip;
        ErrorMessage = errorMessage;
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

    /// <summary>Gets the non-negative skip offset.</summary>
    public int Skip {
        get => _skip;
        init {
            if (value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Skip must be non-negative.");
            }

            _skip = value;
        }
    }

    /// <summary>Gets the human-readable error message.</summary>
    public string ErrorMessage {
        get => _errorMessage;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("Error message cannot be null, empty, or whitespace.", nameof(value));
            }

            _errorMessage = value;
        }
    }
}

/// <summary>
/// Story 4-4 D3 — dispatched by <c>LoadPageEffects</c> on
/// <c>cancellationToken.Register</c> callback OR by the double-registration idempotency guard.
/// Reducer resolves the matching TCS via <c>TrySetCanceled</c> and removes the entry.
/// </summary>
public sealed record LoadPageCancelledAction {
    private readonly string _viewKey = string.Empty;
    private readonly int _skip;

    /// <summary>Initializes a new instance of the <see cref="LoadPageCancelledAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="skip">Non-negative skip offset.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="skip"/> is negative.</exception>
    public LoadPageCancelledAction(string viewKey, int skip) {
        ViewKey = viewKey;
        Skip = skip;
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

    /// <summary>Gets the non-negative skip offset.</summary>
    public int Skip {
        get => _skip;
        init {
            if (value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Skip must be non-negative.");
            }

            _skip = value;
        }
    }
}

/// <summary>
/// Story 4-4 D3 — dispatched from the generated view's <c>DisposeAsync</c>. Reducer sweeps every
/// <c>PendingCompletionsByKey</c> entry whose view-key component matches, calling
/// <c>TrySetCanceled</c> and removing each — preventing orphan TCS entries across route changes.
/// </summary>
public sealed record ClearPendingPagesAction {
    private readonly string _viewKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ClearPendingPagesAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key whose pending TCS entries should be swept.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public ClearPendingPagesAction(string viewKey) {
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

/// <summary>
/// Story 4-4 D7 / AC5 — dispatched when the user toggles a column's visibility via
/// <c>FcColumnPrioritizer</c>. The PURE reducer reads / writes the CSV at
/// <c>GridViewSnapshot.Filters["__hidden"]</c>; the persistence effect chains
/// <see cref="CaptureGridStateAction"/> separately.
/// </summary>
public sealed record ColumnVisibilityChangedAction {
    private readonly string _viewKey = string.Empty;
    private readonly string _columnKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ColumnVisibilityChangedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="columnKey">Declared property name of the toggled column (must not start with <c>__</c>).</param>
    /// <param name="isVisible"><see langword="true"/> to make visible, <see langword="false"/> to hide.</param>
    /// <exception cref="System.ArgumentException">Thrown when either argument is null/empty/whitespace or <paramref name="columnKey"/> starts with <c>__</c>.</exception>
    public ColumnVisibilityChangedAction(string viewKey, string columnKey, bool isVisible) {
        ViewKey = viewKey;
        ColumnKey = columnKey;
        IsVisible = isVisible;
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

    /// <summary>Gets the declared column key.</summary>
    /// <remarks>
    /// Keys starting with <c>__</c> are reserved (see <see cref="ReservedFilterKeys"/> /
    /// <see cref="VirtualizationReservedKeys"/>); the record rejects them at construction to
    /// prevent reserved-key spoofing via direct action dispatch.
    /// </remarks>
    public string ColumnKey {
        get => _columnKey;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("Column key cannot be null, empty, or whitespace.", nameof(value));
            }

            if (value.StartsWith("__", System.StringComparison.Ordinal)) {
                throw new System.ArgumentException(
                    "Column key must not start with '__' — that prefix is reserved for framework-managed filter keys.",
                    nameof(value));
            }

            _columnKey = value;
        }
    }

    /// <summary>Gets a value indicating whether the column should be visible after this action.</summary>
    public bool IsVisible { get; init; }
}

/// <summary>
/// Story 4-4 D6 / AC5 — dispatched from the "Reset to defaults" anchor inside the prioritizer popover.
/// Reducer removes the <c>"__hidden"</c> entry from <c>GridViewSnapshot.Filters</c>; the persistence
/// effect chains <see cref="CaptureGridStateAction"/>.
/// </summary>
public sealed record ResetColumnVisibilityAction {
    private readonly string _viewKey = string.Empty;

    /// <summary>Initializes a new instance of the <see cref="ResetColumnVisibilityAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    public ResetColumnVisibilityAction(string viewKey) {
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

/// <summary>
/// Story 4-4 D4 / AC7 — dispatched by the generated view's <c>@onscroll</c> handler after the
/// JS-side throttle. The PURE reducer validates the scroll offset and updates
/// <c>GridViewSnapshot.ScrollTop</c>; <c>ScrollPersistenceEffect</c> debounces and chains
/// <see cref="CaptureGridStateAction"/>.
/// </summary>
public sealed record ScrollCapturedAction {
    private readonly string _viewKey = string.Empty;
    private readonly double _scrollTop;

    /// <summary>Initializes a new instance of the <see cref="ScrollCapturedAction"/> record.</summary>
    /// <param name="viewKey">Stable per-view key.</param>
    /// <param name="scrollTop">Non-negative finite scroll offset (defence-in-depth over <c>GridViewSnapshot.ScrollTop</c>'s own validator).</param>
    /// <exception cref="System.ArgumentException">Thrown when <paramref name="viewKey"/> is null, empty, or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <paramref name="scrollTop"/> is NaN, infinity, or negative.</exception>
    public ScrollCapturedAction(string viewKey, double scrollTop) {
        ViewKey = viewKey;
        ScrollTop = scrollTop;
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

    /// <summary>Gets the captured scroll offset.</summary>
    public double ScrollTop {
        get => _scrollTop;
        init {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "ScrollTop must be a non-negative finite value.");
            }

            _scrollTop = value;
        }
    }
}
