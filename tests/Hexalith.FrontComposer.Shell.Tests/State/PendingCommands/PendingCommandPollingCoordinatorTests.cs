using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class PendingCommandPollingCoordinatorTests {
    private const string CorrelationId = "01CPZ3NDEKTSV4RRFFQ69G5FAV";
    private const string SecondCorrelationId = "01DPZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public async Task PollOnce_ProcessesPendingCommandsOldestFirstWithinCap() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 1 }),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        time.Advance(TimeSpan.FromSeconds(1));
        state.Register(Registration(SecondCorrelationId, "01BRZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
#pragma warning disable CA2012 // NSubstitute owns the callback-produced ValueTask and the coordinator awaits it once.
        query.QueryAsync(Arg.Any<PendingCommandEntry>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                PendingCommandEntry entry = call[0] as PendingCommandEntry
                    ?? throw new InvalidOperationException("Expected a pending command entry argument.");
                return ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                    PendingCommandOutcomeSource.FallbackPolling,
                    PendingCommandTerminalOutcome.Confirmed,
                    entry.MessageId));
            });
#pragma warning restore CA2012

        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 1 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        state.GetByMessageId("01BRZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Fact]
    public async Task PollOnce_ZeroCapDisablesPendingPolling() {
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 0 }),
            CreateLifecycle(),
            new FakeTimeProvider(),
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", DateTimeOffset.UtcNow));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 0 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            new FakeTimeProvider());

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(0);
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PollOnce_LifecycleConvergenceRunsBeforeAndWithoutStatusTransport() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        CommandLifecycleState lifecycleState = CommandLifecycleState.Idle;
        string? lifecycleMessageId = null;
        bool applyTransition = false;
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(CorrelationId).Returns(_ => lifecycleState);
        lifecycle.GetMessageId(CorrelationId).Returns(_ => lifecycleMessageId);
        lifecycle
            .When(x => x.Transition(CorrelationId, Arg.Any<CommandLifecycleState>(), Arg.Any<string?>(), Arg.Any<bool>()))
            .Do(call => {
                if (!applyTransition) {
                    return;
                }

                lifecycleState = call.ArgAt<CommandLifecycleState>(1);
                lifecycleMessageId = call.ArgAt<string?>(2);
            });
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 1 };
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed("01ARZ3NDEKTSV4RRFFQ69G5FAV")).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        applyTransition = true;
        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        lifecycleState.ShouldBe(CommandLifecycleState.Confirmed);
        lifecycleMessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PollOnce_FailingConvergenceItemIsAttemptedOnceAndDoesNotStarveTransport() {
        const string failedMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string pendingMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        Dictionary<string, (CommandLifecycleState State, string? MessageId)> values = new(StringComparer.Ordinal);
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.State : CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.MessageId : null);
        lifecycle.When(service => service.Transition(
                SecondCorrelationId,
                Arg.Any<CommandLifecycleState>(),
                Arg.Any<string?>(),
                Arg.Any<bool>()))
            .Do(call => values[SecondCorrelationId] = (
                call.ArgAt<CommandLifecycleState>(1),
                call.ArgAt<string?>(2)));
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 2 };
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, failedMessageId, time.GetUtcNow()));
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(failedMessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        state.Register(Registration(SecondCorrelationId, pendingMessageId, time.GetUtcNow()));
        RecordingFixedStatusQuery query = new(pendingMessageId);
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId(pendingMessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        lifecycle.Received(2).Transition(CorrelationId, CommandLifecycleState.Confirmed, failedMessageId, false);
        query.MessageIds.ShouldBe([pendingMessageId]);
    }

    [Fact]
    public async Task PollOnce_ExpiredConvergenceDropsWithoutRequeueAndPermitsTransport() {
        const string expiredMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
        const string pendingMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 14, 12, 0, 0, TimeSpan.Zero));
        Dictionary<string, (CommandLifecycleState State, string? MessageId)> values = new(StringComparer.Ordinal);
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.State : CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0), out var value) ? value.MessageId : null);
        lifecycle.When(service => service.Transition(
                SecondCorrelationId,
                Arg.Any<CommandLifecycleState>(),
                Arg.Any<string?>(),
                Arg.Any<bool>()))
            .Do(call => values[SecondCorrelationId] = (
                call.ArgAt<CommandLifecycleState>(1),
                call.ArgAt<string?>(2)));
        FcShellOptions options = new() {
            MaxPendingCommandPollingPerTick = 2,
            MaxPendingCommandPollingDurationMs = 1_000,
        };
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, expiredMessageId, time.GetUtcNow()));
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(expiredMessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        time.Advance(TimeSpan.FromMilliseconds(1_001));
        state.Register(Registration(SecondCorrelationId, pendingMessageId, time.GetUtcNow()));
        RecordingFixedStatusQuery query = new(pendingMessageId);
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);
        _ = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, expiredMessageId, false);
        query.MessageIds.ShouldBe([pendingMessageId]);
    }

    [Fact]
    public async Task PollOnce_SkipsAlreadyResolvedEntriesMidTick() {
        // P16 — live nudge resolves entries between snapshot capture and per-entry query; coordinator
        // should re-check the entry status to skip wasted HTTP calls.
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        // Resolve the only pending entry BEFORE the coordinator queries.
        state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed("01ARZ3NDEKTSV4RRFFQ69G5FAV"));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(0);
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task PollOnce_StatusQueryThrows_LogsAndContinues() {
        // P9 — polling-coordinator catch should preserve stack trace in the log and not swallow
        // OperationCanceledException issued from the user-supplied cancellation.
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        state.Register(Registration(SecondCorrelationId, "01BRZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
#pragma warning disable CA2012 // NSubstitute owns the callback-produced ValueTask and the coordinator awaits it once.
        query.QueryAsync(Arg.Any<PendingCommandEntry>(), Arg.Any<CancellationToken>())
            .Returns<ValueTask<PendingCommandOutcomeObservation?>>(call => {
                PendingCommandEntry entry = call[0] as PendingCommandEntry
                    ?? throw new InvalidOperationException("Expected a pending command entry argument.");
                return entry.MessageId == "01ARZ3NDEKTSV4RRFFQ69G5FAV"
                    ? throw new InvalidOperationException("simulated transient failure")
                    : ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                        PendingCommandOutcomeSource.FallbackPolling,
                        PendingCommandTerminalOutcome.Confirmed,
                        entry.MessageId));
            });
#pragma warning restore CA2012

        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        // Failure on the first entry must not abort the loop; the second entry still resolves.
        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01BRZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public async Task PollOnce_EventStoreStatusProvider_ResolvesPendingByMessageId() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(
                """
                {
                  "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                  "status": "Completed",
                  "statusCode": 4,
                  "timestamp": "2026-06-04T00:00:00Z",
                  "aggregateId": "counter-1",
                  "eventCount": 1,
                  "rejectionEventType": null,
                  "failureReason": null,
                  "timeoutDuration": null
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        EventStorePendingCommandStatusQuery query = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
            }),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStorePendingCommandStatusQuery>.Instance);
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 5 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        handler.Requests.Single().RequestUri!.PathAndQuery.ShouldBe("/api/v1/commands/status/01ARZ3NDEKTSV4RRFFQ69G5FAV");
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, "01ARZ3NDEKTSV4RRFFQ69G5FAV", false);
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(-1, false)]
    public async Task PollOnce_EventStoreCompletionPublishesOnlyKnownMaterialTarget(
        int eventCount,
        bool expectIndicator) {
        DateTimeOffset now = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider time = new(now);
        FcShellOptions options = new() { MaxPendingCommandPollingPerTick = 5 };
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(options),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        CommandTargetSnapshot target = new(
            "Counter.Count",
            "counter-counts",
            "counter-1",
            CommandTargetChangeKind.Update,
            "Draft",
            "Approved",
            "tenant-1",
            "user-1",
            now.AddSeconds(-1));
        PendingCommandRegistration registration = Registration(
            CorrelationId,
            "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            now) with { TargetSnapshot = target };
        state.Register(registration).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(
                $$"""
                {
                  "correlationId": "01ARZ3NDEKTSV4RRFFQ69G5FAV",
                  "status": "Completed",
                  "statusCode": 4,
                  "timestamp": "2026-06-04T12:00:00Z",
                  "aggregateId": "counter-1",
                  "eventCount": {{eventCount}},
                  "rejectionEventType": null,
                  "failureReason": null,
                  "timeoutDuration": null
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        EventStorePendingCommandStatusQuery query = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
                BaseAddress = new Uri("https://eventstore.test"),
                RequireAccessToken = false,
            }),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStorePendingCommandStatusQuery>.Instance);
        using NewItemIndicatorStateService indicators = new(time);
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns("tenant-1");
        _ = userContext.UserId.Returns("user-1");
        PendingCommandOutcomeResolver resolver = new(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(options),
            userContext);
        PendingCommandPollingCoordinator sut = new(
            state,
            resolver,
            query,
            Microsoft.Extensions.Options.Options.Create(options),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!.Status
            .ShouldBe(PendingCommandStatus.Confirmed);
        if (expectIndicator) {
            NewItemIndicatorEntry indicator = indicators.Snapshot("counter-counts").Single();
            indicator.ViewKey.ShouldBe("counter-counts");
            indicator.EntityKey.ShouldBe("counter-1");
            indicator.MessageId.ShouldBe("01ARZ3NDEKTSV4RRFFQ69G5FAV");
            indicator.CreatedAt.ShouldBe(now);
        }
        else {
            indicators.Snapshot("counter-counts").ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task PollOnce_ExpiredPendingCommand_ResolvesNeedsReviewWithoutQueryingProvider() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(120_000));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                MaxPendingCommandPollingDurationMs = 120_000,
            }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        PendingCommandEntry expired = state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!;
        expired.Status.ShouldBe(PendingCommandStatus.NeedsReview);
        expired.RejectionTitle.ShouldBe("Command needs review");
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Rejected, "01ARZ3NDEKTSV4RRFFQ69G5FAV", false);
    }

    [Fact]
    public async Task PollOnce_ExpiredNeedsReview_FirstWinsOverLateConfirmedObservation() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            CreateLifecycle(),
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration(CorrelationId, "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        time.Advance(TimeSpan.FromMilliseconds(120_000));

        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            Substitute.For<IPendingCommandStatusQuery>(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                MaxPendingCommandPollingDurationMs = 120_000,
            }),
            NullLogger<PendingCommandPollingCoordinator>.Instance,
            time);

        _ = await sut.PollOnceAsync(TestContext.Current.CancellationToken);
        PendingCommandResolutionResult late = state.ResolveTerminal(PendingCommandTerminalObservation.Confirmed("01ARZ3NDEKTSV4RRFFQ69G5FAV"));

        late.Status.ShouldBe(PendingCommandResolutionStatus.DuplicateIgnored);
        PendingCommandEntry entry = state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!;
        entry.Status.ShouldBe(PendingCommandStatus.NeedsReview);
        entry.DuplicateTerminalObservations.ShouldBe(1);
    }

    private static PendingCommandRegistration Registration(string correlationId, string messageId, DateTimeOffset submittedAt) =>
        new(
            CorrelationId: correlationId,
            MessageId: messageId,
            CommandTypeName: "Counter.Increment",
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            EntityKey: "counter-1",
            ExpectedStatusSlot: "Approved",
            PriorStatusSlot: "Draft",
            SubmittedAt: submittedAt);

    private static ILifecycleStateService CreateLifecycle() {
        Dictionary<string, (CommandLifecycleState State, string? MessageId)> values = new(StringComparer.Ordinal);
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0)!, out var value)
                ? value.State
                : CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns(call =>
            values.TryGetValue(call.ArgAt<string>(0)!, out var value)
                ? value.MessageId
                : null);
        lifecycle
            .When(x => x.Transition(
                Arg.Any<string>(),
                Arg.Any<CommandLifecycleState>(),
                Arg.Any<string?>(),
                Arg.Any<bool>()))
            .Do(call => values[call.ArgAt<string>(0)] = (
                call.ArgAt<CommandLifecycleState>(1),
                call.ArgAt<string?>(2)));
        return lifecycle;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class RecordingFixedStatusQuery(string terminalMessageId) : IPendingCommandStatusQuery {
        public List<string> MessageIds { get; } = [];

        public ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
            PendingCommandEntry pendingCommand,
            CancellationToken cancellationToken = default) {
            MessageIds.Add(pendingCommand.MessageId);
            return ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                PendingCommandOutcomeSource.FallbackPolling,
                PendingCommandTerminalOutcome.Confirmed,
                terminalMessageId));
        }
    }
}
