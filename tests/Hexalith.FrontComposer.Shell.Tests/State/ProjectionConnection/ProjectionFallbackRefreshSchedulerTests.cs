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

    [Fact]
    public async Task TriggerReconciliationOnce_SnapshotsVisibleLanes_DedupesAndReportsChangedOnlyFor200Delta() {
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
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

        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 10 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:CustomersProjection", "CustomersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection:dup", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(42, TestContext.Current.CancellationToken);

        result.RefreshedCount.ShouldBe(2);
        result.ChangedViewKeys.ShouldBe(["acme:OrdersProjection"]);
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
        TestConnectionState state = new(new ProjectionConnectionSnapshot(
            ProjectionConnectionStatus.Connected,
            DateTimeOffset.UtcNow,
            ReconnectAttempt: 0,
            LastFailureCategory: null));
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

        ProjectionFallbackRefreshScheduler sut = new(
            state,
            loader,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxProjectionFallbackPollingLanes = 10 }).ToMonitor(),
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
        _ = sut.RegisterLane(new ProjectionFallbackLane("acme:OrdersProjection", "OrdersProjection", "acme", 0, 20, ImmutableDictionary<string, string>.Empty, null, false, null));

        ProjectionReconciliationRefreshResult firstPass = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult secondPass = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);

        // First pass: first observation with items → Changed.
        firstPass.ChangedViewKeys.ShouldBe(["acme:OrdersProjection"]);
        // Second pass: same ETag → NotModified, no false-positive Changed.
        secondPass.ChangedViewKeys.ShouldBeEmpty();
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
