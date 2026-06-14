namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Readable scenario-builder facade over <see cref="FaultInjectingProjectionHubConnection"/>.
/// Each method documents the v0.1 fault inventory (start failure/delay, disconnect, reconnect,
/// closed-after-retry, join failure, leave failure, per-group drop/duplicate/delay, deterministic
/// reorder, partial delivery, cancellation, and disposal races). The builder never exposes
/// payload-bearing publication APIs; raw SignalR client types are not part of the surface.
/// </summary>
internal sealed class ProjectionHubFaultScenarioBuilder {
    public ProjectionHubFaultScenarioBuilder(FaultInjectingProjectionHubConnection connection) {
        ArgumentNullException.ThrowIfNull(connection);
        Connection = connection;
    }

    public FaultInjectingProjectionHubConnection Connection { get; }

    // ---------- Connection lifecycle ----------

    /// <summary>Connection is healthy at start. Default behaviour; no-op staging.</summary>
    public ProjectionHubFaultScenarioBuilder StartConnected() => this;

    /// <summary>Initial <c>StartAsync</c> faults with the supplied exception.</summary>
    public ProjectionHubFaultScenarioBuilder StartFails(Exception exception) {
        Connection.FailNext(HarnessCheckpoint.Start, exception);
        return this;
    }

    /// <summary>Initial <c>StartAsync</c> blocks until <c>ReleaseStart()</c> is called.</summary>
    public ProjectionHubFaultScenarioBuilder StartBlocks() {
        Connection.BlockUntil(HarnessCheckpoint.Start);
        return this;
    }

    public ProjectionHubFaultScenarioBuilder ReleaseStart() {
        Connection.Release(HarnessCheckpoint.Start);
        return this;
    }

    /// <summary>Initial <c>StartAsync</c> completes canceled when crossed.</summary>
    public ProjectionHubFaultScenarioBuilder StartCancels() {
        Connection.CancelNext(HarnessCheckpoint.Start);
        return this;
    }

    // ---------- Group join/leave ----------

    public ProjectionHubFaultScenarioBuilder JoinFails(string projectionType, string tenantId, Exception exception) {
        Connection.FailNext(HarnessCheckpoint.Join(projectionType, tenantId), exception);
        return this;
    }

    public ProjectionHubFaultScenarioBuilder JoinBlocks(string projectionType, string tenantId) {
        Connection.BlockUntil(HarnessCheckpoint.Join(projectionType, tenantId));
        return this;
    }

    public ProjectionHubFaultScenarioBuilder ReleaseJoin(string projectionType, string tenantId) {
        Connection.Release(HarnessCheckpoint.Join(projectionType, tenantId));
        return this;
    }

    public ProjectionHubFaultScenarioBuilder LeaveFails(string projectionType, string tenantId, Exception exception) {
        Connection.FailNext(HarnessCheckpoint.Leave(projectionType, tenantId), exception);
        return this;
    }

    // ---------- Connection state events ----------

    public Task RaiseReconnectingAsync(Exception? cause = null)
        => Connection.RaiseStateAsync(HarnessConnectionStates.Reconnecting(cause));

    public Task RaiseReconnectedAsync(string? connectionId = null)
        => Connection.RaiseStateAsync(HarnessConnectionStates.Reconnected(connectionId));

    public Task RaiseClosedAsync(Exception? cause = null)
        => Connection.RaiseStateAsync(HarnessConnectionStates.Closed(cause));

    public async Task RaiseClosedAfterRetryAsync(Exception cause) {
        // Equivalent to: server signaled Reconnecting then automatic reconnect failed → Closed.
        await Connection.RaiseStateAsync(HarnessConnectionStates.Reconnecting(cause)).ConfigureAwait(false);
        await Connection.RaiseStateAsync(HarnessConnectionStates.Closed(cause)).ConfigureAwait(false);
    }

    // ---------- Nudge faults ----------

    public ProjectionHubFaultScenarioBuilder DropNextNudge(string projectionType, string tenantId) {
        Connection.DropNextNudge(projectionType, tenantId);
        return this;
    }

    public ProjectionHubFaultScenarioBuilder DuplicateNextNudge(string projectionType, string tenantId, int count = 2) {
        Connection.DuplicateNextNudge(projectionType, tenantId, count);
        return this;
    }

    public NudgeQueueToken DelayNextNudge(string projectionType, string tenantId)
        => Connection.DelayNextNudge(projectionType, tenantId);

    public NudgeQueueToken Queue(string projectionType, string tenantId)
        => Connection.QueueNudge(projectionType, tenantId);

    public Task PublishNudgeAsync(string projectionType, string tenantId)
        => Connection.PublishNudgeAsync(projectionType, tenantId);

    public Task ReleaseAsync(NudgeQueueToken token) => Connection.ReleaseAsync(token);

    public Task ReleaseInOrderAsync(IEnumerable<NudgeQueueToken> tokens)
        => Connection.ReleaseInOrderAsync(tokens);

    public ProjectionHubFaultScenarioBuilder FallbackTriggerBlocks() {
        Connection.BlockUntil(HarnessCheckpoint.FallbackTrigger);
        return this;
    }

    public ProjectionHubFaultScenarioBuilder ReleaseFallbackTrigger() {
        Connection.Release(HarnessCheckpoint.FallbackTrigger);
        return this;
    }
}
