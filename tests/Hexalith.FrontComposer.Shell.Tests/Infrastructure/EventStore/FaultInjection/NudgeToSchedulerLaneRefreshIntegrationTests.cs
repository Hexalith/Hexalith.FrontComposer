#pragma warning disable CA2007 // ConfigureAwait — test code (matches FaultInjection directory convention)

using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Story 2.6 AC1(a) end-to-end pin — proves a live SignalR nudge drives the registered grid lane's
/// refresh through the <em>real</em> wiring: fault harness → <see cref="ProjectionSubscriptionService"/>
/// (<c>OnProjectionChangedAsync</c>) → <em>real</em> <see cref="ProjectionFallbackRefreshScheduler"/>
/// → the lane's registered <c>RefreshAsync</c> (the seam the generated view registers via
/// <c>RegisterLane(...)</c>).
/// </summary>
/// <remarks>
/// The existing fault suites drive the subscription service against a <em>stub</em> scheduler
/// (asserting only that <c>TriggerNudgeRefreshAsync</c> is called), and the scheduler unit suite
/// exercises lane refresh + ETag gating against the scheduler directly. Neither connects the two:
/// this pin closes the "nudge → grid lane actually re-queries" gap end-to-end with production types,
/// including projection-type/tenant routing isolation. Confirm-and-pin: ZERO <c>src/</c> change.
/// </remarks>
public sealed class NudgeToSchedulerLaneRefreshIntegrationTests {
    private const string HubPath = "/hubs/projection-changes";

    [Fact]
    public async Task LiveNudge_RefreshesTheRegisteredLane_ThroughTheRealScheduler() {
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler();
        Func<int> ordersRefreshes = RegisterLane(scheduler, "orders:acme", "orders", "acme");
        await using FaultInjectingProjectionHubConnection harness = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, scheduler);

        await sut.SubscribeAsync("orders", "acme", Xunit.TestContext.Current.CancellationToken);

        await harness.PublishNudgeAsync("orders", "acme");

        ordersRefreshes().ShouldBe(1);
    }

    [Fact]
    public async Task LiveNudge_RoutesToMatchingLaneOnly_LeavesOtherProjectionUntouched() {
        ProjectionFallbackRefreshScheduler scheduler = CreateScheduler();
        Func<int> ordersRefreshes = RegisterLane(scheduler, "orders:acme", "orders", "acme");
        Func<int> billingRefreshes = RegisterLane(scheduler, "billing:acme", "billing", "acme");
        await using FaultInjectingProjectionHubConnection harness = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, scheduler);

        await sut.SubscribeAsync("orders", "acme", Xunit.TestContext.Current.CancellationToken);
        await sut.SubscribeAsync("billing", "acme", Xunit.TestContext.Current.CancellationToken);

        await harness.PublishNudgeAsync("orders", "acme");

        ordersRefreshes().ShouldBe(1);
        billingRefreshes().ShouldBe(0);
    }

    private static Func<int> RegisterLane(
        ProjectionFallbackRefreshScheduler scheduler,
        string viewKey,
        string projectionType,
        string tenantId) {
        int count = 0;
        _ = scheduler.RegisterLane(new ProjectionFallbackLane(
            viewKey,
            projectionType,
            tenantId,
            Skip: 0,
            Take: 50,
            Filters: ImmutableDictionary<string, string>.Empty,
            SortColumn: null,
            SortDescending: false,
            SearchQuery: null,
            RefreshAsync: cancellationToken => {
                cancellationToken.ThrowIfCancellationRequested();
                _ = Interlocked.Increment(ref count);
                return ValueTask.FromResult(ProjectionFallbackLaneRefreshOutcome.Changed);
            }));
        return () => Volatile.Read(ref count);
    }

    private static ProjectionFallbackRefreshScheduler CreateScheduler() {
        IOptionsMonitor<FcShellOptions> options = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        options.CurrentValue.Returns(new FcShellOptions { MaxProjectionFallbackPollingLanes = 8 });
        return new ProjectionFallbackRefreshScheduler(
            new TestProjectionConnectionState(),
            Substitute.For<IProjectionPageLoader>(),
            options,
            NullLogger<ProjectionFallbackRefreshScheduler>.Instance);
    }

    private static ProjectionSubscriptionService CreateSut(
        FaultInjectingProjectionHubConnection harness,
        IProjectionFallbackRefreshScheduler scheduler) {
        EventStoreOptions options = new() {
            BaseAddress = new Uri("https://eventstore.test"),
            RequireAccessToken = false,
            ProjectionChangesHubPath = HubPath,
        };
        FaultInjectingProjectionHubConnectionFactory factory = new(harness, new Uri($"https://eventstore.test{HubPath}"));
        return new ProjectionSubscriptionService(
            global::Microsoft.Extensions.Options.Options.Create(options),
            factory,
            new TestProjectionConnectionState(),
            scheduler,
            new TestNotifier(),
            NullLogger<ProjectionSubscriptionService>.Instance);
    }

    private sealed class TestNotifier : IProjectionChangeNotifier {
        public event Action<string>? ProjectionChanged;

        public void NotifyChanged(string projectionType) => ProjectionChanged?.Invoke(projectionType);
    }

    private sealed class TestProjectionConnectionState : IProjectionConnectionState {
        public ProjectionConnectionSnapshot Current { get; private set; } = new(
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

        public void Apply(ProjectionConnectionTransition transition)
            => Current = new ProjectionConnectionSnapshot(
                transition.Status,
                DateTimeOffset.UtcNow,
                transition.ReconnectAttempt,
                transition.FailureCategory);

        private sealed class Registration : IDisposable {
            public void Dispose() {
            }
        }
    }
}
