namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Query response with ETag for cache invalidation.
/// </summary>
/// <typeparam name="T">The projection item type.</typeparam>
/// <param name="Items">The query result items.</param>
/// <param name="TotalCount">Total number of matching items (for pagination).</param>
/// <param name="ETag">ETag for cache validation on subsequent requests.</param>
/// <param name="IsNotModified">True when EventStore returned 304 Not Modified for cache validation.</param>
public record QueryResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    string? ETag,
    bool IsNotModified = false) {
    /// <summary>
    /// Creates an explicit no-change query result for 304 Not Modified responses.
    /// </summary>
    /// <param name="etag">The validator returned by the server, when present.</param>
    /// <returns>A not-modified result with no payload items.</returns>
    public static QueryResult<T> NotModified(string? etag = null) => new([], 0, etag, true);

    /// <summary>
    /// Story 5-2 AC4 — creates a no-change result that reuses cached items so consumers do
    /// not have to fall back to a fresh fetch. <see cref="IsNotModified"/> stays
    /// <see langword="true"/> so reducers can take the explicit no-change path (no fake
    /// success transition / no badge animation).
    /// </summary>
    /// <param name="items">The cached payload items.</param>
    /// <param name="totalCount">The cached total count.</param>
    /// <param name="etag">The cached validator.</param>
    public static QueryResult<T> NotModifiedFromCache(IReadOnlyList<T> items, int totalCount, string? etag)
        => new(items, totalCount, etag, true);
}
