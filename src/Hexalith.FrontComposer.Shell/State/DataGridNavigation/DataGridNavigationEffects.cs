using System.Collections.Concurrent;
using System.Text.Json;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Effects for the per-view DataGrid persistence feature (Story 3-6 D4–D10, D14, D16, D18, D19,
/// D21 / ADR-050). Owns the IO half of Story 2-2's reducer-only <c>DataGridNavigationState</c>
/// contract.
/// </summary>
/// <remarks>
/// <para>
/// <b>Per-key storage:</b> <c>{tenantId}:{userId}:datagrid:{viewKey}</c> where <c>viewKey</c> is
/// the Story 2-2 <c>"{boundedContext}:{projectionTypeFqn}"</c> string (nested colons by design).
/// </para>
/// <para>
/// <b>Debounce (D16):</b> 250 ms per viewKey via <see cref="ConcurrentDictionary{TKey, TValue}"/>
/// of <see cref="CancellationTokenSource"/> anchored on the injected <see cref="TimeProvider"/>.
/// A newer capture cancels the pending write for the same view. <see cref="HandleClearGridState"/>
/// cancels any pending capture BEFORE removing the key (D16 A5) so a clear cannot be shadowed by
/// a stale pending write.
/// </para>
/// <para>
/// <b>Disposal barrier (A2):</b> <c>Dispose()</c> atomically flips an internal flag; every handler
/// short-circuits post-disposal to avoid stashing new CTSes into a cleared dictionary.
/// </para>
/// <para>
/// <b>Fail-closed scope guard (L03):</b> null/empty/whitespace tenant or user → log
/// <see cref="FcDiagnosticIds.HFC2105_StoragePersistenceSkipped"/> at Information with
/// <c>Direction=hydrate|persist</c> payload and return; no synthetic "anonymous" fallback.
/// </para>
/// </remarks>
public sealed class DataGridNavigationEffects : IDisposable {
    /// <summary>Feature segment for the 4-arg <see cref="StorageKeys.BuildKey(string, string, string, string)"/> overload.</summary>
    private const string FeatureSegment = "datagrid";

    /// <summary>Per-view debounce interval (Story 3-6 D16).</summary>
    private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(250);

    /// <summary>JSON options cached on the type (D18). Web defaults + default-omit for compact blobs.</summary>
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
    };

    private readonly IStorageService _storage;
    private readonly IUserContextAccessor _userContextAccessor;
    private readonly ILogger<DataGridNavigationEffects> _logger;
    private readonly IState<DataGridNavigationState> _state;
    private readonly IFrontComposerRegistry? _registry;
    private readonly TimeProvider _timeProvider;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _loggedOutOfScope = new(StringComparer.Ordinal);
    private int _disposed;

    /// <summary>Initializes a new instance of the <see cref="DataGridNavigationEffects"/> class.</summary>
    /// <param name="storage">Storage service for per-view blobs.</param>
    /// <param name="userContextAccessor">Tenant / user scope resolver (L03 fail-closed).</param>
    /// <param name="logger">Logger for HFC2105 / HFC2114 diagnostics.</param>
    /// <param name="state">Read-only state access (used for the reducer-applied snapshot at persist time).</param>
    /// <param name="registry">Optional registry for hydrate-side out-of-scope pruning (D14 / A9). When null, pruning is skipped.</param>
    /// <param name="timeProvider">Time source for deterministic debounce (<see cref="TimeProvider.System"/> in production).</param>
    public DataGridNavigationEffects(
        IStorageService storage,
        IUserContextAccessor userContextAccessor,
        ILogger<DataGridNavigationEffects> logger,
        IState<DataGridNavigationState> state,
        IFrontComposerRegistry? registry = null,
        TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(userContextAccessor);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(state);
        _storage = storage;
        _userContextAccessor = userContextAccessor;
        _logger = logger;
        _state = state;
        _registry = registry;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <summary>
    /// Hydrates all persisted per-view snapshots on app init (Story 3-6 D6 / D14 / D19).
    /// Enumerates keys under <c>{tenantId}:{userId}:datagrid:</c>, reads each with per-key
    /// try/catch isolation, dispatches <see cref="GridViewHydratedAction"/> per valid blob,
    /// and prunes out-of-scope keys.
    /// </summary>
    /// <param name="action">The app initialized action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (IsDisposed()) {
            return;
        }

        await HydrateAsync(dispatcher).ConfigureAwait(false);
    }

    /// <summary>
    /// Re-runs hydrate on <see cref="StorageReadyAction"/> iff hydration state is still
    /// <see cref="DataGridNavigationHydrationState.Idle"/> (Story 3-6 D19).
    /// </summary>
    /// <param name="action">The storage-ready action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleStorageReady(StorageReadyAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (IsDisposed()) {
            return;
        }

        if (_state.Value.HydrationState != DataGridNavigationHydrationState.Idle) {
            return;
        }

        await HydrateAsync(dispatcher).ConfigureAwait(false);
    }

    /// <summary>
    /// Debounced persist handler for <see cref="CaptureGridStateAction"/> (Story 3-6 D6 / D16).
    /// Coalesces rapid captures per viewKey; persists the REDUCER-APPLIED snapshot from
    /// <see cref="IState{TState}.Value"/> rather than the action payload so the reducer's
    /// LRU eviction and validation are honoured.
    /// </summary>
    /// <param name="action">The capture action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleCaptureGridState(CaptureGridStateAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        if (IsDisposed()) {
            return;
        }

        if (!IsValidViewKey(action.ViewKey)) {
            return;
        }

        if (!TryResolveScope(out string tenantId, out string userId, "persist")) {
            return;
        }

        string viewKey = action.ViewKey;
        CancellationTokenSource cts = new();
        CancellationTokenSource? previous = null;
        _pending.AddOrUpdate(
            viewKey,
            cts,
            (_, existing) => {
                previous = existing;
                return cts;
            });

        if (previous is not null) {
            TryCancelAndDispose(previous);
        }

        CancellationToken token = cts.Token;
        try {
            await Task.Delay(DebounceInterval, _timeProvider, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            return;
        }
        finally {
            _pending.TryRemove(new KeyValuePair<string, CancellationTokenSource>(viewKey, cts));
            cts.Dispose();
        }

        if (IsDisposed()) {
            return;
        }

        if (!TryResolveScope(out tenantId, out userId, "persist")) {
            return;
        }

        if (!_state.Value.ViewStates.TryGetValue(viewKey, out GridViewSnapshot? snapshot)) {
            return;
        }

        try {
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment, viewKey);
            GridViewPersistenceBlob blob = GridViewPersistenceBlob.FromSnapshot(snapshot);
            await _storage.SetAsync(key, blob).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("DataGrid persist cancelled — circuit disposing.");
        }
        catch (Exception ex) {
            _logger.LogInformation(
                ex,
                "{DiagnosticId}: DataGrid {Direction} failed — swallowed (next capture retries). ViewKey={ViewKey}.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                "persist",
                viewKey);
        }
    }

    /// <summary>
    /// Clears the persisted storage key AND cancels any pending capture debounce for the viewKey
    /// (Story 3-6 D6 / D16 A5). Cancel-before-remove ordering prevents a post-clear re-persist.
    /// </summary>
    /// <param name="action">The clear action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleClearGridState(ClearGridStateAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        if (IsDisposed()) {
            return;
        }

        if (!IsValidViewKey(action.ViewKey)) {
            return;
        }

        // A5 — cancel in-flight capture BEFORE the RemoveAsync so the pending write cannot
        // re-persist the pre-clear snapshot AFTER the remove finishes.
        if (_pending.TryRemove(action.ViewKey, out CancellationTokenSource? pending)) {
            TryCancelAndDispose(pending);
        }

        if (!TryResolveScope(out string tenantId, out string userId, "persist")) {
            return;
        }

        try {
            string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment, action.ViewKey);
            await _storage.RemoveAsync(key).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("DataGrid clear cancelled — circuit disposing.");
        }
        catch (Exception ex) {
            _logger.LogInformation(
                ex,
                "{DiagnosticId}: DataGrid clear failed — swallowed. ViewKey={ViewKey}.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                action.ViewKey);
        }
    }

    /// <summary>
    /// On-demand hydrate for a specific viewKey (Story 3-6 D7 / ADR-050). Reads the scoped storage
    /// key and dispatches <see cref="GridViewHydratedAction"/> when a valid blob is found. The
    /// Story 2-2 reducer for <see cref="RestoreGridStateAction"/> is a no-op; this effect does the read.
    /// </summary>
    /// <param name="action">The restore action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleRestoreGridState(RestoreGridStateAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (IsDisposed()) {
            return;
        }

        if (!IsValidViewKey(action.ViewKey)) {
            return;
        }

        if (!TryResolveScope(out string tenantId, out string userId, "hydrate")) {
            return;
        }

        string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment, action.ViewKey);
        GridViewPersistenceBlob? blob;
        try {
            blob = await _storage.GetAsync<GridViewPersistenceBlob>(key).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("DataGrid on-demand hydrate cancelled — circuit disposing.");
            return;
        }
        catch (Exception ex) {
            _logger.LogInformation(
                ex,
                "{DiagnosticId}: DataGrid on-demand hydrate failed. Reason={Reason}. ViewKey={ViewKey}.",
                FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                "Corrupt",
                action.ViewKey);
            return;
        }

        if (blob is null) {
            _logger.LogInformation(
                "{DiagnosticId}: DataGrid on-demand hydrate found no stored value. Reason={Reason}. ViewKey={ViewKey}.",
                FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                "Empty",
                action.ViewKey);
            return;
        }

        dispatcher.Dispatch(new GridViewHydratedAction(action.ViewKey, blob.ToSnapshot()));
    }

    /// <inheritdoc />
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) {
            return;
        }

        foreach (KeyValuePair<string, CancellationTokenSource> kvp in _pending) {
            TryCancelAndDispose(kvp.Value);
        }

        _pending.Clear();
    }

    private async Task HydrateAsync(IDispatcher dispatcher) {
        if (!TryResolveScope(out string tenantId, out string userId, "hydrate")) {
            return;
        }

        dispatcher.Dispatch(new DataGridNavigationHydratingAction());

        string prefix = StorageKeys.BuildKey(tenantId, userId, FeatureSegment) + ":";
        IReadOnlyList<string> keys;
        try {
            keys = await _storage.GetKeysAsync(prefix).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("DataGrid hydrate cancelled — circuit disposing.");
            dispatcher.Dispatch(new DataGridNavigationHydratedCompletedAction());
            return;
        }
        catch (Exception ex) {
            _logger.LogInformation(
                ex,
                "{DiagnosticId}: DataGrid hydrate key enumeration failed — hydrate abandoned. Direction={Direction}.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                "hydrate");
            dispatcher.Dispatch(new DataGridNavigationHydratedCompletedAction());
            return;
        }

        HashSet<string> registeredBcs = ResolveRegisteredBoundedContexts(out bool registryFailed);
        foreach (string key in keys) {
            if (IsDisposed()) {
                break;
            }

            if (!TryExtractViewKey(key, prefix, out string viewKey)) {
                _logger.LogInformation(
                    "{DiagnosticId}: DataGrid per-key hydrate rejected malformed storage key. Reason={Reason}. StorageKey={StorageKey}.",
                    FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                    "Corrupt",
                    key);

                try {
                    await _storage.RemoveAsync(key).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    _logger.LogDebug("DataGrid malformed-key prune cancelled — circuit disposing.");
                    break;
                }
                catch (Exception ex) {
                    _logger.LogInformation(
                        ex,
                        "{DiagnosticId}: DataGrid malformed-key prune failed. StorageKey={StorageKey}.",
                        FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                        key);
                }

                continue;
            }

            if (!registryFailed && IsOutOfScope(viewKey, registeredBcs)) {
                if (_loggedOutOfScope.TryAdd(viewKey, 0)) {
                    _logger.LogInformation(
                        "{DiagnosticId}: Pruning stale DataGrid snapshot — bounded context is no longer registered. Reason={Reason}. ViewKey={ViewKey}.",
                        FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                        "OutOfScope",
                        viewKey);
                }

                try {
                    await _storage.RemoveAsync(key).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    _logger.LogDebug("DataGrid out-of-scope prune cancelled — circuit disposing.");
                }
                catch (Exception ex) {
                    _logger.LogInformation(
                        ex,
                        "{DiagnosticId}: DataGrid out-of-scope prune failed. ViewKey={ViewKey}.",
                        FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                        viewKey);
                }

                continue;
            }

            GridViewPersistenceBlob? blob;
            try {
                blob = await _storage.GetAsync<GridViewPersistenceBlob>(key).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                _logger.LogDebug("DataGrid per-key hydrate cancelled — circuit disposing.");
                break;
            }
            catch (Exception ex) {
                _logger.LogInformation(
                    ex,
                    "{DiagnosticId}: DataGrid per-key hydrate failed. Reason={Reason}. ViewKey={ViewKey}.",
                    FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                    "Corrupt",
                    viewKey);
                continue;
            }

            if (blob is null) {
                _logger.LogInformation(
                    "{DiagnosticId}: DataGrid per-key hydrate found no blob at enumerated key. Reason={Reason}. ViewKey={ViewKey}.",
                    FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                    "Empty",
                    viewKey);
                continue;
            }

            GridViewSnapshot snapshot;
            try {
                snapshot = blob.ToSnapshot();
            }
            catch (Exception ex) {
                _logger.LogInformation(
                    ex,
                    "{DiagnosticId}: DataGrid per-key hydrate rejected by snapshot invariants. Reason={Reason}. ViewKey={ViewKey}.",
                    FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                    "Corrupt",
                    viewKey);
                continue;
            }

            dispatcher.Dispatch(new GridViewHydratedAction(viewKey, snapshot));
        }

        dispatcher.Dispatch(new DataGridNavigationHydratedCompletedAction());
    }

    private HashSet<string> ResolveRegisteredBoundedContexts(out bool registryFailed) {
        registryFailed = false;
        HashSet<string> result = new(StringComparer.Ordinal);
        if (_registry is null) {
            registryFailed = true;
            _logger.LogInformation(
                "{DiagnosticId}: DataGrid hydrate — registry unavailable (null), out-of-scope pruning skipped. Reason={Reason}.",
                FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                "RegistryFailure");
            return result;
        }

        try {
            IReadOnlyList<DomainManifest> manifests = _registry.GetManifests();
            foreach (DomainManifest manifest in manifests) {
                if (!string.IsNullOrWhiteSpace(manifest.BoundedContext)) {
                    result.Add(manifest.BoundedContext);
                }
            }
        }
        catch (Exception ex) {
            registryFailed = true;
            _logger.LogInformation(
                ex,
                "{DiagnosticId}: Registry enumeration failed during DataGrid hydrate — out-of-scope pruning abandoned for this pass. Reason={Reason}.",
                FcDiagnosticIds.HFC2114_DataGridHydrationEmpty,
                "RegistryFailure");
        }

        return result;
    }

    private static bool IsOutOfScope(string viewKey, HashSet<string> registeredBcs) {
        int separator = viewKey.IndexOf(':');
        if (separator <= 0 || separator == viewKey.Length - 1) {
            return false;
        }

        string bc = viewKey[..separator];
        return !registeredBcs.Contains(bc);
    }

    private static bool TryExtractViewKey(string storageKey, string prefix, out string viewKey) {
        viewKey = string.Empty;
        if (!storageKey.StartsWith(prefix, StringComparison.Ordinal)) {
            return false;
        }

        string candidate = storageKey[prefix.Length..];
        if (!IsValidViewKey(candidate)) {
            return false;
        }

        viewKey = candidate;
        return true;
    }

    private static bool IsValidViewKey(string viewKey) {
        if (string.IsNullOrWhiteSpace(viewKey)) {
            return false;
        }

        int separator = viewKey.IndexOf(':');
        return separator > 0
            && separator < viewKey.Length - 1
            && !string.IsNullOrWhiteSpace(viewKey[..separator])
            && !string.IsNullOrWhiteSpace(viewKey[(separator + 1)..]);
    }

    private bool IsDisposed() => Volatile.Read(ref _disposed) == 1;

    private static void TryCancelAndDispose(CancellationTokenSource cts) {
        try {
            cts.Cancel();
        }
        catch (ObjectDisposedException) {
            // Already disposed — nothing to cancel.
        }
        finally {
            cts.Dispose();
        }
    }

    private bool TryResolveScope(out string tenantId, out string userId, string direction) {
        string? rawTenant = _userContextAccessor.TenantId;
        string? rawUser = _userContextAccessor.UserId;
        if (string.IsNullOrWhiteSpace(rawTenant) || string.IsNullOrWhiteSpace(rawUser)) {
            _logger.LogInformation(
                "{DiagnosticId}: DataGrid {Direction} skipped — null/empty/whitespace tenant or user context.",
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

    // Retained for future source-gen migration (D18). Unused until Story 9-x.
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1823:Avoid unused private fields",
        Justification = "JsonOptions is reserved for future JsonSerializable source-gen migration (D18).")]
    private static JsonSerializerOptions CurrentJsonOptions() => JsonOptions;
}
