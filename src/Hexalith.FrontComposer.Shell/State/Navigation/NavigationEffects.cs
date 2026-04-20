using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Async side effects for navigation state: persistence on change, hydration on app init
/// (Story 3-2 D12, D14, D15; ADR-037, ADR-038). Mirrors the <c>ThemeEffects</c> / <c>DensityEffects</c>
/// scope-guard pattern.
/// </summary>
/// <remarks>
/// <para>
/// <b>Persist triggers (D14 amended):</b> <see cref="SidebarToggledAction"/>,
/// <see cref="NavGroupToggledAction"/>, <see cref="SidebarExpandedAction"/>.
/// <see cref="NavigationHydratedAction"/> is INTENTIONALLY excluded (ADR-038 — hydrate is read-only).
/// <see cref="ViewportTierChangedAction"/> is INTENTIONALLY excluded (ADR-037 — tier is never persisted).
/// </para>
/// <para>
/// <b>Scope guard (D12):</b> null/empty/whitespace <c>TenantId</c> or <c>UserId</c> →
/// log <see cref="FcDiagnosticIds.HFC2105_StoragePersistenceSkipped"/> at Information and return.
/// No storage call fires.
/// </para>
/// <para>
/// <b>Error handling (D15 amended 2026-04-19):</b>
/// <see cref="OperationCanceledException"/> → <c>LogDebug</c> (expected on circuit disposal);
/// any other <see cref="Exception"/> in persist → log <c>HFC2105</c> at Information with payload;
/// any other exception in hydrate → log <c>HFC2107</c> at Information with <c>Reason=Corrupt</c>
/// and skip the hydrate dispatch.
/// </para>
/// </remarks>
/// <param name="storage">The storage service for persisting navigation preferences.</param>
/// <param name="userContextAccessor">Resolves tenant/user segments for storage key scoping.</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <param name="state">The navigation state (read inside persist handlers post-reducer).</param>
public sealed class NavigationEffects(
    IStorageService storage,
    IUserContextAccessor userContextAccessor,
    ILogger<NavigationEffects> logger,
    IState<FrontComposerNavigationState> state) {
    private const string FeatureSegment = "nav";

    /// <summary>
    /// Hydrates navigation state from storage when the application initializes.
    /// Dispatches <see cref="NavigationHydratedAction"/> when a blob is found; feature defaults
    /// apply when the blob is missing, corrupt, or storage throws.
    /// </summary>
    /// <param name="action">The app initialized action.</param>
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
        NavigationPersistenceBlob? blob;
        ImmutableDictionary<string, bool> groups;
        try {
            blob = await storage.GetAsync<NavigationPersistenceBlob>(key).ConfigureAwait(false);
            if (blob is null) {
                logger.LogInformation(
                    "{DiagnosticId}: Navigation hydration found no stored value — feature defaults apply. Reason={Reason}.",
                    FcDiagnosticIds.HFC2107_NavigationHydrationEmpty,
                    "Empty");
                return;
            }

            groups = blob.CollapsedGroups is null
                ? ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal)
                : blob.CollapsedGroups
                    .Where(static kv => !string.IsNullOrWhiteSpace(kv.Key))
                    .ToImmutableDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.Ordinal);
        }
        catch (OperationCanceledException) {
            logger.LogDebug("Navigation hydration cancelled — circuit disposing.");
            return;
        }
        catch (Exception ex) {
            logger.LogInformation(
                ex,
                "{DiagnosticId}: Navigation hydration errored — feature defaults apply. Reason={Reason}.",
                FcDiagnosticIds.HFC2107_NavigationHydrationEmpty,
                "Corrupt");
            return;
        }

        dispatcher.Dispatch(new NavigationHydratedAction(blob.SidebarCollapsed, groups));
    }

    /// <summary>
    /// Persists navigation state to storage when the user toggles the sidebar (D14 trigger #1).
    /// </summary>
    /// <param name="action">The sidebar toggled action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused; required by the Fluxor signature).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandleSidebarToggled(SidebarToggledAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        return PersistAsync();
    }

    /// <summary>
    /// Persists navigation state to storage when the user toggles a nav group (D14 trigger #2).
    /// </summary>
    /// <param name="action">The nav-group toggled action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused; required by the Fluxor signature).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandleNavGroupToggled(NavGroupToggledAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        return PersistAsync();
    }

    /// <summary>
    /// Persists navigation state to storage when the user expands the sidebar via the rail (D14 trigger #3).
    /// </summary>
    /// <param name="action">The sidebar-expanded action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused; required by the Fluxor signature).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandleSidebarExpanded(SidebarExpandedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        return PersistAsync();
    }

    /// <summary>
    /// Intentional no-op for <see cref="NavigationHydratedAction"/> (ADR-038 — hydrate is read-only
    /// from the storage perspective). Exposed publicly so the ADR-038 contract is asserted by
    /// <c>NavigationEffectsScopeTests.HydrateDoesNotRePersist</c>.
    /// NOT decorated with <c>[EffectMethod]</c> — Fluxor must NEVER auto-subscribe this to the dispatch pipeline.
    /// </summary>
    /// <param name="action">The navigation-hydrated action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused).</param>
    /// <returns>A completed task.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Public instance API contract: ADR-038 exposes HandleNavigationHydrated on NavigationEffects so adopters / tests invoke it symmetrically with the other Handle* persist handlers; marking it static would break that contract.")]
    public Task HandleNavigationHydrated(NavigationHydratedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        return Task.CompletedTask;
    }

    private async Task PersistAsync() {
        if (!TryResolveScope(out string tenantId, out string userId)) {
            return;
        }

        try {
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
            FrontComposerNavigationState snapshot = state.Value;
            Dictionary<string, bool> groups = new(snapshot.CollapsedGroups, StringComparer.Ordinal);
            NavigationPersistenceBlob blob = new(snapshot.SidebarCollapsed, groups);
            await storage.SetAsync(key, blob).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            logger.LogDebug("Navigation persist cancelled — circuit disposing.");
        }
        catch (Exception ex) {
            logger.LogInformation(
                ex,
                "{DiagnosticId}: Navigation persistence failed — swallowed (next toggle retries).",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
        }
    }

    private bool TryResolveScope(out string tenantId, out string userId) {
        string? rawTenant = userContextAccessor.TenantId;
        string? rawUser = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(rawTenant) || string.IsNullOrWhiteSpace(rawUser)) {
            logger.LogInformation(
                "{DiagnosticId}: Navigation persistence skipped — null/empty/whitespace tenant or user context.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
            tenantId = string.Empty;
            userId = string.Empty;
            return false;
        }

        tenantId = rawTenant;
        userId = rawUser;
        return true;
    }
}
