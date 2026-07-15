namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Registers visible projection lanes and refreshes them while realtime is unavailable.</summary>
public interface IProjectionFallbackRefreshScheduler {
    IDisposable RegisterLane(ProjectionFallbackLane lane);

    Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default);

    Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default);

    Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default)
        => Task.FromResult(ProjectionReconciliationRefreshResult.Empty);

    void SetReconciliationGroupHealth(IReadOnlyDictionary<ProjectionFallbackGroupKey, bool> activeGroups) {
    }
}
