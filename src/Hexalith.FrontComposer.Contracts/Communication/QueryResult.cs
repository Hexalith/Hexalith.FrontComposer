namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Query response with ETag for cache invalidation.
/// </summary>
/// <typeparam name="T">The projection item type.</typeparam>
/// <param name="Items">The query result items.</param>
/// <param name="TotalCount">Total number of matching items (for pagination).</param>
/// <param name="ETag">ETag for cache validation on subsequent requests.</param>
public record QueryResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    string? ETag);
