using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.Density;

/// <summary>
/// Async side effects for density state (Story 3-3 Task 3 — rewritten from the Story 3-1 baseline).
/// </summary>
/// <remarks>
/// <para>
/// <b>Persist triggers (D8):</b> <see cref="UserPreferenceChangedAction"/> and
/// <see cref="UserPreferenceClearedAction"/>. <see cref="DensityHydratedAction"/> is INTENTIONALLY
/// excluded (ADR-038 mirror — hydrate is read-only). <see cref="ViewportTierChangedAction"/> is
/// INTENTIONALLY excluded (ADR-040 — tier-force is a pure compute path; the user's preference
/// doesn't change when the browser resizes).
/// </para>
/// <para>
/// <b>Scope guard (inherited Story 3-1 ADR-029):</b> null/empty/whitespace <c>TenantId</c> or
/// <c>UserId</c> → log <see cref="FcDiagnosticIds.HFC2105_StoragePersistenceSkipped"/> at
/// Information and return. No storage call fires.
/// </para>
/// <para>
/// <b>Error handling (Story 3-2 D15 mirror):</b> <see cref="OperationCanceledException"/> →
/// <c>LogDebug</c> (expected on circuit disposal); any other <see cref="Exception"/> in persist →
/// log <c>HFC2105</c> at Information with payload; hydrate errors → <c>HFC2106</c> at Information
/// with <c>Reason=Empty</c>/<c>Corrupt</c>, then dispatch a bootstrap-safe fallback so the later
/// viewport watcher can re-resolve against the measured tier.
/// </para>
/// </remarks>
/// <param name="storage">The storage service for persisting density preferences.</param>
/// <param name="userContextAccessor">Resolves the tenant/user segments used to scope storage keys (Story 3-1 D8).</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <param name="navigationState">Cross-feature read of the current viewport tier (Story 3-3 D7).</param>
/// <param name="options">Adopter-configured deployment default density (Story 3-3 D4).</param>
/// <param name="densityState">Intra-feature read of the current user preference for viewport-driven recompute (Story 3-3 D7).</param>
public sealed class DensityEffects(
    IStorageService storage,
    IUserContextAccessor userContextAccessor,
    ILogger<DensityEffects> logger,
    IState<FrontComposerNavigationState> navigationState,
    IOptions<FcShellOptions> options,
    IState<FrontComposerDensityState> densityState) {
    private const string FeatureSegment = "density";

    /// <summary>
    /// Hydrates density state from storage when the application initializes. Dispatches
    /// <see cref="DensityHydratedAction"/> with the resolved effective density; on any error the
    /// feature defaults apply and the dispatch is skipped.
    /// </summary>
    /// <param name="action">The app-initialized action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (!TryResolveScope(out string tenantId, out string userId)) {
            return;
        }

        string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
        DensityLevel? stored = null;
        try {
            HydratedDensityPreference hydrated = await ReadStoredPreferenceAsync(key).ConfigureAwait(false);
            stored = hydrated.UserPreference;
            if (!hydrated.KeyExists) {
                logger.LogInformation(
                    "{DiagnosticId}: Density hydration found no stored value — bootstrap defaults apply until the viewport watcher emits. Reason={Reason}.",
                    FcDiagnosticIds.HFC2106_ThemeHydrationEmpty,
                    "Empty");
            }
        }
        catch (OperationCanceledException) {
            logger.LogDebug("Density hydration cancelled — circuit disposing.");
            return;
        }
        catch (Exception ex) {
            logger.LogInformation(
                ex,
                "{DiagnosticId}: Density hydration errored — bootstrap defaults apply until the viewport watcher emits. Reason={Reason}.",
                FcDiagnosticIds.HFC2106_ThemeHydrationEmpty,
                "Corrupt");
        }

        DensityLevel resolvedEffective = DensityPrecedence.Resolve(
            stored,
            options.Value.DefaultDensity,
            DensitySurface.Default,
            GetHydrationTier());

        dispatcher.Dispatch(new DensityHydratedAction(stored, resolvedEffective));
    }

    /// <summary>
    /// Cross-feature handler (Story 3-3 D7 / ADR-040). Re-resolves effective density on every
    /// viewport tier transition and dispatches <see cref="EffectiveDensityRecomputedAction"/> when
    /// the value changes. No storage write on this path.
    /// </summary>
    /// <param name="action">The navigation viewport-tier-changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandleViewportTierChanged(ViewportTierChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        // The action's NewTier is authoritative — don't read navigationState.Value.CurrentViewport
        // because that reflects the pre-reducer state at the moment the effect runs.
        FrontComposerDensityState densitySnapshot = densityState.Value;
        DensityLevel newEffective = DensityPrecedence.Resolve(
            userPreference: densitySnapshot.UserPreference,
            deploymentDefault: options.Value.DefaultDensity,
            surface: DensitySurface.Default,
            tier: action.NewTier);

        if (newEffective != densitySnapshot.EffectiveDensity) {
            dispatcher.Dispatch(new EffectiveDensityRecomputedAction(newEffective));
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Persists the user's explicit density choice on <see cref="UserPreferenceChangedAction"/>
    /// (Story 3-3 D8 — persist trigger #1).
    /// </summary>
    /// <param name="action">The user-preference-changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused; required by the Fluxor signature).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandleUserPreferenceChanged(UserPreferenceChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        return PersistAsync(action.NewPreference);
    }

    /// <summary>
    /// Persists a literal <see langword="null"/> on <see cref="UserPreferenceClearedAction"/>
    /// (Story 3-3 D8 — persist trigger #2; clears the stored preference so the resolver falls
    /// through on next hydrate).
    /// </summary>
    /// <param name="action">The user-preference-cleared action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused; required by the Fluxor signature).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandleUserPreferenceCleared(UserPreferenceClearedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        return PersistAsync(null);
    }

    private async Task PersistAsync(DensityLevel? value) {
        if (!TryResolveScope(out string tenantId, out string userId)) {
            return;
        }

        try {
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
            await storage.SetAsync(key, value).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            logger.LogDebug("Density persist cancelled — circuit disposing.");
        }
        catch (Exception ex) {
            logger.LogInformation(
                ex,
                "{DiagnosticId}: Density persistence failed — swallowed (next change retries).",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
        }
    }

    private bool TryResolveScope(out string tenantId, out string userId) {
        string? rawTenant = userContextAccessor.TenantId;
        string? rawUser = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(rawTenant) || string.IsNullOrWhiteSpace(rawUser)) {
            logger.LogInformation(
                "{DiagnosticId}: Density persistence skipped — null/empty/whitespace tenant or user context.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
            tenantId = string.Empty;
            userId = string.Empty;
            return false;
        }

        tenantId = rawTenant;
        userId = rawUser;
        return true;
    }

    private ViewportTier GetHydrationTier() {
        // AppInitialized runs before FcLayoutBreakpointWatcher pushes its first measured tier from JS.
        // The pre-reducer Desktop value is therefore a placeholder during bootstrap; cap to Tablet so
        // touch-sized Comfortable density wins until the real viewport measurement arrives.
        ViewportTier tier = navigationState.Value.CurrentViewport;
        return tier == ViewportTier.Desktop
            ? ViewportTier.Tablet
            : tier;
    }

    private async Task<HydratedDensityPreference> ReadStoredPreferenceAsync(string key) {
        IReadOnlyList<string> matchingKeys = await storage.GetKeysAsync(key).ConfigureAwait(false);
        bool keyExists = matchingKeys.Contains(key, StringComparer.Ordinal);
        if (!keyExists) {
            return new(false, null);
        }

        DensityLevel? stored = await storage.GetAsync<DensityLevel?>(key).ConfigureAwait(false);
        if (stored is DensityLevel typedValue) {
            return new(true, typedValue);
        }

        string? legacyValue = await storage.GetAsync<string>(key).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(legacyValue)
            && Enum.TryParse(legacyValue.Trim(), ignoreCase: true, out DensityLevel migrated)) {
            await storage.SetAsync(key, (DensityLevel?)migrated).ConfigureAwait(false);
            return new(true, migrated);
        }

        return new(true, null);
    }

    private readonly record struct HydratedDensityPreference(bool KeyExists, DensityLevel? UserPreference);
}
