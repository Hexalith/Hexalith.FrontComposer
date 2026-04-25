using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.ProjectionConnection;

public sealed class ProjectionFallbackPollingDriverTests {
    [Fact]
    public async Task Driver_RunsScheduler_OnlyWhileDisconnected_AndStopsOnReconnect() {
        TestableConnectionState state = new();
        TestScheduler scheduler = new();
        IOptionsMonitor<FcShellOptions> options = Microsoft.Extensions.Options.Options.Create(
            new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 1 }).ToMonitor();

        await using ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            options,
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        sut.Start();

        // Initial state is Connected → no polling.
        await Task.Delay(100, TestContext.Current.CancellationToken);
        scheduler.TriggerCount.ShouldBe(0);

        // Disconnect → driver should call TriggerFallbackOnceAsync at least once promptly.
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
        await scheduler.WaitForTriggers(1, TimeSpan.FromSeconds(2)).ConfigureAwait(true);
        scheduler.TriggerCount.ShouldBeGreaterThanOrEqualTo(1);

        // Reconnect → driver loop must exit and stop firing.
        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Connected));
        int countAtReconnect = scheduler.TriggerCount;
        await Task.Delay(150, TestContext.Current.CancellationToken);
        scheduler.TriggerCount.ShouldBeLessThanOrEqualTo(countAtReconnect + 1);
    }

    [Fact]
    public async Task Driver_DoesNotRun_WhenIntervalIsZero() {
        TestableConnectionState state = new();
        TestScheduler scheduler = new();
        IOptionsMonitor<FcShellOptions> options = Microsoft.Extensions.Options.Options.Create(
            new FcShellOptions { ProjectionFallbackPollingIntervalSeconds = 0 }).ToMonitor();

        await using ProjectionFallbackPollingDriver sut = new(
            state,
            scheduler,
            options,
            NullLogger<ProjectionFallbackPollingDriver>.Instance);
        sut.Start();

        state.Apply(new ProjectionConnectionTransition(ProjectionConnectionStatus.Disconnected, FailureCategory: "Closed"));
        await Task.Delay(150, TestContext.Current.CancellationToken);

        scheduler.TriggerCount.ShouldBe(0);
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

        await sut.DisposeAsync();
        int countAtDispose = scheduler.TriggerCount;
        await Task.Delay(150, TestContext.Current.CancellationToken);

        scheduler.TriggerCount.ShouldBe(countAtDispose);
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
}

file static class TestOptionsMonitorExtensions {
    public static IOptionsMonitor<T> ToMonitor<T>(this IOptions<T> options) where T : class {
        IOptionsMonitor<T> monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(options.Value);
        return monitor;
    }
}
