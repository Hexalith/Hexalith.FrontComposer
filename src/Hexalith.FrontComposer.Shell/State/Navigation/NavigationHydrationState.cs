namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Explicit three-state hydration lifecycle for <see cref="FrontComposerNavigationState"/>
/// (Story 3-6 D19). Eliminates the silent false-negative of default-value proxy gates —
/// a user whose real preference happens to match a default is indistinguishable from an
/// un-hydrated state without an explicit marker.
/// </summary>
/// <remarks>
/// Transitions are driven by effect dispatches:
/// <list type="bullet">
///   <item><see cref="Idle"/> → <see cref="Hydrating"/> via <c>NavigationHydratingAction</c>
///     at the start of <c>NavigationEffects.HandleAppInitialized</c>.</item>
///   <item><see cref="Hydrating"/> → <see cref="Hydrated"/> via <c>NavigationHydratedCompletedAction</c>
///     once the hydrate path finishes (happy path OR fail-closed path).</item>
/// </list>
/// <see cref="Hydrated"/> is terminal within a circuit — re-hydrate via <c>StorageReadyAction</c>
/// only re-runs when the state is still <see cref="Idle"/>.
/// </remarks>
public enum NavigationHydrationState {
    /// <summary>Hydration has not started — re-hydrate on <c>StorageReadyAction</c> is permitted.</summary>
    Idle,

    /// <summary>Hydration is in flight — re-hydrate is suppressed to avoid double-apply.</summary>
    Hydrating,

    /// <summary>Hydration has completed (success or fail-closed) — re-hydrate is suppressed.</summary>
    Hydrated,
}
