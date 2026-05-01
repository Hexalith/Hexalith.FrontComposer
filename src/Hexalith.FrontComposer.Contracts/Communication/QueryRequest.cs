namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Projection query parameters.
/// </summary>
/// <param name="ProjectionType">The projection type name to query.</param>
/// <param name="TenantId">Optional requested tenant context for the query. Blank/null values are filled from the authenticated tenant by Shell adapters.</param>
/// <param name="Filter">Legacy filter expression. Kept for v0.x compatibility.</param>
/// <param name="Skip">Number of items to skip for pagination.</param>
/// <param name="Take">Number of items to take for pagination.</param>
/// <param name="ETag">Optional ETag for cache validation.</param>
/// <param name="ColumnFilters">Story 4-3 D2 — per-column filter values keyed by declared property name.</param>
/// <param name="StatusFilters">Story 4-3 D2 — enum-member names (not slot names) selected via status chips.</param>
/// <param name="SearchQuery">Story 4-3 D2 — global search query routed to <c>IProjectionSearchProvider&lt;T&gt;</c>.</param>
/// <param name="SortColumn">Story 4-3 D2 — declared property name selected as the current sort column.</param>
/// <param name="SortDescending">Story 4-3 D2 — sort direction; <c>false</c> is ascending (the default).</param>
/// <param name="Domain">Optional EventStore domain name for REST-backed query execution.</param>
/// <param name="AggregateId">Optional EventStore aggregate identifier for REST-backed query execution.</param>
/// <param name="QueryType">Optional EventStore query type discriminator. Defaults to <paramref name="ProjectionType"/> when omitted by an adapter.</param>
/// <param name="EntityId">Optional EventStore entity identifier for nested projection queries.</param>
/// <param name="ProjectionActorType">Optional EventStore projection actor type.</param>
/// <param name="ETags">Optional explicit ETag validator set. When provided, adapters ignore <paramref name="ETag"/>.</param>
/// <param name="CacheDiscriminator">Story 5-2 — framework-allowlisted ETag cache discriminator. When non-null and accepted by the framework allowlist, the EventStore query client sends <c>If-None-Match</c> from cache and writes 200 OK responses through the cache. Adopter-supplied raw / hashed user input MUST NOT be passed here (Story 5-2 D3 / AC6).</param>
/// <param name="CachePayloadVersion">Story 5-2 — projection payload contract version used when reading / writing the cache. Cached entries with a lower version are treated as diagnostic misses (Story 5-2 D13). Defaults to <c>1</c>.</param>
public record QueryRequest(
    string ProjectionType,
    string? TenantId,
    [property: System.Obsolete("Use ColumnFilters. Scheduled for removal in v1.0-rc2.", error: false)]
    string? Filter = null,
    int? Skip = null,
    int? Take = null,
    string? ETag = null,
    System.Collections.Generic.IReadOnlyDictionary<string, string>? ColumnFilters = null,
    System.Collections.Generic.IReadOnlyList<string>? StatusFilters = null,
    string? SearchQuery = null,
    string? SortColumn = null,
    bool SortDescending = false,
    string? Domain = null,
    string? AggregateId = null,
    string? QueryType = null,
    string? EntityId = null,
    string? ProjectionActorType = null,
    System.Collections.Generic.IReadOnlyList<string>? ETags = null,
    string? CacheDiscriminator = null,
    int CachePayloadVersion = 1);
