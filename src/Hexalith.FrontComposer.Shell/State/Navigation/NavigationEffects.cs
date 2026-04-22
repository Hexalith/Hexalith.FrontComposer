using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Async side effects for navigation state: persistence on change, hydration on app init
/// (Story 3-2 D12 / D14 / D15; ADR-037, ADR-038; Story 3-6 D1 / D2 / D9 / D13 / D19 / D21; ADR-048,
/// ADR-049). Mirrors the <c>ThemeEffects</c> / <c>DensityEffects</c> scope-guard pattern.
/// </summary>
/// <remarks>
/// <para>
/// <b>Persist triggers (D14 amended + Story 3-6 D2):</b> <see cref="SidebarToggledAction"/>,
/// <see cref="NavGroupToggledAction"/>, <see cref="SidebarExpandedAction"/>,
/// <see cref="BoundedContextChangedAction"/> (only when <c>NewBoundedContext</c> is non-null).
/// <see cref="NavigationHydratedAction"/> / <see cref="LastActiveRouteHydratedAction"/> are
/// INTENTIONALLY excluded (ADR-038 — hydrate is read-only).
/// <see cref="ViewportTierChangedAction"/> is INTENTIONALLY excluded (ADR-037 — tier is never persisted).
/// </para>
/// <para>
/// <b>Scope guard (D12):</b> null/empty/whitespace <c>TenantId</c> or <c>UserId</c> →
/// log <see cref="FcDiagnosticIds.HFC2105_StoragePersistenceSkipped"/> at Information and return.
/// No storage call fires.
/// </para>
/// </remarks>
/// <param name="storage">The storage service for persisting navigation preferences.</param>
/// <param name="userContextAccessor">Resolves tenant/user segments for storage key scoping.</param>
/// <param name="logger">Logger for diagnostics.</param>
/// <param name="state">The navigation state (read inside persist handlers post-reducer).</param>
/// <param name="serviceProvider">
/// Late-bound service provider used to resolve <see cref="NavigationManager"/> (Blazor circuit
/// scoped; injecting the concrete type directly into effects breaks non-circuit unit tests).
/// </param>
/// <param name="registry">
/// Optional registry used for D21 hydrate-side pruning of stale <c>LastActiveRoute</c> entries
/// (Story 3-6). When null, pruning is skipped but hydrate still succeeds.
/// </param>
public sealed class NavigationEffects(
    IStorageService storage,
    IUserContextAccessor userContextAccessor,
    ILogger<NavigationEffects> logger,
    IState<FrontComposerNavigationState> state,
    IServiceProvider? serviceProvider = null,
    IFrontComposerRegistry? registry = null) {
    private const string FeatureSegment = "nav";

    /// <summary>
    /// Hydrates navigation state from storage when the application initializes (Story 3-2 D15 /
    /// Story 3-6 D1 / D21). Dispatches <see cref="NavigationHydratingAction"/> first, then
    /// <see cref="NavigationHydratedAction"/> when a blob is found, then
    /// <see cref="LastActiveRouteHydratedAction"/> (possibly with a D21-pruned null when the BC
    /// is no longer registered), and finally <see cref="NavigationHydratedCompletedAction"/>.
    /// Feature defaults apply when the blob is missing, corrupt, or storage throws.
    /// </summary>
    /// <param name="action">The app initialized action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        await HydrateAsync(dispatcher).ConfigureAwait(false);
    }

    /// <summary>
    /// Re-runs hydrate on <see cref="StorageReadyAction"/> iff hydration state is still
    /// <see cref="NavigationHydrationState.Idle"/> (Story 3-6 D19).
    /// </summary>
    /// <param name="action">The storage-ready action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleStorageReady(StorageReadyAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (state.Value.HydrationState != NavigationHydrationState.Idle) {
            return;
        }

        await HydrateAsync(dispatcher).ConfigureAwait(false);
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
    /// Captures <c>LastActiveRoute</c> when the user enters a non-null bounded context
    /// (Story 3-6 D2 / ADR-048). Reads <c>NavigationManager.Uri</c> so deep-link fidelity is
    /// preserved (not just the BC landing page). Dispatches
    /// <see cref="LastActiveRouteChangedAction"/> so the reducer updates state; the subsequent
    /// persist effect writes the updated blob.
    /// </summary>
    /// <param name="action">The bounded-context-changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandleBoundedContextChanged(BoundedContextChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (string.IsNullOrWhiteSpace(action.NewBoundedContext)) {
            return Task.CompletedTask;
        }

        // Scope guard — do not pollute in-memory LastActiveRoute with anon navigation that persist
        // cannot write. Symmetric with PersistAsync's fail-closed behavior; prevents cross-user
        // leak when a later scoped hydrate returns null blob.
        if (!TryResolveScope(out _, out _, "persist")) {
            return Task.CompletedTask;
        }

        NavigationManager? navigation = ResolveNavigationManager();
        if (navigation is null) {
            return Task.CompletedTask;
        }

        string? route = SessionRouteHelper.NormalizeCurrentRoute(navigation);
        if (string.IsNullOrWhiteSpace(route)) {
            return Task.CompletedTask;
        }

        dispatcher.Dispatch(new LastActiveRouteChangedAction(NewCorrelationId(), route));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Persists the nav blob whenever <see cref="LastActiveRouteChangedAction"/> updates the
    /// route (Story 3-6 D2).
    /// </summary>
    /// <param name="action">The last-active-route-changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandleLastActiveRouteChanged(LastActiveRouteChangedAction action, IDispatcher dispatcher) {
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

    private async Task HydrateAsync(IDispatcher dispatcher) {
        if (!TryResolveScope(out string tenantId, out string userId, "hydrate")) {
            return;
        }

        dispatcher.Dispatch(new NavigationHydratingAction());

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
                // Reset to feature defaults — previously the null-blob branch left in-memory state
                // untouched, leaking prior-user SidebarCollapsed / CollapsedGroups / LastActiveRoute
                // across scope flips within the same circuit.
                dispatcher.Dispatch(new NavigationHydratedAction(
                    SidebarCollapsed: false,
                    CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal)));
                dispatcher.Dispatch(new LastActiveRouteHydratedAction(null));
                dispatcher.Dispatch(new NavigationHydratedCompletedAction());
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
            dispatcher.Dispatch(new NavigationHydratedCompletedAction());
            return;
        }
        catch (Exception ex) {
            logger.LogInformation(
                ex,
                "{DiagnosticId}: Navigation hydration errored — feature defaults apply. Reason={Reason}.",
                FcDiagnosticIds.HFC2107_NavigationHydrationEmpty,
                "Corrupt");
            dispatcher.Dispatch(new NavigationHydratedCompletedAction());
            return;
        }

        dispatcher.Dispatch(new NavigationHydratedAction(blob.SidebarCollapsed, groups));

        NavigationManager? navigation = ResolveNavigationManager();
        string? hydratedRoute = null;
        bool prunePersistRequired = false;
        if (!string.IsNullOrWhiteSpace(blob.LastActiveRoute)) {
            if (SessionRouteHelper.TryNormalizePersistedRoute(blob.LastActiveRoute, navigation, out string normalizedRoute)) {
                hydratedRoute = normalizedRoute;
                prunePersistRequired = !string.Equals(blob.LastActiveRoute, normalizedRoute, StringComparison.Ordinal);
            }
            else {
                logger.LogInformation(
                    "{DiagnosticId}: Pruning stale LastActiveRoute — stored route rejected by internal-route/base-path validation. Reason={Reason}.",
                    FcDiagnosticIds.HFC2107_NavigationHydrationEmpty,
                    "Invalid");
                prunePersistRequired = true;
            }
        }

        if (hydratedRoute is not null) {
            string? bc = BoundedContextRouteParser.Parse(hydratedRoute);
            if (bc is not null && IsUnregisteredBoundedContext(bc)) {
                logger.LogInformation(
                    "{DiagnosticId}: Pruning stale LastActiveRoute — bounded context '{BoundedContext}' is no longer registered. Reason={Reason}.",
                    FcDiagnosticIds.HFC2107_NavigationHydrationEmpty,
                    bc,
                    "OutOfScope");
                hydratedRoute = null;
                prunePersistRequired = true;
            }
        }

        dispatcher.Dispatch(new LastActiveRouteHydratedAction(hydratedRoute));
        dispatcher.Dispatch(new NavigationHydratedCompletedAction());

        if (prunePersistRequired) {
            await WriteBlobAsync(tenantId, userId, blob.SidebarCollapsed, groups, hydratedRoute).ConfigureAwait(false);
        }
    }

    private bool IsUnregisteredBoundedContext(string boundedContext) {
        if (registry is null) {
            return false;
        }

        try {
            // BoundedContextRouteParser.Parse lowercases its output; manifests are authored in their
            // natural casing (typically PascalCase). Compare case-insensitively so PascalCase manifests
            // match lowercased parser output.
            IReadOnlyList<DomainManifest> manifests = registry.GetManifests();
            foreach (DomainManifest manifest in manifests) {
                if (string.Equals(manifest.BoundedContext, boundedContext, StringComparison.OrdinalIgnoreCase)) {
                    return false;
                }
            }

            return true;
        }
        catch (Exception ex) {
            logger.LogInformation(
                ex,
                "{DiagnosticId}: Registry enumeration failed during hydrate-side LastActiveRoute prune — preserving route. Reason={Reason}.",
                FcDiagnosticIds.HFC2107_NavigationHydrationEmpty,
                "RegistryFailure");
            return false;
        }
    }

    private NavigationManager? ResolveNavigationManager() {
        if (serviceProvider is null) {
            return null;
        }

        try {
            return serviceProvider.GetService<NavigationManager>();
        }
        catch (ObjectDisposedException) {
            return null;
        }
    }

    private string NewCorrelationId() {
        if (serviceProvider is null) {
            return Guid.NewGuid().ToString("N");
        }

        try {
            return serviceProvider.GetService<IUlidFactory>()?.NewUlid() ?? Guid.NewGuid().ToString("N");
        }
        catch (ObjectDisposedException) {
            return Guid.NewGuid().ToString("N");
        }
    }

    private async Task PersistAsync() {
        if (!TryResolveScope(out string tenantId, out string userId, "persist")) {
            return;
        }

        FrontComposerNavigationState snapshot = state.Value;
        Dictionary<string, bool> groups = new(snapshot.CollapsedGroups, StringComparer.Ordinal);
        await WriteBlobAsync(tenantId, userId, snapshot.SidebarCollapsed, groups, snapshot.LastActiveRoute).ConfigureAwait(false);
    }

    private async Task WriteBlobAsync(
        string tenantId,
        string userId,
        bool sidebarCollapsed,
        IReadOnlyDictionary<string, bool> collapsedGroups,
        string? lastActiveRoute) {
        try {
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
            Dictionary<string, bool> groups = new(collapsedGroups, StringComparer.Ordinal);
            NavigationPersistenceBlob blob = new(sidebarCollapsed, groups, lastActiveRoute);
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

    private bool TryResolveScope(out string tenantId, out string userId, string direction) {
        string? rawTenant = userContextAccessor.TenantId;
        string? rawUser = userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(rawTenant) || string.IsNullOrWhiteSpace(rawUser)) {
            logger.LogInformation(
                "{DiagnosticId}: Navigation {Direction} skipped — null/empty/whitespace tenant or user context.",
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
}
