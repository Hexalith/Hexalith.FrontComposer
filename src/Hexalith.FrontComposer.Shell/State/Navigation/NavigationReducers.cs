using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Pure reducers for <see cref="FrontComposerNavigationState"/> (Story 3-2 D3, D11, D13, D14, D15).
/// </summary>
public static class NavigationReducers
{
    /// <summary>
    /// Flips <see cref="FrontComposerNavigationState.SidebarCollapsed"/> (Story 3-2 D9).
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The sidebar toggled action.</param>
    /// <returns>A new state with <c>SidebarCollapsed</c> flipped.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceSidebarToggled(
        FrontComposerNavigationState state,
        SidebarToggledAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { SidebarCollapsed = !state.SidebarCollapsed };
    }

    /// <summary>
    /// Collapses or expands a single nav group (Story 3-2 D11).
    /// Sparse-by-default: expansion REMOVES the key so the persisted blob stays minimal.
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The nav-group toggled action.</param>
    /// <returns>A new state with <c>CollapsedGroups</c> updated.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceNavGroupToggled(
        FrontComposerNavigationState state,
        NavGroupToggledAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            CollapsedGroups = action.Collapsed
                ? state.CollapsedGroups.SetItem(action.BoundedContext, true)
                : state.CollapsedGroups.Remove(action.BoundedContext),
        };
    }

    /// <summary>
    /// Updates <see cref="FrontComposerNavigationState.CurrentViewport"/>.
    /// Does NOT mutate <c>SidebarCollapsed</c> or <c>CollapsedGroups</c> (Story 3-2 D14 / ADR-037).
    /// When the new tier matches the current tier, returns the same state reference so Fluxor's
    /// <c>IState.StateChanged</c> does not re-notify subscribers (belt-and-suspenders dedup to
    /// complement the JS-side composed-tier dedup per D6).
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The viewport-tier changed action.</param>
    /// <returns>A new state with the updated viewport tier, or the same state if unchanged.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceViewportTierChanged(
        FrontComposerNavigationState state,
        ViewportTierChangedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.CurrentViewport == action.NewTier
            ? state
            : state with { CurrentViewport = action.NewTier };
    }

    /// <summary>
    /// Forces the sidebar expanded (Story 3-2 D13). Idempotent.
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The sidebar-expanded action.</param>
    /// <returns>A new state with <c>SidebarCollapsed = false</c>.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceSidebarExpanded(
        FrontComposerNavigationState state,
        SidebarExpandedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { SidebarCollapsed = false };
    }

    /// <summary>
    /// Replaces <see cref="FrontComposerNavigationState.SidebarCollapsed"/> and
    /// <see cref="FrontComposerNavigationState.CollapsedGroups"/> wholesale from the hydrated blob
    /// (Story 3-2 D15). Does NOT touch <c>CurrentViewport</c> (ADR-037).
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The hydrated action carrying the persisted blob contents.</param>
    /// <returns>A new state with persisted fields replaced.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceNavigationHydrated(
        FrontComposerNavigationState state,
        NavigationHydratedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with
        {
            SidebarCollapsed = action.SidebarCollapsed,
            CollapsedGroups = action.CollapsedGroups,
        };
    }

    /// <summary>
    /// Updates <see cref="FrontComposerNavigationState.CurrentBoundedContext"/> when the route
    /// changes (Story 3-4 D7). NEVER persisted — derived from <c>NavigationManager.Uri</c>.
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The bounded-context-changed action.</param>
    /// <returns>A new state when the value changed; the same instance otherwise.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceBoundedContextChanged(
        FrontComposerNavigationState state,
        BoundedContextChangedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return string.Equals(state.CurrentBoundedContext, action.NewBoundedContext, StringComparison.Ordinal)
            ? state
            : state with { CurrentBoundedContext = action.NewBoundedContext };
    }

    /// <summary>
    /// Updates <see cref="FrontComposerNavigationState.LastActiveRoute"/> to the captured route
    /// (Story 3-6 D1 / D2 / ADR-048). Empty / whitespace routes are coerced to <see langword="null"/>
    /// so the blob's null-convention stays intact (<c>null</c> = "no prior route"; empty string
    /// is INVALID per the cross-story contract table).
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The last-active-route-changed action.</param>
    /// <returns>A new state with the normalised route assigned.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceLastActiveRouteChanged(
        FrontComposerNavigationState state,
        LastActiveRouteChangedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        string? normalised = string.IsNullOrWhiteSpace(action.Route) ? null : action.Route;
        return string.Equals(state.LastActiveRoute, normalised, StringComparison.Ordinal)
            ? state
            : state with { LastActiveRoute = normalised };
    }

    /// <summary>
    /// Replaces <see cref="FrontComposerNavigationState.LastActiveRoute"/> from the hydrated blob
    /// (Story 3-6 D1 / ADR-048). Does NOT trigger re-persistence (ADR-038 mirror).
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The hydrated action carrying the persisted route (nullable).</param>
    /// <returns>A new state with the route field replaced.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceLastActiveRouteHydrated(
        FrontComposerNavigationState state,
        LastActiveRouteHydratedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        string? normalised = string.IsNullOrWhiteSpace(action.Route) ? null : action.Route;
        return state with { LastActiveRoute = normalised };
    }

    /// <summary>
    /// Flips <see cref="FrontComposerNavigationState.StorageReady"/> to <see langword="true"/>
    /// (Story 3-6 D13 / ADR-049). Idempotent — always safe to set true again. NEVER reset within
    /// a circuit (ADR-049 load-bearing invariant).
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The storage-ready action.</param>
    /// <returns>A new state with <c>StorageReady = true</c>.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceStorageReady(
        FrontComposerNavigationState state,
        StorageReadyAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.StorageReady
            ? state
            : state with { StorageReady = true };
    }

    /// <summary>
    /// Flips <see cref="FrontComposerNavigationState.HydrationState"/> from <see cref="NavigationHydrationState.Idle"/>
    /// to <see cref="NavigationHydrationState.Hydrating"/> at the start of the hydrate path
    /// (Story 3-6 D19). Idempotent when already <c>Hydrating</c>; no-op when already <c>Hydrated</c>
    /// (re-hydrate gate upstream already guards this path).
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The navigation-hydrating action.</param>
    /// <returns>A new state with <c>HydrationState = Hydrating</c> when transitioning from <c>Idle</c>.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceNavigationHydrating(
        FrontComposerNavigationState state,
        NavigationHydratingAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.HydrationState == NavigationHydrationState.Hydrated
            ? state
            : state with { HydrationState = NavigationHydrationState.Hydrating };
    }

    /// <summary>
    /// Flips <see cref="FrontComposerNavigationState.HydrationState"/> to <see cref="NavigationHydrationState.Hydrated"/>
    /// at the end of the hydrate path (Story 3-6 D19). Called on BOTH happy path AND fail-closed
    /// path so subsequent <see cref="StorageReadyAction"/> re-triggers hydrate only when the state
    /// is still <c>Idle</c>.
    /// </summary>
    /// <param name="state">The current navigation state.</param>
    /// <param name="action">The navigation-hydrated-completed action.</param>
    /// <returns>A new state with <c>HydrationState = Hydrated</c>.</returns>
    [ReducerMethod]
    public static FrontComposerNavigationState ReduceNavigationHydratedCompleted(
        FrontComposerNavigationState state,
        NavigationHydratedCompletedAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.HydrationState == NavigationHydrationState.Hydrated
            ? state
            : state with { HydrationState = NavigationHydrationState.Hydrated };
    }
}
