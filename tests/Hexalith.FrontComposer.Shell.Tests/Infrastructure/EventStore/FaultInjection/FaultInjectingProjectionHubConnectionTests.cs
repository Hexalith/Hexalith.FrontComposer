#pragma warning disable CA2007 // ConfigureAwait — test code (matches project convention)

using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Story 5-7 harness self-tests. Each test exercises a single named harness capability against
/// the deterministic execution contract documented in the addendum (one-shot checkpoints,
/// async-completion TCS, sanitized disposal, bounded queues, handler isolation,
/// payload-less nudges).
/// </summary>
public sealed class FaultInjectingProjectionHubConnectionTests {
    [Fact]
    public async Task BlockUntil_PausesNextOperation_UntilReleaseCalled() {
        await using FaultInjectingProjectionHubConnection sut = new();
        sut.BlockUntil(HarnessCheckpoint.Start);

        Task pending = sut.StartAsync(TestContext.Current.CancellationToken);
        pending.IsCompleted.ShouldBeFalse();
        sut.IsConnected.ShouldBeFalse();

        sut.Release(HarnessCheckpoint.Start);
        await pending.ConfigureAwait(true);

        sut.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task FailNext_FaultsNextMatchingOperation_OnlyOnce() {
        await using FaultInjectingProjectionHubConnection sut = new();
        sut.FailNext(HarnessCheckpoint.Start, new InvalidOperationException("boom"));

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        sut.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task CancelNext_CompletesAsCanceled_AndReportsOperationCanceledException() {
        await using FaultInjectingProjectionHubConnection sut = new();
        sut.CancelNext(HarnessCheckpoint.Start);

        OperationCanceledException ex = await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
        ex.ShouldNotBeNull();
        sut.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public async Task WaitForAsync_ResolvesWhenCheckpointHitCountReached() {
        await using FaultInjectingProjectionHubConnection sut = new();

        Task waiter = sut.WaitForAsync(HarnessCheckpoint.Start, count: 1, TestContext.Current.CancellationToken);
        waiter.IsCompleted.ShouldBeFalse();

        await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await waiter.ConfigureAwait(true);
    }

    [Fact]
    public async Task BlockedOperation_RespectsCancellationToken_FromCallSite() {
        await using FaultInjectingProjectionHubConnection sut = new();
        sut.BlockUntil(HarnessCheckpoint.Start);

        using CancellationTokenSource cts = new();
        Task pending = sut.StartAsync(cts.Token);
        cts.Cancel();

        OperationCanceledException ex = await Should.ThrowAsync<OperationCanceledException>(
            async () => await pending.ConfigureAwait(true)).ConfigureAwait(true);
        ex.ShouldNotBeNull();

        // Drain the still-armed Block from the script queue so disposal does not raise.
        sut.Release(HarnessCheckpoint.Start);
    }

    [Fact]
    public async Task PreCanceledTokens_DoNotCrossOrMutateLifecycleOperations() {
        await using FaultInjectingProjectionHubConnection sut = new();
        using CancellationTokenSource cts = new();
        await cts.CancelAsync().ConfigureAwait(true);

        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.StartAsync(cts.Token).ConfigureAwait(true)).ConfigureAwait(true);
        sut.IsConnected.ShouldBeFalse();
        sut.GetHitCount(HarnessCheckpoint.Start).ShouldBe(0);

        await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.JoinGroupAsync("orders", "acme", cts.Token).ConfigureAwait(true)).ConfigureAwait(true);
        sut.ObservedActiveGroups.ShouldBeEmpty();

        await sut.JoinGroupAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.LeaveGroupAsync("orders", "acme", cts.Token).ConfigureAwait(true)).ConfigureAwait(true);
        sut.ObservedActiveGroups.ShouldBe(["orders:acme"]);

        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.StopAsync(cts.Token).ConfigureAwait(true)).ConfigureAwait(true);
        sut.IsConnected.ShouldBeTrue();
    }

    [Fact]
    public async Task PerGroup_FailNext_OnlyImpactsThatGroup() {
        await using FaultInjectingProjectionHubConnection sut = new();
        sut.FailNext(HarnessCheckpoint.Join("orders", "acme"), new InvalidOperationException("fail acme"));
        await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.JoinGroupAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        await sut.JoinGroupAsync("billing", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        sut.ObservedActiveGroups.ShouldContain("billing:acme");
        sut.ObservedActiveGroups.ShouldNotContain("orders:acme");
    }

    [Fact]
    public async Task StartAsync_PublishesConnectedState_ToAllSubscribers() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<ProjectionHubConnectionState> observed = [];
        IDisposable reg = sut.OnConnectionStateChanged(change => {
            observed.Add(change.State);
            return Task.CompletedTask;
        });

        await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        observed.ShouldBe([ProjectionHubConnectionState.Connected]);
        reg.Dispose();
    }

    [Fact]
    public async Task RaiseStateAsync_DeliversReconnectingThenReconnected_InOrder() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<ProjectionHubConnectionState> observed = [];
        sut.OnConnectionStateChanged(change => {
            observed.Add(change.State);
            return Task.CompletedTask;
        });

        await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.RaiseStateAsync(HarnessConnectionStates.Reconnecting(new InvalidOperationException("transient"))).ConfigureAwait(true);
        await sut.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2")).ConfigureAwait(true);

        observed.ShouldBe([
            ProjectionHubConnectionState.Connected,
            ProjectionHubConnectionState.Reconnecting,
            ProjectionHubConnectionState.Reconnected,
        ]);
    }

    [Fact]
    public async Task RaiseStateAsync_CrossesNamedCheckpoint_AndUpdatesIsConnectedBeforeHandlers() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<bool> connectedAtHandler = [];
        _ = sut.OnConnectionStateChanged(_ => {
            connectedAtHandler.Add(sut.IsConnected);
            return Task.CompletedTask;
        });
        HarnessCheckpoint checkpoint = HarnessCheckpoint.ConnectionState(ProjectionHubConnectionState.Reconnected);
        sut.BlockUntil(checkpoint);

        Task pending = sut.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2"));
        pending.IsCompleted.ShouldBeFalse();
        sut.IsConnected.ShouldBeFalse();

        sut.Release(checkpoint);
        await pending.ConfigureAwait(true);

        sut.IsConnected.ShouldBeTrue();
        connectedAtHandler.ShouldBe([true]);

        await sut.RaiseStateAsync(HarnessConnectionStates.Reconnecting()).ConfigureAwait(true);
        sut.IsConnected.ShouldBeFalse();
        await sut.RaiseStateAsync(HarnessConnectionStates.Closed()).ConfigureAwait(true);
        sut.IsConnected.ShouldBeFalse();
    }

    [Fact]
    public async Task RaiseClosedAfterRetryAsync_PublishesReconnectingBeforeClosed() {
        await using FaultInjectingProjectionHubConnection connection = new();
        ProjectionHubFaultScenarioBuilder builder = new(connection);
        TaskCompletionSource reconnectingEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseReconnecting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        List<ProjectionHubConnectionState> observed = [];
        _ = connection.OnConnectionStateChanged(async change => {
            observed.Add(change.State);
            if (change.State is ProjectionHubConnectionState.Reconnecting) {
                _ = reconnectingEntered.TrySetResult();
                await releaseReconnecting.Task.ConfigureAwait(true);
            }
        });

        Task pending = builder.RaiseClosedAfterRetryAsync(new InvalidOperationException("transient"));
        await reconnectingEntered.Task.WaitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        observed.ShouldBe([ProjectionHubConnectionState.Reconnecting]);
        pending.IsCompleted.ShouldBeFalse();

        releaseReconnecting.SetResult();
        await pending.ConfigureAwait(true);
        observed.ShouldBe([ProjectionHubConnectionState.Reconnecting, ProjectionHubConnectionState.Closed]);
    }

    [Fact]
    public async Task PublishNudgeAsync_FiresHandlersWithProjectionAndTenant() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<(string Projection, string Tenant)> observed = [];
        sut.OnProjectionChanged((projection, tenant) => {
            observed.Add((projection, tenant));
            return Task.CompletedTask;
        });

        await sut.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        observed.ShouldBe([("orders", "acme")]);
    }

    [Fact]
    public async Task PublishNudgeAsync_CrossesNamedCheckpoint_BeforeDispatch() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<(string Projection, string Tenant)> observed = [];
        _ = sut.OnProjectionChanged((projection, tenant) => {
            observed.Add((projection, tenant));
            return Task.CompletedTask;
        });
        HarnessCheckpoint checkpoint = HarnessCheckpoint.Nudge("orders", "acme");
        sut.BlockUntil(checkpoint);

        Task pending = sut.PublishNudgeAsync("orders", "acme");
        pending.IsCompleted.ShouldBeFalse();
        observed.ShouldBeEmpty();

        sut.Release(checkpoint);
        await pending.ConfigureAwait(true);

        observed.ShouldBe([("orders", "acme")]);
    }

    [Fact]
    public async Task DropNextNudge_SuppressesNextMatchingPublication() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<(string Projection, string Tenant)> observed = [];
        sut.OnProjectionChanged((projection, tenant) => {
            observed.Add((projection, tenant));
            return Task.CompletedTask;
        });

        sut.DropNextNudge("orders", "acme");
        await sut.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        await sut.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);

        observed.ShouldBe([("orders", "acme")]);
    }

    [Fact]
    public async Task DuplicateNextNudge_FiresHandlersTheRequestedNumberOfTimes() {
        await using FaultInjectingProjectionHubConnection sut = new();
        int count = 0;
        sut.OnProjectionChanged((_, _) => {
            count++;
            return Task.CompletedTask;
        });

        sut.DuplicateNextNudge("orders", "acme", count: 3);
        await sut.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);

        count.ShouldBe(3);
    }

    [Fact]
    public async Task QueueNudge_ReleasesInExplicitOrder_ForReorderScenario() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<string> observed = [];
        sut.OnProjectionChanged((projection, _) => {
            observed.Add(projection);
            return Task.CompletedTask;
        });

        NudgeQueueToken first = sut.QueueNudge("orders", "acme");
        NudgeQueueToken second = sut.QueueNudge("billing", "acme");

        await sut.ReleaseInOrderAsync([second, first]).ConfigureAwait(true);

        observed.ShouldBe(["billing", "orders"]);
    }

    [Fact]
    public async Task ReleaseInOrderAsync_ValidatesAllTokens_BeforePublishingAnyNudge() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<string> observed = [];
        _ = sut.OnProjectionChanged((projection, _) => {
            observed.Add(projection);
            return Task.CompletedTask;
        });

        NudgeQueueToken first = sut.QueueNudge("orders", "acme");
        NudgeQueueToken second = sut.QueueNudge("billing", "acme");

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.ReleaseInOrderAsync([first, first]).ConfigureAwait(true)).ConfigureAwait(true);
        observed.ShouldBeEmpty();

        sut.Discard(first);
        sut.Discard(second);
    }

    [Fact]
    public async Task PartialDelivery_DropsSomeGroups_AndDeliversOthers() {
        await using FaultInjectingProjectionHubConnection sut = new();
        List<string> observed = [];
        sut.OnProjectionChanged((projection, _) => {
            observed.Add(projection);
            return Task.CompletedTask;
        });

        sut.DropNextNudge("orders", "acme");
        await sut.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        await sut.PublishNudgeAsync("billing", "acme").ConfigureAwait(true);

        observed.ShouldBe(["billing"]);
    }

    [Fact]
    public async Task DispatchNudge_IsolatesHandlerFailure_FromOtherSubscribers() {
        await using FaultInjectingProjectionHubConnection sut = new();
        bool secondCalled = false;
        sut.OnProjectionChanged((_, _) => throw new InvalidOperationException("subscriber blew up"));
        sut.OnProjectionChanged((_, _) => {
            secondCalled = true;
            return Task.CompletedTask;
        });

        await sut.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);

        secondCalled.ShouldBeTrue();
        sut.CapturedHandlerFailureCategories.ShouldBe([nameof(InvalidOperationException)]);
    }

    [Fact]
    public async Task HandlerRegistrations_AndFailureCategories_AreBounded() {
        FaultInjectingProjectionHubConnection registrationBound = new() { MaxBoundedQueueDepth = 1 };
        _ = registrationBound.OnProjectionChanged((_, _) => Task.CompletedTask);
        _ = Should.Throw<InvalidOperationException>(() => registrationBound.OnProjectionChanged((_, _) => Task.CompletedTask));
        await registrationBound.DisposeAsync().ConfigureAwait(true);

        await using FaultInjectingProjectionHubConnection failureBound = new() { MaxBoundedQueueDepth = 2 };
        _ = failureBound.OnProjectionChanged((_, _) => throw new InvalidOperationException("raw tenant acme"));
        await failureBound.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        await failureBound.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        await failureBound.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);

        failureBound.CapturedHandlerFailureCategories.Count.ShouldBe(2);
        failureBound.CapturedHandlerFailureCategories[^1].ShouldBe("Overflow");
    }

    [Fact]
    public async Task DisposedHarness_SuppressesLaterNudgeAndStateCallbacks() {
        FaultInjectingProjectionHubConnection sut = new();
        int calls = 0;
        _ = sut.OnProjectionChanged((_, _) => {
            calls++;
            return Task.CompletedTask;
        });
        _ = sut.OnConnectionStateChanged(_ => {
            calls++;
            return Task.CompletedTask;
        });

        await sut.DisposeAsync().ConfigureAwait(true);
        await sut.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        await sut.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2")).ConfigureAwait(true);

        calls.ShouldBe(0);
        _ = await Should.ThrowAsync<ObjectDisposedException>(
            async () => await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
    }

    [Fact]
    public async Task TriggerFallbackCheckpointAsync_CrossesNamedCheckpoint() {
        await using FaultInjectingProjectionHubConnection sut = new();
        sut.BlockUntil(HarnessCheckpoint.FallbackTrigger);

        Task pending = sut.TriggerFallbackCheckpointAsync(TestContext.Current.CancellationToken);
        pending.IsCompleted.ShouldBeFalse();

        sut.Release(HarnessCheckpoint.FallbackTrigger);
        await pending.ConfigureAwait(true);
        sut.GetHitCount(HarnessCheckpoint.FallbackTrigger).ShouldBe(1);
    }

    [Fact]
    public async Task DisposeAsync_CrossesNamedCheckpoint_AndCanBeReleasedDeterministically() {
        FaultInjectingProjectionHubConnection sut = new();
        sut.BlockUntil(HarnessCheckpoint.Dispose);

        Task pending = sut.DisposeAsync().AsTask();
        pending.IsCompleted.ShouldBeFalse();

        sut.Release(HarnessCheckpoint.Dispose);
        await pending.ConfigureAwait(true);
        sut.GetHitCount(HarnessCheckpoint.Dispose).ShouldBe(1);
    }

    [Fact]
    public async Task DisposeAsync_ThrowsHarnessDisposal_WhenScriptedActionsRemain() {
        FaultInjectingProjectionHubConnection sut = new();
        sut.FailNext(HarnessCheckpoint.Start, new InvalidOperationException("never crossed"));
        sut.FailNext(HarnessCheckpoint.Join("orders", "acme"), new InvalidOperationException("raw tenant acme"));

        HarnessDisposalException ex = await Should.ThrowAsync<HarnessDisposalException>(
            async () => await sut.DisposeAsync().ConfigureAwait(true)).ConfigureAwait(true);

        ex.Message.ShouldContain("Outstanding scripted actions");
        ex.Message.ShouldNotContain("acme");
        ex.Message.ShouldNotContain("orders");
        ex.Message.ShouldNotContain("orders:acme");
        ex.Message.ShouldNotContain("never crossed");
        ex.Message.ShouldNotContain("raw tenant");
    }

    [Fact]
    public async Task DisposeAsync_ThrowsHarnessDisposal_WhenQueuedNudgesRemain() {
        FaultInjectingProjectionHubConnection sut = new();
        _ = sut.QueueNudge("orders", "acme");

        HarnessDisposalException ex = await Should.ThrowAsync<HarnessDisposalException>(
            async () => await sut.DisposeAsync().ConfigureAwait(true)).ConfigureAwait(true);

        ex.Message.ShouldContain("Outstanding queued nudges");
        ex.Message.ShouldNotContain("acme");
        ex.Message.ShouldNotContain("orders");
    }

    [Fact]
    public async Task DisposeAsync_FailsBlockedTcs_SoNoTestHangs() {
        FaultInjectingProjectionHubConnection sut = new();
        sut.BlockUntil(HarnessCheckpoint.Start);

        Task blocked = sut.StartAsync(TestContext.Current.CancellationToken);

        try {
            await sut.DisposeAsync().ConfigureAwait(true);
        }
        catch (HarnessDisposalException) {
            // Expected diagnostic.
        }

        Exception? failure = await Record.ExceptionAsync(async () => await blocked.ConfigureAwait(true)).ConfigureAwait(true);
        failure.ShouldNotBeNull();
        failure.ShouldBeOfType<ObjectDisposedException>();
    }

    [Fact]
    public async Task WaitForAsync_CanceledWaiter_DoesNotCreateDisposalDiagnostic() {
        FaultInjectingProjectionHubConnection sut = new();
        using CancellationTokenSource cts = new();

        Task waiter = sut.WaitForAsync(HarnessCheckpoint.Start, count: 1, cts.Token);
        await cts.CancelAsync().ConfigureAwait(true);

        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await waiter.ConfigureAwait(true)).ConfigureAwait(true);
        await sut.DisposeAsync().ConfigureAwait(true);
    }

    [Fact]
    public void QueueNudge_RejectsBlankSegments_FailClosed() {
        FaultInjectingProjectionHubConnection sut = new();
        _ = Should.Throw<ArgumentException>(() => sut.QueueNudge(string.Empty, "acme"));
        _ = Should.Throw<ArgumentException>(() => sut.QueueNudge("orders", "  "));
    }

    [Fact]
    public void QueueNudge_RejectsColonSegments_FailClosed() {
        FaultInjectingProjectionHubConnection sut = new();
        _ = Should.Throw<ArgumentException>(() => sut.QueueNudge("orders:bad", "acme"));
        _ = Should.Throw<ArgumentException>(() => sut.QueueNudge("orders", "ac:me"));
    }

    [Fact]
    public async Task ScriptedAction_ExceedingBoundedDepth_FailsWithDiagnostic() {
        FaultInjectingProjectionHubConnection sut = new() { MaxBoundedQueueDepth = 2 };
        sut.FailNext(HarnessCheckpoint.Start, new InvalidOperationException("a"));
        sut.FailNext(HarnessCheckpoint.Start, new InvalidOperationException("b"));
        InvalidOperationException ex = Should.Throw<InvalidOperationException>(
            () => sut.FailNext(HarnessCheckpoint.Start, new InvalidOperationException("c")));
        ex.Message.ShouldContain("MaxBoundedQueueDepth");

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
        await sut.DisposeAsync().ConfigureAwait(true);
    }
}
