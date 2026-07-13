using Hexalith.FrontComposer.Shell.Services;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

/// <summary>
/// Story 11.15 (M19 cluster 5) — matrix tests for the shared <see cref="SnapshotPublisher{T}"/>
/// primitive: replay-enabled vs disabled, current/replay never stale after a fresher update,
/// idempotent unsubscribe (including near a publish → no later callbacks), atomic conditional
/// <see cref="SnapshotPublisher{T}.TryApply"/>, and per-subscriber non-fatal fault isolation.
/// </summary>
public sealed class SnapshotPublisherTests {
    private static readonly Action<Exception> NoOpFaultHandler = static _ => { };

    private sealed record Snap(int Value);

    [Fact]
    public void Subscribe_ReplayEnabled_ImmediatelyDeliversCurrent() {
        SnapshotPublisher<Snap> sut = new(new Snap(1), NoOpFaultHandler);
        List<int> observed = [];

        using IDisposable _ = sut.Subscribe(s => observed.Add(s.Value), replay: true);

        observed.ShouldBe([1]);
    }

    [Fact]
    public void Subscribe_ReplayDisabled_DoesNotDeliverCurrent_ButReceivesNextPublish() {
        SnapshotPublisher<Snap> sut = new(new Snap(1), NoOpFaultHandler);
        List<int> observed = [];

        using IDisposable _ = sut.Subscribe(s => observed.Add(s.Value), replay: false);
        observed.ShouldBeEmpty();

        sut.Publish(new Snap(2));
        observed.ShouldBe([2]);
    }

    [Fact]
    public void Subscribe_ReplayEnabled_AfterPublish_DeliversLatest_NotStale() {
        SnapshotPublisher<Snap> sut = new(new Snap(1), NoOpFaultHandler);
        sut.Publish(new Snap(5));
        List<int> observed = [];

        using IDisposable _ = sut.Subscribe(s => observed.Add(s.Value), replay: true);

        // A joining subscriber replays the CURRENT (5), never the stale initial (1).
        observed.ShouldBe([5]);
        sut.Current.Value.ShouldBe(5);
    }

    [Fact]
    public void Publish_DeliversToAllSubscribers_AndAdvancesCurrent() {
        SnapshotPublisher<Snap> sut = new(new Snap(0), NoOpFaultHandler);
        List<int> a = [];
        List<int> b = [];
        using IDisposable _ = sut.Subscribe(s => a.Add(s.Value), replay: false);
        using IDisposable __ = sut.Subscribe(s => b.Add(s.Value), replay: false);

        sut.Publish(new Snap(7));

        a.ShouldBe([7]);
        b.ShouldBe([7]);
        sut.Current.Value.ShouldBe(7);
    }

    [Fact]
    public void TryApply_TransformReturnsNull_IsNoOp_NoStateChange_NoDelivery() {
        SnapshotPublisher<Snap> sut = new(new Snap(3), NoOpFaultHandler);
        List<int> observed = [];
        using IDisposable _ = sut.Subscribe(s => observed.Add(s.Value), replay: false);

        bool published = sut.TryApply(
            static (Snap _) => (Snap?)null,
            out Snap snapshot,
            out Action<Snap>[] handlers);

        published.ShouldBeFalse();
        handlers.ShouldBeEmpty();
        snapshot.Value.ShouldBe(3);
        sut.Current.Value.ShouldBe(3);
        observed.ShouldBeEmpty();
    }

    [Fact]
    public void TryApply_TransformReturnsSnapshot_AdvancesCurrent_ThenDelivers() {
        SnapshotPublisher<Snap> sut = new(new Snap(3), NoOpFaultHandler);
        List<int> observed = [];
        using IDisposable _ = sut.Subscribe(s => observed.Add(s.Value), replay: false);

        bool published = sut.TryApply(
            current => new Snap(current.Value + 1),
            out Snap snapshot,
            out Action<Snap>[] handlers);

        published.ShouldBeTrue();
        snapshot.Value.ShouldBe(4);
        // Current advances immediately (atomic under the lock); fan-out is the caller's step.
        sut.Current.Value.ShouldBe(4);
        observed.ShouldBeEmpty();

        sut.Deliver(handlers, snapshot);
        observed.ShouldBe([4]);
    }

    [Fact]
    public void Unsubscribe_IsIdempotent_DisposeTwice_IsSafe_AndStopsFutureCallbacks() {
        SnapshotPublisher<Snap> sut = new(new Snap(0), NoOpFaultHandler);
        List<int> observed = [];
        IDisposable subscription = sut.Subscribe(s => observed.Add(s.Value), replay: false);

        subscription.Dispose();
        subscription.Dispose(); // idempotent — no throw

        sut.Publish(new Snap(9));

        observed.ShouldBeEmpty();
    }

    [Fact]
    public async Task Unsubscribe_DuringCapturedPublish_PreventsCallbackAfterDisposeReturns() {
        SnapshotPublisher<Snap> sut = new(new Snap(0), NoOpFaultHandler);
        TaskCompletionSource<bool> firstHandlerEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseFirstHandler = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool disposedHandlerCalled = false;
        using IDisposable first = sut.Subscribe(
            _snapshot => {
                firstHandlerEntered.TrySetResult(true);
                releaseFirstHandler.Task.GetAwaiter().GetResult();
            },
            replay: false);
        using IDisposable disposed = sut.Subscribe(_ => disposedHandlerCalled = true, replay: false);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        Task publish = Task.Run(() => sut.Publish(new Snap(1)), cancellationToken);
        await firstHandlerEntered.Task.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
        try {
            disposed.Dispose();
        }
        finally {
            _ = releaseFirstHandler.TrySetResult(true);
        }

        await publish.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);

        disposedHandlerCalled.ShouldBeFalse();
    }

    [Fact]
    public void Publish_OneThrowingSubscriber_DoesNotStopHealthySubscribers_AndLogsWarning() {
        Exception? capturedFault = null;
        SnapshotPublisher<Snap> sut = new(new Snap(0), ex => capturedFault = ex);
        bool healthyCalled = false;
        using IDisposable _ = sut.Subscribe(static _ => throw new InvalidOperationException("boom"), replay: false);
        using IDisposable __ = sut.Subscribe(_ => healthyCalled = true, replay: false);

        sut.Publish(new Snap(1));

        healthyCalled.ShouldBeTrue();
        capturedFault.ShouldBeOfType<InvalidOperationException>();
    }

    [Fact]
    public void Subscribe_ReplayRacingConcurrentPublish_NeverObservesFreshThenStale() {
        // Story 11.15 review (round 2): the previous version triggered the competing Publish from INSIDE
        // the replay handler, which forced [1,2] regardless of whether replay ran under the publisher
        // lock — so it could not catch a regression that moves replay outside the lock. This version
        // races Subscribe(replay:true) against Publish on independent threads (aligned by a Barrier) and
        // asserts the joining subscriber's observation stream is monotonic non-decreasing (a fresher
        // snapshot is never followed by a staler one). Correct code satisfies the invariant on EVERY
        // interleaving, so there are zero false failures; a replay-outside-the-lock regression violates
        // it under repeated load.
        const int iterations = 1000;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        for (int i = 0; i < iterations; i++) {
            SnapshotPublisher<Snap> sut = new(new Snap(1), NoOpFaultHandler);
            var observed = new System.Collections.Concurrent.ConcurrentQueue<int>();
            using Barrier gate = new(2);
            IDisposable? subscription = null;

            var subscriber = new Thread(() => {
                gate.SignalAndWait(cancellationToken);
                subscription = sut.Subscribe(s => observed.Enqueue(s.Value), replay: true);
            }) { IsBackground = true };
            var publisher = new Thread(() => {
                gate.SignalAndWait(cancellationToken);
                sut.Publish(new Snap(2));
            }) { IsBackground = true };

            subscriber.Start();
            publisher.Start();
            subscriber.Join(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            publisher.Join(TimeSpan.FromSeconds(5)).ShouldBeTrue();
            subscription?.Dispose();

            int[] sequence = observed.ToArray();
            for (int j = 1; j < sequence.Length; j++) {
                sequence[j].ShouldBeGreaterThanOrEqualTo(
                    sequence[j - 1],
                    $"Iteration {i}: subscriber observed fresh-then-stale ordering [{string.Join(",", sequence)}].");
            }

            sut.Current.Value.ShouldBe(2);
        }
    }
}
