namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Provisional query contract for projection access.
/// Story 1.3 may extend query behavior through companion abstractions while
/// keeping this interface stable for existing implementers.
/// </summary>
public interface IQueryService
{
    /// <summary>
    /// Executes a query against a projection type.
    /// </summary>
    /// <typeparam name="T">The projection item type.</typeparam>
    /// <param name="request">The query parameters.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The query result containing items, total count, and ETag.</returns>
    Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default);
}
