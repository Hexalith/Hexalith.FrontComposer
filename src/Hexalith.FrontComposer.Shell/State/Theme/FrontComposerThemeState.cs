namespace Hexalith.FrontComposer.Shell.State.Theme;

/// <summary>
/// Fluxor state record for the application color theme (Story 3-1 / Story 3-6 D19).
/// Positional syntax enables <c>state with { CurrentTheme = action.NewTheme }</c> in reducers.
/// </summary>
/// <param name="CurrentTheme">The currently active theme value.</param>
/// <param name="HydrationState">
/// Transient three-state hydration marker (Story 3-6 D19). Initial value <see cref="ThemeHydrationState.Idle"/>;
/// flips <c>Idle → Hydrating → Hydrated</c> via dedicated reducers. NEVER persisted. Re-hydrate
/// via <c>StorageReadyAction</c> only runs when this is <see cref="ThemeHydrationState.Idle"/>.
/// </param>
public record FrontComposerThemeState(
    ThemeValue CurrentTheme,
    ThemeHydrationState HydrationState = ThemeHydrationState.Idle);
