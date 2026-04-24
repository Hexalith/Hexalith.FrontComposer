namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Projection query parameters.
/// </summary>
/// <param name="ProjectionType">The projection type name to query.</param>
/// <param name="TenantId">The tenant context for the query.</param>
/// <param name="Filter">Legacy filter expression. Kept for v0.x compatibility.</param>
/// <param name="Skip">Number of items to skip for pagination.</param>
/// <param name="Take">Number of items to take for pagination.</param>
/// <param name="ETag">Optional ETag for cache validation.</param>
/// <param name="ColumnFilters">Story 4-3 D2 — per-column filter values keyed by declared property name.</param>
/// <param name="StatusFilters">Story 4-3 D2 — enum-member names (not slot names) selected via status chips.</param>
/// <param name="SearchQuery">Story 4-3 D2 — global search query routed to <c>IProjectionSearchProvider&lt;T&gt;</c>.</param>
/// <param name="SortColumn">Story 4-3 D2 — declared property name selected as the current sort column.</param>
/// <param name="SortDescending">Story 4-3 D2 — sort direction; <c>false</c> is ascending (the default).</param>
public record QueryRequest(
    string ProjectionType,
    string TenantId,
    [property: System.Obsolete("Use ColumnFilters. Scheduled for removal in v1.0-rc2.", error: false)]
    string? Filter = null,
    int? Skip = null,
    int? Take = null,
    string? ETag = null,
    System.Collections.Generic.IReadOnlyDictionary<string, string>? ColumnFilters = null,
    System.Collections.Generic.IReadOnlyList<string>? StatusFilters = null,
    string? SearchQuery = null,
    string? SortColumn = null,
    bool SortDescending = false);
