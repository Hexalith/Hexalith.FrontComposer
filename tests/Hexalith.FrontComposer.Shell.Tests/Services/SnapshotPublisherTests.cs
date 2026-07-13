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
    public void Subscribe_ReplayRacingPublish_ObservesCurrentBeforeFresherSnapshot() {
        SnapshotPublisher<Snap> sut = new(new Snap(1), NoOpFaultHandler);
        var observed = new System.Collections.Concurrent.ConcurrentQueue<int>();
        Thread? publisher = null;

        using IDisposable _ = sut.Subscribe(
            snapshot => {
                if (snapshot.Value == 1) {
                    publisher = new Thread(() => sut.Publish(new Snap(2))) { IsBackground = true };
                    publisher.Start();
                    SpinWait.SpinUntil(
                            () => (publisher.ThreadState & (ThreadState.WaitSleepJoin | ThreadState.Stopped)) != 0,
                            TimeSpan.FromSeconds(5))
                        .ShouldBeTrue("The concurrent publisher must reach the publisher lock before replay returns.");
                }

                observed.Enqueue(snapshot.Value);
            },
            replay: true);

        publisher.ShouldNotBeNull();
        publisher.Join(TimeSpan.FromSeconds(5)).ShouldBeTrue();
        observed.ToArray().ShouldBe([1, 2]);
    }
}
