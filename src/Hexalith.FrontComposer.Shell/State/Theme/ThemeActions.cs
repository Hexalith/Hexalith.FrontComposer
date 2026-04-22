namespace Hexalith.FrontComposer.Shell.State.Theme;

/// <summary>
/// Dispatched when the application theme changes.
/// </summary>
/// <param name="CorrelationId">Correlation identifier for tracing.</param>
/// <param name="NewTheme">The new theme value to apply.</param>
public record ThemeChangedAction(string CorrelationId, ThemeValue NewTheme);

/// <summary>
/// Dispatched by <c>ThemeEffects.HandleAppInitialized</c> / <c>HandleStorageReady</c> at the start
/// of the hydrate path (Story 3-6 D19). Reducer flips
/// <see cref="FrontComposerThemeState.HydrationState"/> from <see cref="ThemeHydrationState.Idle"/>
/// to <see cref="ThemeHydrationState.Hydrating"/>. NEVER persisted.
/// </summary>
public sealed record ThemeHydratingAction;

/// <summary>
/// Dispatched by <c>ThemeEffects.HandleAppInitialized</c> / <c>HandleStorageReady</c> as the final
/// step of the hydrate path (Story 3-6 D19). Reducer flips
/// <see cref="FrontComposerThemeState.HydrationState"/> to <see cref="ThemeHydrationState.Hydrated"/>.
/// Called on BOTH happy path AND fail-closed path.
/// </summary>
public sealed record ThemeHydratedCompletedAction;
