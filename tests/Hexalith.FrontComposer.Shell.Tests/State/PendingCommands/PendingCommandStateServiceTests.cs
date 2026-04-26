using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class PendingCommandStateServiceTests {
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public void Register_AcceptedCommand_StoresOnlyFrameworkMetadata() {
        PendingCommandStateService sut = Create();

        PendingCommandRegistrationResult result = sut.Register(Registration());

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        PendingCommandEntry entry = sut.GetByMessageId(MessageId).ShouldNotBeNull();
        entry.CorrelationId.ShouldBe("corr-1");
        entry.MessageId.ShouldBe(MessageId);
        entry.CommandTypeName.ShouldBe("Counter.Increment");
        entry.EntityKey.ShouldBe("counter-1");
        entry.Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-ulid")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAI")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAVEXTRA")]
    public void Register_MalformedMessageId_FailsClosed(string badMessageId) {
        PendingCommandStateService sut = Create();

        PendingCommandRegistrationResult result = sut.Register(Registration(messageId: badMessageId));

        result.Status.ShouldBe(PendingCommandRegistrationStatus.InvalidMessageId);
        sut.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void Register_DuplicateWithMatchingMetadata_Merges() {
        PendingCommandStateService sut = Create();
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandRegistrationResult result = sut.Register(Registration());

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Merged);
        sut.Snapshot().Count.ShouldBe(1);
    }

    [Fact]
    public void Register_DuplicateWithConflictingMetadata_RejectsSecondRegistration() {
        PendingCommandStateService sut = Create();
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandRegistrationResult result = sut.Register(Registration(entityKey: "counter-2"));

        result.Status.ShouldBe(PendingCommandRegistrationStatus.ConflictingMetadata);
        sut.GetByMessageId(MessageId)!.EntityKey.ShouldBe("counter-1");
    }

    [Fact]
    public void ResolveTerminal_FirstOutcomeWins_AndTransitionsLifecycleOnce() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult first = sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId));
        PendingCommandResolutionResult duplicate = sut.ResolveTerminal(PendingCommandTerminalObservation.Rejected(MessageId, "Failed", "No change was applied."));

        first.Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        first.Entry!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        duplicate.Status.ShouldBe(PendingCommandResolutionStatus.DuplicateIgnored);
        lifecycle.Received(1).Transition("corr-1", CommandLifecycleState.Confirmed, MessageId);
        lifecycle.DidNotReceive().Transition("corr-1", CommandLifecycleState.Rejected, MessageId);
    }

    [Fact]
    public void ResolveTerminal_IdempotentConfirmed_PreservesAlreadyAppliedOutcome() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult result = sut.ResolveTerminal(PendingCommandTerminalObservation.IdempotentConfirmed(MessageId));

        result.Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        result.Entry!.Status.ShouldBe(PendingCommandStatus.IdempotentConfirmed);
        result.Entry.DuplicateTerminalObservations.ShouldBe(0);
        lifecycle.Received(1).Transition("corr-1", CommandLifecycleState.Confirmed, MessageId);
    }

    [Fact]
    public void ResolveTerminal_UnknownMessageId_IsIgnoredWithoutLifecycleMutation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);

        PendingCommandResolutionResult result = sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId));

        result.Status.ShouldBe(PendingCommandResolutionStatus.UnknownMessageId);
        lifecycle.DidNotReceiveWithAnyArgs().Transition(default!, default, default);
    }

    [Fact]
    public void Register_WhenCapExceeded_EvictsOldestAsUnresolved() {
        PendingCommandStateService sut = Create(maxEntries: 1);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        string nextMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";

        PendingCommandRegistrationResult result = sut.Register(Registration(
            correlationId: "corr-2",
            messageId: nextMessageId,
            entityKey: "counter-2"));

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        result.EvictedEntry!.MessageId.ShouldBe(MessageId);
        result.EvictedEntry.Status.ShouldBe(PendingCommandStatus.NeedsReview);
        sut.GetByMessageId(MessageId).ShouldBeNull();
        sut.GetByMessageId(nextMessageId).ShouldNotBeNull();
    }

    [Fact]
    public void Dispose_ClearsCircuitLocalState() {
        PendingCommandStateService sut = Create();
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        sut.Dispose();

        sut.Snapshot().ShouldBeEmpty();
    }

    private static PendingCommandStateService Create(
        int maxEntries = 64,
        ILifecycleStateService? lifecycle = null) =>
        new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandEntries = maxEntries }),
            lifecycle ?? Substitute.For<ILifecycleStateService>(),
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<PendingCommandStateService>.Instance);

    private static PendingCommandRegistration Registration(
        string correlationId = "corr-1",
        string messageId = MessageId,
        string entityKey = "counter-1") =>
        new(
            CorrelationId: correlationId,
            MessageId: messageId,
            CommandTypeName: "Counter.Increment",
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            EntityKey: entityKey,
            ExpectedStatusSlot: "Approved",
            PriorStatusSlot: "Draft");
}
