using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.ProjectionConnection;

public sealed class ProjectionFallbackRefreshSchedulerTests {
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

    private sealed class TestConnectionState(ProjectionConnectionSnapshot snapshot) : IProjectionConnectionState {
        public ProjectionConnectionSnapshot Current { get; private set; } = snapshot;

        public IDisposable Subscribe(Action<ProjectionConnectionSnapshot> handler, bool replay = true)
            => new Registration();

        public void Apply(ProjectionConnectionTransition transition) {
            Current = new ProjectionConnectionSnapshot(
                transition.Status,
                DateTimeOffset.UtcNow,
                transition.ReconnectAttempt,
                transition.FailureCategory);
        }
    }

    private sealed class Registration : IDisposable {
        public void Dispose() {
        }
    }
}

file static class OptionsMonitorExtensions {
    public static IOptionsMonitor<T> ToMonitor<T>(this IOptions<T> options) where T : class {
        IOptionsMonitor<T> monitor = Substitute.For<IOptionsMonitor<T>>();
        monitor.CurrentValue.Returns(options.Value);
        return monitor;
    }
}
