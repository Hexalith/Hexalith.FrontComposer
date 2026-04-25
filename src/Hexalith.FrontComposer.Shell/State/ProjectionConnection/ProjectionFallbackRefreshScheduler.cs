using System.Collections.Concurrent;
using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Visible projection lane metadata used by bounded fallback polling.</summary>
public sealed record ProjectionFallbackLane(
    string ViewKey,
    int Skip,
    int Take,
    IImmutableDictionary<string, string> Filters,
    string? SortColumn,
    bool SortDescending,
    string? SearchQuery,
    string? TenantId = null);

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
    private readonly ConcurrentDictionary<string, ProjectionFallbackLane> _lanes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, byte> _inFlight = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IDisposable RegisterLane(ProjectionFallbackLane lane) {
        ArgumentNullException.ThrowIfNull(lane);
        ValidateLane(lane);
        _lanes[lane.ViewKey] = lane;
        return new Registration(() => _ = _lanes.TryRemove(lane.ViewKey, out _));
    }

    /// <inheritdoc />
    public async Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
        FcShellOptions current = options.CurrentValue;
        if (current.ProjectionFallbackPollingIntervalSeconds <= 0 || !connectionState.Current.IsDisconnected) {
            return 0;
        }

        return await RefreshMatchingAsync(
            _lanes.Values.OrderBy(static lane => lane.ViewKey, StringComparer.Ordinal),
            current.MaxProjectionFallbackPollingLanes,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
        string safeProjectionType = EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType));
        string safeTenantId = EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId));
        IEnumerable<ProjectionFallbackLane> matches = _lanes.Values
            .Where(lane => string.Equals(ExtractProjectionTypeFqn(lane.ViewKey), safeProjectionType, StringComparison.Ordinal)
                && string.Equals(lane.TenantId, safeTenantId, StringComparison.Ordinal))
            .OrderBy(static lane => lane.ViewKey, StringComparer.Ordinal);
        return RefreshMatchingAsync(matches, options.CurrentValue.MaxProjectionFallbackPollingLanes, cancellationToken);
    }

    private async Task<int> RefreshMatchingAsync(
        IEnumerable<ProjectionFallbackLane> lanes,
        int maxLanes,
        CancellationToken cancellationToken) {
        int refreshed = 0;
        foreach (ProjectionFallbackLane lane in lanes.Take(Math.Max(0, maxLanes))) {
            if (await RefreshLaneAsync(lane, cancellationToken).ConfigureAwait(false)) {
                refreshed++;
            }
        }

        return refreshed;
    }

    private async Task<bool> RefreshLaneAsync(ProjectionFallbackLane lane, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_inFlight.TryAdd(lane.ViewKey, 0)) {
            return false;
        }

        try {
            string projectionType = ExtractProjectionTypeFqn(lane.ViewKey);
            _ = await loader.LoadPageAsync(
                projectionType,
                lane.Skip,
                lane.Take,
                lane.Filters,
                lane.SortColumn,
                lane.SortDescending,
                lane.SearchQuery,
                cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            logger.LogWarning(
                ex,
                "Projection refresh failed. FailureCategory={FailureCategory}",
                ex.GetType().Name);
            return false;
        }
        finally {
            _ = _inFlight.TryRemove(lane.ViewKey, out _);
        }
    }

    private static void ValidateLane(ProjectionFallbackLane lane) {
        if (string.IsNullOrWhiteSpace(lane.ViewKey)) {
            throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(lane));
        }

        if (lane.Skip < 0) {
            throw new ArgumentOutOfRangeException(nameof(lane), lane.Skip, "Skip must be non-negative.");
        }

        if (lane.Take <= 0) {
            throw new ArgumentOutOfRangeException(nameof(lane), lane.Take, "Take must be positive.");
        }
    }

    private static string ExtractProjectionTypeFqn(string viewKey) {
        int separator = viewKey.IndexOf(':');
        return separator > 0 && separator < viewKey.Length - 1
            ? viewKey[(separator + 1)..]
            : viewKey;
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
