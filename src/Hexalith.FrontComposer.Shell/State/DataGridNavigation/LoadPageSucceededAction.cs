namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D3 — dispatched by <c>LoadPageEffects.HandleLoadPageAsync</c> on
/// <c>IQueryService.QueryAsync</c> success. The reducer writes the page, updates
/// <c>TotalCountByKey</c> / <c>LastElapsedMsByKey</c>, enqueues the insertion-order token,
/// and resolves the matching TCS via <c>TrySetResult</c>.
/// </summary>
public sealed record LoadPageSucceededAction {

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
        long elapsedMs,
        TaskCompletionSource<object>? completion = null) {
        ViewKey = viewKey;
        Skip = skip;
        Items = items;
        Completion = completion;
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
        get;
        init {
            if (string.IsNullOrWhiteSpace(value)) {
                throw new System.ArgumentException("View key cannot be null, empty, or whitespace.", nameof(value));
            }

            field = value;
        }
    } = string.Empty;

    /// <summary>Gets the skip offset matching the triggering <see cref="LoadPageAction"/>.</summary>
    public int Skip {
        get;
        init {
            if (value < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(value), value, "Skip must be non-negative.");
            }

            field = value;
        }
    }

    /// <summary>Gets the row payload, or <see langword="null"/> (treated as failure by the reducer's D3 guard).</summary>
    public IReadOnlyList<object>? Items { get; init; }

    /// <summary>Gets the originating TCS, used to reject stale terminal actions for superseded same-page requests.</summary>
    public TaskCompletionSource<object>? Completion { get; init; }

    /// <summary>Gets the server-reported total row count across pages.</summary>
    public int TotalCount { get; init; }

    /// <summary>Gets the measured elapsed milliseconds.</summary>
    public long ElapsedMs { get; init; }
}
