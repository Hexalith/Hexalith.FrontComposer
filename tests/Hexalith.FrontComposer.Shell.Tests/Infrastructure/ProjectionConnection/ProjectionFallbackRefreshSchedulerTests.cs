using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.ProjectionConnection;

public sealed class ProjectionFallbackRefreshSchedulerTests {
    [Fact]
    public async Task SynchronousCustomReconciliationCallbackLeavesDeadlineAndLaterReadAvailable()
    {
        using ManualResetEventSlim release = new(false);
        using CancellationTokenSource firstCancellation = new();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 })
                .ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => {
                if (Interlocked.Increment(ref reads) == 1) {
                    started.TrySetResult();
                    release.Wait(CancellationToken.None);
                }
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });
        Task<ProjectionReconciliationRefreshResult>? first = null;
        try
        {
            Task invocation = Task.Run(() => { first = scheduler.TriggerReconciliationOnceAsync(1, firstCancellation.Token); },
                TestContext.Current.CancellationToken);
            await invocation.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            await started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            firstCancellation.Cancel();
            ProjectionReconciliationRefreshResult next = await scheduler.TriggerReconciliationOnceAsync(
                2, TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            next.RefreshedCount.ShouldBe(1);
            reads.ShouldBe(2);
        }
        finally
        {
            release.Set();
        }
        if (first is not null) {
            await Should.ThrowAsync<OperationCanceledException>(() => first).WaitAsync(
                TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UncooperativeCustomLaneHasBoundedSweepAndCannotBlockLaterRead(bool fallback)
    {
        FakeTimeProvider time = new();
        TestConnectionState connection = new(new ProjectionConnectionSnapshot(
            fallback ? ProjectionConnectionStatus.Disconnected : ProjectionConnectionStatus.Connected,
            time.GetUtcNow(), 0, null));
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> abandoned = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int firstReads = 0;
        int secondReads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(connection, Substitute.For<IProjectionPageLoader>(),
            Substitute.For<IDispatcher>(), new MutableLoadedPageState(PageState("a", ["cached"], 1)),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 2,
            }).ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance, timeProvider: time);
        using IDisposable first = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => {
                if (Interlocked.Increment(ref firstReads) == 1) {
                    started.TrySetResult();
                    return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(abandoned.Task);
                }
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });
        using IDisposable second = scheduler.RegisterLane(DefaultLane("b") with {
            RefreshAsync = _ => {
                Interlocked.Increment(ref secondReads);
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });

        Task<int> sweep = fallback ? scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken)
            : scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Task.Yield();
        time.Advance(TimeSpan.FromSeconds(5));
        (await sweep.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        secondReads.ShouldBe(1);

        Task<int> next = fallback ? scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken)
            : scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        (await next.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(2);
        firstReads.ShouldBe(2);
        abandoned.SetException(new InvalidOperationException("late provider fault"));
        await Task.Yield();
        firstReads.ShouldBe(2);
    }

    [Fact]
    public async Task BlockingCancellationCallbackCannotHoldNudgeSweepPastDeadline()
    {
        FakeTimeProvider time = new();
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cancellationEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using ManualResetEventSlim releaseCancellation = new(false);
        int laterReads = 0;
        TestConnectionState connection = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected, time.GetUtcNow(), 0, null));
        ProjectionFallbackRefreshScheduler scheduler = new(connection, Substitute.For<IProjectionPageLoader>(),
            Substitute.For<IDispatcher>(), new MutableLoadedPageState(PageState("a", ["cached"], 1)),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 2 })
                .ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance, timeProvider: time);
        using IDisposable first = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = token => {
                _ = token.Register(() => {
                    cancellationEntered.TrySetResult();
                    releaseCancellation.Wait();
                });
                started.TrySetResult();
                return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(held.Task);
            },
        });
        using IDisposable second = scheduler.RegisterLane(DefaultLane("b") with {
            RefreshAsync = _ => {
                Interlocked.Increment(ref laterReads);
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });
        try
        {
            Task<int> sweep = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
            await started.Task.WaitAsync(TestContext.Current.CancellationToken);
            await Task.Yield();
            time.Advance(TimeSpan.FromSeconds(5));
            (await sweep.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken)).ShouldBe(1);
            laterReads.ShouldBe(1);
            await cancellationEntered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
        finally
        {
            releaseCancellation.Set();
            held.TrySetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        }
    }

    [Theory]
    [InlineData("nudge", false)]
    [InlineData("nudge", true)]
    [InlineData("fallback", false)]
    [InlineData("fallback", true)]
    public async Task TimedOutObservedReadRetiresOnlyOldAdmissionAndAllowsCurrentRecovery(
        string ownerKind, bool oldReadReturnsAfterNewRead)
    {
        const string view = "acme:OrdersProjection";
        FakeTimeProvider time = new();
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> oldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource oldStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource newStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        TestConnectionState connection = new(new ProjectionConnectionSnapshot(
            ownerKind == "fallback" ? ProjectionConnectionStatus.Disconnected : ProjectionConnectionStatus.Connected,
            time.GetUtcNow(), 0, null));
        ProjectionFallbackRefreshScheduler scheduler = new(connection, Substitute.For<IProjectionPageLoader>(),
            Substitute.For<IDispatcher>(), new MutableLoadedPageState(PageState(view, ["cached"], 1)),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 1,
            }).ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane(view) with {
            RefreshAsync = _ => {
                int read = Interlocked.Increment(ref reads);
                if (read == 1) {
                    oldStarted.TrySetResult();
                    return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(oldRead.Task);
                }
                newStarted.TrySetResult();
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });
        ReconnectionReconciliationStateService state = new(time,
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state,
            Substitute.For<IDispatcher>(), time, NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<int> oldOwner = ownerKind == "fallback"
            ? scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken)
            : scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await oldStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        recovery.IsCompleted.ShouldBeFalse();
        time.Advance(TimeSpan.FromSeconds(30));
        ProjectionReconciliationRefreshResult expired = await recovery.WaitAsync(TestContext.Current.CancellationToken);
        expired.HasIncompleteEligibleReads.ShouldBeTrue();
        expired.RefreshedCount.ShouldBe(0);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);

        if (!oldReadReturnsAfterNewRead) {
            oldRead.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            (await oldOwner.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        }
        await newStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult current = await coordinator.ReconcileAsync(
            TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        current.RefreshedCount.ShouldBe(1);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
        if (oldReadReturnsAfterNewRead) {
            oldRead.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            (await oldOwner.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        }
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TimedOutObservedNudgeCannotDispatchOldRowsAroundReplacementRead(bool oldReadReturnsAfterNewRead)
    {
        const string view = "acme:OrdersProjection";
        FakeTimeProvider time = new();
        TaskCompletionSource<ProjectionPageResult> oldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource newDispatched = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => Interlocked.Increment(ref reads) == 1
                ? oldRead.Task
                : Task.FromResult(new ProjectionPageResult(["fresh"], 1, "\"fresh\"")));
        MutableLoadedPageState pages = new(PageState(view, ["cached"], 1) with {
            StalePages = ImmutableHashSet<(string ViewKey, int Skip)>.Empty.Add((view, 0)),
        });
        LoadedPageReducers reducers = new(Microsoft.Extensions.Options.Options.Create(new FcShellOptions()).ToMonitor(),
            NullLogger<LoadedPageReducers>.Instance);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        dispatcher.When(x => x.Dispatch(Arg.Any<LoadPageSucceededAction>())).Do(call => {
            LoadPageSucceededAction action = call.Arg<LoadPageSucceededAction>();
            pages.Value = reducers.ReduceLoadPageSucceeded(pages.Value, action);
            if (action.Items?.Contains("fresh") == true) {
                newDispatched.TrySetResult();
            }
        });
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                time.GetUtcNow(), 0, null)), loader, dispatcher, pages,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane(view));
        ReconnectionReconciliationStateService state = new(time,
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state, dispatcher, time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<int> owner = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        reads.ShouldBe(1);
        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(30));
        (await recovery.WaitAsync(TestContext.Current.CancellationToken)).HasIncompleteEligibleReads.ShouldBeTrue();
        if (!oldReadReturnsAfterNewRead) {
            oldRead.SetResult(new ProjectionPageResult(["old"], 1, "\"old\""));
            (await owner.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        }
        await newDispatched.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        if (oldReadReturnsAfterNewRead) {
            oldRead.SetResult(new ProjectionPageResult(["old"], 1, "\"old\""));
            (await owner.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        }

        pages.Value.PagesByKey[(view, 0)].ShouldBe(["fresh"]);
        pages.Value.StalePages.ShouldNotContain((view, 0));
        dispatcher.DidNotReceive().Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.Items != null && action.Items.Contains("old")));
        ProjectionReconciliationRefreshResult current = await coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        current.RefreshedCount.ShouldBe(1);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
    }

    [Fact]
    public async Task LateObservedOwnerCannotReleaseStillRunningReplacementAdmission()
    {
        FakeTimeProvider time = new();
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> oldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> replacementRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource oldStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replacementStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                time.GetUtcNow(), 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => {
                int read = Interlocked.Increment(ref reads);
                if (read == 1) {
                    oldStarted.TrySetResult();
                    return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(oldRead.Task);
                }
                replacementStarted.TrySetResult();
                return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(replacementRead.Task);
            },
        });
        ReconnectionReconciliationStateService state = new(time,
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state,
            Substitute.For<IDispatcher>(), time, NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<int> owner = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await oldStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(30));
        (await recovery.WaitAsync(TestContext.Current.CancellationToken)).HasIncompleteEligibleReads.ShouldBeTrue();
        await replacementStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        oldRead.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        (await owner.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken))
            .ShouldBe(0);
        reads.ShouldBe(2);
        replacementRead.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
    }

    [Fact]
    public async Task TimedOutHeldLaneRetiresOnlyItsAdmissionAndLaterCurrentReadRecovers()
    {
        const string view = "acme:OrdersProjection";
        FakeTimeProvider time = new();
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> first = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> second = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        ProjectionFallbackLane lane = DefaultLane(view) with {
            RefreshAsync = _ => {
                int number = Interlocked.Increment(ref reads);
                if (number == 1) {
                    firstStarted.TrySetResult();
                    return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(first.Task);
                }
                if (number == 2) {
                    secondStarted.TrySetResult();
                    return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(second.Task);
                }
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        };
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                time.GetUtcNow(), 0, null)), Substitute.For<IProjectionPageLoader>(),
            Substitute.For<IDispatcher>(), new MutableLoadedPageState(PageState(view, ["cached"], 1)),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 1,
            }).ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable registration = scheduler.RegisterLane(lane);
        ReconnectionReconciliationStateService state = new(time, NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state,
            Substitute.For<IDispatcher>(), time, NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> expiredRead = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        await firstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(30));
        (await expiredRead.WaitAsync(TestContext.Current.CancellationToken)).HasIncompleteEligibleReads.ShouldBeTrue();
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);

        Task<ProjectionReconciliationRefreshResult> currentRead = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        await secondStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        first.TrySetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        await Task.Yield();
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        reads.ShouldBe(2);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Reconciling);
        second.TrySetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        (await currentRead.WaitAsync(TestContext.Current.CancellationToken)).RefreshedCount.ShouldBe(1);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
    }

    [Fact]
    public async Task SameTenantUserReplacementUsesNewLaneAndRejectsHeldOldRead()
    {
        const string view = "acme:OrdersProjection";
        TestTenantContextAccessor scope = new() { TenantId = "acme", UserId = "user-a" };
        MutableLoadedPageState pages = new(PageState(view, ["cached-a"], 1));
        TaskCompletionSource<ProjectionPageResult> heldA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource startedA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ => {
                if (Interlocked.Increment(ref reads) == 1) {
                    startedA.TrySetResult();
                    return heldA.Task;
                }
                return Task.FromResult(new ProjectionPageResult(["current-b"], 1, "b-etag"));
            });
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), loader, dispatcher, pages,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 1,
            }).ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance, scope);
        ProjectionFallbackLane laneA = DefaultLane(view) with { UserId = "user-a", ScopeGeneration = 0 };
        using IDisposable registrationA = scheduler.RegisterReplacingLane(laneA);
        Task<int> oldRead = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await startedA.Task.WaitAsync(TestContext.Current.CancellationToken);

        scope.UserId = "user-b";
        pages.Value = LoadedPageReducers.ReduceScopeChanged(pages.Value, new ScopeChangedAction());
        ProjectionFallbackLane laneB = laneA with { UserId = "user-b", ScopeGeneration = pages.Value.ScopeGeneration };
        using IDisposable registrationB = scheduler.RegisterReplacingLane(laneB);
        using IDisposable coMountedB = scheduler.RegisterLane(laneB);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.ViewKey == view && action.OriginScopeGeneration == 1 && action.Items!.Contains("current-b")));

        heldA.TrySetResult(new ProjectionPageResult(["private-a"], 1, "a-etag"));
        (await oldRead.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        dispatcher.DidNotReceive().Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.Items!.Contains("private-a")));
        reads.ShouldBe(2);
    }

    [Fact]
    public async Task LegacyLaneCapturesRegistrationUserAndRejectsSameTenantSwitch()
    {
        const string view = "acme:OrdersProjection";
        TestTenantContextAccessor scope = new() { TenantId = "acme", UserId = "user-a" };
        MutableLoadedPageState pages = new(PageState(view, ["cached"], 1));
        TaskCompletionSource<ProjectionPageResult> heldA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource startedA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ => {
                if (Interlocked.Increment(ref reads) == 1) {
                    startedA.TrySetResult();
                    return heldA.Task;
                }
                return Task.FromResult(new ProjectionPageResult(["current-b"], 1, "b-etag"));
            });
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), loader, dispatcher, pages,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 1,
            }).ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance, scope);
        ProjectionFallbackLane legacy = DefaultLane(view);
        using IDisposable registrationA = scheduler.RegisterReplacingLane(legacy);
        Task<int> oldRead = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await startedA.Task.WaitAsync(TestContext.Current.CancellationToken);

        scope.UserId = "user-b";
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        using IDisposable registrationB = scheduler.RegisterReplacingLane(legacy);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        heldA.TrySetResult(new ProjectionPageResult(["private-a"], 1, "a-etag"));
        (await oldRead.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(0);
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.Items!.Contains("current-b")));
        dispatcher.DidNotReceive().Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.Items!.Contains("private-a")));
        reads.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PageEvictedBetweenClassificationAndReductionRestoresReadRowsWithoutEarlyRecovery(bool notModified)
    {
        const string view = "acme:OrdersProjection";
        MutableLoadedPageState pages = new(PageState(view, ["cached"], 1));
        int reads = 0;
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ =>
            Task.FromResult(Interlocked.Increment(ref reads) == 1
                ? new ProjectionPageResult(["cached"], 1, "same-etag")
                : new ProjectionPageResult(["fresh-row"], 1, "same-etag", IsNotModified: notModified)));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        LoadPageNotModifiedAction? delayed = null;
        dispatcher.When(x => x.Dispatch(Arg.Any<LoadPageNotModifiedAction>())).Do(call => {
            delayed = call.Arg<LoadPageNotModifiedAction>();
            pages.Value = pages.Value with { PagesByKey = pages.Value.PagesByKey.Remove((view, 0)) };
        });
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(loader, dispatcher, pages);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane(view));
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        pages.Value = pages.Value with { StalePages = pages.Value.StalePages.Add((view, 0)) };

        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        delayed.ShouldNotBeNull().TotalCount.ShouldBe(1);
        pages.Value.StalePages.ShouldContain((view, 0));
        pages.Value = LoadedPageReducers.ReduceLoadPageNotModified(pages.Value, delayed);
        pages.Value.PagesByKey[(view, 0)].Single().ShouldBe("fresh-row");
        pages.Value.StalePages.ShouldNotContain((view, 0));
    }

    [Fact]
    public void MissingPageAndEmptyUnchangedPayloadCannotClearStaleTruth()
    {
        const string view = "acme:OrdersProjection";
        LoadedPageState state = PageState(view, ["cached"], 1) with {
            StalePages = ImmutableHashSet<(string ViewKey, int Skip)>.Empty.Add((view, 0)),
        };
        state = state with { PagesByKey = state.PagesByKey.Remove((view, 0)) };
        LoadedPageState next = LoadedPageReducers.ReduceLoadPageNotModified(state,
            new LoadPageNotModifiedAction(view, 0, []) { OriginScopeGeneration = state.ScopeGeneration, TotalCount = 1 });
        next.PagesByKey.ShouldBeEmpty();
        next.StalePages.ShouldContain((view, 0));
        next.SuccessfulReadSequenceByView.ShouldBe(state.SuccessfulReadSequenceByView);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FiniteSweepsRotateAcrossMoreEligibleViewsThanOneBudget(bool nudge)
    {
        ConcurrentDictionary<string, int> attempts = new(StringComparer.Ordinal);
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Disconnected,
                DateTimeOffset.UtcNow, 1, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 2,
            }).ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        List<IDisposable> registrations = [];
        try
        {
            for (int index = 0; index < 5; index++)
            {
                string key = $"lane-{index}";
                registrations.Add(scheduler.RegisterLane(DefaultLane(key) with {
                    RefreshAsync = _ => {
                        attempts.AddOrUpdate(key, 1, static (_, count) => count + 1);
                        return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
                    },
                }));
            }
            for (int pass = 0; pass < 3; pass++)
            {
                int refreshed = nudge
                    ? await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)
                    : await scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken);
                refreshed.ShouldBe(2);
                attempts.Values.Sum().ShouldBe((pass + 1) * 2);
            }
            attempts.Keys.Count.ShouldBe(5);
        }
        finally
        {
            foreach (IDisposable registration in registrations) { registration.Dispose(); }
        }
    }

    [Fact]
    public async Task ReconciliationRechecksEligibleViewsBeyondInitialFiniteBudget()
    {
        ConcurrentDictionary<string, int> attempts = new(StringComparer.Ordinal);
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 1, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 2 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        List<IDisposable> registrations = [];
        try
        {
            for (int index = 0; index < 5; index++)
            {
                string key = $"lane-{index}";
                registrations.Add(scheduler.RegisterLane(DefaultLane(key) with {
                    RefreshAsync = _ => {
                        attempts.AddOrUpdate(key, 1, static (_, count) => count + 1);
                        return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
                    },
                }));
            }
            FakeTimeProvider time = new();
            ReconnectionReconciliationStateService state = new(time, NullLogger<ReconnectionReconciliationStateService>.Instance);
            using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state, Substitute.For<IDispatcher>(),
                time, NullLogger<ReconnectionReconciliationCoordinator>.Instance);
            ProjectionReconciliationRefreshResult result = await coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
            result.HasIncompleteEligibleReads.ShouldBeFalse();
            attempts.Keys.Count.ShouldBe(5);
            state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
        }
        finally
        {
            foreach (IDisposable registration in registrations) { registration.Dispose(); }
        }
    }

    [Fact]
    public async Task DirectHeldReconciliationReadCannotHoldLaterLaneOrDispatchAfterDeadline()
    {
        FakeTimeProvider time = new();
        TaskCompletionSource<ProjectionPageResult> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource secondStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => held.Task);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        MutableLoadedPageState pages = new(new LoadedPageState());
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                time.GetUtcNow(), 0, null)), loader, dispatcher, pages,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 2 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable first = scheduler.RegisterLane(DefaultLane("a"));
        using IDisposable second = scheduler.RegisterLane(DefaultLane("b") with {
            RefreshAsync = _ => {
                secondStarted.TrySetResult();
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });
        ReconnectionReconciliationStateService state = new(time,
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state, dispatcher, time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        await secondStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMilliseconds(29_999));
        recovery.IsCompleted.ShouldBeFalse();
        time.Advance(TimeSpan.FromMilliseconds(1));
        ProjectionReconciliationRefreshResult expired = await recovery.WaitAsync(TestContext.Current.CancellationToken);
        expired.HasIncompleteEligibleReads.ShouldBeTrue();
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        pages.Value = pages.Value with { ScopeGeneration = pages.Value.ScopeGeneration + 1 };
        held.SetResult(new ProjectionPageResult(["late"], 1, "late-etag"));
        await Task.Delay(50, TestContext.Current.CancellationToken);
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageNotModifiedAction>());
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        // The old registrations belong to the prior page scope. Current owners
        // replace them before a new reconciliation is allowed to refresh.
        using IDisposable currentA = scheduler.RegisterReplacingLane(DefaultLane("a"));
        using IDisposable currentB = scheduler.RegisterReplacingLane(DefaultLane("b") with {
            RefreshAsync = _ => ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed),
        });
        ProjectionReconciliationRefreshResult current = await coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        current.RefreshedCount.ShouldBe(2);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
    }

    [Fact]
    public async Task NewReadyLaneWakesPumpAlreadyWaitingOnUnrelatedHeldRead()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replayBStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsB = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable laneA = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldA.Task),
        });
        using IDisposable laneB = scheduler.RegisterLane(DefaultLane("b") with {
            ProjectionType = "OtherProjection",
            RefreshAsync = _ => Interlocked.Increment(ref readsB) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldB.Task)
                : ReplayB(),
        });
        ValueTask<ProjectionFallbackLaneRefreshOutcome> ReplayB()
        {
            replayBStarted.TrySetResult();
            return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        }

        Task<int> originalA = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        _ = await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        System.Reflection.FieldInfo pump = typeof(ProjectionFallbackRefreshScheduler).GetField("_replayPumpRunning",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).ShouldNotBeNull();
        SpinWait.SpinUntil(() => (int)pump.GetValue(scheduler).ShouldNotBeNull() == 1,
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
        await Task.Delay(100, TestContext.Current.CancellationToken);

        Task<int> originalB = scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken);
        _ = await scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken);
        heldB.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await originalB.ConfigureAwait(true);
        await replayBStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        originalA.IsCompleted.ShouldBeFalse();
        readsB.ShouldBe(2);
        heldA.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await originalA.ConfigureAwait(true);
    }

    [Fact]
    public async Task ReleasedIncompleteLaneIsRecheckedAheadOfNewEarlierBudgetConsumer()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldOriginal = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldRecheck = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsC = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 2 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable laneA = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed),
        });
        using IDisposable laneC = scheduler.RegisterLane(DefaultLane("c") with {
            ProjectionType = "CProjection",
            RefreshAsync = _ => Interlocked.Increment(ref readsC) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldOriginal.Task)
                : new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldRecheck.Task),
        });
        Task<int> originalC = scheduler.TriggerNudgeRefreshAsync("CProjection", "acme", TestContext.Current.CancellationToken);
        ReconnectionReconciliationStateService state = new(new FakeTimeProvider(),
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state, Substitute.For<IDispatcher>(),
            new FakeTimeProvider(), NullLogger<ReconnectionReconciliationCoordinator>.Instance);
        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        recovery.IsCompleted.ShouldBeFalse();
        using IDisposable laneB = scheduler.RegisterLane(DefaultLane("b") with {
            ProjectionType = "BProjection",
            RefreshAsync = _ => ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed),
        });

        heldOriginal.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        SpinWait.SpinUntil(() => Volatile.Read(ref readsC) >= 2, TimeSpan.FromSeconds(2)).ShouldBeTrue();
        recovery.IsCompleted.ShouldBeFalse();
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Reconciling);
        heldRecheck.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await originalC.ConfigureAwait(true);
        ProjectionReconciliationRefreshResult result = await recovery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        result.HasIncompleteEligibleReads.ShouldBeFalse();
        result.ChangedViewKeys.ShouldContain("a");
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
    }

    [Fact]
    public async Task ConcurrentGroupHealthPublicationNeverExposesPartiallyBuiltSnapshot()
    {
        const string viewKey = "acme:OrdersProjection";
        IProjectionPageLoader loader = LoaderReturning(new ProjectionPageResult([], 0, null, IsNotModified: true));
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(loader, Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(PageState(viewKey, [], totalCount: 0)));
        using IDisposable lane = scheduler.RegisterLane(DefaultLane(viewKey));
        scheduler.SetReconciliationGroupHealth(new Dictionary<ProjectionFallbackGroupKey, bool> {
            [new("OrdersProjection", "acme")] = false,
        });
        using BlockingGroupHealthSnapshot replacement = new(
            new KeyValuePair<ProjectionFallbackGroupKey, bool>(new("OtherProjection", "acme"), true),
            new KeyValuePair<ProjectionFallbackGroupKey, bool>(new("OrdersProjection", "acme"), true));
        Task publication = Task.Run(() => scheduler.SetReconciliationGroupHealth(replacement), TestContext.Current.CancellationToken);
        await replacement.FirstEntryRead.Task.WaitAsync(TestContext.Current.CancellationToken);
        try
        {
            (await scheduler.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken))
                .ShouldBe(ProjectionReconciliationRefreshResult.Empty);
            _ = loader.DidNotReceiveWithAnyArgs().LoadPageAsync(default!, default, default, default!, default, default,
                default, TestContext.Current.CancellationToken);
        }
        finally
        {
            replacement.Release();
            await publication.WaitAsync(TestContext.Current.CancellationToken);
        }
        (await scheduler.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken)).RefreshedCount.ShouldBe(1);
    }

    [Fact]
    public async Task EmptyBlockingGroupHealthSnapshotEnumeratesWithoutReadingAFirstEntry()
    {
        using BlockingGroupHealthSnapshot empty = new();
        empty.ToArray().ShouldBeEmpty();
        empty.FirstEntryRead.Task.IsCompleted.ShouldBeFalse();

        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(
            LoaderReturning(new ProjectionPageResult([], 0, null, IsNotModified: true)),
            Substitute.For<IDispatcher>(), new MutableLoadedPageState(PageState("acme:OrdersProjection", [], 0)));
        scheduler.SetReconciliationGroupHealth(empty);
        (await scheduler.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken))
            .ShouldBe(ProjectionReconciliationRefreshResult.Empty);
    }

    [Fact]
    public async Task ReconciliationWaitsOnlyForTheEligibleSkippedRead()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsA = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 2 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable laneA = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => Interlocked.Increment(ref readsA) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldA.Task)
                : ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified),
        });
        using IDisposable laneB = scheduler.RegisterLane(DefaultLane("b") with {
            ProjectionType = "OtherProjection",
            RefreshAsync = _ => new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldB.Task),
        });
        scheduler.SetReconciliationGroupHealth(new Dictionary<ProjectionFallbackGroupKey, bool> {
            [new("OtherProjection", "acme")] = false,
        });
        Task<int> originalA = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        Task<int> originalB = scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken);
        ReconnectionReconciliationStateService state = new(new FakeTimeProvider(),
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state, Substitute.For<IDispatcher>(),
            new FakeTimeProvider(), NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        recovery.IsCompleted.ShouldBeFalse();
        heldA.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await originalA.ConfigureAwait(true);
        ProjectionReconciliationRefreshResult result = await recovery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        result.RefreshedCount.ShouldBe(1);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
        originalB.IsCompleted.ShouldBeFalse();
        heldB.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await originalB.ConfigureAwait(true);
    }

    [Fact]
    public async Task ReconciliationRetainsFirstPassChangeAfterASecondUnchangedRead()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldC = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsA = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 2 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable laneA = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => ValueTask.FromResult(Interlocked.Increment(ref readsA) == 1
                ? ProjectionFallbackLaneRefreshOutcome.Changed : ProjectionFallbackLaneRefreshOutcome.NotModified),
        });
        using IDisposable laneC = scheduler.RegisterLane(DefaultLane("c") with {
            ProjectionType = "CProjection",
            RefreshAsync = _ => heldC.Task.IsCompleted
                ? ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified)
                : new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldC.Task),
        });
        Task<int> originalC = scheduler.TriggerNudgeRefreshAsync("CProjection", "acme", TestContext.Current.CancellationToken);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        ReconnectionReconciliationStateService state = new(new FakeTimeProvider(),
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state, dispatcher,
            new FakeTimeProvider(), NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        recovery.IsCompleted.ShouldBeFalse();
        heldC.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await originalC.ConfigureAwait(true);
        ProjectionReconciliationRefreshResult result = await recovery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        result.ChangedViewKeys.ShouldBe(["a"]);
        // The initial pass can queue one C replay. If it wins C's next
        // admission, one extra recheck reads A before the final A/C pass.
        // Every counted result is an actual A read plus one current C read.
        readsA.ShouldBeInRange(2, 3);
        result.RefreshedCount.ShouldBe(readsA + 1);
        result.RefreshedViewKeys.ShouldContain("c");
        result.HasIncompleteEligibleReads.ShouldBeFalse();
        state.Current.Changed.ShouldBeTrue();
        dispatcher.Received(1).Dispatch(Arg.Is<MarkReconciliationSweepAction>(action =>
            action.ViewKeys.Count == 1 && action.ViewKeys[0] == "a"));
    }

    [Fact]
    public async Task LaterSuccessClearsOnlyItsOwnEarlierReconciliationFailure()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldC = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource aStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource cStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsA = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 3 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable laneA = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => {
                if (Interlocked.Increment(ref readsA) == 1) {
                    aStarted.TrySetResult();
                    return ValueTask.FromException<ProjectionFallbackLaneRefreshOutcome>(new InvalidOperationException("first read failed"));
                }
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });
        using IDisposable laneB = scheduler.RegisterLane(DefaultLane("b") with {
            ProjectionType = "BProjection",
            RefreshAsync = _ => ValueTask.FromException<ProjectionFallbackLaneRefreshOutcome>(new InvalidOperationException("read failed")),
        });
        using IDisposable laneC = scheduler.RegisterLane(DefaultLane("c") with {
            ProjectionType = "CProjection",
            RefreshAsync = _ => {
                cStarted.TrySetResult();
                return heldC.Task.IsCompleted
                    ? ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified)
                    : new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldC.Task);
            },
        });
        Task<int> originalC = scheduler.TriggerNudgeRefreshAsync("CProjection", "acme", TestContext.Current.CancellationToken);
        await cStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        ReconnectionReconciliationStateService state = new(new FakeTimeProvider(),
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state, Substitute.For<IDispatcher>(),
            new FakeTimeProvider(), NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        recovery.IsCompleted.ShouldBeFalse();
        await aStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        System.Reflection.FieldInfo inFlightField = typeof(ProjectionFallbackRefreshScheduler).GetField("_inFlight",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        ConcurrentDictionary<string, TaskCompletionSource<bool>> inFlight =
            (ConcurrentDictionary<string, TaskCompletionSource<bool>>)inFlightField.GetValue(scheduler)!;
        SpinWait.SpinUntil(() => !inFlight.ContainsKey("a"), TimeSpan.FromSeconds(2)).ShouldBeTrue();
        heldC.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await originalC.ConfigureAwait(true);
        ProjectionReconciliationRefreshResult result = await recovery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        result.ChangedViewKeys.ShouldBe(["a"]);
        result.FailedViewKeys.ShouldBe(["b"]);
        // The first A callback fails. One queued C replay may make an extra
        // recheck read A; each successful count is a later A or current C read.
        readsA.ShouldBeInRange(2, 3);
        result.RefreshedCount.ShouldBe(readsA);
        result.RefreshedViewKeys.ShouldContain("c");
        result.HasIncompleteEligibleReads.ShouldBeFalse();
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
    }

    [Fact]
    public async Task ReconciliationWaitsForEligibleHeldReadAndThenReportsActualRecovery()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => Interlocked.Increment(ref reads) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(held.Task)
                : ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified),
        });
        ReconnectionReconciliationStateService state = new(new FakeTimeProvider(),
            NullLogger<ReconnectionReconciliationStateService>.Instance);
        using ReconnectionReconciliationCoordinator coordinator = new(scheduler, state,
            Substitute.For<IDispatcher>(), new FakeTimeProvider(),
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<int> original = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        Task<ProjectionReconciliationRefreshResult> recovery = coordinator.ReconcileAsync(TestContext.Current.CancellationToken);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Reconciling);
        recovery.IsCompleted.ShouldBeFalse();
        held.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await original.ConfigureAwait(true);
        ProjectionReconciliationRefreshResult result = await recovery.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        result.RefreshedCount.ShouldBe(1);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
        state.Current.DataRead.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadyReplayBehindNewlyStalledReplayStillStartsWithinBudget()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> originalB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> originalC = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> replayB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replayCStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsB = 0;
        int readsC = 0;
        FcShellOptions config = new() { MaxProjectionFallbackPollingLanes = 1 };
        IOptionsMonitor<FcShellOptions> monitor = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        monitor.CurrentValue.Returns(_ => config);
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            monitor,
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable laneB = scheduler.RegisterLane(DefaultLane("b") with {
            ProjectionType = "BProjection",
            RefreshAsync = _ => Interlocked.Increment(ref readsB) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(originalB.Task)
                : new ValueTask<ProjectionFallbackLaneRefreshOutcome>(replayB.Task),
        });
        using IDisposable laneC = scheduler.RegisterLane(DefaultLane("c") with {
            ProjectionType = "CProjection",
            RefreshAsync = _ => Interlocked.Increment(ref readsC) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(originalC.Task)
                : CompleteC(),
        });
        ValueTask<ProjectionFallbackLaneRefreshOutcome> CompleteC()
        {
            replayCStarted.TrySetResult();
            return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        }

        Task<int> firstB = scheduler.TriggerNudgeRefreshAsync("BProjection", "acme", TestContext.Current.CancellationToken);
        Task<int> firstC = scheduler.TriggerNudgeRefreshAsync("CProjection", "acme", TestContext.Current.CancellationToken);
        (await scheduler.TriggerNudgeRefreshAsync("BProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        (await scheduler.TriggerNudgeRefreshAsync("CProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        config = new FcShellOptions { MaxProjectionFallbackPollingLanes = 2 };
        originalB.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        originalC.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        await replayCStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        readsB.ShouldBe(2);
        readsC.ShouldBe(2);
        replayB.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await Task.WhenAll(firstB, firstC).WaitAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    [Fact]
    public async Task CanceledOriginalReadWakesItsPendingNudge()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> original = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replayStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => Interlocked.Increment(ref reads) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(original.Task)
                : Replay(),
        });
        ValueTask<ProjectionFallbackLaneRefreshOutcome> Replay()
        {
            replayStarted.TrySetResult();
            return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        }

        Task<int> first = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        original.SetCanceled(TestContext.Current.CancellationToken);
        _ = await Should.ThrowAsync<OperationCanceledException>(async () => await first.ConfigureAwait(true));
        await replayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        reads.ShouldBe(2);
    }

    [Fact]
    public async Task FinalLaneDisposalWakesPumpEvenWhenTransportIgnoresCancellation()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsB = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 2 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        IDisposable laneA = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldA.Task),
        });
        using IDisposable laneB = scheduler.RegisterLane(DefaultLane("b") with {
            ProjectionType = "OtherProjection",
            RefreshAsync = _ => Interlocked.Increment(ref readsB) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldB.Task)
                : ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified),
        });
        Task<int> firstA = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        Task<int> firstB = scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        (await scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        heldB.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await firstB.ConfigureAwait(true);
        SpinWait.SpinUntil(() => Volatile.Read(ref readsB) == 2, TimeSpan.FromSeconds(5)).ShouldBeTrue();
        System.Reflection.FieldInfo field = typeof(ProjectionFallbackRefreshScheduler).GetField("_replayPumpRunning",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        SpinWait.SpinUntil(() => (int)field.GetValue(scheduler)! == 1, TimeSpan.FromSeconds(5)).ShouldBeTrue();

        laneA.Dispose();
        SpinWait.SpinUntil(() => (int)field.GetValue(scheduler)! == 0, TimeSpan.FromSeconds(5)).ShouldBeTrue();
        heldA.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await firstA.ConfigureAwait(true);
    }

    [Fact]
    public async Task DisposedHeldLaneCannotEraseReplacementInFlightRead()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> oldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> newRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource oldStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replacementStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int replacementReads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        IDisposable oldLane = scheduler.RegisterLane(DefaultLane("same") with {
            RefreshAsync = _ => {
                oldStarted.TrySetResult();
                return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(oldRead.Task);
            },
        });
        Task<int> oldPass = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await oldStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        oldLane.Dispose();
        using IDisposable replacement = scheduler.RegisterLane(DefaultLane("same") with {
            RefreshAsync = _ => {
                Interlocked.Increment(ref replacementReads);
                replacementStarted.TrySetResult();
                return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(newRead.Task);
            },
        });
        Task<int> newPass = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await replacementStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        replacementReads.ShouldBe(1);
        oldRead.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        (await oldPass.ConfigureAwait(true)).ShouldBe(0);

        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        replacementReads.ShouldBe(1);
        newRead.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        (await newPass.ConfigureAwait(true)).ShouldBe(1);
    }

    [Fact]
    public async Task ReadyPendingLaneReplaysBeforeAnotherHeldLaneFinishes()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replayedB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsB = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldA.Task),
        });
        _ = scheduler.RegisterLane(DefaultLane("b") with {
            ProjectionType = "OtherProjection",
            RefreshAsync = _ => Interlocked.Increment(ref readsB) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldB.Task)
                : CompleteB(),
        });
        ValueTask<ProjectionFallbackLaneRefreshOutcome> CompleteB()
        {
            replayedB.TrySetResult();
            return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        }

        Task<int> firstA = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        Task<int> firstB = scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(0);
        (await scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(0);
        heldB.TrySetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        _ = await firstB.ConfigureAwait(true);
        await replayedB.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        firstA.IsCompleted.ShouldBeFalse();
        heldA.TrySetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        _ = await firstA.ConfigureAwait(true);
    }

    [Fact]
    public async Task ReconciliationWithEligibleHeldLaneIsIncompleteRatherThanAnEmptySuccess()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => {
                started.TrySetResult();
                return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(held.Task);
            },
        });
        Task<int> owner = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        try {
            ProjectionReconciliationRefreshResult result = await scheduler.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken).ConfigureAwait(true);
            result.HasIncompleteEligibleReads.ShouldBeTrue();
            result.RefreshedCount.ShouldBe(0);
        }
        finally {
            held.TrySetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
            _ = await owner.ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task ReplayPumpWaitsForHeldLaneCompletionWithoutPeriodicBudgetReads()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource startedA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource startedB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replayedA = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int readsA = 0;
        int readsB = 0;
        int optionReads = 0;
        FcShellOptions config = new() { MaxProjectionFallbackPollingLanes = 1 };
        IOptionsMonitor<FcShellOptions> monitor = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        monitor.CurrentValue.Returns(_ => {
            Interlocked.Increment(ref optionReads);
            return config;
        });
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(), monitor,
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => {
                if (Interlocked.Increment(ref readsA) == 1) {
                    startedA.TrySetResult();
                    return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldA.Task);
                }
                return CompleteReplay();
            },
        });
        _ = scheduler.RegisterLane(DefaultLane("b") with {
            ProjectionType = "OtherProjection",
            RefreshAsync = _ => {
                Interlocked.Increment(ref readsB);
                startedB.TrySetResult();
                return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldB.Task);
            },
        });
        ValueTask<ProjectionFallbackLaneRefreshOutcome> CompleteReplay()
        {
            replayedA.TrySetResult();
            return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        }

        Task<int> firstA = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await startedA.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(0);
        Task<int> firstB = scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken);
        await startedB.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        (await scheduler.TriggerNudgeRefreshAsync("OtherProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(0);
        heldB.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        (await firstB.ConfigureAwait(true)).ShouldBe(1);
        int beforePump = Volatile.Read(ref optionReads);
        SpinWait.SpinUntil(() => Volatile.Read(ref optionReads) > beforePump, TimeSpan.FromSeconds(2)).ShouldBeTrue();
        SpinWait.SpinUntil(() => Volatile.Read(ref readsB) == 2, TimeSpan.FromSeconds(2)).ShouldBeTrue();
        int whileHeld = Volatile.Read(ref optionReads);
        await Task.Delay(180, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Volatile.Read(ref optionReads).ShouldBeInRange(whileHeld, whileHeld + 1);
        readsA.ShouldBe(1);

        heldA.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        (await firstA.ConfigureAwait(true)).ShouldBe(1);
        await replayedA.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        readsA.ShouldBe(2);
    }

    [Theory]
    [InlineData("fallback", false)]
    [InlineData("fallback", true)]
    [InlineData("nudge", false)]
    [InlineData("nudge", true)]
    [InlineData("reconciliation", false)]
    [InlineData("reconciliation", true)]
    public async Task AdmittedSkippedOrStaleReadConsumesSingleAttemptBudget(string sweep, bool becomesStale)
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> heldRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        MutableLoadedPageState pages = new(new LoadedPageState());
        int firstReads = 0;
        int secondReads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(
                sweep == "fallback" ? ProjectionConnectionStatus.Disconnected : ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)),
            Substitute.For<IProjectionPageLoader>(), Substitute.For<IDispatcher>(), pages,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 1,
            }).ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => {
                Interlocked.Increment(ref firstReads);
                return becomesStale
                    ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(heldRead.Task)
                    : ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Skipped);
            },
        });
        _ = scheduler.RegisterLane(DefaultLane("b") with {
            Skip = 20,
            RefreshAsync = _ => {
                Interlocked.Increment(ref secondReads);
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });

        async Task<int> RunSweepAsync() => sweep switch {
            "fallback" => await scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true),
            "reconciliation" => (await scheduler.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken).ConfigureAwait(true)).RefreshedCount,
            _ => await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true),
        };
        Task<int> pass = RunSweepAsync();
        if (becomesStale) {
            SpinWait.SpinUntil(() => Volatile.Read(ref firstReads) == 1, TimeSpan.FromSeconds(2)).ShouldBeTrue();
            pages.Value = pages.Value with { ScopeGeneration = pages.Value.ScopeGeneration + 1 };
            heldRead.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        }

        (await pass.ConfigureAwait(true)).ShouldBe(0);
        firstReads.ShouldBe(1);
        secondReads.ShouldBe(0);
    }

    [Fact]
    public async Task FallbackReplayQueuedDuringDisconnectStopsAfterReconnectButConnectedNudgeRuns()
    {
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> firstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TestConnectionState connection = new(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Disconnected,
            DateTimeOffset.UtcNow, 0, null));
        int reads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(connection, Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane("fallback-replay") with {
            RefreshAsync = _ => {
                if (Interlocked.Increment(ref reads) == 1) {
                    firstStarted.TrySetResult();
                    return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(firstRead.Task);
                }
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
            },
        });
        Task<int> first = scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        reads.ShouldBe(1);
        (await scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(0);
        connection.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        firstRead.SetResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
        (await first.ConfigureAwait(true)).ShouldBe(1);
        await Task.Delay(100, TestContext.Current.CancellationToken).ConfigureAwait(true);
        reads.ShouldBe(1);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(1);
        reads.ShouldBe(2);
    }

    [Theory]
    [InlineData("fallback")]
    [InlineData("nudge")]
    [InlineData("reconciliation")]
    public async Task HeldNudgeReplayUsesASeparatelyBudgetedSweep(string sweep)
    {
        const string viewKey = "acme:OrdersProjection";
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> firstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource replayStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(
                sweep == "fallback" ? ProjectionConnectionStatus.Disconnected : ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)),
            Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = scheduler.RegisterLane(DefaultLane(viewKey) with {
            RefreshAsync = _ => {
                if (Interlocked.Increment(ref reads) == 1) {
                    firstStarted.TrySetResult();
                    return new ValueTask<ProjectionFallbackLaneRefreshOutcome>(firstRead.Task);
                }
                replayStarted.TrySetResult();
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.NotModified);
            },
        });

        async Task<int> RunSweepAsync() => sweep switch {
            "fallback" => await scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true),
            "reconciliation" => (await scheduler.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken).ConfigureAwait(true)).RefreshedCount,
            _ => await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true),
        };
        Task<int> firstSweep = RunSweepAsync();
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        reads.ShouldBe(1);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        firstRead.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        (await firstSweep.ConfigureAwait(true)).ShouldBe(1);
        reads.ShouldBe(1);
        await replayStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken).ConfigureAwait(true);
        reads.ShouldBe(2);
    }

    [Fact]
    public async Task ReconciliationRetainsFailedReplayAfterEarlierChangedRead()
    {
        const string viewKey = "acme:OrdersProjection";
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> firstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(
            Substitute.For<IProjectionPageLoader>(), Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(PageState(viewKey, ["cached"], 1)));
        _ = scheduler.RegisterLane(DefaultLane(viewKey) with {
            RefreshAsync = _ => Interlocked.Increment(ref reads) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(firstRead.Task)
                : throw new InvalidOperationException("failed replay"),
        });

        Task<ProjectionReconciliationRefreshResult> pass = scheduler.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        firstRead.SetResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        ProjectionReconciliationRefreshResult result = await pass.ConfigureAwait(true);
        reads.ShouldBe(2);
        result.ChangedViewKeys.ShouldBeEmpty();
        result.FailedViewKeys.ShouldBe([viewKey]);
    }

    [Fact]
    public async Task SuccessfulSamePassReplayResolvesEarlierFailure()
    {
        const string viewKey = "acme:OrdersProjection";
        TaskCompletionSource<ProjectionFallbackLaneRefreshOutcome> firstRead = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(
            Substitute.For<IProjectionPageLoader>(), Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(PageState(viewKey, ["cached"], 1)));
        using IDisposable lane = scheduler.RegisterLane(DefaultLane(viewKey) with {
            RefreshAsync = _ => Interlocked.Increment(ref reads) == 1
                ? new ValueTask<ProjectionFallbackLaneRefreshOutcome>(firstRead.Task)
                : ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed),
        });

        Task<ProjectionReconciliationRefreshResult> pass = scheduler.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        firstRead.SetException(new InvalidOperationException("first read failed"));
        ProjectionReconciliationRefreshResult result = await pass.ConfigureAwait(true);
        reads.ShouldBe(2);
        result.RefreshedCount.ShouldBe(1);
        result.ChangedViewKeys.ShouldBe([viewKey]);
        result.FailedViewKeys.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SuccessfulUnchangedValidator200ClearsMatchingStalePage(bool hasEtag)
    {
        const string viewKey = "acme:OrdersProjection";
        MutableLoadedPageState pages = new(PageState(viewKey, ["cached"], 1));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        List<LoadPageNotModifiedAction> completions = [];
        dispatcher.When(x => x.Dispatch(Arg.Any<LoadPageNotModifiedAction>()))
            .Do(call => {
                LoadPageNotModifiedAction action = call.Arg<LoadPageNotModifiedAction>();
                completions.Add(action);
                pages.Value = LoadedPageReducers.ReduceLoadPageNotModified(pages.Value, action);
            });
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(
            LoaderReturning(new ProjectionPageResult(["cached"], 1, hasEtag ? "\"v1\"" : null)), dispatcher, pages);
        _ = scheduler.RegisterLane(DefaultLane(viewKey));

        (await scheduler.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken)).RefreshedCount.ShouldBe(1);
        pages.Value = pages.Value with { StalePages = pages.Value.StalePages.Add((viewKey, 0)) };
        (await scheduler.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken)).RefreshedCount.ShouldBe(1);

        completions.Count.ShouldBe(1);
        pages.Value.StalePages.ShouldNotContain((viewKey, 0));
        pages.Value.PagesByKey[(viewKey, 0)].ShouldBe(["cached"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task FailedEligibleLanesConsumeFallbackAndNudgeAttemptBudget(bool fallback)
    {
        int attempts = 0;
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Disconnected, DateTimeOffset.UtcNow, 1, "Closed"));
        ProjectionFallbackRefreshScheduler scheduler = new(
            state, Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 1,
            }).ToMonitor(), NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        for (int index = 0; index < 3; index++)
        {
            _ = scheduler.RegisterLane(DefaultLane("acme:OrdersProjection:" + index) with {
                RefreshAsync = _ => {
                    Interlocked.Increment(ref attempts);
                    throw new InvalidOperationException("fixture failure");
                },
            });
        }

        int refreshed = fallback
            ? await scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken).ConfigureAwait(true)
            : await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);

        refreshed.ShouldBe(0);
        attempts.ShouldBe(1);
    }

    [Fact]
    public async Task HeldResultWithReusedGenerationCannotUpdateValidatorOrDispatch()
    {
        const string viewKey = "acme:OrdersProjection";
        TaskCompletionSource<ProjectionPageResult> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        int calls = 0;
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                if (Interlocked.Increment(ref calls) == 1) {
                    started.TrySetResult();
                    return held.Task;
                }
                return Task.FromResult(new ProjectionPageResult(["current"], 1, "etag-1"));
            });
        MutableLoadedPageState pages = new(new LoadedPageState {
            ActiveRequestIdentityByView = ImmutableDictionary<string, string>.Empty.Add(viewKey, "A"),
            ActiveRequestGenerationByView = ImmutableDictionary<string, long>.Empty.Add(viewKey, 7),
        });
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(loader, dispatcher, pages);
        _ = scheduler.RegisterLane(DefaultLane(viewKey));

        Task<int> oldRefresh = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        pages.Value = pages.Value with { ActiveRequestIdentityByView = pages.Value.ActiveRequestIdentityByView.SetItem(viewKey, "B") };
        held.TrySetResult(new ProjectionPageResult(["old"], 1, "etag-1"));
        (await oldRefresh.ConfigureAwait(true)).ShouldBe(0);
        (await scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ShouldBe(1);
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.RequestIdentity == "B"));
    }

    [Fact]
    public async Task HeldRefresh_AfterScopeClear_DoesNotDispatchOldOrigin() {
        TaskCompletionSource<ProjectionPageResult> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                started.TrySetResult();
                return held.Task;
            });

        MutableLoadedPageState pages = new(new LoadedPageState());
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        LoadPageSucceededAction? dispatched = null;
        dispatcher.When(x => x.Dispatch(Arg.Any<LoadPageSucceededAction>()))
            .Do(call => dispatched = call.Arg<LoadPageSucceededAction>());
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(loader, dispatcher, pages);
        _ = scheduler.RegisterLane(DefaultLane("acme:OrdersProjection"));

        Task<int> refresh = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        pages.Value = LoadedPageReducers.ReduceScopeChanged(pages.Value, new ScopeChangedAction());
        held.TrySetResult(new ProjectionPageResult(["old"], 1, "v1"));
        _ = await refresh.ConfigureAwait(true);

        dispatched.ShouldBeNull();
    }

    [Fact]
    public async Task HeldNotModified_AfterScopeClear_CannotCompleteNewScopePage() {
        const string viewKey = "acme:OrdersProjection";
        TaskCompletionSource<ProjectionPageResult> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                started.TrySetResult();
                return held.Task;
            });
        MutableLoadedPageState pages = new(PageState(viewKey, ["old"], 1));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        LoadPageNotModifiedAction? dispatched = null;
        dispatcher.When(x => x.Dispatch(Arg.Any<LoadPageNotModifiedAction>()))
            .Do(call => dispatched = call.Arg<LoadPageNotModifiedAction>());
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(loader, dispatcher, pages);
        _ = scheduler.RegisterLane(DefaultLane(viewKey));

        Task<int> refresh = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        LoadedPageState cleared = LoadedPageReducers.ReduceScopeChanged(pages.Value, new ScopeChangedAction());
        TaskCompletionSource<object> pendingB = new(TaskCreationOptions.RunContinuationsAsynchronously);
        pages.Value = cleared with {
            PagesByKey = ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>>.Empty.Add((viewKey, 0), ["current"]),
            PendingCompletionsByKey = ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>>.Empty.Add((viewKey, 0), pendingB),
        };
        held.TrySetResult(new ProjectionPageResult(["old"], 1, "v1", IsNotModified: true));
        _ = await refresh.ConfigureAwait(true);

        dispatched.ShouldBeNull();
        pendingB.Task.IsCompleted.ShouldBeFalse();
        LoadedPageReducers.ReduceLoadPageNotModified(pages.Value,
            new LoadPageNotModifiedAction(viewKey, 0, ["current"]) { OriginScopeGeneration = 1 })
            .PendingCompletionsByKey.ShouldBeEmpty();
        pendingB.Task.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task HeldFallbackAResultCannotDispatchAfterABAReturnsToSameCriteria() {
        const string viewKey = "acme:OrdersProjection";
        TaskCompletionSource<ProjectionPageResult> held = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ => {
                started.TrySetResult();
                return held.Task;
            });
        MutableLoadedPageState pages = new(new LoadedPageState {
            ActiveRequestIdentityByView = ImmutableDictionary<string, string>.Empty.Add(viewKey, "A"),
            ActiveRequestGenerationByView = ImmutableDictionary<string, long>.Empty.Add(viewKey, 1),
        });
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(loader, dispatcher, pages);
        _ = scheduler.RegisterLane(DefaultLane(viewKey));

        Task<int> refresh = scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken).ConfigureAwait(true);
        pages.Value = pages.Value with {
            ActiveRequestIdentityByView = pages.Value.ActiveRequestIdentityByView.SetItem(viewKey, "B"),
            ActiveRequestGenerationByView = pages.Value.ActiveRequestGenerationByView.SetItem(viewKey, 2),
        };
        pages.Value = pages.Value with {
            ActiveRequestIdentityByView = pages.Value.ActiveRequestIdentityByView.SetItem(viewKey, "A"),
            ActiveRequestGenerationByView = pages.Value.ActiveRequestGenerationByView.SetItem(viewKey, 3),
        };
        held.TrySetResult(new ProjectionPageResult(["old-A"], 1, "v1"));

        (await refresh.ConfigureAwait(true)).ShouldBe(0);
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());
    }

    [Fact]
    public async Task TriggerFallbackOnce_PollsOnlyWhenDisconnected_AndBoundsLaneCount() {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Disconnected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 1,
            LastFailureCategory: "Closed"));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(Array.Empty<object>(), 0, null, IsNotModified: true)));

        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 1,
            }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);

        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:CustomersProjection", "CustomersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        int refreshed = await sut.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken);

        refreshed.ShouldBe(1);
        _ = loader.Received(1).LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerFallbackOnce_DoesNotPoll_WhenConnectedOrDisabled() {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 0 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        int refreshed = await sut.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken);

        refreshed.ShouldBe(0);
        _ = loader.DidNotReceiveWithAnyArgs().LoadPageAsync(default!, default, default, default!, default, default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TriggerNudgeRefreshAsync_RequeriesOnlyMatchingTenantProjectionLane() {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(Array.Empty<object>(), 0, null)));

        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 10 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("other:OrdersProjection", "OrdersProjection", "other", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:CustomersProjection", "CustomersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        int refreshed = await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);

        refreshed.ShouldBe(1);
        _ = loader.Received(1).LoadPageAsync(
            "OrdersProjection",
            0,
            20,
            Arg.Any<IImmutableDictionary<string, string>>(),
            null,
            false,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerReconciliationOnce_SnapshotsVisibleLanes_RefreshesEveryDistinctView() {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            "CustomersProjection",
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(Array.Empty<object>(), 0, null, IsNotModified: true)));
        loader.LoadPageAsync(
            "OrdersProjection",
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(new object[] { "order-2" }, 1, "\"v2\"")));

        LoadedPageState pageState = new() {
            PagesByKey = ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>>.Empty
                .Add(("acme:CustomersProjection", 0), [])
                .Add(("acme:OrdersProjection", 0), ["order-1"]),
            TotalCountByKey = ImmutableDictionary<string, int>.Empty
                .Add("acme:CustomersProjection", 0)
                .Add("acme:OrdersProjection", 1),
        };
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(
            loader,
            Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(pageState));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:CustomersProjection", "CustomersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection:dup", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(42, TestContext.Current.CancellationToken);

        result.RefreshedCount.ShouldBe(3);
        result.ChangedViewKeys.ShouldBe(["acme:OrdersProjection", "acme:OrdersProjection:dup"]);
        _ = loader.Received(2).LoadPageAsync(
            "OrdersProjection",
            0,
            20,
            Arg.Any<IImmutableDictionary<string, string>>(),
            null,
            false,
            null,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReconciliationSameQueryDistinctViewsDispatchesBothAndCoMountedViewReadsOnce()
    {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(["row"], 1, "etag")));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler(loader, dispatcher,
            new MutableLoadedPageState(new LoadedPageState()));
        ProjectionFallbackLane shared = DefaultLane("a");
        using IDisposable firstMount = scheduler.RegisterLane(shared);
        using IDisposable secondMount = scheduler.RegisterLane(shared);
        using IDisposable otherView = scheduler.RegisterLane(shared with { ViewKey = "b" });

        ProjectionReconciliationRefreshResult result = await scheduler.TriggerReconciliationOnceAsync(1,
            TestContext.Current.CancellationToken);

        result.RefreshedCount.ShouldBe(2);
        result.RefreshedViewKeys.ShouldBe(["a", "b"]);
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.ViewKey == "a"));
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.ViewKey == "b"));
        _ = loader.Received(2).LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdmittedSkippedReadUsesOneAttemptAndDoesNotBecomeEmptyPass()
    {
        int firstReads = 0;
        int secondReads = 0;
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(ProjectionConnectionStatus.Connected,
                DateTimeOffset.UtcNow, 0, null)), Substitute.For<IProjectionPageLoader>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        using IDisposable first = scheduler.RegisterLane(DefaultLane("a") with {
            RefreshAsync = _ => {
                firstReads++;
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Skipped);
            },
        });
        using IDisposable second = scheduler.RegisterLane(DefaultLane("b") with {
            RefreshAsync = _ => {
                secondReads++;
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            },
        });

        ProjectionReconciliationRefreshResult result = await scheduler.TriggerReconciliationOnceAsync(1,
            TestContext.Current.CancellationToken);

        firstReads.ShouldBe(1);
        secondReads.ShouldBe(0);
        result.ShouldNotBe(ProjectionReconciliationRefreshResult.Empty);
        result.SkippedViewKeys.ShouldBe(["a"]);
        result.RefreshedCount.ShouldBe(0);
    }

    [Fact]
    public async Task TriggerReconciliationOnce_RespectsLaneCap() {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(new object[] { "changed" }, 1, "\"v1\"")));

        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 1 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("b", "BProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("a", "AProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(43, TestContext.Current.CancellationToken);

        result.RefreshedCount.ShouldBe(1);
        _ = loader.Received(1).LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerReconciliationOnce_DetectsChangeViaETagComparison() {
        // P51 / DN4=b — wire-level ETag is the canonical change signal. First refresh records the
        // ETag and reports Changed (initial observation with items). Second refresh with the SAME
        // ETag must report NotModified — no false positives for class-typed projections that lack
        // value-equality semantics.
        const string viewKey = "acme:OrdersProjection";
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            "OrdersProjection",
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(new object[] { "order-1" }, 1, "\"v1\"")));

        ProjectionFallbackRefreshScheduler sut = CreateScheduler(
            loader,
            Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(PageState(viewKey, ["order-1"], totalCount: 1)));
        _ = sut.RegisterLane(DefaultLane(viewKey));

        ProjectionReconciliationRefreshResult firstPass = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult secondPass = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);

        // First pass: first observation with items → Changed.
        firstPass.ChangedViewKeys.ShouldBe([viewKey]);
        // Second pass: same ETag → NotModified, no false-positive Changed.
        secondPass.ChangedViewKeys.ShouldBeEmpty();

        IDispatcher continuityDispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler continuitySut = CreateScheduler(
            LoaderReturning(new Queue<ProjectionPageResult>([
                new ProjectionPageResult(["order-1"], 1, "\"v1\""),
                new ProjectionPageResult(["order-1"], 1, "\"v2\"", IsNotModified: true),
                new ProjectionPageResult(["order-1"], 1, "\"v2\""),
            ])),
            continuityDispatcher,
            new MutableLoadedPageState(PageState(viewKey, ["order-1"], totalCount: 1)));
        _ = continuitySut.RegisterLane(DefaultLane(viewKey));

        (await continuitySut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken))
            .ChangedViewKeys.ShouldBe([viewKey]);
        (await continuitySut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken))
            .ChangedViewKeys.ShouldBeEmpty();
        (await continuitySut.TriggerReconciliationOnceAsync(3, TestContext.Current.CancellationToken))
            .ChangedViewKeys.ShouldBeEmpty();
        continuityDispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.ViewKey == viewKey));
        continuityDispatcher.Received(2).Dispatch(Arg.Is<LoadPageNotModifiedAction>(action =>
            action.ViewKey == viewKey));

        ThrowOnceDispatcher throwingDispatcher = new();
        ProjectionFallbackRefreshScheduler retrySut = CreateScheduler(
            LoaderReturning(new ProjectionPageResult(["order-2"], 1, "\"v2\"")),
            throwingDispatcher,
            new MutableLoadedPageState(PageState(viewKey, ["order-1"], totalCount: 1)));
        _ = retrySut.RegisterLane(DefaultLane(viewKey));

        (await retrySut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken))
            .FailedViewKeys.ShouldBe([viewKey]);
        (await retrySut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken))
            .ChangedViewKeys.ShouldBe([viewKey]);
        throwingDispatcher.SuccessfulDispatchCount.ShouldBe(2); // Stale marker, then the successful page.

        TaskCompletionSource<bool> replayLoadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionPageResult> releaseReplayLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader replayLoader = Substitute.For<IProjectionPageLoader>();
        replayLoader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(
                _ => {
                    replayLoadStarted.TrySetResult(true);
                    return releaseReplayLoad.Task;
                },
                _ => Task.FromResult(new ProjectionPageResult(["order-3"], 1, "\"v3\"")));
        ProjectionFallbackRefreshScheduler replaySut = CreateScheduler(
            replayLoader,
            Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(PageState(viewKey, ["order-2"], totalCount: 1)));
        _ = replaySut.RegisterLane(DefaultLane(viewKey));

        Task<ProjectionReconciliationRefreshResult> changedPass = replaySut.TriggerReconciliationOnceAsync(
            1,
            TestContext.Current.CancellationToken);
        _ = await replayLoadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        (await replaySut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        releaseReplayLoad.SetResult(new ProjectionPageResult(["order-3"], 1, "\"v3\""));

        (await changedPass.ConfigureAwait(true)).ChangedViewKeys.ShouldBe([viewKey]);
    }

    [Fact]
    public async Task TriggerReconciliationOnce_SkipsLanesWithoutTenant() {
        // P29 / P46 — fail-closed: a lane registered without a tenant is skipped during the
        // reconciliation pass and does not consume any of the loader's mock budget.
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(Array.Empty<object>(), 0, null, IsNotModified: true)));

        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 10 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        // Lane WITHOUT tenant — must be skipped by reconciliation but still permitted for
        // registration so that disconnected fallback polling can pick it up later if/when
        // a tenant becomes available.
        _ = sut.RegisterLane(new ProjectionFallbackLane("anon:OrdersProjection", "OrdersProjection", null, 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(99, TestContext.Current.CancellationToken);

        // Only the tenant-bearing lane was reconciled.
        result.RefreshedCount.ShouldBe(1);
    }

    [Fact]
    public async Task TriggerReconciliationOnce_BudgetZero_SkipsAllLanes() {
        // P22 — budget==0 produces a single warning and skips the pass entirely.
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 0 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);

        result.ShouldBe(ProjectionReconciliationRefreshResult.Empty);
        _ = loader.DidNotReceiveWithAnyArgs().LoadPageAsync(default!, default, default, default!, default, default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TriggerReconciliationOnce_DistinctFiltersBypassDedupe() {
        // P26 — two lanes with the same projection-type/tenant/page but different filters must
        // NOT collapse via dedupe.
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(Array.Empty<object>(), 0, null, IsNotModified: true)));

        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 10 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        ImmutableDictionary<string, string> filtersA = ImmutableDictionary<string, string>.Empty.Add("status", "open");
        ImmutableDictionary<string, string> filtersB = ImmutableDictionary<string, string>.Empty.Add("status", "closed");
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:Orders:open", "OrdersProjection", "acme", 0, 20, filtersA, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:Orders:closed", "OrdersProjection", "acme", 0, 20, filtersB, null, false, null));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(101, TestContext.Current.CancellationToken);

        result.RefreshedCount.ShouldBe(2);
    }

    [Fact]
    public async Task TriggerReconciliationOnce_StaleEpochDoesNotRollBackLatestObservedEpoch() {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(Array.Empty<object>(), 0, null, IsNotModified: true)));

        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 10 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        _ = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult stale = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        _ = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);

        stale.ShouldBe(ProjectionReconciliationRefreshResult.Empty);
        _ = loader.Received(2).LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TriggerReconciliationOnce_SkipsLaneWhenGroupRejoinFailed() {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 10 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        sut.SetReconciliationGroupHealth(new Dictionary<ProjectionFallbackGroupKey, bool> {
            [new("OrdersProjection", "acme")] = false,
        });

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(5, TestContext.Current.CancellationToken);

        result.ShouldBe(ProjectionReconciliationRefreshResult.Empty);
        _ = loader.DidNotReceiveWithAnyArgs().LoadPageAsync(default!, default, default, default!, default, default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task TriggerReconciliationOnce_NoEtagLaneDoesNotRepeatChangedForSameSignature() {
        const string viewKey = "acme:OrdersProjection";
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(new object[] { "order-1" }, 1, null)));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        MutableLoadedPageState loadedPages = new(PageState(viewKey, ["existing"], totalCount: 1));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, dispatcher, loadedPages);
        _ = sut.RegisterLane(DefaultLane(viewKey));

        ProjectionReconciliationRefreshResult first = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult second = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);
        loadedPages.Value = new LoadedPageState();
        ProjectionReconciliationRefreshResult afterEviction = await sut.TriggerReconciliationOnceAsync(3, TestContext.Current.CancellationToken);

        first.ChangedViewKeys.ShouldBe([viewKey]);
        second.ChangedViewKeys.ShouldBeEmpty();
        afterEviction.ChangedViewKeys.ShouldBe([viewKey]);
        dispatcher.Received(2).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.ViewKey == viewKey));
    }

    [Fact]
    public async Task TriggerReconciliationOnce_FilterDelimiterValuesDoNotCollapseDedupeKey() {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProjectionPageResult(Array.Empty<object>(), 0, null, IsNotModified: true)));
        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 10 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("a", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty.Add("x,y", "z"), null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("b", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty.Add("x", "y=z"), null, false, null));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(6, TestContext.Current.CancellationToken);

        result.RefreshedCount.ShouldBe(2);
    }

    [Fact]
    public async Task TriggerReconciliationOnce_NoEtagEqualCountsAndChangedRows_DispatchesChanged() {
        const string viewKey = "acme:OrdersProjection";
        object[] overLimitRows = Enumerable.Range(0, 129).Select(static value => (object)value).ToArray();
        string oversizedRow = new('x', 17 * 1024);
        JsonElement deepRow = DeepJsonRow(33);
        Queue<ProjectionPageResult> results = new([
            new ProjectionPageResult([JsonRow("{\"id\":1,\"values\":[1,2]}")], 1, null),
            new ProjectionPageResult([JsonRow("{\"id\":1,\"values\":[2,1]}")], 1, null),
            new ProjectionPageResult([JsonRow("{\"id\":1,\"values\":[2,1]}")], 2, null),
            new ProjectionPageResult(overLimitRows, overLimitRows.Length, null),
            new ProjectionPageResult(overLimitRows, overLimitRows.Length, null),
            new ProjectionPageResult([oversizedRow], 1, null),
            new ProjectionPageResult([oversizedRow], 1, null),
            new ProjectionPageResult([deepRow], 1, null),
            new ProjectionPageResult([deepRow], 1, null),
            new ProjectionPageResult([new ThrowingRow()], 1, null),
            new ProjectionPageResult([new ThrowingRow()], 1, null),
        ]);
        IProjectionPageLoader loader = LoaderReturning(results);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        MutableLoadedPageState loadedPages = new(PageState(viewKey, ["existing"], totalCount: 1));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, dispatcher, loadedPages);
        _ = sut.RegisterLane(DefaultLane(viewKey));

        ProjectionReconciliationRefreshResult first = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult second = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult totalOnly = await sut.TriggerReconciliationOnceAsync(3, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult boundedFirst = await sut.TriggerReconciliationOnceAsync(4, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult boundedRepeat = await sut.TriggerReconciliationOnceAsync(5, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult oversizedFirst = await sut.TriggerReconciliationOnceAsync(6, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult oversizedRepeat = await sut.TriggerReconciliationOnceAsync(7, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult deepFirst = await sut.TriggerReconciliationOnceAsync(8, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult deepRepeat = await sut.TriggerReconciliationOnceAsync(9, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult failedFirst = await sut.TriggerReconciliationOnceAsync(10, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult failedRepeat = await sut.TriggerReconciliationOnceAsync(11, TestContext.Current.CancellationToken);

        first.ChangedViewKeys.ShouldBe([viewKey]);
        second.ChangedViewKeys.ShouldBe([viewKey]);
        totalOnly.ChangedViewKeys.ShouldBe([viewKey]);
        boundedFirst.ChangedViewKeys.ShouldBe([viewKey]);
        boundedRepeat.ChangedViewKeys.ShouldBe([viewKey]);
        oversizedFirst.ChangedViewKeys.ShouldBe([viewKey]);
        oversizedRepeat.ChangedViewKeys.ShouldBe([viewKey]);
        deepFirst.ChangedViewKeys.ShouldBe([viewKey]);
        deepRepeat.ChangedViewKeys.ShouldBe([viewKey]);
        failedFirst.ChangedViewKeys.ShouldBe([viewKey]);
        failedRepeat.ChangedViewKeys.ShouldBe([viewKey]);
        dispatcher.Received(11).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.ViewKey == viewKey));
    }

    [Fact]
    public async Task TriggerReconciliationOnce_NoEtagEquivalentCanonicalRows_DoesNotDispatchAgain() {
        const string viewKey = "acme:OrdersProjection";
        Queue<ProjectionPageResult> results = new([
            new ProjectionPageResult([JsonRow("{\"id\":1,\"details\":{\"name\":\"one\",\"rank\":2},\"values\":[1,2]}")], 1, null),
            new ProjectionPageResult([JsonRow("{\"values\":[1,2],\"details\":{\"rank\":2,\"name\":\"one\"},\"id\":1}")], 1, null),
        ]);
        IProjectionPageLoader loader = LoaderReturning(results);
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        MutableLoadedPageState loadedPages = new(PageState(viewKey, ["existing"], totalCount: 1));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, dispatcher, loadedPages);
        _ = sut.RegisterLane(DefaultLane(viewKey));

        ProjectionReconciliationRefreshResult first = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult second = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);

        first.ChangedViewKeys.ShouldBe([viewKey]);
        second.ChangedViewKeys.ShouldBeEmpty();
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.ViewKey == viewKey));
    }

    [Fact]
    public async Task TriggerReconciliationOnce_EqualEtagAndMissingEmptyPage_RebuildsPage() {
        const string viewKey = "acme:OrdersProjection";
        IProjectionPageLoader loader = LoaderReturning(new ProjectionPageResult([], 0, "\"v1\""));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        MutableLoadedPageState loadedPages = new(PageState(viewKey, [], totalCount: 0));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, dispatcher, loadedPages);
        _ = sut.RegisterLane(DefaultLane(viewKey));

        _ = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        dispatcher.ClearReceivedCalls();
        loadedPages.Value = new LoadedPageState();

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);

        result.ChangedViewKeys.ShouldBe([viewKey]);
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.ViewKey == viewKey && action.Items != null && action.Items.Count == 0));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageNotModifiedAction>());
    }

    [Fact]
    public async Task TriggerReconciliationOnce_NotModifiedAndMissingEmptyPage_RebuildsPage() {
        const string viewKey = "acme:OrdersProjection";
        IProjectionPageLoader loader = LoaderReturning(new Queue<ProjectionPageResult>([
            new ProjectionPageResult([], 0, "\"v1\"", IsNotModified: true),
            new ProjectionPageResult([], 0, "\"v1\""),
        ]));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        MutableLoadedPageState loadedPages = new(new LoadedPageState());
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, dispatcher, loadedPages);
        _ = sut.RegisterLane(DefaultLane(viewKey));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);

        result.ChangedViewKeys.ShouldBe([viewKey]);
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.ViewKey == viewKey && action.Items != null && action.Items.Count == 0));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageNotModifiedAction>());

        loadedPages.Value = PageState(viewKey, [], totalCount: 0);
        dispatcher.ClearReceivedCalls();
        ProjectionReconciliationRefreshResult afterRebuild = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);

        afterRebuild.ChangedViewKeys.ShouldBeEmpty();
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());

        IDispatcher malformedDispatcher = Substitute.For<IDispatcher>();
        IProjectionPageLoader malformedLoader = LoaderReturning(new Queue<ProjectionPageResult>([
            new ProjectionPageResult([], -1, "\"v1\"", IsNotModified: true),
            new ProjectionPageResult(null!, 0, "\"v1\"", IsNotModified: true),
        ]));
        ProjectionFallbackRefreshScheduler malformedSut = CreateScheduler(
            malformedLoader,
            malformedDispatcher,
            new MutableLoadedPageState(new LoadedPageState()));
        _ = malformedSut.RegisterLane(DefaultLane(viewKey));

        (await malformedSut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken))
            .FailedViewKeys.ShouldBe([viewKey]);
        (await malformedSut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken))
            .FailedViewKeys.ShouldBe([viewKey]);
        malformedDispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());
        malformedDispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageNotModifiedAction>());
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(0, false)]
    public async Task MissingEmptyPageWithPositiveTotalNeedsOutOfRangeProof(int skip, bool isOutOfRange)
    {
        const string viewKey = "acme:OrdersProjection";
        IProjectionPageLoader loader = LoaderReturning(new ProjectionPageResult([], 5, "\"v1\"", IsNotModified: true));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        MutableLoadedPageState pages = new(new LoadedPageState());
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, dispatcher, pages);
        using IDisposable lane = sut.RegisterLane(DefaultLane(viewKey) with { Skip = skip });

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(
            1, TestContext.Current.CancellationToken);

        if (isOutOfRange) {
            result.ChangedViewKeys.ShouldBe([viewKey]);
            result.FailedViewKeys.ShouldBeEmpty();
            dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
                action.ViewKey == viewKey && action.Skip == skip && action.TotalCount == 5
                && action.Items != null && action.Items.Count == 0));
        }
        else {
            result.RefreshedCount.ShouldBe(0);
            result.FailedViewKeys.ShouldBe([viewKey]);
            dispatcher.Received(1).Dispatch(Arg.Is<MarkProjectionStaleAction>(action =>
                action.ViewKey == viewKey && action.Skip == skip));
            dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());
        }
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageNotModifiedAction>());
    }

    [Theory]
    [InlineData(10, true)]
    [InlineData(0, false)]
    public async Task EmptyPageEvictedDuringUnchangedHandoffNeedsOutOfRangeProof(int skip, bool isOutOfRange)
    {
        const string viewKey = "acme:OrdersProjection";
        (string ViewKey, int Skip) key = (viewKey, skip);
        MutableLoadedPageState pages = new(new LoadedPageState {
            PagesByKey = ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>>.Empty.Add(key, []),
            StalePages = ImmutableHashSet<(string ViewKey, int Skip)>.Empty.Add(key),
            TotalCountByKey = ImmutableDictionary<string, int>.Empty.Add(viewKey, 5),
        });
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        dispatcher.When(x => x.Dispatch(Arg.Any<LoadPageNotModifiedAction>())).Do(call => {
            LoadPageNotModifiedAction action = call.Arg<LoadPageNotModifiedAction>();
            pages.Value = pages.Value with { PagesByKey = pages.Value.PagesByKey.Remove(key) };
            pages.Value = LoadedPageReducers.ReduceLoadPageNotModified(pages.Value, action);
        });
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(
            LoaderReturning(new ProjectionPageResult([], 5, "\"v1\"", IsNotModified: true)),
            dispatcher, pages);
        using IDisposable lane = sut.RegisterLane(DefaultLane(viewKey) with { Skip = skip });

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(
            1, TestContext.Current.CancellationToken);

        result.ChangedViewKeys.ShouldBeEmpty();
        if (isOutOfRange) {
            result.RefreshedCount.ShouldBe(1);
            pages.Value.PagesByKey[key].ShouldBeEmpty();
            pages.Value.TotalCountByKey[viewKey].ShouldBe(5);
            pages.Value.StalePages.ShouldNotContain(key);
            pages.Value.SuccessfulReadSequenceByPage.ShouldContainKey(key);
        }
        else {
            result.RefreshedCount.ShouldBe(0);
            result.SkippedViewKeys.ShouldBe([viewKey]);
            pages.Value.PagesByKey.ShouldNotContainKey(key);
            pages.Value.StalePages.ShouldContain(key);
            pages.Value.SuccessfulReadSequenceByPage.ShouldNotContainKey(key);
        }
        dispatcher.Received(1).Dispatch(Arg.Any<LoadPageNotModifiedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());
    }

    [Fact]
    public async Task RegisterLane_EquivalentContracts_RefcountUntilFinalDisposal() {
        const string viewKey = "acme:OrdersProjection";
        IProjectionPageLoader loader = LoaderReturning(new ProjectionPageResult([], 0, null, IsNotModified: true));
        MutableLoadedPageState loadedPages = new(PageState(viewKey, [], totalCount: 0));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, Substitute.For<IDispatcher>(), loadedPages);
        ProjectionFallbackLane firstLane = DefaultLane(
            viewKey,
            ImmutableDictionary<string, string>.Empty.Add("status", "open").Add("region", "west"));
        ProjectionFallbackLane secondLane = DefaultLane(
            viewKey,
            ImmutableDictionary<string, string>.Empty.Add("region", "west").Add("status", "open"));

        IDisposable first = sut.RegisterLane(firstLane);
        IDisposable second = sut.RegisterLane(secondLane);

        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        first.Dispose();
        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        second.Dispose();
        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        _ = loader.Received(2).LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegisterReplacingLane_CoMountedCriteriaHandoffKeepsOnlyCurrentContract()
    {
        const string viewKey = "acme:OrdersProjection";
        IProjectionPageLoader loader = LoaderReturning(new ProjectionPageResult([], 0, null, IsNotModified: true));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(PageState(viewKey, [], totalCount: 0)));
        ProjectionFallbackLane old = DefaultLane(viewKey,
            ImmutableDictionary<string, string>.Empty.Add("status", "open"));
        ProjectionFallbackLane current = old with {
            Filters = ImmutableDictionary<string, string>.Empty.Add("status", "closed"),
        };
        IDisposable firstOld = sut.RegisterReplacingLane(old);
        IDisposable secondOld = sut.RegisterReplacingLane(old);
        IDisposable firstCurrent = sut.RegisterReplacingLane(current);
        firstOld.Dispose();

        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        _ = loader.Received(1).LoadPageAsync("OrdersProjection", 0, current.Take,
            Arg.Is<IImmutableDictionary<string, string>>(filters => filters["status"] == "closed"),
            current.SortColumn, current.SortDescending, current.SearchQuery, Arg.Any<CancellationToken>());

        IDisposable secondCurrent = sut.RegisterReplacingLane(current);
        secondOld.Dispose();
        firstCurrent.Dispose();
        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        secondCurrent.Dispose();
        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public async Task RegisterLane_ConflictingTenant_RejectsAndRetainsIncumbent() {
        const string viewKey = "shared-view";
        IProjectionPageLoader loader = LoaderReturning(new ProjectionPageResult([], 0, null, IsNotModified: true));
        MutableLoadedPageState loadedPages = new(PageState(viewKey, [], totalCount: 0));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, Substitute.For<IDispatcher>(), loadedPages);
        _ = sut.RegisterLane(DefaultLane(viewKey));

        InvalidOperationException exception = Should.Throw<InvalidOperationException>(() =>
            sut.RegisterLane(DefaultLane(viewKey) with { TenantId = "other" }));

        exception.Message.ShouldBe("A conflicting fallback lane contract is already registered for this view.");
        exception.Message.ShouldNotContain("acme", Case.Insensitive);
        exception.Message.ShouldNotContain("other", Case.Insensitive);
        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "other", TestContext.Current.CancellationToken)).ShouldBe(0);
    }

    [Fact]
    public void RegisterLane_ConflictingQueryOrCallback_RejectsEveryContractDifference() {
        const string viewKey = "shared-view";
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(
            Substitute.For<IProjectionPageLoader>(),
            Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(new LoadedPageState()));
        Func<CancellationToken, ValueTask<ProjectionFallbackLaneRefreshOutcome>> incumbentCallback = CallbackOutcomeAsync;
        Func<CancellationToken, ValueTask<ProjectionFallbackLaneRefreshOutcome>> equalButDistinctCallback = CallbackOutcomeAsync;
        incumbentCallback.ShouldNotBeSameAs(equalButDistinctCallback);
        incumbentCallback.ShouldBe(equalButDistinctCallback);
        ProjectionFallbackLane incumbent = DefaultLane(
            viewKey,
            ImmutableDictionary<string, string>.Empty.Add("status", "open")) with { RefreshAsync = incumbentCallback };
        _ = sut.RegisterLane(incumbent);
        ProjectionFallbackLane[] conflicts = [
            incumbent with { ProjectionType = "CustomersProjection" },
            incumbent with { Skip = 20 },
            incumbent with { Take = 50 },
            incumbent with { Filters = incumbent.Filters.SetItem("status", "closed") },
            incumbent with { SortColumn = "Name" },
            incumbent with { SortDescending = true },
            incumbent with { SearchQuery = "query" },
            incumbent with { RefreshAsync = null },
            incumbent with { RefreshAsync = equalButDistinctCallback },
        ];

        foreach (ProjectionFallbackLane conflict in conflicts) {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => sut.RegisterLane(conflict));
            exception.Message.ShouldBe("A conflicting fallback lane contract is already registered for this view.");
        }
    }

    private ValueTask<ProjectionFallbackLaneRefreshOutcome> CallbackOutcomeAsync(CancellationToken cancellationToken)
        => ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);

    [Fact]
    public async Task RegisterLane_AfterFinalDisposal_AllowsNewContractAndClearsSignature() {
        const string viewKey = "shared-view";
        ProjectionPageResult page = new([JsonRow("{\"id\":1}")], 1, null);
        IProjectionPageLoader loader = LoaderReturning(page);
        MutableLoadedPageState loadedPages = new(PageState(viewKey, ["existing"], totalCount: 1));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, Substitute.For<IDispatcher>(), loadedPages);
        IDisposable registration = sut.RegisterLane(DefaultLane(viewKey));

        ProjectionReconciliationRefreshResult first = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        registration.Dispose();
        _ = sut.RegisterLane(DefaultLane(viewKey) with { TenantId = "other" });
        ProjectionReconciliationRefreshResult second = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);

        first.ChangedViewKeys.ShouldBe([viewKey]);
        second.ChangedViewKeys.ShouldBe([viewKey]);

        IDispatcher etagDispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler etagSut = CreateScheduler(
            LoaderReturning(new ProjectionPageResult(["replacement"], 1, "\"shared-etag\"")),
            etagDispatcher,
            new MutableLoadedPageState(PageState(viewKey, ["existing"], totalCount: 1)));
        IDisposable etagRegistration = etagSut.RegisterLane(DefaultLane(viewKey));
        (await etagSut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken))
            .ChangedViewKeys.ShouldBe([viewKey]);
        etagRegistration.Dispose();
        _ = etagSut.RegisterLane(DefaultLane(viewKey) with { TenantId = "other" });
        (await etagSut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken))
            .ChangedViewKeys.ShouldBe([viewKey]);
        etagDispatcher.Received(2).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.ViewKey == viewKey));

        IDispatcher successorDispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler successorSut = CreateScheduler(
            LoaderReturning(new Queue<ProjectionPageResult>([
                new ProjectionPageResult(["leftover"], 1, "\"successor-etag\"", IsNotModified: true),
                new ProjectionPageResult(["replacement"], 1, "\"successor-etag\""),
            ])),
            successorDispatcher,
            new MutableLoadedPageState(PageState(viewKey, ["leftover"], totalCount: 1)));
        IDisposable predecessor = successorSut.RegisterLane(DefaultLane(viewKey));
        predecessor.Dispose();
        _ = successorSut.RegisterLane(DefaultLane(viewKey) with { TenantId = "other" });

        (await successorSut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken))
            .ChangedViewKeys.ShouldBeEmpty();
        (await successorSut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken))
            .ChangedViewKeys.ShouldBe([viewKey]);
        successorDispatcher.Received(1).Dispatch(Arg.Is<LoadPageNotModifiedAction>(action =>
            action.ViewKey == viewKey));
        successorDispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.ViewKey == viewKey
            && action.Items != null
            && action.Items.SequenceEqual(new object[] { "replacement" })));

        TaskCompletionSource<ProjectionPageResult> staleResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource staleReadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader staleLoader = Substitute.For<IProjectionPageLoader>();
        staleLoader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
            Arg.Any<string?>(), Arg.Any<CancellationToken>()).Returns(_ => {
                staleReadStarted.TrySetResult();
                return staleResult.Task;
            });
        IDispatcher staleDispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler staleSut = CreateScheduler(staleLoader, staleDispatcher,
            new MutableLoadedPageState(new LoadedPageState()));
        IDisposable incumbent = staleSut.RegisterLane(DefaultLane(viewKey));

        Task<int> staleRefresh = Task.Run(
            () => staleSut.TriggerNudgeRefreshAsync(
                "OrdersProjection",
                "acme",
                TestContext.Current.CancellationToken),
            TestContext.Current.CancellationToken);
        await staleReadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        (await staleSut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        incumbent.Dispose();
        _ = staleSut.RegisterLane(DefaultLane(viewKey) with { TenantId = "other" });
        staleResult.TrySetResult(new ProjectionPageResult([], 0, "\"v1\"", IsNotModified: true));

        (await staleRefresh.ConfigureAwait(true)).ShouldBe(0);
        staleDispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());
        staleDispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageNotModifiedAction>());

        TaskCompletionSource<bool> ownerLoadStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionPageResult> releaseOwnerLoad = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader replacementLoader = Substitute.For<IProjectionPageLoader>();
        replacementLoader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(
                _ => {
                    ownerLoadStarted.TrySetResult(true);
                    return releaseOwnerLoad.Task;
                },
                _ => Task.FromResult(new ProjectionPageResult(["replacement"], 1, "\"v2\"")));
        IDispatcher replacementDispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler replacementSut = CreateScheduler(
            replacementLoader,
            replacementDispatcher,
            new MutableLoadedPageState(PageState(viewKey, ["existing"], totalCount: 1)));
        IDisposable priorOwner = replacementSut.RegisterLane(DefaultLane(viewKey));
        Task<int> priorRefresh = replacementSut.TriggerNudgeRefreshAsync(
            "OrdersProjection",
            "acme",
            TestContext.Current.CancellationToken);
        _ = await ownerLoadStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        priorOwner.Dispose();
        _ = replacementSut.RegisterLane(DefaultLane(viewKey) with { TenantId = "other" });

        (await replacementSut.TriggerNudgeRefreshAsync("OrdersProjection", "other", TestContext.Current.CancellationToken)).ShouldBe(1);
        releaseOwnerLoad.SetResult(new ProjectionPageResult(["prior"], 1, "\"v1\""));

        (await priorRefresh.ConfigureAwait(true)).ShouldBe(0);
        (await replacementSut.TriggerNudgeRefreshAsync("OrdersProjection", "other", TestContext.Current.CancellationToken)).ShouldBe(1);
        replacementDispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.ViewKey == viewKey
            && action.Items != null
            && action.Items.SequenceEqual(new object[] { "replacement" })));
    }

    [Fact]
    public async Task RegisterLane_ConcurrentEquivalentAndConflictingRegistrations_PreserveSingleIncumbent() {
        const string viewKey = "shared-view";
        IProjectionPageLoader loader = LoaderReturning(new ProjectionPageResult([], 0, null, IsNotModified: true));
        MutableLoadedPageState loadedPages = new(PageState(viewKey, [], totalCount: 0));
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, Substitute.For<IDispatcher>(), loadedPages);
        ProjectionFallbackLane incumbent = DefaultLane(viewKey);
        IDisposable initial = sut.RegisterLane(incumbent);
        ConcurrentBag<IDisposable> equivalentRegistrations = [];
        ConcurrentBag<InvalidOperationException> conflicts = [];

        Parallel.For(0, 32, index => {
            if (index % 2 == 0) {
                equivalentRegistrations.Add(sut.RegisterLane(incumbent with { }));
                return;
            }

            try {
                _ = sut.RegisterLane(incumbent with { TenantId = $"other-{index}" });
            }
            catch (InvalidOperationException exception) {
                conflicts.Add(exception);
            }
        });

        conflicts.Count.ShouldBe(16);
        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(1);
        initial.Dispose();
        foreach (IDisposable registration in equivalentRegistrations) {
            registration.Dispose();
        }

        (await sut.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken)).ShouldBe(0);
        _ = sut.RegisterLane(incumbent with { TenantId = "other" });
    }

    [Fact]
    public async Task FailedEligibleReadReportsFailureAndDispatchesOnlyScopedStaleness()
    {
        const string view = "acme:OrdersProjection";
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(), Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<ProjectionPageResult>(new InvalidOperationException("transport secret")));
        MutableLoadedPageState pages = new(PageState(view, ["cached"], 1));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, dispatcher, pages);
        using IDisposable registration = sut.RegisterLane(DefaultLane(view));
        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        result.RefreshedCount.ShouldBe(0);
        result.ChangedViewKeys.ShouldBeEmpty();
        result.FailedViewKeys.ShouldBe([view]);
        dispatcher.Received(1).Dispatch(Arg.Is<MarkProjectionStaleAction>(action => action.ViewKey == view && action.Skip == 0
            && action.OriginScopeGeneration == pages.Value.ScopeGeneration));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageFailedAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageSucceededAction>());
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageNotModifiedAction>());
    }


    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LoaderLaneReadSlowerThanFiveSecondsIsStillApplied(bool fallback)
    {
        const string view = "acme:OrdersProjection";
        FakeTimeProvider time = new();
        TaskCompletionSource started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<ProjectionPageResult> read = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<IImmutableDictionary<string, string>>(), Arg.Any<string?>(), Arg.Any<bool>(),
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                started.TrySetResult();
                return read.Task;
            });
        MutableLoadedPageState pages = new(PageState(view, ["cached"], 1));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        ProjectionFallbackRefreshScheduler scheduler = new(
            new TestConnectionState(new ProjectionConnectionSnapshot(
                fallback ? ProjectionConnectionStatus.Disconnected : ProjectionConnectionStatus.Connected,
                time.GetUtcNow(),
                fallback ? 1 : 0,
                fallback ? "Closed" : null)),
            loader,
            dispatcher,
            pages,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions
            {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 10,
            }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance,
            tenantContextAccessor: null,
            timeProvider: time);
        using IDisposable lane = scheduler.RegisterLane(DefaultLane(view));
        Task<int> sweep = fallback
            ? scheduler.TriggerFallbackOnceAsync(TestContext.Current.CancellationToken)
            : scheduler.TriggerNudgeRefreshAsync("OrdersProjection", "acme", TestContext.Current.CancellationToken);

        await started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await Task.Delay(50, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(6));
        sweep.IsCompleted.ShouldBeFalse();
        read.SetResult(new ProjectionPageResult(["slow-fresh"], 1, "\"slow-fresh\""));

        (await sweep.WaitAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.ViewKey == view && action.Items != null && action.Items.Contains("slow-fresh")));
    }

    private static ProjectionFallbackRefreshScheduler CreateScheduler(
        IProjectionPageLoader loader,
        IDispatcher dispatcher,
        IState<LoadedPageState> loadedPages) {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
        return new ProjectionFallbackRefreshScheduler(
            state,
            loader,
            dispatcher,
            loadedPages,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                ProjectionFallbackPollingIntervalSeconds = 15,
                MaxProjectionFallbackPollingLanes = 10,
            }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
    }

    private static ProjectionFallbackLane DefaultLane(
        string viewKey,
        IImmutableDictionary<string, string>? filters = null)
        => new(
            viewKey,
            "OrdersProjection",
            "acme",
            0,
            20,
            filters ?? ImmutableDictionary<string, string>.Empty,
            null,
            false,
            null);

    private static IProjectionPageLoader LoaderReturning(ProjectionPageResult result) {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));
        return loader;
    }

    private static IProjectionPageLoader LoaderReturning(Queue<ProjectionPageResult> results) {
        IProjectionPageLoader loader = Substitute.For<IProjectionPageLoader>();
        loader.LoadPageAsync(
            Arg.Any<string>(),
            Arg.Any<int>(),
            Arg.Any<int>(),
            Arg.Any<IImmutableDictionary<string, string>>(),
            Arg.Any<string?>(),
            Arg.Any<bool>(),
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(results.Dequeue()));
        return loader;
    }

    private static LoadedPageState PageState(string viewKey, IReadOnlyList<object> items, int totalCount)
        => new() {
            PagesByKey = ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>>.Empty
                .Add((viewKey, 0), items),
            TotalCountByKey = ImmutableDictionary<string, int>.Empty.Add(viewKey, totalCount),
        };

    private static JsonElement JsonRow(string json) {
        using JsonDocument document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private static JsonElement DeepJsonRow(int depth) {
        string json = string.Concat(Enumerable.Repeat("{\"nested\":", depth))
            + "0"
            + new string('}', depth);
        return JsonRow(json);
    }

    private sealed class TestConnectionState(ProjectionConnectionSnapshot snapshot) : IProjectionConnectionState {
        public ProjectionConnectionSnapshot Current { get; private set; } = snapshot;

        public IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true)
            => new Registration();

        public void Apply(ProjectionConnectionTransition transition) => Current = new ProjectionConnectionSnapshot(
                transition.Status,
                DateTimeOffset.UtcNow,
                transition.ReconnectAttempt,
                transition.FailureCategory);
    }

    private sealed class Registration : IDisposable {
        public void Dispose() {
        }
    }

    private sealed class MutableLoadedPageState(LoadedPageState value) : IState<LoadedPageState> {
        public LoadedPageState Value { get; set; } = value;

        public event EventHandler? StateChanged {
            add {
            }

            remove {
            }
        }
    }

    private sealed class ThrowOnceDispatcher : IDispatcher {
        private bool _throw = true;

        public int SuccessfulDispatchCount { get; private set; }

        public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched {
            add {
            }

            remove {
            }
        }

        public void Dispatch(object action) {
            if (_throw) {
                _throw = false;
                throw new InvalidOperationException("Intentional dispatcher failure.");
            }

            SuccessfulDispatchCount++;
        }
    }

    private sealed class ThrowingRow {
        private readonly string _failure = "Intentional serialization failure.";

        public string Value => throw new InvalidOperationException(_failure);
    }
}

file static class OptionsMonitorExtensions {
    public static IOptionsMonitor<T> ToMonitor<T>(this IOptions<T> options) where T : class {
        IOptionsMonitor<T> monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(options.Value);
        return monitor;
    }
}
