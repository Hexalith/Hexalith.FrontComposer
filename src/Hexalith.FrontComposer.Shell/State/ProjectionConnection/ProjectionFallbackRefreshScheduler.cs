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

            if (await RefreshLaneAsync(entry.Lane, cancellationToken).ConfigureAwait(false)) {
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

            if (await RefreshLaneAsync(entry.Lane, cancellationToken).ConfigureAwait(false)) {
                refreshed++;
            }
        }

        return refreshed;
    }

    private async Task<bool> RefreshLaneAsync(ProjectionFallbackLane lane, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_inFlight.TryAdd(lane.ViewKey, 0)) {
            // P1 — mark pending so the final nudge gets a replay after the in-flight refresh
            // resolves. The dedupe window must not drop the last nudge after a failure.
            _pendingRetry[lane.ViewKey] = 0;
            return false;
        }

        bool succeeded;
        try {
            _ = await loader.LoadPageAsync(
                lane.ProjectionType,
                lane.Skip,
                lane.Take,
                lane.Filters,
                lane.SortColumn,
                lane.SortDescending,
                lane.SearchQuery,
                cancellationToken).ConfigureAwait(false);
            succeeded = true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            // P5 — log only the redacted exception type. Raw exception messages can carry
            // tenant/group/payload data; structured `FailureCategory` keeps logs bounded.
            logger.LogWarning(
                "Projection refresh failed. FailureCategory={FailureCategory}",
                ex.GetType().Name);
            succeeded = false;
        }
        finally {
            _ = _inFlight.TryRemove(lane.ViewKey, out _);
        }

        // P1 — if a nudge arrived during the in-flight window, replay exactly once. Pending is
        // cleared before retry so further bursts during the retry can themselves enqueue a
        // single follow-up.
        if (_pendingRetry.TryRemove(lane.ViewKey, out _) && !cancellationToken.IsCancellationRequested) {
            return await RefreshLaneAsync(lane, cancellationToken).ConfigureAwait(false);
        }

        return succeeded;
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

    private sealed class LaneEntry(ProjectionFallbackLane lane) {
        public ProjectionFallbackLane Lane { get; } = lane;
        public int RefCount = 1;
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
