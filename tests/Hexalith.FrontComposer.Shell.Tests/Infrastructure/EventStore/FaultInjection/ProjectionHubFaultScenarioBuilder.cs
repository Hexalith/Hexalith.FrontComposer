using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Readable scenario-builder facade over <see cref="FaultInjectingProjectionHubConnection"/>.
/// Each method documents the v0.1 fault inventory (start failure/delay, disconnect, reconnect,
/// closed-after-retry, join failure, leave failure, per-group drop/duplicate/delay, deterministic
/// reorder, partial delivery, cancellation, and disposal races). The builder never exposes
/// payload-bearing publication APIs; raw SignalR client types are not part of the surface.
/// </summary>
internal sealed class ProjectionHubFaultScenarioBuilder {
    private readonly FaultInjectingProjectionHubConnection _connection;

    public ProjectionHubFaultScenarioBuilder(FaultInjectingProjectionHubConnection connection) {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
    }

    public FaultInjectingProjectionHubConnection Connection => _connection;

    // ---------- Connection lifecycle ----------

    /// <summary>Connection is healthy at start. Default behaviour; no-op staging.</summary>
    public ProjectionHubFaultScenarioBuilder StartConnected() => this;

    /// <summary>Initial <c>StartAsync</c> faults with the supplied exception.</summary>
    public ProjectionHubFaultScenarioBuilder StartFails(Exception exception) {
        _connection.FailNext(HarnessCheckpoint.Start, exception);
        return this;
    }

    /// <summary>Initial <c>StartAsync</c> blocks until <c>ReleaseStart()</c> is called.</summary>
    public ProjectionHubFaultScenarioBuilder StartBlocks() {
        _connection.BlockUntil(HarnessCheckpoint.Start);
        return this;
    }

    public ProjectionHubFaultScenarioBuilder ReleaseStart() {
        _connection.Release(HarnessCheckpoint.Start);
        return this;
    }

    /// <summary>Initial <c>StartAsync</c> completes canceled when crossed.</summary>
    public ProjectionHubFaultScenarioBuilder StartCancels() {
        _connection.CancelNext(HarnessCheckpoint.Start);
        return this;
    }

    // ---------- Group join/leave ----------

    public ProjectionHubFaultScenarioBuilder JoinFails(string projectionType, string tenantId, Exception exception) {
        _connection.FailNext(HarnessCheckpoint.Join(projectionType, tenantId), exception);
        return this;
    }

    public ProjectionHubFaultScenarioBuilder JoinBlocks(string projectionType, string tenantId) {
        _connection.BlockUntil(HarnessCheckpoint.Join(projectionType, tenantId));
        return this;
    }

    public ProjectionHubFaultScenarioBuilder ReleaseJoin(string projectionType, string tenantId) {
        _connection.Release(HarnessCheckpoint.Join(projectionType, tenantId));
        return this;
    }

    public ProjectionHubFaultScenarioBuilder LeaveFails(string projectionType, string tenantId, Exception exception) {
        _connection.FailNext(HarnessCheckpoint.Leave(projectionType, tenantId), exception);
        return this;
    }

    // ---------- Connection state events ----------

    public Task RaiseReconnectingAsync(Exception? cause = null)
        => _connection.RaiseStateAsync(HarnessConnectionStates.Reconnecting(cause));

    public Task RaiseReconnectedAsync(string? connectionId = null)
        => _connection.RaiseStateAsync(HarnessConnectionStates.Reconnected(connectionId));

    public Task RaiseClosedAsync(Exception? cause = null)
        => _connection.RaiseStateAsync(HarnessConnectionStates.Closed(cause));

    public async Task RaiseClosedAfterRetryAsync(Exception cause) {
        // Equivalent to: server signaled Reconnecting then automatic reconnect failed → Closed.
        await _connection.RaiseStateAsync(HarnessConnectionStates.Reconnecting(cause)).ConfigureAwait(false);
        await _connection.RaiseStateAsync(HarnessConnectionStates.Closed(cause)).ConfigureAwait(false);
    }

    // ---------- Nudge faults ----------

    public ProjectionHubFaultScenarioBuilder DropNextNudge(string projectionType, string tenantId) {
        _connection.DropNextNudge(projectionType, tenantId);
        return this;
    }

    public ProjectionHubFaultScenarioBuilder DuplicateNextNudge(string projectionType, string tenantId, int count = 2) {
        _connection.DuplicateNextNudge(projectionType, tenantId, count);
        return this;
    }

    public NudgeQueueToken DelayNextNudge(string projectionType, string tenantId)
        => _connection.DelayNextNudge(projectionType, tenantId);

    public NudgeQueueToken Queue(string projectionType, string tenantId)
        => _connection.QueueNudge(projectionType, tenantId);

    public Task PublishNudgeAsync(string projectionType, string tenantId)
        => _connection.PublishNudgeAsync(projectionType, tenantId);

    public Task ReleaseAsync(NudgeQueueToken token) => _connection.ReleaseAsync(token);

    public Task ReleaseInOrderAsync(IEnumerable<NudgeQueueToken> tokens)
        => _connection.ReleaseInOrderAsync(tokens);

    public ProjectionHubFaultScenarioBuilder FallbackTriggerBlocks() {
        _connection.BlockUntil(HarnessCheckpoint.FallbackTrigger);
        return this;
    }

    public ProjectionHubFaultScenarioBuilder ReleaseFallbackTrigger() {
        _connection.Release(HarnessCheckpoint.FallbackTrigger);
        return this;
    }
}
