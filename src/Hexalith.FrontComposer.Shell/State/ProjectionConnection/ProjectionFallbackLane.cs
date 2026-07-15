using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Visible projection lane metadata used by bounded fallback polling.</summary>
/// <remarks>
/// Story 5-3 review DN4 — `ProjectionType` and `TenantId` are first-class fields so nudge routing
/// no longer relies on parsing `ViewKey`. Adopters MUST supply both; `ViewKey` remains the
/// dedupe identity for refcounted registrations.
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
