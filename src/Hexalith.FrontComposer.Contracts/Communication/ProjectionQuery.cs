namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Defines the canonical criteria used to query a projection.
/// </summary>
/// <param name="ProjectionType">The projection type name to query.</param>
/// <param name="Skip">The number of items to skip for pagination.</param>
/// <param name="Take">The number of items to take for pagination.</param>
/// <param name="ColumnFilters">Per-column filter values keyed by declared property name.</param>
/// <param name="StatusFilters">Enum-member names selected through status filters.</param>
/// <param name="SearchQuery">The global search query.</param>
/// <param name="SortColumn">The declared property name used for ordering.</param>
/// <param name="SortDescending">Whether the ordering is descending.</param>
public sealed record ProjectionQuery(
    string ProjectionType,
    int? Skip = null,
    int? Take = null,
    System.Collections.Generic.IReadOnlyDictionary<string, string>? ColumnFilters = null,
    System.Collections.Generic.IReadOnlyList<string>? StatusFilters = null,
    string? SearchQuery = null,
    string? SortColumn = null,
    bool SortDescending = false);
