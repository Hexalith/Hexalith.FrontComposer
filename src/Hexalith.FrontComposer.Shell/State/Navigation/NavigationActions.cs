using System.Collections.Immutable;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Dispatched when the user manually toggles the sidebar collapsed state (Story 3-2 D9).
/// Flips <see cref="FrontComposerNavigationState.SidebarCollapsed"/>.
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
public sealed record SidebarToggledAction(string CorrelationId);

/// <summary>
/// Dispatched when the user expands or collapses a single nav category (Story 3-2 D11).
/// Sparse-by-default: collapsed entries are set, expanded entries are removed from the map.
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
/// <param name="BoundedContext">The bounded context key whose category was toggled.</param>
/// <param name="Collapsed"><see langword="true"/> to collapse; <see langword="false"/> to expand.</param>
public sealed record NavGroupToggledAction(string CorrelationId, string BoundedContext, bool Collapsed);

/// <summary>
/// Dispatched when <c>FcLayoutBreakpointWatcher</c> reports a new viewport tier (Story 3-2 D4 / ADR-036).
/// NEVER triggers persistence (ADR-037).
/// </summary>
/// <param name="NewTier">The new observed viewport tier.</param>
public sealed record ViewportTierChangedAction(ViewportTier NewTier);

/// <summary>
/// Dispatched when the user clicks a <c>FcCollapsedNavRail</c> button to force the sidebar open
/// (Story 3-2 D13). Idempotent — always sets <see cref="FrontComposerNavigationState.SidebarCollapsed"/>
/// to <see langword="false"/>.
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
public sealed record SidebarExpandedAction(string CorrelationId);

/// <summary>
/// Dispatched by <c>NavigationEffects.HandleAppInitialized</c> after loading the persisted blob
/// (Story 3-2 D15 / ADR-038). Reducer REPLACES <see cref="FrontComposerNavigationState.SidebarCollapsed"/>
/// and <see cref="FrontComposerNavigationState.CollapsedGroups"/> wholesale.
/// Does NOT trigger re-persistence (ADR-038).
/// </summary>
/// <param name="SidebarCollapsed">The hydrated sidebar collapsed flag.</param>
/// <param name="CollapsedGroups">The hydrated per-bounded-context collapsed flags.</param>
public sealed record NavigationHydratedAction(
    bool SidebarCollapsed,
    ImmutableDictionary<string, bool> CollapsedGroups);

/// <summary>
/// Dispatched by <c>NavigationEffects.HandleBoundedContextChanged</c> whenever a non-null bounded
/// context is entered (Story 3-6 D2 / ADR-048). Captures the full route from
/// <c>NavigationManager.Uri</c> for deep-link fidelity on next boot. Reducer replaces
/// <see cref="FrontComposerNavigationState.LastActiveRoute"/>; persistence effect writes the
/// updated blob. Empty / whitespace routes are rejected by the reducer (coerced to null per D1
/// null-convention).
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
/// <param name="Route">The captured route (full URI path + query + fragment), or <see langword="null"/>.</param>
public sealed record LastActiveRouteChangedAction(string CorrelationId, string? Route);

/// <summary>
/// Dispatched by <c>NavigationEffects.HandleAppInitialized</c> after reading the persisted blob
/// (Story 3-6 D1 / ADR-048). Reducer replaces <see cref="FrontComposerNavigationState.LastActiveRoute"/>;
/// does NOT trigger re-persistence (ADR-038 mirror).
/// </summary>
/// <param name="Route">The hydrated last-active route, or <see langword="null"/> when absent.</param>
public sealed record LastActiveRouteHydratedAction(string? Route);

/// <summary>
/// Dispatched exactly once per circuit by <c>ScopeFlipObserverEffect</c> via <c>IScopeReadinessGate</c>
/// when <c>IUserContextAccessor</c> transitions empty → authenticated (Story 3-6 D13 / ADR-049).
/// Reducer flips <see cref="FrontComposerNavigationState.StorageReady"/> to <see langword="true"/>;
/// subscriber hydrate effects (Theme, Density, Navigation, CommandPalette, CapabilityDiscovery,
/// DataGridNavigation) re-run their hydrate path iff their feature's <c>HydrationState</c> is
/// <c>Idle</c>. Sign-out mid-circuit does NOT reset the flag (ADR-049 load-bearing invariant).
/// </summary>
/// <param name="CorrelationId">ULID correlation identifier for tracing.</param>
public sealed record StorageReadyAction(string CorrelationId);

/// <summary>
/// Dispatched by <c>NavigationEffects.HandleAppInitialized</c> / <c>HandleStorageReady</c> at the
/// start of the hydrate path (Story 3-6 D19). Reducer flips
/// <see cref="FrontComposerNavigationState.HydrationState"/> from <see cref="NavigationHydrationState.Idle"/>
/// to <see cref="NavigationHydrationState.Hydrating"/>. NEVER persisted.
/// </summary>
public sealed record NavigationHydratingAction;

/// <summary>
/// Dispatched by <c>NavigationEffects.HandleAppInitialized</c> / <c>HandleStorageReady</c> as the
/// final step of the hydrate path (Story 3-6 D19). Reducer flips
/// <see cref="FrontComposerNavigationState.HydrationState"/> to <see cref="NavigationHydrationState.Hydrated"/>.
/// Called on BOTH happy path AND fail-closed path so subsequent <c>StorageReadyAction</c>
/// re-triggers hydrate only when the state is still <c>Idle</c>. NEVER persisted.
/// </summary>
public sealed record NavigationHydratedCompletedAction;
