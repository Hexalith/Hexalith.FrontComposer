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
}
