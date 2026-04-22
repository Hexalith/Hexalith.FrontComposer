using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Fluxor state record for the framework-owned sidebar navigation (Story 3-2 D3 / ADR-037 and
/// Story 3-6 ADR-048 / D19).
/// </summary>
/// <param name="SidebarCollapsed">Whether the sidebar is collapsed (user preference; persisted).</param>
/// <param name="CollapsedGroups">Per-bounded-context collapsed flag — sparse map (D11); persisted.</param>
/// <param name="CurrentViewport">Observed viewport tier — derived at runtime, NEVER persisted (ADR-037).</param>
/// <param name="CurrentBoundedContext">
/// The bounded-context segment of the current route — derived from <c>NavigationManager.Uri</c>
/// at route-change time, NEVER persisted (Story 3-4 D7 / D8 — drives the palette's contextual
/// scoring bonus). <see langword="null"/> when on the home route or a non-domain route.
/// </param>
/// <param name="LastActiveRoute">
/// The last visited domain route captured on <c>BoundedContextChangedAction</c> with a non-null
/// bounded-context segment (Story 3-6 D1 / D2 / ADR-048). Persisted via <c>NavigationPersistenceBlob</c>.
/// <see langword="null"/> = "no prior route captured"; empty string is invalid and never persisted
/// (guarded by <c>ReduceLastActiveRouteChanged</c>).
/// </param>
/// <param name="StorageReady">
/// Transient flag flipped once per circuit by <see cref="StorageReadyAction"/> when the scope
/// becomes available post-prerender (Story 3-6 D13 / ADR-049). NEVER persisted — explicitly
/// excluded from <c>NavigationPersistenceBlob</c> serialisation. Sign-out mid-circuit does NOT
/// reset the flag; future writes fail-closed on the L03 scope guard (ADR-049 load-bearing invariant).
/// </param>
/// <param name="HydrationState">
/// Transient three-state hydration marker (Story 3-6 D19). Initial value <see cref="NavigationHydrationState.Idle"/>;
/// flips <c>Idle → Hydrating → Hydrated</c> via dedicated reducers. NEVER persisted. Re-hydrate
/// via <see cref="StorageReadyAction"/> only runs when this is <see cref="NavigationHydrationState.Idle"/>.
/// </param>
public record FrontComposerNavigationState(
    bool SidebarCollapsed,
    ImmutableDictionary<string, bool> CollapsedGroups,
    ViewportTier CurrentViewport,
    string? CurrentBoundedContext = null,
    string? LastActiveRoute = null,
    bool StorageReady = false,
    NavigationHydrationState HydrationState = NavigationHydrationState.Idle);
