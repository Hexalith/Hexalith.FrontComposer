#pragma warning disable CA2007 // ConfigureAwait — test code (matches project convention)

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;
using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Story 5-7 fault scenarios driven by <see cref="FaultInjectingProjectionHubConnection"/>
/// against the real <see cref="ProjectionSubscriptionService"/>. Validates the harness shape
/// against production semantics (D11 — first consumer) and adds the deferred Story 5-3 race
/// scenarios + Story 5-4/5-5 reconcile/idempotency interactions called out by the FR24-FR29
/// traceability matrix.
/// </summary>
public sealed class ProjectionSubscriptionServiceFaultTests {
    [Fact]
    public async Task InitialStartFailure_SurfacesDisconnectedState_WithStickyCategory() {
        // FR24b — sticky InitialStartFailed distinct from later disconnect categories.
        await using FaultInjectingProjectionHubConnection harness = new();
        TestProjectionConnectionState state = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, state: state);

        harness.FailNext(HarnessCheckpoint.Start, new InvalidOperationException("token expired"));

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        state.Current.Status.ShouldBe(ProjectionConnectionStatus.Disconnected);
        state.Current.LastFailureCategory.ShouldBe("InitialStartFailed");
    }

    [Fact]
    public async Task ClosedAfterInitialStartFailure_PreservesCategory() {
        // FR25d — once InitialStartFailed is sticky, a later Closed event must not overwrite it
        // with a less-specific category.
        await using FaultInjectingProjectionHubConnection harness = new();
        TestProjectionConnectionState state = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, state: state);

        harness.FailNext(HarnessCheckpoint.Start, new InvalidOperationException("token expired"));
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        await harness.RaiseStateAsync(HarnessConnectionStates.Closed(new IOException("transport"))).ConfigureAwait(true);

        state.Current.Status.ShouldBe(ProjectionConnectionStatus.Disconnected);
        state.Current.LastFailureCategory.ShouldBe("InitialStartFailed");
    }

    [Fact]
    public async Task FailedRejoin_MarksGroupDegraded_AndSkipsNudgeRefresh_UntilNextSuccessfulRejoin() {
        // FR25b — failed rejoin marks the affected group only; nudges for that group skip until
        // the next successful rejoin restores it.
        await using FaultInjectingProjectionHubConnection harness = new();
        TestNotifier notifier = new();
        TestRefreshScheduler refresh = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, notifier: notifier, scheduler: refresh);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        refresh.NudgeRefreshes.Clear();

        harness.FailNext(HarnessCheckpoint.Join("orders", "acme"), new InvalidOperationException("transient"));
        await harness.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2")).ConfigureAwait(true);

        await harness.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        refresh.NudgeRefreshes.ShouldBeEmpty();
        notifier.Changed.ShouldBeEmpty();

        await harness.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-3")).ConfigureAwait(true);
        await harness.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        refresh.NudgeRefreshes.ShouldBe([("orders", "acme")]);
    }

    [Fact]
    public async Task PartialDelivery_DropsOneGroup_DeliversTheOther() {
        // FR26c — partial nudge delivery should not cross-contaminate active groups.
        await using FaultInjectingProjectionHubConnection harness = new();
        TestRefreshScheduler refresh = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, scheduler: refresh);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.SubscribeAsync("billing", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        refresh.NudgeRefreshes.Clear();

        harness.DropNextNudge("orders", "acme");
        await harness.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);
        await harness.PublishNudgeAsync("billing", "acme").ConfigureAwait(true);

        refresh.NudgeRefreshes.ShouldBe([("billing", "acme")]);
    }

    [Fact]
    public async Task NudgeReorder_DoesNotReplay_UsesQueueFlushOrder() {
        // FR29a — out-of-order nudges must be deterministically reproducible. Even when the test
        // releases queued nudges in inverse order, the production code processes them as
        // independent refresh requests without command replay.
        await using FaultInjectingProjectionHubConnection harness = new();
        TestRefreshScheduler refresh = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, scheduler: refresh);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.SubscribeAsync("billing", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        refresh.NudgeRefreshes.Clear();

        NudgeQueueToken first = harness.QueueNudge("orders", "acme");
        NudgeQueueToken second = harness.QueueNudge("billing", "acme");

        await harness.ReleaseInOrderAsync([second, first]).ConfigureAwait(true);

        refresh.NudgeRefreshes.ShouldBe([("billing", "acme"), ("orders", "acme")]);
    }

    [Fact]
    public async Task DuplicateNudge_DispatchedToHandlersTwice_DoesNotCrashService() {
        // FR29b — duplicate terminal outcomes must be handled idempotently. The service
        // forwards each call to the scheduler; idempotency is owned by Story 5-5 resolver.
        await using FaultInjectingProjectionHubConnection harness = new();
        TestRefreshScheduler refresh = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, scheduler: refresh);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        refresh.NudgeRefreshes.Clear();

        harness.DuplicateNextNudge("orders", "acme", count: 2);
        await harness.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);

        refresh.NudgeRefreshes.ShouldBe([("orders", "acme"), ("orders", "acme")]);
    }

    [Fact]
    public async Task DisposeDuringRejoin_DoesNotCorruptState_AndRespectsDisposalToken() {
        // FR29c — dispose during in-flight rejoin: the join blocked on a checkpoint must complete
        // (faulted/canceled via the disposal token) so DisposeAsync does not deadlock. Production
        // code uses the service's disposal CTS to cancel the rejoin sweep.
        await using FaultInjectingProjectionHubConnection harness = new();
        ProjectionSubscriptionService sut = CreateSut(harness);

        try {
            await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);

            harness.BlockUntil(HarnessCheckpoint.Join("orders", "acme"));
            Task reconnected = harness.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2"));

            await sut.DisposeAsync().ConfigureAwait(true);

            // Reconnected may complete cleanly (rejoin saw cancellation) or with an
            // OperationCanceledException; either is acceptable as long as it doesn't hang.
            Exception? failure = await Record.ExceptionAsync(async () => await reconnected.ConfigureAwait(true)).ConfigureAwait(true);
            (failure is null || failure is OperationCanceledException).ShouldBeTrue();
        }
        finally {
            // Drain the still-armed Block so harness disposal does not flag outstanding state.
            harness.Release(HarnessCheckpoint.Join("orders", "acme"));
        }
    }

    [Fact]
    public async Task DuplicateSubscribeDuringReconnect_SafelyDeduplicates_NoExtraJoin() {
        // Story 5-3 deferred — duplicate Subscribe while a reconnect is in flight must not
        // double-join the group.
        await using FaultInjectingProjectionHubConnection harness = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);

        await harness.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2")).ConfigureAwait(true);
        int hitsAfterReconnect = harness.GetHitCount(HarnessCheckpoint.Join("orders", "acme"));

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);

        // Second SubscribeAsync is a no-op — the group is already active.
        harness.GetHitCount(HarnessCheckpoint.Join("orders", "acme")).ShouldBe(hitsAfterReconnect);
    }

    [Fact]
    public async Task DisposalSuppressesLaterCallbacks_NudgeAfterDispose_DoesNotInvokeNotifier() {
        // Story 5-3 deferred — callback suppression after disposal.
        await using FaultInjectingProjectionHubConnection harness = new();
        TestNotifier notifier = new();
        ProjectionSubscriptionService sut = CreateSut(harness, notifier: notifier);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);
        await sut.DisposeAsync().ConfigureAwait(true);

        notifier.Changed.Clear();
        await harness.PublishNudgeAsync("orders", "acme").ConfigureAwait(true);

        notifier.Changed.ShouldBeEmpty();
    }

    [Fact]
    public async Task RejoinFailure_LogsFailureCategoryOnly_NoRawTenantOrException() {
        // FR28 — fault-path log must be sanitized: failure category only.
        await using FaultInjectingProjectionHubConnection harness = new();
        const string sensitive = "tenant=acme group=orders:acme token=Bearer-secret";
        CapturingLogger<ProjectionSubscriptionService> logger = new();
        await using ProjectionSubscriptionService sut = CreateSut(harness, logger: logger);

        await sut.SubscribeAsync("orders", "acme", TestContext.Current.CancellationToken).ConfigureAwait(true);

        harness.FailNext(HarnessCheckpoint.Join("orders", "acme"), new InvalidOperationException(sensitive));
        await harness.RaiseStateAsync(HarnessConnectionStates.Reconnected("conn-2")).ConfigureAwait(true);

        logger.Entries.ShouldNotBeEmpty();
        foreach (CapturingLogger<ProjectionSubscriptionService>.Entry entry in logger.Entries) {
            entry.Message.ShouldNotContain("Bearer-secret");
            entry.Message.ShouldNotContain("acme");
            entry.Message.ShouldNotContain(sensitive);
        }

        logger.Entries.ShouldContain(e => e.Message.Contains("InvalidOperationException", StringComparison.Ordinal));
    }

    // ---------- helpers ----------

    private static ProjectionSubscriptionService CreateSut(
        FaultInjectingProjectionHubConnection harness,
        IProjectionConnectionState? state = null,
        IProjectionFallbackRefreshScheduler? scheduler = null,
        IProjectionChangeNotifier? notifier = null,
        ILogger<ProjectionSubscriptionService>? logger = null) {
        const string hubPath = "/hubs/projection-changes";
        EventStoreOptions options = new() {
            BaseAddress = new Uri("https://eventstore.test"),
            RequireAccessToken = false,
            ProjectionChangesHubPath = hubPath,
        };
        FaultInjectingProjectionHubConnectionFactory factory = new(harness, new Uri($"https://eventstore.test{hubPath}"));
        return new ProjectionSubscriptionService(
            global::Microsoft.Extensions.Options.Options.Create(options),
            factory,
            state ?? new TestProjectionConnectionState(),
            scheduler ?? new TestRefreshScheduler(),
            notifier ?? new TestNotifier(),
            logger ?? NullLogger<ProjectionSubscriptionService>.Instance);
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

            return new Registration(() => { });
        }

        public void Apply(ProjectionConnectionTransition transition)
            => Current = new ProjectionConnectionSnapshot(
                transition.Status,
                DateTimeOffset.UtcNow,
                transition.ReconnectAttempt,
                transition.FailureCategory);
    }

    private sealed class TestRefreshScheduler : IProjectionFallbackRefreshScheduler {
        public List<(string ProjectionType, string TenantId)> NudgeRefreshes { get; } = [];

        public IDisposable RegisterLane(ProjectionFallbackLane lane) => new Registration(() => { });

        public Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

        public Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
            NudgeRefreshes.Add((projectionType, tenantId));
            return Task.FromResult(1);
        }
    }

    private sealed class TestNotifier : IProjectionChangeNotifier {
        public event Action<string>? ProjectionChanged;
        public List<string> Changed { get; } = [];

        public void NotifyChanged(string projectionType) {
            Changed.Add(projectionType);
            ProjectionChanged?.Invoke(projectionType);
        }
    }

    private sealed class Registration(Action onDispose) : IDisposable {
        public void Dispose() => onDispose();
    }

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<Entry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add(new Entry(logLevel, formatter(state, exception), exception?.GetType().Name));

        public sealed record Entry(LogLevel Level, string Message, string? ExceptionType);
    }
}
