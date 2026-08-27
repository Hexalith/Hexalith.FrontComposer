using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Infrastructure.PendingCommands;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.PendingCommands;

public sealed class PendingCommandPollingDriverTests {
    [Fact]
    public async Task Driver_TicksAtCommandCadence_WhileProjectionFallbackRemainsConnectedOnly() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        TestPendingPolling pending = new();
        IOptionsMonitor<FcShellOptions> commandOptions = new StaticOptionsMonitor(new FcShellOptions {
            PendingCommandPollingIntervalMs = 1_000,
        });
        PendingCommandPollingDriver commandDriver = new(
            pending,
            commandOptions,
            time,
            NullLogger<PendingCommandPollingDriver>.Instance);

        TestProjectionConnectionState projectionState = new();
        TestProjectionScheduler scheduler = new();
        ProjectionFallbackPollingDriver projectionDriver = new(
            projectionState,
            scheduler,
            new StaticOptionsMonitor(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 15 }),
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        try {
            commandDriver.Start();
            projectionDriver.Start();

            time.Advance(TimeSpan.FromMilliseconds(999));
            pending.Calls.ShouldBe(0);
            scheduler.TriggerCount.ShouldBe(0);

            time.Advance(TimeSpan.FromMilliseconds(1));
            await pending.WaitForCallsAsync(1).ConfigureAwait(true);

            pending.Calls.ShouldBe(1);
            scheduler.TriggerCount.ShouldBe(0);
        }
        finally {
            await commandDriver.DisposeAsync().ConfigureAwait(true);
            await projectionDriver.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Driver_DoesNotPoll_WhenCommandIntervalIsZero() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        TestPendingPolling pending = new();
        PendingCommandPollingDriver sut = new(
            pending,
            new StaticOptionsMonitor(new FcShellOptions { PendingCommandPollingIntervalMs = 0 }),
            time,
            NullLogger<PendingCommandPollingDriver>.Instance);
        try {
            sut.Start();

            time.Advance(TimeSpan.FromSeconds(5));
            await Task.Yield();

            pending.Calls.ShouldBe(0);
        }
        finally {
            await sut.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task DisposeAsync_WhenPollIgnoresCancellation_CompletesWithinBoundedWait() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        BlockingPendingPolling pending = new();
        PendingCommandPollingDriver sut = new(
            pending,
            new StaticOptionsMonitor(new FcShellOptions { PendingCommandPollingIntervalMs = 1_000 }),
            time,
            NullLogger<PendingCommandPollingDriver>.Instance);
        sut.Start();

        time.Advance(TimeSpan.FromSeconds(1));
        await pending.WaitForStartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Task dispose = sut.DisposeAsync().AsTask();
        time.Advance(TimeSpan.FromSeconds(2));
        await dispose.ConfigureAwait(true);
    }

    [Fact]
    public async Task Tick_SynchronousCoordinatorBlock_DoesNotHoldDriverLockDuringBoundedDispose() {
        FakeTimeProvider time = new();
        using SynchronouslyBlockingPendingPolling pending = new();
        PendingCommandPollingDriver sut = new(
            pending,
            new StaticOptionsMonitor(new FcShellOptions { PendingCommandPollingIntervalMs = 1_000 }),
            time,
            NullLogger<PendingCommandPollingDriver>.Instance);
        sut.Start();
        time.Advance(TimeSpan.FromSeconds(1));
        await pending.Started.WaitAsync(TestContext.Current.CancellationToken);

        Task dispose = sut.DisposeAsync().AsTask();
        time.Advance(TimeSpan.FromSeconds(2));
        await dispose.WaitAsync(TestContext.Current.CancellationToken);

        pending.Release();
    }

    [Fact]
    public async Task Tick_FatalCoordinatorFault_RemainsObservableThroughPublishedPollTask() {
        FakeTimeProvider time = new();
        FatalPendingPolling pending = new();
        PendingCommandPollingDriver sut = new(
            pending,
            new StaticOptionsMonitor(new FcShellOptions { PendingCommandPollingIntervalMs = 1_000 }),
            time,
            NullLogger<PendingCommandPollingDriver>.Instance);
        sut.Start();
        time.Advance(TimeSpan.FromSeconds(1));
        await pending.Called.WaitAsync(TestContext.Current.CancellationToken);
        await Task.Yield();

        _ = await Should.ThrowAsync<AccessViolationException>(() => sut.DisposeAsync().AsTask());
    }

    private sealed class TestPendingPolling : IPendingCommandPollingCoordinator {
        private readonly TaskCompletionSource _firstCall = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;

        public int Calls => Volatile.Read(ref _calls);

        public Task<int> PollOnceAsync(CancellationToken cancellationToken = default) {
            int calls = Interlocked.Increment(ref _calls);
            if (calls == 1) {
                _ = _firstCall.TrySetResult();
            }

            return Task.FromResult(0);
        }

        public async Task WaitForCallsAsync(int minimum) {
            if (Calls >= minimum) {
                return;
            }

            using CancellationTokenSource cts = new(TimeSpan.FromSeconds(2));
            await _firstCall.Task.WaitAsync(cts.Token).ConfigureAwait(false);
        }
    }

    private sealed class BlockingPendingPolling : IPendingCommandPollingCoordinator {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<int> _neverCompletes = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<int> PollOnceAsync(CancellationToken cancellationToken = default) {
            _ = _started.TrySetResult();
            return _neverCompletes.Task;
        }

        public Task WaitForStartAsync(CancellationToken cancellationToken)
            => _started.Task.WaitAsync(cancellationToken);
    }

    private sealed class SynchronouslyBlockingPendingPolling : IPendingCommandPollingCoordinator, IDisposable {
        private readonly ManualResetEventSlim _release = new(initialState: false);
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task Started => _started.Task;

        public Task<int> PollOnceAsync(CancellationToken cancellationToken = default) {
            _ = _started.TrySetResult();
            _release.Wait(CancellationToken.None);
            return Task.FromResult(0);
        }

        public void Release() => _release.Set();
        public void Dispose() => _release.Dispose();
    }

    private sealed class FatalPendingPolling : IPendingCommandPollingCoordinator {
        private readonly TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task Called => _called.Task;

        public Task<int> PollOnceAsync(CancellationToken cancellationToken = default) {
            _ = _called.TrySetResult();
            return Task.FromException<int>(Activator.CreateInstance<AccessViolationException>());
        }
    }

    private sealed class StaticOptionsMonitor(FcShellOptions value) : IOptionsMonitor<FcShellOptions> {
        public FcShellOptions CurrentValue => value;

        public FcShellOptions Get(string? name) => value;

        public IDisposable? OnChange(Action<FcShellOptions, string?> listener) => new Registration();
    }

    private sealed class TestProjectionConnectionState : IProjectionConnectionState {
        public ProjectionConnectionSnapshot Current { get; } = new(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null);

        public IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true) {
            if (replay) {
                handler(Current);
            }

            return new Registration();
        }

        public void Apply(ProjectionConnectionTransition transition) {
        }
    }

    private sealed class TestProjectionScheduler : IProjectionFallbackRefreshScheduler {
        private int _triggerCount;

        public int TriggerCount => Volatile.Read(ref _triggerCount);

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Registration();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
            _ = Interlocked.Increment(ref _triggerCount);
            return Task.FromResult(0);
        }

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    private sealed class Registration : IDisposable {
        public void Dispose() {
        }
    }
}
