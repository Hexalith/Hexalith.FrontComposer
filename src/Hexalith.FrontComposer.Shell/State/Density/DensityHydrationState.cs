namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Explicit three-state hydration lifecycle for <see cref="FrontComposerDensityState"/>
/// (Story 3-6 D19). Replaces the default-value proxy gate that would silently false-negative
/// when the user's actual preference equals the default deployment density.
/// </summary>
public enum DensityHydrationState {
    /// <summary>Hydration has not started — re-hydrate on <c>StorageReadyAction</c> is permitted.</summary>
    Idle,

    /// <summary>Hydration is in flight — re-hydrate is suppressed to avoid double-apply.</summary>
    Hydrating,

    /// <summary>Hydration has completed (success or fail-closed) — re-hydrate is suppressed.</summary>
    Hydrated,
}
