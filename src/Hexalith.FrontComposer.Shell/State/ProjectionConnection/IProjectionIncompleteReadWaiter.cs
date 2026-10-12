namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Waits for an eligible read skipped by a reconciliation pass to release its lane.</summary>
internal interface IProjectionIncompleteReadWaiter
{
    /// <summary>Returns false when the epoch is obsolete; cancellation ends the wait.</summary>
    Task<bool> WaitForIncompleteReadAsync(long epoch, ProjectionReconciliationRefreshResult pass, CancellationToken cancellationToken);

    /// <summary>Runs a new bounded pass with unresolved lanes ahead of other eligible lanes.</summary>
    Task<ProjectionReconciliationRefreshResult> RecheckIncompleteReadsAsync(
        long epoch, IReadOnlyCollection<string> viewKeys, CancellationToken cancellationToken);
}
