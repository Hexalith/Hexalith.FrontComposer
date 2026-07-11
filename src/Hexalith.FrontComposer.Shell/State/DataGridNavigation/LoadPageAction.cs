using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D3 / AC2 — dispatched by the generated view's <c>LoadPageAsync</c> provider
/// callback when Fluent requests a page range. <c>LoadPageEffects</c> registers a
/// <c>TaskCompletionSource</c> keyed by <c>(ViewKey, Skip)</c> in
/// <c>LoadedPageState.PendingCompletionsByKey</c> before awaiting
/// <c>IQueryService.QueryAsync</c>; the provider callback awaits that TCS.
/// </summary>
public sealed record LoadPageAction {

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
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the non-negative skip offset.</summary>
    public int Skip {
        get;
        init {
            if (value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Skip must be non-negative.");
            }

            field = value;
        }
    }

    /// <summary>Gets the positive page size.</summary>
    public int Take {
        get;
        init {
            if (value <= 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Take must be strictly positive.");
            }

            field = value;
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
