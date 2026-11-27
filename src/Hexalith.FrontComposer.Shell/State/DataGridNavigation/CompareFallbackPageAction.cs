using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>Compares and applies a fallback read against the current page in one reducer turn.</summary>
internal sealed record CompareFallbackPageAction(
    LoadPageNotModifiedAction Page,
    bool IsNotModified,
    bool ValidatorReliable,
    string? PriorValidator,
    string? CurrentValidator,
    long OriginReadSequence,
    long? PriorReadSequence,
    object AdmissionGate,
    Task AdmissionRetired,
    TaskCompletionSource<ProjectionLaneRefreshResult> Result,
    CancellationToken CancellationToken)
{
    /// <summary>Gets or sets the comparison result selected by the reducer before effects run.</summary>
    public ProjectionLaneRefreshResult Outcome { get; set; }

    /// <summary>Gets or sets the exact successful-read sequence updated by the reducer.</summary>
    public long? AppliedReadSequence { get; set; }

    /// <summary>Gets or sets the public terminal action published after the comparison commits.</summary>
    public object? Notification { get; set; }
}
