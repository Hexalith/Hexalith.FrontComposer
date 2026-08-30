using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.ProjectionConnection;

public sealed class ProjectionFallbackPollingDriverTests {
    [Fact]
    public async Task Driver_RunsScheduler_OnlyWhileDisconnected_AndStopsOnReconnect() {
        TestableConnectionState state = new();
        TestScheduler scheduler = new();
        IOptionsMonitor<FcShellOptions> options = Microsoft.Extensions.Options.Options.Create(
            new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 1 }).ToMonitor();

        ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            options,
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        try {
            sut.Start();

            // Initial state is Connected → no polling.
            await Task.Delay(100, TestContext.Current.CancellationToken).ConfigureAwait(true);
            scheduler.TriggerCount.ShouldBe(0);

            // Disconnect → driver should call TriggerFallbackOnceAsync at least once promptly.
            state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
            await scheduler.WaitForTriggers(1, TimeSpan.FromSeconds(2)).ConfigureAwait(true);
            scheduler.TriggerCount.ShouldBeGreaterThanOrEqualTo(1);

            // Reconnect → driver loop must exit and stop firing.
            state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
            int countAtReconnect = scheduler.TriggerCount;
            await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true);
            scheduler.TriggerCount.ShouldBeLessThanOrEqualTo(countAtReconnect + 1);
        }
        finally {
            await sut.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Driver_DoesNotRun_WhenIntervalIsZero() {
        TestableConnectionState state = new();
        TestScheduler scheduler = new();
        IOptionsMonitor<FcShellOptions> options = Microsoft.Extensions.Options.Options.Create(
            new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 0 }).ToMonitor();

        ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            options,
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        try {
            sut.Start();

            state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
            await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true);

            scheduler.TriggerCount.ShouldBe(0);
        }
        finally {
            await sut.DisposeAsync().ConfigureAwait(true);
        }
    }

    [Fact]
    public async Task Driver_StopsLoop_OnDispose() {
        TestableConnectionState state = new();
        TestScheduler scheduler = new();
        IOptionsMonitor<FcShellOptions> options = Microsoft.Extensions.Options.Options.Create(
            new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 1 }).ToMonitor();

        ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            options,
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        sut.Start();
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
        await scheduler.WaitForTriggers(1, TimeSpan.FromSeconds(2)).ConfigureAwait(true);

        await sut.DisposeAsync().ConfigureAwait(true);
        int countAtDispose = scheduler.TriggerCount;
        await Task.Delay(150, TestContext.Current.CancellationToken).ConfigureAwait(true);

        scheduler.TriggerCount.ShouldBe(countAtDispose);
    }

    [Fact]
    public async Task DisposeAsync_WhenSchedulerIgnoresCancellation_CompletesWithinBoundedWait() {
        TestableConnectionState state = new();
        BlockingScheduler scheduler = new();
        IOptionsMonitor<FcShellOptions> options = Microsoft.Extensions.Options.Options.Create(
            new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 1 }).ToMonitor();
        ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            options,
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        sut.Start();

        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
        await scheduler.WaitForStartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        Task dispose = sut.DisposeAsync().AsTask();
        Task completed = await Task.WhenAny(
            dispose,
            Task.Delay(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken)).ConfigureAwait(true);

        completed.ShouldBe(dispose);
        await dispose.ConfigureAwait(true);
    }

    [Fact]
    public async Task Driver_RuntimeOptionChanges_StartAndStopPollingWhileDisconnected() {
        FakeTimeProvider time = new();
        TestableConnectionState state = new();
        CancellableScheduler scheduler = new();
        MutableOptionsMonitor options = new(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 0 });
        ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            options,
            NullLogger<ProjectionFallbackPollingDriver>.Instance,
            time);
        sut.Start();
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));

        options.Update(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 15 });
        await scheduler.WaitForCallsAsync(1, TestContext.Current.CancellationToken);
        options.Update(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 0 });
        await scheduler.WaitForCompletionsAsync(1, TestContext.Current.CancellationToken);
        options.Update(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 15 });
        await scheduler.WaitForCallsAsync(2, TestContext.Current.CancellationToken);

        scheduler.MaxConcurrency.ShouldBe(1);
        await sut.DisposeAsync().ConfigureAwait(true);
    }

    [Fact]
    public async Task Driver_RapidConnectedDisconnectedFlap_RestartsAfterCanceledLoopUnwinds() {
        FakeTimeProvider time = new();
        TestableConnectionState state = new();
        CancellableScheduler scheduler = new();
        ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            new MutableOptionsMonitor(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 15 }),
            NullLogger<ProjectionFallbackPollingDriver>.Instance,
            time);
        sut.Start();
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
        await scheduler.WaitForCallsAsync(1, TestContext.Current.CancellationToken);

        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "ClosedAgain"));

        await scheduler.WaitForCallsAsync(2, TestContext.Current.CancellationToken);
        scheduler.MaxConcurrency.ShouldBe(1);
        await sut.DisposeAsync().ConfigureAwait(true);
    }

    [Fact]
    public void Start_WhenOptionRegistrationFails_CleansUpStateRegistration() {
        TestableConnectionState state = new();
        ProjectionFallbackPollingDriver sut = new(
            state,
            new TestScheduler(),
            new ThrowingRegistrationOptionsMonitor(),
            NullLogger<ProjectionFallbackPollingDriver>.Instance);

        Should.Throw<InvalidOperationException>(sut.Start);

        state.SubscriberCount.ShouldBe(0);
    }

    [Fact]
    public async Task Driver_FatalLoopFault_IsObservedAndNeverHotRestarted() {
        TestableConnectionState state = new();
        FatalScheduler scheduler = new();
        MutableOptionsMonitor options = new(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 15 });
        ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            options,
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        sut.Start();

        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
        await scheduler.Called.WaitAsync(TestContext.Current.CancellationToken);
        await Task.Yield();
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "StillClosed"));
        options.Update(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 30 });
        await Task.Yield();

        scheduler.Calls.ShouldBe(1);
        _ = await Should.ThrowAsync<AccessViolationException>(() => sut.DisposeAsync().AsTask());
        state.SubscriberCount.ShouldBe(0);
    }

    [Fact]
    public async Task DisposeAsync_WhenOptionRegistrationThrows_StillUnsubscribesState() {
        TestableConnectionState state = new();
        ProjectionFallbackPollingDriver sut = new(
            state,
            new TestScheduler(),
            new ThrowingDisposeOptionsMonitor(new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 0 }),
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        sut.Start();

        await Should.NotThrowAsync(async () => await sut.DisposeAsync().ConfigureAwait(false));

        state.SubscriberCount.ShouldBe(0);
    }

    private sealed class TestableConnectionState : IProjectionConnectionState {
        private readonly object _sync = new();
        private readonly List<Action<ProjectionConnectionSnapshot>> _handlers = [];
        private ProjectionConnectionSnapshot _current = new(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null);

        public ProjectionConnectionSnapshot Current {
            get {
                lock (_sync) {
                    return _current;
                }
            }
        }

        public int SubscriberCount {
            get {
                lock (_sync) {
                    return _handlers.Count;
                }
            }
        }

        public IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true) {
            lock (_sync) {
                _handlers.Add(handler);
                if (replay) {
                    handler(_current);
                }
            }

            return new Sub(this, handler);
        }

        public void Apply(ProjectionConnectionTransition transition) {
            ProjectionConnectionSnapshot snapshot = new(
                transition.Status,
                DateTimeOffset.UtcNow,
                transition.ReconnectAttempt,
                transition.FailureCategory);
            Action<ProjectionConnectionSnapshot>[] handlers;
            lock (_sync) {
                _current = snapshot;
                handlers = [.. _handlers];
            }

            foreach (Action<ProjectionConnectionSnapshot> h in handlers) {
                h(snapshot);
            }
        }

        private void Unsubscribe(Action<ProjectionConnectionSnapshot> handler) {
            lock (_sync) {
                _ = _handlers.Remove(handler);
            }
        }

        private sealed class Sub(TestableConnectionState owner, Action<ProjectionConnectionSnapshot> handler) : IDisposable {
            private int _disposed;

            public void Dispose() {
                if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                    owner.Unsubscribe(handler);
                }
            }
        }
    }

    private sealed class TestScheduler : IProjectionFallbackRefreshScheduler {
        private readonly TaskCompletionSource _firstTrigger = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _triggerCount;

        public int TriggerCount => Volatile.Read(ref _triggerCount);

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Reg();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
            int n = Interlocked.Increment(ref _triggerCount);
            if (n == 1) {
                _ = _firstTrigger.TrySetResult();
            }

            return Task.FromResult(0);
        }

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public async Task WaitForTriggers(int minimum, TimeSpan timeout) {
            using CancellationTokenSource cts = new(timeout);
            try {
                await _firstTrigger.Task.WaitAsync(cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw new TimeoutException($"TestScheduler did not reach {minimum} triggers within {timeout}.");
            }
        }

        private sealed class Reg : IDisposable {
            public void Dispose() {
            }
        }
    }

    private sealed class BlockingScheduler : IProjectionFallbackRefreshScheduler {
        private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<int> _neverCompletes = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Reg();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
            _ = _started.TrySetResult();
            return _neverCompletes.Task;
        }

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public Task WaitForStartAsync(CancellationToken cancellationToken)
            => _started.Task.WaitAsync(cancellationToken);

        private sealed class Reg : IDisposable {
            public void Dispose() {
            }
        }
    }

    private sealed class CancellableScheduler : IProjectionFallbackRefreshScheduler {
        private readonly TaskCompletionSource _callChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _completionChanged = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _active;
        private int _calls;
        private int _completions;
        private int _maxConcurrency;

        public int MaxConcurrency => Volatile.Read(ref _maxConcurrency);

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Reg();

        public async Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
            int active = Interlocked.Increment(ref _active);
            UpdateMaximum(active);
            _ = Interlocked.Increment(ref _calls);
            _ = _callChanged.TrySetResult();
            try {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
                return 0;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                return 0;
            }
            finally {
                _ = Interlocked.Decrement(ref _active);
                _ = Interlocked.Increment(ref _completions);
                _ = _completionChanged.TrySetResult();
            }
        }

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        public async Task WaitForCallsAsync(int expected, CancellationToken cancellationToken) {
            while (Volatile.Read(ref _calls) < expected) {
                await _callChanged.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                await Task.Yield();
            }
        }

        public async Task WaitForCompletionsAsync(int expected, CancellationToken cancellationToken) {
            while (Volatile.Read(ref _completions) < expected) {
                await _completionChanged.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
                await Task.Yield();
            }
        }

        private void UpdateMaximum(int candidate) {
            int current;
            do {
                current = Volatile.Read(ref _maxConcurrency);
                if (candidate <= current) {
                    return;
                }
            }
            while (Interlocked.CompareExchange(ref _maxConcurrency, candidate, current) != current);
        }

        private sealed class Reg : IDisposable {
            public void Dispose() {
            }
        }
    }

    private sealed class FatalScheduler : IProjectionFallbackRefreshScheduler {
        private readonly TaskCompletionSource _called = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _calls;

        public Task Called => _called.Task;
        public int Calls => Volatile.Read(ref _calls);
        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Reg();

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
            _ = Interlocked.Increment(ref _calls);
            _ = _called.TrySetResult();
            return Task.FromException<int>(Activator.CreateInstance<AccessViolationException>());
        }

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(0);

        private sealed class Reg : IDisposable {
            public void Dispose() {
            }
        }
    }

    private sealed class MutableOptionsMonitor(FcShellOptions value) : IOptionsMonitor<FcShellOptions> {
        private Action<FcShellOptions, string?>? _listener;
        public FcShellOptions CurrentValue { get; private set; } = value;
        public FcShellOptions Get(string? name) => CurrentValue;
        public IDisposable OnChange(Action<FcShellOptions, string?> listener) {
            _listener += listener;
            return new CallbackRegistration(() => _listener -= listener);
        }

        public void Update(FcShellOptions value) {
            CurrentValue = value;
            _listener?.Invoke(value, null);
        }
    }

    private sealed class ThrowingRegistrationOptionsMonitor : IOptionsMonitor<FcShellOptions> {
        public FcShellOptions CurrentValue { get; } = new();
        public FcShellOptions Get(string? name) => CurrentValue;
        public IDisposable? OnChange(Action<FcShellOptions, string?> listener)
            => throw new InvalidOperationException("registration failed");
    }

    private sealed class ThrowingDisposeOptionsMonitor(FcShellOptions value) : IOptionsMonitor<FcShellOptions> {
        public FcShellOptions CurrentValue => value;
        public FcShellOptions Get(string? name) => value;
        public IDisposable OnChange(Action<FcShellOptions, string?> listener) => new ThrowingRegistration();
    }

    private sealed class ThrowingRegistration : IDisposable {
        public void Dispose() => throw new InvalidOperationException("dispose failed");
    }

    private sealed class CallbackRegistration(Action callback) : IDisposable {
        public void Dispose() => callback();
    }
}

file static class TestOptionsMonitorExtensions {
    public static IOptionsMonitor<T> ToMonitor<T>(this IOptions<T> options) where T : class {
        IOptionsMonitor<T> monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(options.Value);
        return monitor;
    }
}
