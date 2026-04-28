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

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Visible projection lane metadata used by bounded fallback polling.</summary>
/// <remarks>
/// Story 5-3 review DN4 — `ProjectionType` and `TenantId` are first-class fields so nudge routing
/// no longer relies on parsing `ViewKey`. Adopters MUST supply both; `ViewKey` remains the
/// dedupe identity for refcounted registrations.
/// </remarks>
public sealed record ProjectionFallbackLane(
    string ViewKey,
    string ProjectionType,
    string? TenantId,
    int Skip,
    int Take,
    IImmutableDictionary<string, string> Filters,
    string? SortColumn,
    bool SortDescending,
    string? SearchQuery,
    Func<CancellationToken, ValueTask<ProjectionFallbackLaneRefreshOutcome>>? RefreshAsync = null);

/// <summary>Result produced by custom visible-lane refresh callbacks.</summary>
public enum ProjectionFallbackLaneRefreshOutcome {
    Skipped,
    NotModified,
    Changed,
}

/// <summary>Projection group health observed during the latest reconnect rejoin pass.</summary>
public readonly record struct ProjectionFallbackGroupKey(string ProjectionType, string TenantId);

/// <summary>Registers visible projection lanes and refreshes them while realtime is unavailable.</summary>
public interface IProjectionFallbackRefreshScheduler {
    IDisposable RegisterLane(ProjectionFallbackLane lane);

    Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default);

    Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default);

    Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default)
        => Task.FromResult(ProjectionReconciliationRefreshResult.Empty);

    void SetReconciliationGroupHealth(IReadOnlyDictionary<ProjectionFallbackGroupKey, bool> activeGroups) {
    }
}

/// <summary>Summary returned by an epoch-scoped reconnect reconciliation pass.</summary>
public sealed record ProjectionReconciliationRefreshResult(int RefreshedCount, IReadOnlyList<string> ChangedViewKeys) {
    public static ProjectionReconciliationRefreshResult Empty { get; } = new(0, []);
}

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

        // P2 — refcount duplicate registrations on the same ViewKey instead of overwriting.
        // The first disposer to drop the count to zero removes the entry; cross-component
        // disposers no longer cancel each other.
        LaneEntry entry = _lanes.AddOrUpdate(
            lane.ViewKey,
            static (_, l) => new LaneEntry(l),
            static (_, existing, _) => {
                Interlocked.Increment(ref existing.RefCount);
                return existing;
            },
            lane);

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

            if (await RefreshLaneAsync(entry.Lane, cancellationToken).ConfigureAwait(false) is not ProjectionLaneRefreshResult.Skipped) {
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

            if (await RefreshLaneAsync(entry.Lane, cancellationToken).ConfigureAwait(false) is not ProjectionLaneRefreshResult.Skipped) {
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
            logger.LogWarning(
                "Reconciliation budget is zero (MaxProjectionFallbackPollingLanes={Budget}); reconcile pass skipped.",
                budget);
            return ProjectionReconciliationRefreshResult.Empty;
        }

        // P28 — apply lane cap AFTER dedupe so two lanes that hash to the same dedupe key do
        // not consume the budget twice.
        ProjectionFallbackLane[] orderedLanes = _lanes.Values
            .Select(static entry => entry.Lane)
            .OrderBy(static lane => lane.ViewKey, StringComparer.Ordinal)
            .ToArray();

        List<string> changed = [];
        int refreshed = 0;
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (ProjectionFallbackLane lane in orderedLanes) {
            cancellationToken.ThrowIfCancellationRequested();
            if (refreshed >= budget) {
                break;
            }

            // P29 — fail-closed: lanes without a tenant must not enter the reconciliation pass.
            // The cache layer would itself fail-closed on missing tenant, but we want this to
            // surface as a structured "lane skipped" diagnostic rather than as a silent miss.
            if (string.IsNullOrWhiteSpace(lane.TenantId)) {
                logger.LogInformation(
                    "Reconciliation skipped lane without tenant context. ViewKey={ViewKey}, ProjectionType={ProjectionType}",
                    lane.ViewKey,
                    lane.ProjectionType);
                continue;
            }

            if (!seen.Add(BuildDedupeKey(lane))) {
                continue;
            }

            if (!IsGroupEligibleForReconciliation(lane)) {
                logger.LogInformation(
                    "Reconciliation skipped lane for degraded projection group. ViewKey={ViewKey}, ProjectionType={ProjectionType}",
                    lane.ViewKey,
                    lane.ProjectionType);
                continue;
            }

            ProjectionLaneRefreshResult result = await RefreshLaneAsync(lane, cancellationToken).ConfigureAwait(false);
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

    private async Task<ProjectionLaneRefreshResult> RefreshLaneAsync(ProjectionFallbackLane lane, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
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
                outcome = ClassifyRefreshResult(lane, result);
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

        // P1 — if a nudge arrived during the in-flight window, replay exactly once. Pending is
        // cleared before retry so further bursts during the retry can themselves enqueue a
        // single follow-up. P24 — recursion is bounded to depth 1 because _pendingRetry is
        // cleared atomically and only one replay token can be set per lane.
        if (_pendingRetry.TryRemove(lane.ViewKey, out _) && !cancellationToken.IsCancellationRequested) {
            return await RefreshLaneAsync(lane, cancellationToken).ConfigureAwait(false);
        }

        return outcome;
    }

    /// <summary>
    /// DN4=b — classify the refresh result as Changed/NotModified using the wire-level ETag as
    /// the canonical change signal. P25 — guard against negative TotalCount which indicates a
    /// protocol failure rather than data, and never let a malformed value drive a Changed.
    /// P23 — null-guard before any dereference of result.Items in the comparison branches.
    /// </summary>
    private ProjectionLaneRefreshResult ClassifyRefreshResult(ProjectionFallbackLane lane, ProjectionPageResult result) {
        if (result.IsNotModified) {
            dispatcher.Dispatch(new LoadPageNotModifiedAction(lane.ViewKey, lane.Skip, result.Items ?? []));
            return ProjectionLaneRefreshResult.NotModified;
        }

        // P25 — negative TotalCount is a protocol issue, not a Changed signal.
        if (result.TotalCount < 0) {
            logger.LogWarning(
                "Projection refresh returned negative TotalCount; treating as protocol failure. ViewKey={ViewKey}",
                lane.ViewKey);
            return ProjectionLaneRefreshResult.Skipped;
        }

        string laneIdentity = BuildDedupeKey(lane);
        string? newEtag = result.ETag;
        bool hadPrevious = _lastEtagByLane.TryGetValue(laneIdentity, out string? previousEtag);
        if (!string.IsNullOrEmpty(newEtag)) {
            _lastEtagByLane[laneIdentity] = newEtag;
        }

        bool reducerVisibleDelta = HasReducerVisibleDelta(lane, result);
        if (string.IsNullOrEmpty(newEtag)) {
            string signature = BuildNoEtagSignature(result);
            bool signatureChanged = !_lastNoEtagSignatureByLane.TryGetValue(laneIdentity, out string? previousSignature)
                || !string.Equals(previousSignature, signature, StringComparison.Ordinal);
            _lastNoEtagSignatureByLane[laneIdentity] = signature;
            if (signatureChanged && reducerVisibleDelta) {
                DispatchPageSuccess(lane, result);
                return ProjectionLaneRefreshResult.Changed;
            }

            return ProjectionLaneRefreshResult.NotModified;
        }

        if (!hadPrevious || !string.Equals(previousEtag, newEtag, StringComparison.Ordinal)) {
            if (reducerVisibleDelta) {
                DispatchPageSuccess(lane, result);
                return ProjectionLaneRefreshResult.Changed;
            }

            return ProjectionLaneRefreshResult.NotModified;
        }

        return ProjectionLaneRefreshResult.NotModified;
    }

    private void DecrementLane(string viewKey, LaneEntry expected) {
        if (!_lanes.TryGetValue(viewKey, out LaneEntry? current) || !ReferenceEquals(current, expected)) {
            return;
        }

        if (Interlocked.Decrement(ref current.RefCount) <= 0) {
            // Best-effort removal: only succeed when the entry is still the one we owned.
            // KeyValuePair-based TryRemove avoids a TOCTOU race with a concurrent registration
            // that would have replaced the entry under a same ViewKey.
            ICollection<KeyValuePair<string, LaneEntry>> col = _lanes;
            _ = col.Remove(new KeyValuePair<string, LaneEntry>(viewKey, current));
            // Drop the cached ETag for the removed lane so a re-registration with the same key
            // starts from a clean delta-detection slate.
            string laneIdentity = BuildDedupeKey(expected.Lane);
            _ = _lastEtagByLane.TryRemove(laneIdentity, out _);
            _ = _lastNoEtagSignatureByLane.TryRemove(laneIdentity, out _);
        }
    }

    private static void ValidateLane(ProjectionFallbackLane lane) {
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

    private bool HasReducerVisibleDelta(ProjectionFallbackLane lane, ProjectionPageResult result) {
        (string ViewKey, int Skip) pageKey = (lane.ViewKey, lane.Skip);
        LoadedPageState state = loadedPages.Value;
        bool hasPage = state.PagesByKey.TryGetValue(pageKey, out IReadOnlyList<object>? currentItems);
        bool totalSame = state.TotalCountByKey.TryGetValue(lane.ViewKey, out int currentTotal)
            && currentTotal == result.TotalCount;

        if (!hasPage) {
            return (result.Items?.Count ?? 0) > 0 || result.TotalCount > 0;
        }

        return !totalSame || !ReferenceEquals(currentItems, result.Items);
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

    private static string BuildNoEtagSignature(ProjectionPageResult result)
        => string.Concat(
            result.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "|",
            (result.Items?.Count ?? 0).ToString(System.Globalization.CultureInfo.InvariantCulture));

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
