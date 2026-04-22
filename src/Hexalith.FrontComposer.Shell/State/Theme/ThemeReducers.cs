
using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.Theme;
/// <summary>
/// Pure reducers for <see cref="FrontComposerThemeState"/>.
/// </summary>
public static class ThemeReducers {
    /// <summary>
    /// Applies a theme change to the state.
    /// </summary>
    /// <param name="state">The current theme state.</param>
    /// <param name="action">The theme changed action.</param>
    /// <returns>A new state with the updated theme.</returns>
    [ReducerMethod]
    public static FrontComposerThemeState ReduceThemeChanged(FrontComposerThemeState state, ThemeChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state with { CurrentTheme = action.NewTheme };
    }

    /// <summary>
    /// Flips <see cref="FrontComposerThemeState.HydrationState"/> from <see cref="ThemeHydrationState.Idle"/>
    /// to <see cref="ThemeHydrationState.Hydrating"/> at the start of the hydrate path
    /// (Story 3-6 D19). No-op when already <c>Hydrated</c>.
    /// </summary>
    /// <param name="state">The current theme state.</param>
    /// <param name="action">The theme-hydrating action.</param>
    /// <returns>A new state with <c>HydrationState = Hydrating</c> when transitioning from <c>Idle</c>.</returns>
    [ReducerMethod]
    public static FrontComposerThemeState ReduceThemeHydrating(
        FrontComposerThemeState state,
        ThemeHydratingAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.HydrationState == ThemeHydrationState.Hydrated
            ? state
            : state with { HydrationState = ThemeHydrationState.Hydrating };
    }

    /// <summary>
    /// Flips <see cref="FrontComposerThemeState.HydrationState"/> to <see cref="ThemeHydrationState.Hydrated"/>
    /// at the end of the hydrate path (Story 3-6 D19). Called on BOTH happy and fail-closed paths.
    /// </summary>
    /// <param name="state">The current theme state.</param>
    /// <param name="action">The theme-hydrated-completed action.</param>
    /// <returns>A new state with <c>HydrationState = Hydrated</c>.</returns>
    [ReducerMethod]
    public static FrontComposerThemeState ReduceThemeHydratedCompleted(
        FrontComposerThemeState state,
        ThemeHydratedCompletedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.HydrationState == ThemeHydrationState.Hydrated
            ? state
            : state with { HydrationState = ThemeHydrationState.Hydrated };
    }
}
