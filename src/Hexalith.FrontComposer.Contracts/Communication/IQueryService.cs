namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Stable query contract for projection access. <see cref="QueryRequest"/> composes
/// canonical <see cref="ProjectionQuery"/> criteria with transport and cache metadata.
/// </summary>
public interface IQueryService {
    /// <summary>
    /// Executes a query against a projection type.
    /// </summary>
    /// <typeparam name="T">The projection item type.</typeparam>
    /// <param name="request">The composed projection query request.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The query result containing items, total count, and ETag.</returns>
    Task<QueryResult<T>> QueryAsync<T>(QueryRequest request, CancellationToken cancellationToken = default);
}
