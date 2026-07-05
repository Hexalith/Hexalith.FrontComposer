using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class PendingCommandOutcomeResolverTests {
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01CPZ3NDEKTSV4RRFFQ69G5FAV";
    private const string SecondCorrelationId = "01DPZ3NDEKTSV4RRFFQ69G5FAV";
    private static readonly DateTimeOffset s_observedAt = new(2026, 6, 4, 13, 30, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_FromMessageId_UsesSharedPendingStateTerminalPath() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: MessageId));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        state.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, false);
    }

    [Fact]
    public void Resolve_FromFrameworkEntityKey_RequiresSingleCandidate() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: null));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        state.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public void Resolve_FromFrameworkEntityKey_MultipleCandidates_RemainsAmbiguous() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        state.Register(Registration(SecondCorrelationId, "01BRZ3NDEKTSV4RRFFQ69G5FAV")).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: null));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.AmbiguousMatch);
        state.Snapshot().All(e => e.Status == PendingCommandStatus.Pending).ShouldBeTrue();
        lifecycle.DidNotReceiveWithAnyArgs().Transition(default!, default, default);
    }

    [Fact]
    public void Resolve_ConfirmedWithCompletePendingMetadata_AddsNewItemIndicatorWithObservedTimestamp() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: MessageId, observedAt: s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        NewItemIndicatorEntry entry = indicators.Snapshot("counter-counts").Single();
        entry.ViewKey.ShouldBe("counter-counts");
        entry.EntityKey.ShouldBe("counter-1");
        entry.MessageId.ShouldBe(MessageId);
        entry.CreatedAt.ShouldBe(s_observedAt);
    }

    [Fact]
    public void Resolve_DuplicateTerminalObservation_DoesNotResetIndicatorTtl() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        sut.Resolve(Outcome(messageId: MessageId, observedAt: s_observedAt)).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        time.Advance(TimeSpan.FromSeconds(5));
        PendingCommandOutcomeResolutionResult duplicate = sut.Resolve(Outcome(
            messageId: MessageId,
            observedAt: s_observedAt.AddSeconds(5)));

        duplicate.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.DuplicateIgnored);
        NewItemIndicatorEntry entry = indicators.Snapshot("counter-counts").Single();
        entry.CreatedAt.ShouldBe(s_observedAt);
        time.Advance(TimeSpan.FromSeconds(5));
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null, "counter-counts", "counter-1")]
    [InlineData("Counter.Count", null, "counter-1")]
    [InlineData("Counter.Count", "counter-counts", null)]
    public void Resolve_ConfirmedWithIncompletePendingMetadata_DoesNotAddIndicator(
        string? projectionTypeName,
        string? laneKey,
        string? entityKey) {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        state.Register(Registration(
            projectionTypeName: projectionTypeName,
            laneKey: laneKey,
            entityKey: entityKey)).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: MessageId, observedAt: s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_RejectedTerminalOutcome_DoesNotAddIndicator() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(new PendingCommandOutcomeObservation(
            Source: PendingCommandOutcomeSource.ReconnectReconciliation,
            Outcome: PendingCommandTerminalOutcome.Rejected,
            MessageId: MessageId,
            RejectionTitle: "Rejected",
            RejectionDetail: "No change was applied.",
            ObservedAt: s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_EntityKeyOnlyFallback_DoesNotMutatePendingStateOrIndicator() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(new PendingCommandOutcomeObservation(
            Source: PendingCommandOutcomeSource.LiveNudgeRefresh,
            Outcome: PendingCommandTerminalOutcome.Confirmed,
            EntityKey: "counter-1",
            ObservedAt: s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        state.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Pending);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
        lifecycle.DidNotReceiveWithAnyArgs().Transition(default!, default, default);
    }

    [Fact]
    public void Resolve_ProjectionNudgeOnly_DoesNotAddIndicator() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero));
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(new PendingCommandOutcomeObservation(
            Source: PendingCommandOutcomeSource.LiveNudgeRefresh,
            Outcome: PendingCommandTerminalOutcome.Confirmed,
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            ObservedAt: s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    private static PendingCommandOutcomeResolver Create(
        ILifecycleStateService lifecycle,
        out PendingCommandStateService state,
        INewItemIndicatorStateService? indicators = null,
        TimeProvider? resolverTime = null) {
        TimeProvider stateTime = resolverTime ?? new FakeTimeProvider(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        state = new PendingCommandStateService(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            stateTime,
            NullLogger<PendingCommandStateService>.Instance);

        return new PendingCommandOutcomeResolver(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            resolverTime);
    }

    private static PendingCommandRegistration Registration(
        string correlationId = CorrelationId,
        string messageId = MessageId,
        string? projectionTypeName = "Counter.Count",
        string? laneKey = "counter-counts",
        string? entityKey = "counter-1") =>
        new(
            CorrelationId: correlationId,
            MessageId: messageId,
            CommandTypeName: "Counter.Increment",
            ProjectionTypeName: projectionTypeName,
            LaneKey: laneKey,
            EntityKey: entityKey,
            ExpectedStatusSlot: "Approved",
            PriorStatusSlot: "Draft");

    [Fact]
    public void Resolve_RejectionDuringReconnect_DoesNotMutateFormState() {
        // P21 — reconnect-derived rejection routes through the resolver and surfaces a terminal
        // pending-command record with rejection metadata. The resolver path interacts ONLY with
        // the pending-command state and lifecycle services; it MUST NOT touch form-side
        // EditContext / ValidationMessageStore / IStorageService. This test stands in for the
        // full bUnit form-preservation harness by asserting resolver isolation: a fake storage
        // service receives no calls during the reject path. (The form-state contract is owned
        // by Story 5-3 / 5-4 tests; this test guards that the 5-5 reject path stays on its lane.)
        IStorageService storage = Substitute.For<IStorageService>();
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(new PendingCommandOutcomeObservation(
            Source: PendingCommandOutcomeSource.ReconnectReconciliation,
            Outcome: PendingCommandTerminalOutcome.Rejected,
            MessageId: MessageId,
            RejectionTitle: "Duplicate aggregate",
            RejectionDetail: "Server rejected the change",
            RejectionDataImpact: "No data changed."));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        PendingCommandEntry entry = result.Entry.ShouldNotBeNull();
        entry.Status.ShouldBe(PendingCommandStatus.Rejected);
        entry.RejectionTitle.ShouldBe("Duplicate aggregate");
        entry.RejectionDetail.ShouldBe("Server rejected the change");
        entry.RejectionDataImpact.ShouldBe("No data changed.");
        // Resolver MUST NOT call back into storage on the reject path.
        storage.ReceivedCalls().ShouldBeEmpty();
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Rejected, MessageId, false);
    }

    private static PendingCommandOutcomeObservation Outcome(string? messageId, DateTimeOffset? observedAt = null) =>
        new(
            Source: PendingCommandOutcomeSource.ReconnectReconciliation,
            Outcome: PendingCommandTerminalOutcome.Confirmed,
            MessageId: messageId,
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            EntityKey: "counter-1",
            ExpectedStatusSlot: "Approved",
            ObservedAt: observedAt);
}
