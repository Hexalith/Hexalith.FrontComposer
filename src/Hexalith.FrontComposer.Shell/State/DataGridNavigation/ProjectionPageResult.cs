namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D3 / Story 5-2 AC4 — non-generic page result returned by
/// <see cref="IProjectionPageLoader"/>. Story 5-2 adds <see cref="IsNotModified"/> so the
/// effect can take the explicit no-change path (no loading flash, no synthetic success
/// transition) when EventStore returns 304 Not Modified for a cached page.
/// </summary>
/// <param name="Items">Loaded rows (never null; may be empty). When <see cref="IsNotModified"/> is true these are the cached rows reused from the local ETag cache.</param>
/// <param name="TotalCount">Server-reported (or cached) total count across all pages.</param>
/// <param name="ETag">Optional ETag for cache validation on subsequent requests.</param>
/// <param name="IsNotModified">True when EventStore returned 304 Not Modified and the cached payload was reused.</param>
public sealed record ProjectionPageResult(
    IReadOnlyList<object> Items,
    int TotalCount,
    string? ETag,
    bool IsNotModified = false);
