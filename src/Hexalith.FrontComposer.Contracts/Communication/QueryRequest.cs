namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Projection query parameters.
/// </summary>
/// <param name="ProjectionType">The projection type name to query.</param>
/// <param name="TenantId">The tenant context for the query.</param>
/// <param name="Filter">Optional filter expression.</param>
/// <param name="Skip">Number of items to skip for pagination.</param>
/// <param name="Take">Number of items to take for pagination.</param>
/// <param name="ETag">Optional ETag for cache validation.</param>
public record QueryRequest(
    string ProjectionType,
    string TenantId,
    string? Filter = null,
    int? Skip = null,
    int? Take = null,
    string? ETag = null);
