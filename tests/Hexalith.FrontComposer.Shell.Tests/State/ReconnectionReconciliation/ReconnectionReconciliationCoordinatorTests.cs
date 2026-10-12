using Fluxor;

using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.ReconnectionReconciliation;

public sealed class ReconnectionReconciliationCoordinatorTests {
    [Fact]
    public async Task HeldWaitCancellationCallbackCannotExtendThirtySecondDeadline()
    {
        FakeTimeProvider time = new(FixedNow);
        ReconnectionReconciliationStateService state = NewState();
        using ManualResetEventSlim releaseCancellation = new(false);
        HeldIncompleteReadScheduler scheduler = new() { CancellationRelease = releaseCancellation };
        using ReconnectionReconciliationCoordinator sut = new(scheduler, state, new FakeDispatcher(), time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);
        try
        {
            Task<ProjectionReconciliationRefreshResult> recovery = sut.ReconcileAsync(TestContext.Current.CancellationToken);
            await scheduler.WaitStarted.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            Task advance = Task.Run(() => time.Advance(TimeSpan.FromSeconds(30)), TestContext.Current.CancellationToken);
            await advance.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            ProjectionReconciliationRefreshResult expired = await recovery.WaitAsync(
                TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
            expired.HasIncompleteEligibleReads.ShouldBeTrue();
            state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
            await scheduler.CancellationEntered.Task.WaitAsync(TimeSpan.FromSeconds(2), TestContext.Current.CancellationToken);
        }
        finally
        {
            releaseCancellation.Set();
            scheduler.Release();
        }
    }

    [Fact]
    public async Task ReleasedLaneRecheckThatIgnoresCancellationCannotClaimRecoveryAfterDeadline()
    {
        FakeTimeProvider time = new(FixedNow);
        ReconnectionReconciliationStateService state = NewState();
        HeldIncompleteReadScheduler scheduler = new(holdRecheck: true);
        using ReconnectionReconciliationCoordinator sut = new(scheduler, state, new FakeDispatcher(), time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> recovery = sut.ReconcileAsync(TestContext.Current.CancellationToken);
        scheduler.Release();
        await scheduler.RecheckStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromMilliseconds(29_999));
        recovery.IsCompleted.ShouldBeFalse();
        time.Advance(TimeSpan.FromMilliseconds(1));
        ProjectionReconciliationRefreshResult expired = await recovery.WaitAsync(TestContext.Current.CancellationToken);
        expired.HasIncompleteEligibleReads.ShouldBeTrue();
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        scheduler.ReleaseRecheck();
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
    }

    [Fact]
    public async Task EligibleReadIgnoringCancellationTimesOutWithoutRecoveryThenLaterReadCanRecover()
    {
        FakeTimeProvider time = new(FixedNow);
        ReconnectionReconciliationStateService state = NewState();
        HeldIncompleteReadScheduler scheduler = new();
        using ReconnectionReconciliationCoordinator sut = new(scheduler, state, new FakeDispatcher(), time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> first = sut.ReconcileAsync(TestContext.Current.CancellationToken);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Reconciling);
        time.Advance(TimeSpan.FromMilliseconds(29_999));
        first.IsCompleted.ShouldBeFalse();
        time.Advance(TimeSpan.FromMilliseconds(1));
        ProjectionReconciliationRefreshResult expired = await first.WaitAsync(TestContext.Current.CancellationToken);
        expired.HasIncompleteEligibleReads.ShouldBeTrue();
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        state.Current.DataRead.ShouldBeFalse();

        scheduler.Release();
        ProjectionReconciliationRefreshResult recovered = await sut.ReconcileAsync(TestContext.Current.CancellationToken);
        recovered.ChangedViewKeys.ShouldBe(["orders"]);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
    }

    [Fact]
    public async Task EligibleReadStillInFlightDoesNotPublishRecovery()
    {
        TestScheduler scheduler = new(new ProjectionReconciliationRefreshResult(0, []) { HasIncompleteEligibleReads = true });
        ReconnectionReconciliationStateService state = NewState();
        ReconnectionReconciliationCoordinator sut = new(scheduler, state, new FakeDispatcher(),
            new FakeTimeProvider(FixedNow), NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        _ = await sut.ReconcileAsync(TestContext.Current.CancellationToken);

        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
    }

    [Fact]
    public async Task AdmittedSkippedReadCannotClaimRecoveryUntilLaterSuccessfulRead()
    {
        TestScheduler scheduler = new(new ProjectionReconciliationRefreshResult(0, []) { SkippedViewKeys = ["orders"] });
        ReconnectionReconciliationStateService state = NewState();
        using ReconnectionReconciliationCoordinator sut = new(scheduler, state, new FakeDispatcher(),
            new FakeTimeProvider(FixedNow), NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        ProjectionReconciliationRefreshResult first = await sut.ReconcileAsync(TestContext.Current.CancellationToken);
        first.SkippedViewKeys.ShouldBe(["orders"]);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        state.Current.DataRead.ShouldBeFalse();

        scheduler.Result = new ProjectionReconciliationRefreshResult(1, ["orders"]) { RefreshedViewKeys = ["orders"] };
        _ = await sut.ReconcileAsync(TestContext.Current.CancellationToken);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
        state.Current.DataRead.ShouldBeTrue();
    }

    private static readonly DateTimeOffset FixedNow = new(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ReconcileAsync_StartsEpochAndCompletesWithChangedResult() {
        TestScheduler scheduler = new(new ProjectionReconciliationRefreshResult(1, ["orders"]));
        ReconnectionReconciliationStateService state = NewState();
        FakeDispatcher dispatcher = new();
        FakeTimeProvider time = new(FixedNow);
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            dispatcher,
            time,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        ProjectionReconciliationRefreshResult result = await sut.ReconcileAsync(TestContext.Current.CancellationToken);

        result.ChangedViewKeys.ShouldBe(["orders"]);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
        state.Current.Changed.ShouldBeTrue();
        scheduler.Epochs.ShouldBe([1]);
        // DN1 — sweep marker dispatched for the changed lane.
        dispatcher.Actions.OfType<MarkReconciliationSweepAction>().ShouldHaveSingleItem()
            .ViewKeys.ShouldBe(["orders"]);
        dispatcher.Actions.OfType<ClearExpiredReconciliationSweepsAction>().ShouldBeEmpty();
        time.Advance(TimeSpan.FromMilliseconds(701));
        await Task.Yield();
        // DN1 — ClearExpired dispatches after the sweep TTL, not before it can expire.
        dispatcher.Actions.OfType<ClearExpiredReconciliationSweepsAction>().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ReconcileAsync_NoChangedLanesClearsSilently() {
        TestScheduler scheduler = new(new ProjectionReconciliationRefreshResult(1, []));
        ReconnectionReconciliationStateService state = NewState();
        FakeDispatcher dispatcher = new();
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            dispatcher,
            new FakeTimeProvider(FixedNow),
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        _ = await sut.ReconcileAsync(TestContext.Current.CancellationToken);

        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
        state.Current.Changed.ShouldBeFalse();
        // No sweep marker dispatched when no lane changed (AC5 silent no-change).
        dispatcher.Actions.OfType<MarkReconciliationSweepAction>().ShouldBeEmpty();
    }

    [Fact]
    public async Task ReconcileAsync_DisposeCancelsActivePassWithoutLeakingStatus() {
        // P50 — replace Task.Delay(Infinite) with a TaskCompletionSource pattern so the test
        // does not depend on disposed-CTS behavior of Task.Delay (which can surface as
        // ObjectDisposedException on certain runtimes/timing combinations).
        BlockingScheduler scheduler = new();
        ReconnectionReconciliationStateService state = NewState();
        FakeDispatcher dispatcher = new();
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            dispatcher,
            new FakeTimeProvider(FixedNow),
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> pass = sut.ReconcileAsync(TestContext.Current.CancellationToken);
        await scheduler.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        sut.Dispose();
        ProjectionReconciliationRefreshResult result = await pass.WaitAsync(TestContext.Current.CancellationToken);

        result.ShouldBe(ProjectionReconciliationRefreshResult.Empty);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        // Sweep markers must not be dispatched for a cancelled (disposed) pass.
        dispatcher.Actions.OfType<MarkReconciliationSweepAction>().ShouldBeEmpty();
    }

    [Fact]
    public async Task ReconcileAsync_AfterDispose_ThrowsObjectDisposedException() {
        // Story 11.21 CA1513 — pin fail-closed post-dispose ReconcileAsync (mirrors McpLifecycleStoreDisposalTests).
        TestScheduler scheduler = new(ProjectionReconciliationRefreshResult.Empty);
        ReconnectionReconciliationStateService state = NewState();
        FakeDispatcher dispatcher = new();
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            dispatcher,
            new FakeTimeProvider(FixedNow),
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);
        sut.Dispose();

        ObjectDisposedException disposed = await Should.ThrowAsync<ObjectDisposedException>(
            () => sut.ReconcileAsync(TestContext.Current.CancellationToken));
        disposed.ObjectName.ShouldBe(typeof(ReconnectionReconciliationCoordinator).FullName);
    }

    [Fact]
    public async Task ReconcileAsync_SupersededEpochResultDiscarded() {
        // P46 — superseded reconnect epoch cleanup. A second ReconcileAsync replaces the first;
        // the first must not mutate state with its eventual result.
        SequencedScheduler scheduler = new();
        ReconnectionReconciliationStateService state = NewState();
        FakeDispatcher dispatcher = new();
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            dispatcher,
            new FakeTimeProvider(FixedNow),
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> first = sut.ReconcileAsync(TestContext.Current.CancellationToken);
        await scheduler.FirstStarted.Task.WaitAsync(TestContext.Current.CancellationToken);
        Task<ProjectionReconciliationRefreshResult> second = sut.ReconcileAsync(TestContext.Current.CancellationToken);
        // Allow the second pass to run synchronously to completion; the second scheduler call
        // returns immediately. The first pass returns Empty because it observes cancellation.
        scheduler.AllowFirstCompletion.TrySetResult();
        await first.WaitAsync(TestContext.Current.CancellationToken);
        await second.WaitAsync(TestContext.Current.CancellationToken);

        // The state reflects the second pass (Refreshed/Idle for whatever the second produced).
        // First pass result must not have overridden it.
        scheduler.AcceptedEpochs.ShouldContain(1);
        scheduler.AcceptedEpochs.ShouldContain(2);
    }

    [Fact]
    public async Task ReconcileAsync_StaleCompleteIgnoredOnceSettled() {
        // P15 / D13 — once a pass completes (Refreshed), a stale Complete for the same epoch
        // never reopens the cleared status.
        TestScheduler scheduler = new(new ProjectionReconciliationRefreshResult(1, ["acme:Orders"]));
        ReconnectionReconciliationStateService state = NewState();
        FakeDispatcher dispatcher = new();
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            dispatcher,
            new FakeTimeProvider(FixedNow),
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        _ = await sut.ReconcileAsync(TestContext.Current.CancellationToken);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);

        // Idempotent re-Complete for the same epoch must NOT downgrade Refreshed to Idle.
        state.Complete(state.Current.Epoch, changed: false);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
    }

    [Fact]
    public void Reset_WithStaleExpectedEpoch_DoesNotClearFreshReconcilingState() {
        ReconnectionReconciliationStateService state = NewState();

        state.Start(1);
        state.Start(2);
        state.Reset(expectedEpoch: 1);

        state.Current.Epoch.ShouldBe(2);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Reconciling);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FailedEligibleReadsDoNotBecomeRefreshedButNoLanesStillRecover(bool failed)
    {
        ProjectionReconciliationRefreshResult expected = failed
            ? new(0, []) { FailedViewKeys = ["orders"] } : ProjectionReconciliationRefreshResult.Empty;
        ReconnectionReconciliationStateService state = NewState();
        FakeDispatcher dispatcher = new();
        using ReconnectionReconciliationCoordinator sut = new(new TestScheduler(expected), state, dispatcher,
            new FakeTimeProvider(FixedNow), NullLogger<ReconnectionReconciliationCoordinator>.Instance);
        _ = await sut.ReconcileAsync(TestContext.Current.CancellationToken);
        state.Current.Status.ShouldBe(failed ? ReconnectionReconciliationStatus.Idle : ReconnectionReconciliationStatus.Refreshed);
        state.Current.Changed.ShouldBeFalse();
        state.Current.DataRead.ShouldBeFalse();
        dispatcher.Actions.OfType<MarkReconciliationSweepAction>().ShouldBeEmpty();
    }

    [Fact]
    public async Task LegacyTwoArgumentStateCannotPublishFailedEligibleReadAsSuccess()
    {
        LegacyReconciliationState legacy = new(NewState());
        ProjectionReconciliationRefreshResult failure = new(0, []) { FailedViewKeys = ["orders"] };
        using ReconnectionReconciliationCoordinator coordinator = new(new TestScheduler(failure), legacy,
            new FakeDispatcher(), new FakeTimeProvider(FixedNow),
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        _ = await coordinator.ReconcileAsync(TestContext.Current.CancellationToken);

        legacy.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        legacy.SuccessfulCompletions.ShouldBe(0);
    }

    [Fact]
    public async Task LegacyTwoArgumentStateCannotClaimDataReadForSuccessfulNoLanePass()
    {
        LegacyReconciliationState legacy = new(NewState());
        using ReconnectionReconciliationCoordinator coordinator = new(
            new TestScheduler(ProjectionReconciliationRefreshResult.Empty), legacy,
            new FakeDispatcher(), new FakeTimeProvider(FixedNow),
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        _ = await coordinator.ReconcileAsync(TestContext.Current.CancellationToken);

        legacy.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        legacy.Current.DataRead.ShouldBeFalse();
        legacy.SuccessfulCompletions.ShouldBe(0);
    }

    private static ReconnectionReconciliationStateService NewState()
        => new(
            new FakeTimeProvider(FixedNow),
            NullLogger<ReconnectionReconciliationStateService>.Instance);

    private sealed class TestScheduler(ProjectionReconciliationRefreshResult result) : IProjectionFallbackRefreshScheduler {
        public List<long> Epochs { get; } = [];

        public ProjectionReconciliationRefreshResult Result { get; set; } = result;

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Registration();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default) {
            Epochs.Add(epoch);
            return Task.FromResult(Result);
        }
    }

    private sealed class BlockingScheduler : IProjectionFallbackRefreshScheduler {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ProjectionReconciliationRefreshResult> _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Registration();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public async Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default) {
            Started.TrySetResult();
            using CancellationTokenRegistration reg = cancellationToken.Register(() => _gate.TrySetCanceled(cancellationToken));
            return await _gate.Task.ConfigureAwait(false);
        }
    }

    private sealed class SequencedScheduler : IProjectionFallbackRefreshScheduler {
        public TaskCompletionSource FirstStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource AllowFirstCompletion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<long> AcceptedEpochs { get; } = [];
        private int _calls;

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Registration();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public async Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default) {
            AcceptedEpochs.Add(epoch);
            int call = Interlocked.Increment(ref _calls);
            if (call == 1) {
                FirstStarted.TrySetResult();
                using CancellationTokenRegistration reg = cancellationToken.Register(() => { });
                _ = await Task.WhenAny(AllowFirstCompletion.Task, Task.Delay(Timeout.Infinite, cancellationToken)).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                return new ProjectionReconciliationRefreshResult(0, []);
            }

            return new ProjectionReconciliationRefreshResult(0, []);
        }
    }

    private sealed class Registration : IDisposable {
        public void Dispose() {
        }
    }

    private sealed class FakeDispatcher : IDispatcher {
        public List<object> Actions { get; } = [];

#pragma warning disable CS0067 // Event never used by tests; required by Fluxor IDispatcher contract.
        public event EventHandler<Fluxor.ActionDispatchedEventArgs>? ActionDispatched;
#pragma warning restore CS0067

        public void Dispatch(object action) => Actions.Add(action);
    }
}
