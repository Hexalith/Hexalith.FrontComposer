using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.Json;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.ProjectionConnection;

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
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(
            loader,
            Substitute.For<IDispatcher>(),
            new MutableLoadedPageState(PageState(viewKey, ["existing"], totalCount: 1)));
        _ = sut.RegisterLane(DefaultLane(viewKey));

        ProjectionReconciliationRefreshResult first = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult second = await sut.TriggerReconciliationOnceAsync(2, TestContext.Current.CancellationToken);

        first.ChangedViewKeys.ShouldBe([viewKey]);
        second.ChangedViewKeys.ShouldBeEmpty();
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
        Queue<ProjectionPageResult> results = new([
            new ProjectionPageResult([JsonRow("{\"id\":1,\"values\":[1,2]}")], 1, null),
            new ProjectionPageResult([JsonRow("{\"id\":1,\"values\":[2,1]}")], 1, null),
            new ProjectionPageResult(overLimitRows, overLimitRows.Length, null),
            new ProjectionPageResult(overLimitRows, overLimitRows.Length, null),
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
        ProjectionReconciliationRefreshResult boundedFirst = await sut.TriggerReconciliationOnceAsync(3, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult boundedRepeat = await sut.TriggerReconciliationOnceAsync(4, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult failedFirst = await sut.TriggerReconciliationOnceAsync(5, TestContext.Current.CancellationToken);
        ProjectionReconciliationRefreshResult failedRepeat = await sut.TriggerReconciliationOnceAsync(6, TestContext.Current.CancellationToken);

        first.ChangedViewKeys.ShouldBe([viewKey]);
        second.ChangedViewKeys.ShouldBe([viewKey]);
        boundedFirst.ChangedViewKeys.ShouldBe([viewKey]);
        boundedRepeat.ChangedViewKeys.ShouldBe([viewKey]);
        failedFirst.ChangedViewKeys.ShouldBe([viewKey]);
        failedRepeat.ChangedViewKeys.ShouldBe([viewKey]);
        dispatcher.Received(6).Dispatch(Arg.Is<LoadPageSucceededAction>(action => action.ViewKey == viewKey));
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
        IProjectionPageLoader loader = LoaderReturning(new ProjectionPageResult([], 0, "\"v1\"", IsNotModified: true));
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        MutableLoadedPageState loadedPages = new(new LoadedPageState());
        ProjectionFallbackRefreshScheduler sut = CreateScheduler(loader, dispatcher, loadedPages);
        _ = sut.RegisterLane(DefaultLane(viewKey));

        ProjectionReconciliationRefreshResult result = await sut.TriggerReconciliationOnceAsync(1, TestContext.Current.CancellationToken);

        result.ChangedViewKeys.ShouldBe([viewKey]);
        dispatcher.Received(1).Dispatch(Arg.Is<LoadPageSucceededAction>(action =>
            action.ViewKey == viewKey && action.Items != null && action.Items.Count == 0));
        dispatcher.DidNotReceive().Dispatch(Arg.Any<LoadPageNotModifiedAction>());
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
        ProjectionFallbackLane incumbent = DefaultLane(viewKey);
        _ = sut.RegisterLane(incumbent);
        Func<CancellationToken, ValueTask<ProjectionFallbackLaneRefreshOutcome>> callback =
            static _ => ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
        ProjectionFallbackLane[] conflicts = [
            incumbent with { ProjectionType = "CustomersProjection" },
            incumbent with { Skip = 20 },
            incumbent with { Take = 50 },
            incumbent with { Filters = incumbent.Filters.Add("status", "open") },
            incumbent with { SortColumn = "Name" },
            incumbent with { SortDescending = true },
            incumbent with { SearchQuery = "query" },
            incumbent with { RefreshAsync = callback },
        ];

        foreach (ProjectionFallbackLane conflict in conflicts) {
            InvalidOperationException exception = Should.Throw<InvalidOperationException>(() => sut.RegisterLane(conflict));
            exception.Message.ShouldBe("A conflicting fallback lane contract is already registered for this view.");
        }
    }

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
