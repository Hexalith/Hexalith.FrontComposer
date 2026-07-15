using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Runs one visible-lane catch-up pass after the projection hub reconnects.</summary>
public interface IReconnectionReconciliationCoordinator {
    Task<ProjectionReconciliationRefreshResult> ReconcileAsync(CancellationToken cancellationToken = default);
}
