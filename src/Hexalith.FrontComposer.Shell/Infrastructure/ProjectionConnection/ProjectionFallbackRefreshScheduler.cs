using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;

/// <summary>Bounded fallback refresh scheduler that reuses the Story 5-2 page loader seam.</summary>
public sealed class ProjectionFallbackRefreshScheduler(
    IProjectionConnectionState connectionState,
    IProjectionPageLoader loader,
    IDispatcher dispatcher,
    IState<LoadedPageState> loadedPages,
    IOptionsMonitor<FcShellOptions> options,
    ILogger<ProjectionFallbackRefreshScheduler> logger) : IProjectionFallbackRefreshScheduler {
    private readonly ConcurrentDictionary<string, LaneEntry> _lanes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _pendingRetry = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<ProjectionFallbackGroupKey, bool> _activeGroups = new();
    // DN4=b — last-known ETag per lane key. Compared against the response ETag on each refresh
    // to detect a wire-level data change without relying on per-item Equals (which falls back
    // to reference equality for class-typed adopter projections).
    private readonly ConcurrentDictionary<string, string> _lastEtagByLane = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _lastNoEtagSignatureByLane = new(StringComparer.Ordinal);
    private readonly object _laneGate = new();
    private long _latestObservedEpoch;

    public ProjectionFallbackRefreshScheduler(
        IProjectionConnectionState connectionState,
        IProjectionPageLoader loader,
        IOptionsMonitor<FcShellOptions> options,
        ILogger<ProjectionFallbackRefreshScheduler> logger)
        : this(connectionState, loader, new NoopDispatcher(), new StaticLoadedPageState(), options, logger) {
    }

    /// <inheritdoc />
    public IDisposable RegisterLane(ProjectionFallbackLane lane) {
        ArgumentNullException.ThrowIfNull(lane);
        ValidateLane(lane);

        LaneEntry entry;
        lock (_laneGate) {
            if (_lanes.TryGetValue(lane.ViewKey, out LaneEntry? existing)) {
                if (!LaneContractsAreEquivalent(existing.Lane, lane)) {
                    throw new InvalidOperationException("A conflicting fallback lane contract is already registered for this view.");
                }

                existing.RefCount++;
                entry = existing;
            }
            else {
                entry = new LaneEntry(lane);
                if (!_lanes.TryAdd(lane.ViewKey, entry)) {
                    throw new InvalidOperationException("The fallback lane registration could not be completed.");
                }
            }
        }

        return new Registration(() => DecrementLane(lane.ViewKey, entry));
    }

    /// <inheritdoc />
    public void SetReconciliationGroupHealth(IReadOnlyDictionary<ProjectionFallbackGroupKey, bool> activeGroups) {
        ArgumentNullException.ThrowIfNull(activeGroups);
        _activeGroups.Clear();
        foreach (KeyValuePair<ProjectionFallbackGroupKey, bool> group in activeGroups) {
            _activeGroups[group.Key] = group.Value;
        }
    }

    /// <inheritdoc />
    public async Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
        using Activity? activity = FrontComposerTelemetry.StartProjectionFallbackPoll();
        FcShellOptions current = options.CurrentValue;
        if (current.ProjectionFallbackPollingIntervalSeconds <= 0 || !connectionState.Current.IsDisconnected) {
            FrontComposerTelemetry.SetOutcome(activity, "skipped");
            return 0;
        }

        int refreshed = 0;
        int budget = Math.Max(0, current.MaxProjectionFallbackPollingLanes);
        // F12 — track whether the hub reconnected mid-loop so the outer span can be tagged
        // `outcome=stale_after_reconnect` when an in-progress sweep stopped due to
        // reconnection rather than hitting the budget cap.
        bool reconnectedDuringLoop = false;
        foreach (LaneEntry entry in _lanes.Values.OrderBy(static x => x.Lane.ViewKey, StringComparer.Ordinal)) {
            if (refreshed >= budget) {
                break;
            }

            // P20 — re-check disconnected per-lane so an in-progress sweep stops promptly when
            // the hub reconnects mid-loop. AC7 explicitly requires "stops when the hub reconnects".
            if (!connectionState.Current.IsDisconnected) {
                reconnectedDuringLoop = true;
                break;
            }

            if (await RefreshLaneAsync(entry, cancellationToken).ConfigureAwait(false) is not ProjectionLaneRefreshResult.Skipped) {
                refreshed++;
            }
        }

        string finalOutcome = reconnectedDuringLoop
            ? "stale_after_reconnect"
            : refreshed > 0 ? "refreshed" : "empty";
        FrontComposerTelemetry.SetOutcome(activity, finalOutcome);
        return refreshed;
    }

    /// <inheritdoc />
    public async Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
        string safeProjectionType = EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType));
        string safeTenantId = EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId));
        using Activity? activity = FrontComposerTelemetry.StartProjectionNudge(
            safeProjectionType,
            FrontComposerTelemetry.TenantMarker(safeTenantId));
        int budget = Math.Max(0, options.CurrentValue.MaxProjectionFallbackPollingLanes);
        int refreshed = 0;
        IEnumerable<LaneEntry> matches = _lanes.Values
            .Where(entry => string.Equals(entry.Lane.ProjectionType, safeProjectionType, StringComparison.Ordinal)
                && string.Equals(entry.Lane.TenantId, safeTenantId, StringComparison.Ordinal))
            .OrderBy(static entry => entry.Lane.ViewKey, StringComparer.Ordinal);
        foreach (LaneEntry entry in matches) {
            if (refreshed >= budget) {
                break;
            }

            if (await RefreshLaneAsync(entry, cancellationToken).ConfigureAwait(false) is not ProjectionLaneRefreshResult.Skipped) {
                refreshed++;
            }
        }

        FrontComposerTelemetry.SetOutcome(activity, refreshed > 0 ? "refreshed" : "empty");
        return refreshed;
    }

    /// <inheritdoc />
    public async Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryObserveEpoch(epoch)) {
            return ProjectionReconciliationRefreshResult.Empty;
        }

        int budget = Math.Max(0, options.CurrentValue.MaxProjectionFallbackPollingLanes);
        // P22 — log when the configured budget is zero so misconfiguration is visible instead
        // of silently suppressing every reconcile pass.
        if (budget == 0) {
            FrontComposerHotPathLog.ReconciliationBudgetZero(
                logger,
                budget);
            return ProjectionReconciliationRefreshResult.Empty;
        }

        // P28 — apply lane cap AFTER dedupe so two lanes that hash to the same dedupe key do
        // not consume the budget twice.
        LaneEntry[] orderedLanes = _lanes.Values
            .OrderBy(static entry => entry.Lane.ViewKey, StringComparer.Ordinal)
            .ToArray();

        List<string> changed = [];
        int refreshed = 0;
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (LaneEntry entry in orderedLanes) {
            ProjectionFallbackLane lane = entry.Lane;
            cancellationToken.ThrowIfCancellationRequested();
            if (refreshed >= budget) {
                break;
            }

            // P29 — fail-closed: lanes without a tenant must not enter the reconciliation pass.
            // The cache layer would itself fail-closed on missing tenant, but we want this to
            // surface as a structured "lane skipped" diagnostic rather than as a silent miss.
            if (string.IsNullOrWhiteSpace(lane.TenantId)) {
                FrontComposerHotPathLog.ReconciliationLaneMissingTenant(
                    logger,
                    lane.ViewKey,
                    lane.ProjectionType);
                continue;
            }

            if (!seen.Add(BuildDedupeKey(lane))) {
                continue;
            }

            if (!IsGroupEligibleForReconciliation(lane)) {
                FrontComposerHotPathLog.ReconciliationLaneDegraded(
                    logger,
                    lane.ViewKey,
                    lane.ProjectionType);
                continue;
            }

            ProjectionLaneRefreshResult result = await RefreshLaneAsync(entry, cancellationToken).ConfigureAwait(false);
            if (result is ProjectionLaneRefreshResult.Skipped) {
                continue;
            }

            refreshed++;
            if (result is ProjectionLaneRefreshResult.Changed) {
                changed.Add(lane.ViewKey);
            }
        }

        return refreshed == 0 && changed.Count == 0
            ? ProjectionReconciliationRefreshResult.Empty
            : new ProjectionReconciliationRefreshResult(refreshed, changed);
    }

    private async Task<ProjectionLaneRefreshResult> RefreshLaneAsync(LaneEntry entry, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ProjectionFallbackLane lane = entry.Lane;
        if (!IsLaneActive(entry)) {
            return ProjectionLaneRefreshResult.Skipped;
        }

        if (!_inFlight.TryAdd(lane.ViewKey, 0)) {
            // P1 — mark pending so the final nudge gets a replay after the in-flight refresh
            // resolves. The dedupe window must not drop the last nudge after a failure.
            _pendingRetry[lane.ViewKey] = 0;
            return ProjectionLaneRefreshResult.Skipped;
        }

        ProjectionLaneRefreshResult outcome;
        try {
            if (lane.RefreshAsync is not null) {
                outcome = MapCustomOutcome(await lane.RefreshAsync(cancellationToken).ConfigureAwait(false));
            }
            else {
                ProjectionPageResult result = await loader.LoadPageAsync(
                    lane.ProjectionType,
                    lane.Skip,
                    lane.Take,
                    lane.Filters,
                    lane.SortColumn,
                    lane.SortDescending,
                    lane.SearchQuery,
                    cancellationToken).ConfigureAwait(false);
                outcome = ClassifyRefreshResult(entry, result);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            // P5 — log only the redacted exception type. Raw exception messages can carry
            // tenant/group/payload data; structured `FailureCategory` keeps logs bounded.
            FrontComposerLog.ProjectionRefreshFailed(logger, lane.ProjectionType, ex.GetType().Name);
            outcome = ProjectionLaneRefreshResult.Skipped;
        }
        finally {
            _ = _inFlight.TryRemove(lane.ViewKey, out _);
        }

        if (!IsLaneActive(entry)) {
            outcome = ProjectionLaneRefreshResult.Skipped;
        }

        // P1 — if a nudge arrived during the in-flight window, replay exactly once. Pending is
        // cleared before retry so further bursts during the retry can themselves enqueue a
        // single follow-up. P24 — recursion is bounded to depth 1 because _pendingRetry is
        // cleared atomically and only one replay token can be set per lane.
        if (_pendingRetry.TryRemove(lane.ViewKey, out _) && !cancellationToken.IsCancellationRequested) {
            LaneEntry? retryEntry = GetActiveLane(lane.ViewKey);
            if (retryEntry is not null) {
                return await RefreshLaneAsync(retryEntry, cancellationToken).ConfigureAwait(false);
            }
        }

        return outcome;
    }

    /// <summary>
    /// DN4=b — classify the refresh result as Changed/NotModified using the wire-level ETag as
    /// the canonical change signal. P25 — guard against negative TotalCount which indicates a
    /// protocol failure rather than data, and never let a malformed value drive a Changed.
    /// P23 — null-guard before any dereference of result.Items in the comparison branches.
    /// </summary>
    private ProjectionLaneRefreshResult ClassifyRefreshResult(LaneEntry entry, ProjectionPageResult result) {
        ProjectionFallbackLane lane = entry.Lane;
        if (!IsLaneActive(entry)) {
            return ProjectionLaneRefreshResult.Skipped;
        }

        bool reducerPageMissing = !HasReducerPage(lane);
        if (result.IsNotModified) {
            if (reducerPageMissing) {
                DispatchPageSuccess(lane, result);
                return ProjectionLaneRefreshResult.Changed;
            }

            dispatcher.Dispatch(new LoadPageNotModifiedAction(lane.ViewKey, lane.Skip, result.Items ?? []));
            return ProjectionLaneRefreshResult.NotModified;
        }

        // P25 — negative TotalCount is a protocol issue, not a Changed signal.
        if (result.TotalCount < 0) {
            FrontComposerHotPathLog.ProjectionRefreshNegativeCount(
                logger,
                lane.ViewKey);
            return ProjectionLaneRefreshResult.Skipped;
        }

        string laneIdentity = lane.ViewKey;
        string? newEtag = result.ETag;
        if (string.IsNullOrEmpty(newEtag)) {
            string signature = ProjectionFallbackRowSignature.Create(result, out bool signatureReliable);
            bool signatureChanged;
            lock (_laneGate) {
                if (!IsLaneActiveWithoutLock(entry)) {
                    return ProjectionLaneRefreshResult.Skipped;
                }

                _ = _lastEtagByLane.TryRemove(laneIdentity, out _);
                signatureChanged = !signatureReliable
                    || !_lastNoEtagSignatureByLane.TryGetValue(laneIdentity, out string? previousSignature)
                    || !string.Equals(previousSignature, signature, StringComparison.Ordinal);
                _lastNoEtagSignatureByLane[laneIdentity] = signature;
            }

            if (signatureChanged || reducerPageMissing) {
                DispatchPageSuccess(lane, result);
                return ProjectionLaneRefreshResult.Changed;
            }

            return ProjectionLaneRefreshResult.NotModified;
        }

        bool etagChanged;
        lock (_laneGate) {
            if (!IsLaneActiveWithoutLock(entry)) {
                return ProjectionLaneRefreshResult.Skipped;
            }

            bool hadPrevious = _lastEtagByLane.TryGetValue(laneIdentity, out string? previousEtag);
            etagChanged = !hadPrevious || !string.Equals(previousEtag, newEtag, StringComparison.Ordinal);
            _lastEtagByLane[laneIdentity] = newEtag;
            _ = _lastNoEtagSignatureByLane.TryRemove(laneIdentity, out _);
        }

        if (etagChanged || reducerPageMissing) {
            DispatchPageSuccess(lane, result);
            return ProjectionLaneRefreshResult.Changed;
        }

        return ProjectionLaneRefreshResult.NotModified;
    }

    private void DecrementLane(string viewKey, LaneEntry expected) {
        lock (_laneGate) {
            if (!_lanes.TryGetValue(viewKey, out LaneEntry? current) || !ReferenceEquals(current, expected)) {
                return;
            }

            current.RefCount--;
            if (current.RefCount == 0) {
                _ = _lanes.TryRemove(new KeyValuePair<string, LaneEntry>(viewKey, current));
                _ = _lastEtagByLane.TryRemove(viewKey, out _);
                _ = _lastNoEtagSignatureByLane.TryRemove(viewKey, out _);
            }
        }
    }

    private LaneEntry? GetActiveLane(string viewKey) {
        lock (_laneGate) {
            return _lanes.TryGetValue(viewKey, out LaneEntry? entry) ? entry : null;
        }
    }

    private bool IsLaneActive(LaneEntry entry) {
        lock (_laneGate) {
            return IsLaneActiveWithoutLock(entry);
        }
    }

    private bool IsLaneActiveWithoutLock(LaneEntry entry)
        => _lanes.TryGetValue(entry.Lane.ViewKey, out LaneEntry? current)
            && ReferenceEquals(current, entry);

    private static bool LaneContractsAreEquivalent(ProjectionFallbackLane first, ProjectionFallbackLane second)
        => string.Equals(first.ViewKey, second.ViewKey, StringComparison.Ordinal)
            && string.Equals(first.ProjectionType, second.ProjectionType, StringComparison.Ordinal)
            && string.Equals(first.TenantId, second.TenantId, StringComparison.Ordinal)
            && first.Skip == second.Skip
            && first.Take == second.Take
            && FiltersAreEquivalent(first.Filters, second.Filters)
            && string.Equals(first.SortColumn, second.SortColumn, StringComparison.Ordinal)
            && first.SortDescending == second.SortDescending
            && string.Equals(first.SearchQuery, second.SearchQuery, StringComparison.Ordinal)
            && ReferenceEquals(first.RefreshAsync, second.RefreshAsync);

    private static bool FiltersAreEquivalent(
        IImmutableDictionary<string, string> first,
        IImmutableDictionary<string, string> second) {
        if (first.Count != second.Count) {
            return false;
        }

        using IEnumerator<KeyValuePair<string, string>> firstEnumerator = first
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .GetEnumerator();
        using IEnumerator<KeyValuePair<string, string>> secondEnumerator = second
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .GetEnumerator();
        while (firstEnumerator.MoveNext() && secondEnumerator.MoveNext()) {
            if (!string.Equals(firstEnumerator.Current.Key, secondEnumerator.Current.Key, StringComparison.Ordinal)
                || !string.Equals(firstEnumerator.Current.Value, secondEnumerator.Current.Value, StringComparison.Ordinal)) {
                return false;
            }
        }

        return true;
    }

    private static void ValidateLane(ProjectionFallbackLane lane) {
        ArgumentNullException.ThrowIfNull(lane.Filters);

        if (string.IsNullOrWhiteSpace(lane.ViewKey)) {
            throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(lane));
        }

        if (string.IsNullOrWhiteSpace(lane.ProjectionType)) {
            throw new ArgumentException("ProjectionType cannot be null, empty, or whitespace.", nameof(lane));
        }

        if (lane.ProjectionType.Contains(':', StringComparison.Ordinal)) {
            throw new ArgumentException("ProjectionType must not contain ':' (reserved by EventStore group format).", nameof(lane));
        }

        if (lane.TenantId is not null && lane.TenantId.Contains(':', StringComparison.Ordinal)) {
            throw new ArgumentException("TenantId must not contain ':' (reserved by EventStore group format).", nameof(lane));
        }

        if (lane.Skip < 0) {
            throw new ArgumentOutOfRangeException(nameof(lane), lane.Skip, "Skip must be non-negative.");
        }

        if (lane.Take <= 0) {
            throw new ArgumentOutOfRangeException(nameof(lane), lane.Take, "Take must be positive.");
        }
    }

    /// <summary>
    /// P26 — include sort/filter/search state in the dedupe key so two lanes against the same
    /// projection-type/tenant/page-window but different filters do not coalesce. P27 — fail-closed
    /// on missing TenantId at the dedupe layer (defence-in-depth on top of the explicit lane skip
    /// in <see cref="TriggerReconciliationOnceAsync"/>).
    /// </summary>
    private static string BuildDedupeKey(ProjectionFallbackLane lane) {
        if (string.IsNullOrWhiteSpace(lane.TenantId)) {
            // The reconciliation pass already filters these out; this is defence-in-depth. The
            // unique key prevents two no-tenant lanes from accidentally collapsing.
            return string.Concat("__no-tenant__|", JsonEncodedText.Encode(lane.ViewKey).ToString());
        }

        // Stable filter representation with JSON escaping so delimiter characters in keys/values
        // cannot collapse distinct logical lanes.
        string filtersFingerprint = lane.Filters is { Count: > 0 }
            ? string.Join(
                "\u001f",
                lane.Filters
                    .OrderBy(static kv => kv.Key, StringComparer.Ordinal)
                    .Select(static kv => string.Concat(
                        JsonEncodedText.Encode(kv.Key).ToString(),
                        "\u001e",
                        JsonEncodedText.Encode(kv.Value).ToString())))
            : string.Empty;

        return string.Join(
            "\u001d",
            JsonEncodedText.Encode(lane.TenantId!).ToString(),
            JsonEncodedText.Encode(lane.ProjectionType).ToString(),
            lane.Skip.ToString(System.Globalization.CultureInfo.InvariantCulture),
            lane.Take.ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonEncodedText.Encode(lane.SortColumn ?? string.Empty).ToString(),
            lane.SortDescending ? "desc" : "asc",
            JsonEncodedText.Encode(lane.SearchQuery ?? string.Empty).ToString(),
            filtersFingerprint);
    }

    private bool TryObserveEpoch(long epoch) {
        while (true) {
            long observed = Volatile.Read(ref _latestObservedEpoch);
            if (epoch < observed) {
                return false;
            }

            if (Interlocked.CompareExchange(ref _latestObservedEpoch, epoch, observed) == observed) {
                return true;
            }
        }
    }

    private bool IsGroupEligibleForReconciliation(ProjectionFallbackLane lane) {
        if (string.IsNullOrWhiteSpace(lane.TenantId) || _activeGroups.IsEmpty) {
            return true;
        }

        ProjectionFallbackGroupKey key = new(lane.ProjectionType, lane.TenantId);
        return !_activeGroups.TryGetValue(key, out bool active) || active;
    }

    private bool HasReducerPage(ProjectionFallbackLane lane) {
        (string ViewKey, int Skip) pageKey = (lane.ViewKey, lane.Skip);
        return loadedPages.Value.PagesByKey.ContainsKey(pageKey);
    }

    private void DispatchPageSuccess(ProjectionFallbackLane lane, ProjectionPageResult result)
        => dispatcher.Dispatch(new LoadPageSucceededAction(
            lane.ViewKey,
            lane.Skip,
            result.Items,
            result.TotalCount,
            elapsedMs: 0));

    private static ProjectionLaneRefreshResult MapCustomOutcome(ProjectionFallbackLaneRefreshOutcome outcome)
        => outcome switch {
            ProjectionFallbackLaneRefreshOutcome.Changed => ProjectionLaneRefreshResult.Changed,
            ProjectionFallbackLaneRefreshOutcome.NotModified => ProjectionLaneRefreshResult.NotModified,
            _ => ProjectionLaneRefreshResult.Skipped,
        };

    private sealed class LaneEntry(ProjectionFallbackLane lane) {
        public ProjectionFallbackLane Lane { get; } = lane;
        public int RefCount = 1;
    }

    private enum ProjectionLaneRefreshResult {
        Skipped,
        NotModified,
        Changed,
    }

    private sealed class Registration(Action dispose) : IDisposable {
        private int _disposed;

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                dispose();
            }
        }
    }

    private sealed class NoopDispatcher : IDispatcher {
#pragma warning disable CS0067 // Required by Fluxor IDispatcher; no subscribers for fallback constructor.
        public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;
#pragma warning restore CS0067

        public void Dispatch(object action) {
        }
    }

    private sealed class StaticLoadedPageState : IState<LoadedPageState> {
        public LoadedPageState Value { get; } = new();

#pragma warning disable CS0067 // Static state never changes.
        public event EventHandler? StateChanged;
#pragma warning restore CS0067
    }
}
