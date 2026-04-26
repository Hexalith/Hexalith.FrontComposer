using System.Collections.Concurrent;
using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
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
    string? SearchQuery);

/// <summary>Registers visible projection lanes and refreshes them while realtime is unavailable.</summary>
public interface IProjectionFallbackRefreshScheduler {
    IDisposable RegisterLane(ProjectionFallbackLane lane);

    Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default);

    Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default);

    Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default)
        => Task.FromResult(ProjectionReconciliationRefreshResult.Empty);
}

/// <summary>Summary returned by an epoch-scoped reconnect reconciliation pass.</summary>
public sealed record ProjectionReconciliationRefreshResult(int RefreshedCount, IReadOnlyList<string> ChangedViewKeys) {
    public static ProjectionReconciliationRefreshResult Empty { get; } = new(0, []);
}

/// <summary>Bounded fallback refresh scheduler that reuses the Story 5-2 page loader seam.</summary>
public sealed class ProjectionFallbackRefreshScheduler(
    IProjectionConnectionState connectionState,
    IProjectionPageLoader loader,
    IOptionsMonitor<FcShellOptions> options,
    ILogger<ProjectionFallbackRefreshScheduler> logger) : IProjectionFallbackRefreshScheduler {
    private readonly ConcurrentDictionary<string, LaneEntry> _lanes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _pendingRetry = new(StringComparer.Ordinal);
    // DN4=b — last-known ETag per lane key. Compared against the response ETag on each refresh
    // to detect a wire-level data change without relying on per-item Equals (which falls back
    // to reference equality for class-typed adopter projections).
    private readonly ConcurrentDictionary<string, string> _lastEtagByLane = new(StringComparer.Ordinal);
    private long _latestObservedEpoch;

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
    public async Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
        FcShellOptions current = options.CurrentValue;
        if (current.ProjectionFallbackPollingIntervalSeconds <= 0 || !connectionState.Current.IsDisconnected) {
            return 0;
        }

        int refreshed = 0;
        int budget = Math.Max(0, current.MaxProjectionFallbackPollingLanes);
        foreach (LaneEntry entry in _lanes.Values.OrderBy(static x => x.Lane.ViewKey, StringComparer.Ordinal)) {
            if (refreshed >= budget) {
                break;
            }

            // P20 — re-check disconnected per-lane so an in-progress sweep stops promptly when
            // the hub reconnects mid-loop. AC7 explicitly requires "stops when the hub reconnects".
            if (!connectionState.Current.IsDisconnected) {
                break;
            }

            if (await RefreshLaneAsync(entry.Lane, cancellationToken).ConfigureAwait(false) is not ProjectionLaneRefreshResult.Skipped) {
                refreshed++;
            }
        }

        return refreshed;
    }

    /// <inheritdoc />
    public async Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
        string safeProjectionType = EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType));
        string safeTenantId = EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId));
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

        return refreshed;
    }

    /// <inheritdoc />
    public async Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        // P21 — track latest observed epoch and short-circuit superseded passes early.
        long previous = Interlocked.Exchange(ref _latestObservedEpoch, epoch);
        if (epoch < previous) {
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
        catch (Exception ex) when (ex is not OperationCanceledException) {
            // P5 — log only the redacted exception type. Raw exception messages can carry
            // tenant/group/payload data; structured `FailureCategory` keeps logs bounded.
            logger.LogWarning(
                "Projection refresh failed. FailureCategory={FailureCategory}",
                ex.GetType().Name);
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
            return ProjectionLaneRefreshResult.NotModified;
        }

        // P25 — negative TotalCount is a protocol issue, not a Changed signal.
        if (result.TotalCount < 0) {
            logger.LogWarning(
                "Projection refresh returned negative TotalCount; treating as protocol failure. ViewKey={ViewKey}",
                lane.ViewKey);
            return ProjectionLaneRefreshResult.Skipped;
        }

        string? newEtag = result.ETag;
        bool hadPrevious = _lastEtagByLane.TryGetValue(lane.ViewKey, out string? previousEtag);
        if (!string.IsNullOrEmpty(newEtag)) {
            _lastEtagByLane[lane.ViewKey] = newEtag;
        }

        // No ETag at all: fall back to "any visible data" — the wire didn't give us a delta
        // signal, so we must conservatively treat a non-empty page as Changed on the first
        // observation only.
        if (string.IsNullOrEmpty(newEtag)) {
            if (!hadPrevious) {
                return (result.Items?.Count ?? 0) > 0 || result.TotalCount > 0
                    ? ProjectionLaneRefreshResult.Changed
                    : ProjectionLaneRefreshResult.NotModified;
            }

            // We had a previous ETag but the new response carries none — treat as Changed
            // conservatively.
            return ProjectionLaneRefreshResult.Changed;
        }

        // First observation for this lane in the current circuit — record and return Changed
        // only when there is data to surface.
        if (!hadPrevious) {
            return (result.Items?.Count ?? 0) > 0 || result.TotalCount > 0
                ? ProjectionLaneRefreshResult.Changed
                : ProjectionLaneRefreshResult.NotModified;
        }

        return string.Equals(previousEtag, newEtag, StringComparison.Ordinal)
            ? ProjectionLaneRefreshResult.NotModified
            : ProjectionLaneRefreshResult.Changed;
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
            _ = _lastEtagByLane.TryRemove(viewKey, out _);
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
            return string.Concat("__no-tenant__|", lane.ViewKey);
        }

        // Stable filter representation: sort by key then concatenate. Empty filters produce an
        // empty section so the dedupe key remains compact for the common case.
        string filtersFingerprint = lane.Filters is { Count: > 0 }
            ? string.Join(
                ",",
                lane.Filters
                    .OrderBy(static kv => kv.Key, StringComparer.Ordinal)
                    .Select(static kv => $"{kv.Key}={kv.Value}"))
            : string.Empty;

        return string.Concat(
            lane.TenantId,
            "|",
            lane.ProjectionType,
            "|",
            lane.Skip.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "|",
            lane.Take.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "|",
            lane.SortColumn ?? string.Empty,
            "|",
            lane.SortDescending ? "desc" : "asc",
            "|",
            lane.SearchQuery ?? string.Empty,
            "|",
            filtersFingerprint);
    }

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
}
