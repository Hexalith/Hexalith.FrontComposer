using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Badges;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

/// <summary>
/// Effects for the capability-discovery feature (Story 3-5 D9 / D10 / D13 / ADR-046).
/// Hydrates the seen-set from storage on app init, drives <see cref="BadgeCountService.InitializeAsync"/>
/// + bridges <see cref="IBadgeCountService.CountChanged"/> into <see cref="BadgeCountChangedAction"/>
/// dispatches, and persists the seen-set on <see cref="CapabilityVisitedAction"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Scope guard (D10 / L03):</b> hydrate AND persist paths fail-closed when
/// <see cref="IUserContextAccessor.TenantId"/> or <see cref="IUserContextAccessor.UserId"/> is
/// null/empty/whitespace. Both paths log <see cref="FcDiagnosticIds.HFC2105_StoragePersistenceSkipped"/>
/// at Information with a structured <c>{Direction}</c> payload (<c>"hydrate"</c> | <c>"persist"</c>)
/// so operators can disambiguate the two skips. The hydrate path STILL dispatches
/// <see cref="SeenCapabilitiesHydratedAction"/> with <see cref="ImmutableHashSet{T}.Empty"/> so
/// the rendering pipeline unblocks even when scope is unavailable.
/// </para>
/// <para>
/// <b>Bridge subscription (D6 / D8):</b> the effect subscribes to
/// <see cref="IBadgeCountService.CountChanged"/> in the constructor and dispatches one
/// <see cref="BadgeCountChangedAction"/> per emission. Disposed via
/// <see cref="IDisposable"/> on circuit teardown.
/// </para>
/// </remarks>
public sealed class CapabilityDiscoveryEffects : IDisposable {
    private const string FeatureSegment = "capability-seen";
    private const string DirectionHydrate = "hydrate";
    private const string DirectionPersist = "persist";

    private readonly IDispatcher _dispatcher;
    private readonly IStorageService _storage;
    private readonly IUserContextAccessor _userContextAccessor;
    private readonly IBadgeCountService _badgeCountService;
    private readonly IState<FrontComposerCapabilityDiscoveryState> _state;
    private readonly ILogger<CapabilityDiscoveryEffects> _logger;
    private readonly IDisposable _bridgeSubscription;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CapabilityDiscoveryEffects"/> class.
    /// </summary>
    /// <param name="dispatcher">The Fluxor dispatcher (used by the bridge subscription).</param>
    /// <param name="storage">Storage service for the seen-set blob.</param>
    /// <param name="userContextAccessor">Tenant/user scope resolver (L03 fail-closed).</param>
    /// <param name="badgeCountService">Producer of <see cref="IBadgeCountService.CountChanged"/>.</param>
    /// <param name="state">Read-only access to the current state for the persist payload.</param>
    /// <param name="logger">Logger for HFC2105 / HFC2112 diagnostics.</param>
    public CapabilityDiscoveryEffects(
        IDispatcher dispatcher,
        IStorageService storage,
        IUserContextAccessor userContextAccessor,
        IBadgeCountService badgeCountService,
        IState<FrontComposerCapabilityDiscoveryState> state,
        ILogger<CapabilityDiscoveryEffects> logger) {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(userContextAccessor);
        ArgumentNullException.ThrowIfNull(badgeCountService);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(logger);

        _dispatcher = dispatcher;
        _storage = storage;
        _userContextAccessor = userContextAccessor;
        _badgeCountService = badgeCountService;
        _state = state;
        _logger = logger;

        _bridgeSubscription = _badgeCountService.CountChanged.Subscribe(
            onNext: SafeDispatchCountChanged,
            onError: static _ => { /* swallow — never crash the circuit on bridge errors */ },
            onCompleted: static () => { /* expected on dispose */ });
    }

    /// <summary>
    /// Hydrates the seen-set from storage and triggers the badge-count seed fetch.
    /// </summary>
    /// <param name="action">The app initialized action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        await HydrateSeenSetAsync(dispatcher).ConfigureAwait(false);
        await SeedBadgeCountsAsync(dispatcher).ConfigureAwait(false);
    }

    /// <summary>
    /// Re-runs hydrate on <see cref="Navigation.StorageReadyAction"/> iff the capability
    /// hydration state is still <see cref="CapabilityDiscoveryHydrationState.Idle"/>
    /// (Story 3-6 D19 / ADR-049).
    /// </summary>
    /// <param name="action">The storage-ready action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleStorageReady(Navigation.StorageReadyAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (_state.Value.HydrationState != CapabilityDiscoveryHydrationState.Idle) {
            return;
        }

        await HydrateSeenSetAsync(dispatcher).ConfigureAwait(false);
        await SeedBadgeCountsAsync(dispatcher).ConfigureAwait(false);
    }

    /// <summary>
    /// Persists the seen-set to storage when a capability is visited (Story 3-5 D9 / D13).
    /// </summary>
    /// <param name="action">The visited action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleCapabilityVisited(CapabilityVisitedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        if (!TryResolveScope(out string tenantId, out string userId, DirectionPersist)) {
            return;
        }

        try {
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
            ImmutableHashSet<string> snapshot = _state.Value.SeenCapabilities;
            await _storage.SetAsync(key, snapshot, CancellationToken.None).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("Capability-seen persist cancelled — circuit disposing.");
        }
        catch (Exception ex) {
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Capability-seen persistence failed for '{CapabilityId}'. ExceptionType={ExceptionType}; ExceptionMessage={ExceptionMessage}.",
                FcDiagnosticIds.HFC2112_BadgeInitialFetchFault,
                action.CapabilityId,
                ex.GetType().Name,
                ex.Message);
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _bridgeSubscription.Dispose();
    }

    private async Task HydrateSeenSetAsync(IDispatcher dispatcher) {
        if (!TryResolveScope(out string tenantId, out string userId, DirectionHydrate)) {
            dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
                ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
            return;
        }

        ImmutableHashSet<string> hydrated = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);
        try {
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
            ImmutableHashSet<string>? blob = await _storage
                .GetAsync<ImmutableHashSet<string>>(key, CancellationToken.None)
                .ConfigureAwait(false);
            if (blob is not null && blob.Count > 0) {
                hydrated = blob.WithComparer(StringComparer.Ordinal);
            }
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("Capability-seen hydrate cancelled — circuit disposing.");
        }
        catch (Exception ex) {
            // D13 hydrate-side parity: storage faults during hydrate are fail-soft — dispatch
            // the empty seen-set so the rendering pipeline unblocks. Log HFC2112 with the same
            // call-site-hint convention.
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Capability-seen hydrate failed — defaulting to empty set. ExceptionType={ExceptionType}; ExceptionMessage={ExceptionMessage}.",
                FcDiagnosticIds.HFC2112_BadgeInitialFetchFault,
                ex.GetType().Name,
                ex.Message);
        }

        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(hydrated));
    }

    private async Task SeedBadgeCountsAsync(IDispatcher dispatcher) {
        if (_badgeCountService is BadgeCountService concrete) {
            await concrete.InitializeAsync(CancellationToken.None).ConfigureAwait(false);
        }

        // Snapshot AFTER InitializeAsync resolved (or no-op if adopter-supplied).
        ImmutableDictionary<Type, int> snapshot;
        try {
            snapshot = ImmutableDictionary.CreateRange(_badgeCountService.Counts);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Badge counts snapshot threw during seed — dispatching empty dictionary. ExceptionType={ExceptionType}; ExceptionMessage={ExceptionMessage}.",
                FcDiagnosticIds.HFC2112_BadgeInitialFetchFault,
                ex.GetType().Name,
                ex.Message);
            snapshot = ImmutableDictionary<Type, int>.Empty;
        }

        dispatcher.Dispatch(new BadgeCountsSeededAction(snapshot));
    }

    private void SafeDispatchCountChanged(BadgeCountChangedArgs args) {
        if (_disposed) {
            return;
        }

        try {
            _dispatcher.Dispatch(new BadgeCountChangedAction(args.ProjectionType, args.NewCount));
        }
        catch (ObjectDisposedException) {
            // Circuit torn down between the bridge fire and this dispatch — safe to drop.
        }
    }

    private bool TryResolveScope(out string tenantId, out string userId, string direction) {
        string? rawTenant = _userContextAccessor.TenantId;
        string? rawUser = _userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(rawTenant) || string.IsNullOrWhiteSpace(rawUser)) {
            _logger.LogInformation(
                "{DiagnosticId}: Capability-seen {Direction} skipped — null/empty/whitespace tenant or user context.",
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
