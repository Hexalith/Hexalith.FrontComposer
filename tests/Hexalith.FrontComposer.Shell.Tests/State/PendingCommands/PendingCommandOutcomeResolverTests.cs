using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class PendingCommandOutcomeResolverTests {
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public void Resolve_FromMessageId_UsesSharedPendingStateTerminalPath() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: MessageId));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        state.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        lifecycle.Received(1).Transition("corr-1", CommandLifecycleState.Confirmed, MessageId);
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
        state.Register(Registration("corr-2", "01BRZ3NDEKTSV4RRFFQ69G5FAV")).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: null));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.AmbiguousMatch);
        state.Snapshot().All(e => e.Status == PendingCommandStatus.Pending).ShouldBeTrue();
        lifecycle.DidNotReceiveWithAnyArgs().Transition(default!, default, default);
    }

    private static PendingCommandOutcomeResolver Create(
        ILifecycleStateService lifecycle,
        out PendingCommandStateService state) {
        state = new PendingCommandStateService(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<PendingCommandStateService>.Instance);

        return new PendingCommandOutcomeResolver(state, NullLogger<PendingCommandOutcomeResolver>.Instance);
    }

    private static PendingCommandRegistration Registration(
        string correlationId = "corr-1",
        string messageId = MessageId) =>
        new(
            CorrelationId: correlationId,
            MessageId: messageId,
            CommandTypeName: "Counter.Increment",
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            EntityKey: "counter-1",
            ExpectedStatusSlot: "Approved",
            PriorStatusSlot: "Draft");

    private static PendingCommandOutcomeObservation Outcome(string? messageId) =>
        new(
            Source: PendingCommandOutcomeSource.ReconnectReconciliation,
            Outcome: PendingCommandTerminalOutcome.Confirmed,
            MessageId: messageId,
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            EntityKey: "counter-1",
            ExpectedStatusSlot: "Approved");
}
