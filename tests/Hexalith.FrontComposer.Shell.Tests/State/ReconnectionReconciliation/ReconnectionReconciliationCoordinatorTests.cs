using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.ReconnectionReconciliation;

public sealed class ReconnectionReconciliationCoordinatorTests {
    [Fact]
    public async Task ReconcileAsync_StartsEpochAndCompletesWithChangedResult() {
        TestScheduler scheduler = new(new ProjectionReconciliationRefreshResult(1, ["orders"]));
        ReconnectionReconciliationStateService state = NewState();
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        ProjectionReconciliationRefreshResult result = await sut.ReconcileAsync(TestContext.Current.CancellationToken);

        result.ChangedViewKeys.ShouldBe(["orders"]);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Refreshed);
        state.Current.Changed.ShouldBeTrue();
        scheduler.Epochs.ShouldBe([1]);
    }

    [Fact]
    public async Task ReconcileAsync_NoChangedLanesClearsSilently() {
        TestScheduler scheduler = new(new ProjectionReconciliationRefreshResult(1, []));
        ReconnectionReconciliationStateService state = NewState();
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        _ = await sut.ReconcileAsync(TestContext.Current.CancellationToken);

        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
        state.Current.Changed.ShouldBeFalse();
    }

    [Fact]
    public async Task ReconcileAsync_DisposeCancelsActivePassWithoutLeakingStatus() {
        BlockingScheduler scheduler = new();
        ReconnectionReconciliationStateService state = NewState();
        ReconnectionReconciliationCoordinator sut = new(
            scheduler,
            state,
            NullLogger<ReconnectionReconciliationCoordinator>.Instance);

        Task<ProjectionReconciliationRefreshResult> pass = sut.ReconcileAsync(TestContext.Current.CancellationToken);
        await scheduler.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        sut.Dispose();
        ProjectionReconciliationRefreshResult result = await pass.WaitAsync(TestContext.Current.CancellationToken);

        result.ShouldBe(ProjectionReconciliationRefreshResult.Empty);
        state.Current.Status.ShouldBe(ReconnectionReconciliationStatus.Idle);
    }

    private static ReconnectionReconciliationStateService NewState()
        => new(
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<ReconnectionReconciliationStateService>.Instance);

    private sealed class TestScheduler(ProjectionReconciliationRefreshResult result) : IProjectionFallbackRefreshScheduler {
        public List<long> Epochs { get; } = [];

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Registration();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default) {
            Epochs.Add(epoch);
            return Task.FromResult(result);
        }
    }

    private sealed class BlockingScheduler : IProjectionFallbackRefreshScheduler {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Registration();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public async Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default) {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return ProjectionReconciliationRefreshResult.Empty;
        }
    }

    private sealed class Registration : IDisposable {
        public void Dispose() {
        }
    }
}
