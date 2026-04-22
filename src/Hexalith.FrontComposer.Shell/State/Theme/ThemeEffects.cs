
using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.State.Theme;
/// <summary>
/// Async side effects for theme state: persistence on change, hydration on app init.
/// </summary>
/// <param name="storage">The storage service for persisting theme preferences.</param>
/// <param name="options">Shell options providing the accent color for ThemeSettings.</param>
/// <param name="userContextAccessor">Resolves the tenant/user segments used to scope storage keys (Story 3-1 D8).</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <param name="themeService">Optional Fluent UI theme applier for user-driven theme changes.</param>
public class ThemeEffects(
    IStorageService storage,
    IOptions<FcShellOptions> options,
    IUserContextAccessor userContextAccessor,
    ILogger<ThemeEffects> logger,
    IThemeService? themeService = null,
    IState<FrontComposerThemeState>? state = null) {
    private const string FeatureSegment = "theme";
    private const string DirectionHydrate = "hydrate";
    private const string DirectionPersist = "persist";

    /// <summary>
    /// Hydrates theme state from storage when the application initializes.
    /// If storage is empty or throws, Feature defaults apply.
    /// </summary>
    /// <param name="action">The app initialized action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        await HydrateAsync(action.CorrelationId, dispatcher).ConfigureAwait(false);
    }

    /// <summary>
    /// Re-runs hydrate on <see cref="Navigation.StorageReadyAction"/> iff the theme hydration state
    /// is still <see cref="ThemeHydrationState.Idle"/> (Story 3-6 D19).
    /// </summary>
    /// <param name="action">The storage-ready action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleStorageReady(Navigation.StorageReadyAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (state is not null && state.Value.HydrationState != ThemeHydrationState.Idle) {
            return;
        }

        await HydrateAsync(action.CorrelationId, dispatcher).ConfigureAwait(false);
    }

    private async Task HydrateAsync(string correlationId, IDispatcher dispatcher) {
        if (!TryResolveScope(out string tenantId, out string userId, DirectionHydrate)) {
            return;
        }

        dispatcher.Dispatch(new ThemeHydratingAction());

        try {
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
            // Retrieve as nullable enum so missing keys remain null while stored values hydrate strongly typed.
            ThemeValue? stored = await storage.GetAsync<ThemeValue?>(key).ConfigureAwait(false);
            if (stored is ThemeValue theme) {
                dispatcher.Dispatch(new ThemeChangedAction(correlationId, theme));
            }
            else {
                logger.LogInformation(
                    "{DiagnosticId}: Theme hydration found no stored value — feature defaults apply.",
                    FcDiagnosticIds.HFC2106_ThemeHydrationEmpty);
            }
        }
        catch (OperationCanceledException) {
            logger.LogDebug("Theme hydration cancelled — circuit disposing.");
            // Consistent with NavigationEffects / DensityEffects / DataGridNavigationEffects — every
            // terminal path dispatches Completed so HydrationState leaves Hydrating. Prevents the
            // re-hydrate StorageReady gate from blocking forever on a transient cancellation.
            dispatcher.Dispatch(new ThemeHydratedCompletedAction());
            return;
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to hydrate theme state from storage");
            dispatcher.Dispatch(new ThemeHydratedCompletedAction());
            return;
        }

        dispatcher.Dispatch(new ThemeHydratedCompletedAction());
    }

    /// <summary>
    /// Persists theme state to storage when the theme changes.
    /// </summary>
    /// <param name="action">The theme changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused but required by Fluxor).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleThemeChanged(ThemeChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ThemeMode mode = action.NewTheme switch {
            ThemeValue.Light => ThemeMode.Light,
            ThemeValue.Dark => ThemeMode.Dark,
            _ => ThemeMode.System,
        };

        if (!TryResolveScope(out string tenantId, out string userId, DirectionPersist)) {
            await ApplyThemeAsync(mode).ConfigureAwait(false);
            return;
        }

        try {
            await ApplyThemeAsync(mode).ConfigureAwait(false);
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
            await storage.SetAsync(key, action.NewTheme).ConfigureAwait(false);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to persist theme state");
        }
    }

    private bool TryResolveScope(out string tenantId, out string userId, string direction) {
        string? rawTenant = userContextAccessor.TenantId;
        string? rawUser = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(rawTenant) || string.IsNullOrWhiteSpace(rawUser)) {
            logger.LogInformation(
                "{DiagnosticId}: Theme {Direction} skipped — null/empty/whitespace tenant or user context.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                direction);
            tenantId = string.Empty;
            userId = string.Empty;
            return false;
        }

        tenantId = rawTenant;
        userId = rawUser;
        return true;
    }

    private Task ApplyThemeAsync(ThemeMode mode)
        => themeService is null
            ? Task.CompletedTask
            : themeService.SetThemeAsync(new ThemeSettings(options.Value.AccentColor, 0, 0, mode, true));
}
