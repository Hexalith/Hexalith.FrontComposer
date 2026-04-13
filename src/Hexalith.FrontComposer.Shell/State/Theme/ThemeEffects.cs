namespace Hexalith.FrontComposer.Shell.State.Theme;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Storage;

using Microsoft.Extensions.Logging;

/// <summary>
/// Async side effects for theme state: persistence on change, hydration on app init.
/// </summary>
/// <param name="storage">The storage service for persisting theme preferences.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class ThemeEffects(IStorageService storage, ILogger<ThemeEffects> logger)
{
    /// <summary>
    /// Hydrates theme state from storage when the application initializes.
    /// If storage is empty or throws, Feature defaults apply.
    /// </summary>
    /// <param name="action">The app initialized action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        try
        {
            string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "theme");

            // Retrieve as nullable enum so missing keys remain null while stored values hydrate strongly typed.
            ThemeValue? stored = await storage.GetAsync<ThemeValue?>(key).ConfigureAwait(false);
            if (stored is ThemeValue theme)
            {
                dispatcher.Dispatch(new ThemeChangedAction(action.CorrelationId, theme));
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to hydrate theme state from storage");
        }
    }

    /// <summary>
    /// Persists theme state to storage when the theme changes.
    /// </summary>
    /// <param name="action">The theme changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused but required by Fluxor).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleThemeChanged(ThemeChangedAction action, IDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(action);
        try
        {
            string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "theme");
            await storage.SetAsync(key, action.NewTheme).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to persist theme state");
        }
    }
}
