namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Explicit three-state hydration lifecycle for <see cref="DataGridNavigationState"/>
/// (Story 3-6 D19 / A7). Replaces the default-value proxy gate (<c>ViewStates.IsEmpty</c>)
/// that would silently false-negative when a user has no captured snapshots yet.
/// </summary>
public enum DataGridNavigationHydrationState {
    /// <summary>Hydration has not started — re-hydrate on <c>StorageReadyAction</c> is permitted.</summary>
    Idle,

    /// <summary>Hydration is in flight — re-hydrate is suppressed to avoid double-apply.</summary>
    Hydrating,

    /// <summary>Hydration has completed (success or fail-closed) — re-hydrate is suppressed.</summary>
    Hydrated,
}
