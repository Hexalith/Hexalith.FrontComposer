namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>Safe provenance for the last completed page request on a view.</summary>
/// <param name="Identity">Monotonic completed result identity within the view.</param>
/// <param name="RequestIdentity">Normalized request identity supplied by the view.</param>
/// <param name="TotalCount">The completed result count.</param>
/// <param name="Failed">Whether this request failed.</param>
/// <param name="OperatorInitiated">Whether this request follows an operator change.</param>
public sealed record LoadedPageResult(
    long Identity,
    string RequestIdentity,
    int TotalCount,
    bool Failed,
    bool OperatorInitiated,
    int Skip = 0,
    int Take = 0) {
    /// <summary>Safe, classified operator copy for a failed result.</summary>
    public string? ErrorMessage { get; init; }
}
