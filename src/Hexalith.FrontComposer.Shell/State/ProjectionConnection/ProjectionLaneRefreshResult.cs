namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Records the outcome and admission status of one physical fallback read.</summary>
internal enum ProjectionLaneRefreshResult
{
    Skipped,
    ReadSkipped,
    StaleRead,
    NotModified,
    Changed,
    Failed,
}
