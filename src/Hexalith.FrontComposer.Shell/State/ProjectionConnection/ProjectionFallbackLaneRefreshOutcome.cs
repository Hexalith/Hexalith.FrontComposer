namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Result produced by custom visible-lane refresh callbacks.</summary>
public enum ProjectionFallbackLaneRefreshOutcome {
    Skipped,
    NotModified,
    Changed,
}
