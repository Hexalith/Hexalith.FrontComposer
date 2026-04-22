namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Explicit three-state hydration lifecycle for <see cref="FrontComposerCommandPaletteState"/>
/// (Story 3-6 D19 / deviation from Story 3-4 Task 0.10 prereq).
/// </summary>
/// <remarks>
/// Story 3-6's Task 0.10 prereq claimed that Story 3-4 shipped a <c>CommandPaletteHydrationState</c>
/// enum; it did not. Added here to support <see cref="StorageReadyAction"/> re-hydrate gating in
/// <c>CommandPaletteEffects.HandleStorageReady</c> without regressing to the <c>RecentRouteUrls.IsEmpty</c>
/// proxy-gate that D19 explicitly rejects.
/// </remarks>
public enum CommandPaletteHydrationState {
    /// <summary>Hydration has not started — re-hydrate on <c>StorageReadyAction</c> is permitted.</summary>
    Idle,

    /// <summary>Hydration is in flight — re-hydrate is suppressed to avoid double-apply.</summary>
    Hydrating,

    /// <summary>Hydration has completed (success or fail-closed) — re-hydrate is suppressed.</summary>
    Hydrated,
}
