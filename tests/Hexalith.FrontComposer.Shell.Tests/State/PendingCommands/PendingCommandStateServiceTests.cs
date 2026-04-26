using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
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
    public void Registration_ConstructionFailsClosedOnEmptyMessageId() =>
        Should.Throw<ArgumentException>(() => Registration(messageId: ""));

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
        lifecycle.Received(1).Transition("corr-1", CommandLifecycleState.Confirmed, MessageId, false);
        lifecycle.DidNotReceive().Transition("corr-1", CommandLifecycleState.Rejected, MessageId, Arg.Any<bool>());
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
        // P8 — IdempotentConfirmed must surface idempotencyResolved=true so FcLifecycleWrapper
        // renders the "already confirmed" Info bar instead of the Success celebration.
        lifecycle.Received(1).Transition("corr-1", CommandLifecycleState.Confirmed, MessageId, true);
    }

    [Fact]
    public void ResolveTerminal_UnknownMessageId_IsIgnoredWithoutLifecycleMutation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);

        PendingCommandResolutionResult result = sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId));

        result.Status.ShouldBe(PendingCommandResolutionStatus.UnknownMessageId);
        lifecycle.DidNotReceiveWithAnyArgs().Transition(default!, default, default, default);
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
        // P3 — evicted entries are re-inserted as terminal so Snapshot/FcPendingCommandSummary
        // can surface the unresolved tail; the cap applies to pending entries only.
        sut.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.NeedsReview);
        sut.GetByMessageId(nextMessageId)!.Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Fact]
    public void Dispose_ClearsCircuitLocalState() {
        PendingCommandStateService sut = Create();
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        sut.Dispose();

        sut.Snapshot().ShouldBeEmpty();
    }

    // P20 / DN7 — lowercase Crockford ULIDs are accepted and normalized to canonical uppercase.
    [Fact]
    public void Register_AcceptsLowercaseUlid_NormalizesToUppercase() {
        PendingCommandStateService sut = Create();
        string lower = MessageId.ToLowerInvariant();

        PendingCommandRegistrationResult result = sut.Register(Registration(messageId: lower));

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        sut.GetByMessageId(MessageId).ShouldNotBeNull();
        sut.GetByMessageId(lower).ShouldNotBeNull();
    }

    // P20 / P17 — second registration after the entry already reached a terminal outcome surfaces
    // MergedTerminal so generated forms can skip duplicate AcknowledgedAction dispatch.
    [Fact]
    public void Register_AfterTerminalResolution_ReturnsMergedTerminal() {
        PendingCommandStateService sut = Create();
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.Resolved);

        PendingCommandRegistrationResult result = sut.Register(Registration());

        result.Status.ShouldBe(PendingCommandRegistrationStatus.MergedTerminal);
        result.Entry!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    // P20 — out-of-order then duplicate observations: rejected wins after pending, second confirmed is a no-op.
    [Fact]
    public void ResolveTerminal_OutOfOrderAndDuplicate_FirstWinsOnly() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult first = sut.ResolveTerminal(PendingCommandTerminalObservation.Rejected(MessageId, "Save failed", "No data changed."));
        PendingCommandResolutionResult later = sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId));

        first.Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        first.Entry!.Status.ShouldBe(PendingCommandStatus.Rejected);
        later.Status.ShouldBe(PendingCommandResolutionStatus.DuplicateIgnored);
        lifecycle.Received(1).Transition("corr-1", CommandLifecycleState.Rejected, MessageId, false);
        lifecycle.DidNotReceive().Transition("corr-1", CommandLifecycleState.Confirmed, MessageId, Arg.Any<bool>());
    }

    // P20 / P5 — terminal resolution purges the message id from the insertion order so a steady
    // stream of resolved+new registrations does not leak slots beyond MaxPendingCommandEntries.
    [Fact]
    public void ResolveTerminal_KeepsTerminalEntryButFreesPendingSlot() {
        PendingCommandStateService sut = Create(maxEntries: 1);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId));

        // The terminal entry stays visible in Snapshot; the pending slot is free, so the next
        // registration succeeds without evicting the terminal record.
        string nextMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        PendingCommandRegistrationResult result = sut.Register(Registration(
            correlationId: "corr-2",
            messageId: nextMessageId,
            entityKey: "counter-2"));
        result.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        result.EvictedEntry.ShouldBeNull();
        sut.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    // P20 / DN3 — fail-closed scope reset clears outstanding pending entries when the
    // accessor reports a different (tenant, user) snapshot.
    [Fact]
    public void EnforceScopeBoundary_FlushesPendingState_WhenTenantOrUserChanges() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns("tenant-a");
        accessor.UserId.Returns("user-1");
        PendingCommandStateService sut = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            accessor,
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<PendingCommandStateService>.Instance);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        accessor.TenantId.Returns("tenant-b");
        sut.Register(Registration(correlationId: "corr-2", messageId: "01BRZ3NDEKTSV4RRFFQ69G5FAV", entityKey: "counter-2"))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        sut.GetByMessageId(MessageId).ShouldBeNull();
        lifecycle.Received(1).Transition("corr-1", CommandLifecycleState.Rejected, MessageId);
    }

    private static PendingCommandStateService Create(
        int maxEntries = 64,
        ILifecycleStateService? lifecycle = null) =>
        new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandEntries = maxEntries }),
            lifecycle ?? Substitute.For<ILifecycleStateService>(),
            userContext: null,
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
