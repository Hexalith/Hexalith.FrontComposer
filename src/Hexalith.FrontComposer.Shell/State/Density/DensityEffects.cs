
using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.Density;
/// <summary>
/// Async side effects for density state: persistence on change, hydration on app init.
/// </summary>
/// <param name="storage">The storage service for persisting density preferences.</param>
/// <param name="logger">Logger for diagnostics.</param>
public class DensityEffects(IStorageService storage, ILogger<DensityEffects> logger) {
    /// <summary>
    /// Hydrates density state from storage when the application initializes.
    /// If storage is empty or throws, Feature defaults apply.
    /// </summary>
    /// <param name="action">The app initialized action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        try {
            string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "density");

            // Retrieve as nullable enum so missing keys remain null while stored values hydrate strongly typed.
            DensityLevel? stored = await storage.GetAsync<DensityLevel?>(key).ConfigureAwait(false);
            if (stored is DensityLevel density) {
                dispatcher.Dispatch(new DensityChangedAction(action.CorrelationId, density));
            }
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to hydrate density state from storage");
        }
    }

    /// <summary>
    /// Persists density state to storage when the density changes.
    /// </summary>
    /// <param name="action">The density changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused but required by Fluxor).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleDensityChanged(DensityChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        try {
            string key = StorageKeys.BuildKey(StorageKeys.DefaultTenantId, StorageKeys.DefaultUserId, "density");
            await storage.SetAsync(key, action.NewDensity).ConfigureAwait(false);
        }
        catch (Exception ex) {
            logger.LogWarning(ex, "Failed to persist density state");
        }
    }
}
