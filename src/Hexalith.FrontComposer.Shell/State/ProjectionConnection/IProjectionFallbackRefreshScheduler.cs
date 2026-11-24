namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Registers visible projection lanes and refreshes them while realtime is unavailable.</summary>
public interface IProjectionFallbackRefreshScheduler {
    /// <summary>Registers a visible view and shares an existing registration with an equivalent contract.</summary>
    /// <param name="lane">The view and scoped projection query to refresh.</param>
    /// <returns>A lease that releases this owner when disposed.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="lane"/> or its filter map is null.</exception>
    /// <exception cref="ArgumentException">The view key or projection scope is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The pagination bounds are invalid.</exception>
    /// <exception cref="InvalidOperationException">A conflicting contract is already registered for the view key.</exception>
    IDisposable RegisterLane(ProjectionFallbackLane lane);

    /// <summary>Moves a generated shared view to a new current contract while older owners release their leases.</summary>
    /// <param name="lane">The current view and scoped projection query to refresh.</param>
    /// <returns>A lease that releases this owner when disposed.</returns>
    /// <remarks>Implementations without replacement support use <see cref="RegisterLane"/>.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="lane"/> or its filter map is null.</exception>
    /// <exception cref="ArgumentException">The view key or projection scope is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The pagination bounds are invalid.</exception>
    /// <exception cref="InvalidOperationException">The implementation uses <see cref="RegisterLane"/> and a conflicting contract exists.</exception>
    IDisposable RegisterReplacingLane(ProjectionFallbackLane lane) => RegisterLane(lane);

    /// <summary>Polls registered lanes once while fallback polling is enabled and realtime is disconnected.</summary>
    /// <param name="cancellationToken">A token that cancels the sweep.</param>
    /// <returns>The number of lanes that returned changed or unchanged data.</returns>
    Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default);

    /// <summary>Refreshes registered lanes for a specific projection and tenant after a realtime nudge.</summary>
    /// <param name="projectionType">The projection type to match.</param>
    /// <param name="tenantId">The tenant to match.</param>
    /// <param name="cancellationToken">A token that cancels the sweep.</param>
    /// <returns>The number of matching lanes that returned changed or unchanged data.</returns>
    /// <exception cref="ArgumentException">The projection type or tenant is blank or contains a colon or control character.</exception>
    Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default);

    /// <summary>Reconciles registered projections for the current reconnection epoch.</summary>
    /// <param name="epoch">The reconnection generation that owns the sweep.</param>
    /// <param name="cancellationToken">A token that cancels reconciliation.</param>
    /// <returns>The sweep results, or an empty result for a stale epoch or an implementation without reconciliation support.</returns>
    Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default)
        => Task.FromResult(ProjectionReconciliationRefreshResult.Empty);

    /// <summary>Replaces the group health snapshot used to select lanes for reconciliation.</summary>
    /// <param name="activeGroups">Group status by projection and tenant. An explicit false excludes a group.</param>
    /// <remarks>The default implementation ignores group updates.</remarks>
    /// <exception cref="ArgumentNullException">An implementation that stores group health receives a null snapshot.</exception>
    void SetReconciliationGroupHealth(IReadOnlyDictionary<ProjectionFallbackGroupKey, bool> activeGroups) {
    }
}
