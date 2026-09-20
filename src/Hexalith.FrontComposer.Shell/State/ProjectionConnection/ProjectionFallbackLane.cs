using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Visible projection lane metadata used by bounded fallback polling.</summary>
/// <remarks>
/// Story 5-3 review DN4 — `ProjectionType` and `TenantId` are first-class fields so nudge routing
/// no longer relies on parsing `ViewKey`. Adopters MUST supply both.
/// Story 11.29 — `ViewKey` alone is no longer the dedupe identity. A second registration under the
/// same `ViewKey` is refcounted only when every field (<see cref="ProjectionType"/>,
/// <see cref="TenantId"/>, <see cref="Skip"/>, <see cref="Take"/>, <see cref="Filters"/>,
/// <see cref="SortColumn"/>, <see cref="SortDescending"/>, <see cref="SearchQuery"/>, and
/// <see cref="RefreshAsync"/> by reference) matches the incumbent registration; any other
/// difference is rejected as a conflicting registration rather than silently coalesced.
/// </remarks>
public sealed record ProjectionFallbackLane(
    string ViewKey,
    string ProjectionType,
    string? TenantId,
    int Skip,
    int Take,
    IImmutableDictionary<string, string> Filters,
    string? SortColumn,
    bool SortDescending,
    string? SearchQuery,
    Func<CancellationToken, ValueTask<ProjectionFallbackLaneRefreshOutcome>>? RefreshAsync = null);
