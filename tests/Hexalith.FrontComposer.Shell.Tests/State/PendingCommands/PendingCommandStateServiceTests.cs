using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class PendingCommandStateServiceTests {
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01CPZ3NDEKTSV4RRFFQ69G5FAV";
    private const string SecondCorrelationId = "01DPZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public void Register_AcceptedCommand_StoresOnlyFrameworkMetadata() {
        PendingCommandStateService sut = Create();

        PendingCommandRegistrationResult result = sut.Register(Registration());

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        PendingCommandEntry entry = sut.GetByMessageId(MessageId).ShouldNotBeNull();
        entry.CorrelationId.ShouldBe(CorrelationId);
        entry.MessageId.ShouldBe(MessageId);
        entry.CommandTypeName.ShouldBe("Counter.Increment");
        entry.EntityKey.ShouldBe("counter-1");
        entry.Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Theory]
    [InlineData("not-a-ulid")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAI")]
    [InlineData("Z1ARZ3NDEKTSV4RRFFQ69G5FAV")]
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

    [Theory]
    [InlineData("not-a-ulid")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAI")]
    [InlineData("Z1CPZ3NDEKTSV4RRFFQ69G5FAV")]
    [InlineData("01ARZ3NDEKTSV4RRFFQ69G5FAVEXTRA")]
    public void Register_MalformedCorrelationId_FailsClosedWithoutStateMutation(string badCorrelationId) {
        PendingCommandStateService sut = Create();

        PendingCommandRegistrationResult result = sut.Register(Registration(correlationId: badCorrelationId));

        result.Status.ShouldBe(PendingCommandRegistrationStatus.InvalidCorrelationId);
        sut.Snapshot().ShouldBeEmpty();
    }

    [Fact]
    public void Registration_ConstructionFailsClosedOnEmptyCorrelationId() =>
        Should.Throw<ArgumentException>(() => Registration(correlationId: ""));

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
    public void Register_DuplicateTargetDifferingOnlyByCapturedAt_Merges() {
        PendingCommandStateService sut = Create();
        DateTimeOffset capturedAt = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
        PendingCommandRegistration first = Registration() with { TargetSnapshot = Target(capturedAt) };
        PendingCommandRegistration duplicate = Registration() with { TargetSnapshot = Target(capturedAt.AddHours(1)) };
        sut.Register(first).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandRegistrationResult result = sut.Register(duplicate);

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Merged);
        result.Entry.ShouldNotBeNull().TargetSnapshot.ShouldBe(first.TargetSnapshot);
    }

    [Theory]
    [InlineData("projection")]
    [InlineData("view")]
    [InlineData("entity")]
    [InlineData("change")]
    [InlineData("prior")]
    [InlineData("expected")]
    [InlineData("tenant")]
    [InlineData("user")]
    public void Register_DuplicateWithAnyMaterialTargetDifferenceConflictsAndRetainsOriginal(string difference) {
        PendingCommandStateService sut = Create();
        DateTimeOffset capturedAt = new(2026, 8, 13, 10, 0, 0, TimeSpan.Zero);
        CommandTargetSnapshot original = Target(capturedAt);
        CommandTargetSnapshot changed = difference switch {
            "projection" => Target(capturedAt, projectionTypeName: "Counter.Other"),
            "view" => Target(capturedAt, viewKey: "other-counts"),
            "entity" => Target(capturedAt, entityKey: "counter-2"),
            "change" => Target(capturedAt, changeKind: CommandTargetChangeKind.Delete),
            "prior" => Target(capturedAt, priorStatus: "Pending"),
            "expected" => Target(capturedAt, expectedStatus: "Published"),
            "tenant" => Target(capturedAt, tenantId: "tenant-2"),
            "user" => Target(capturedAt, userId: "user-2"),
            _ => throw new InvalidOperationException(difference),
        };
        sut.Register(Registration() with { TargetSnapshot = original }).Status
            .ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandRegistrationResult result = sut.Register(Registration() with { TargetSnapshot = changed });

        result.Status.ShouldBe(PendingCommandRegistrationStatus.ConflictingMetadata);
        sut.GetByMessageId(MessageId).ShouldNotBeNull().TargetSnapshot.ShouldBeSameAs(original);
    }

    [Fact]
    public void ResolveTerminal_FirstOutcomeWins_AndTransitionsLifecycleOnce() {
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult first = sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId));
        PendingCommandResolutionResult duplicate = sut.ResolveTerminal(PendingCommandTerminalObservation.Rejected(MessageId, "Failed", "No change was applied."));

        first.Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        first.Entry!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        duplicate.Status.ShouldBe(PendingCommandResolutionStatus.DuplicateIgnored);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, false);
        lifecycle.DidNotReceive().Transition(CorrelationId, CommandLifecycleState.Rejected, MessageId, Arg.Any<bool>());
    }

    [Fact]
    public void ResolveTerminal_IdempotentConfirmed_PreservesAlreadyAppliedOutcome() {
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult result = sut.ResolveTerminal(PendingCommandTerminalObservation.IdempotentConfirmed(MessageId));

        result.Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        result.Entry!.Status.ShouldBe(PendingCommandStatus.IdempotentConfirmed);
        result.Entry.DuplicateTerminalObservations.ShouldBe(0);
        // P8 — IdempotentConfirmed must surface idempotencyResolved=true so FcLifecycleWrapper
        // renders the "already confirmed" Info bar instead of the Success celebration.
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, true);
    }

    [Fact]
    public void ResolveTerminal_NeedsReview_TransitionsLifecycleToRejectedReviewSurface() {
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult result = sut.ResolveTerminal(PendingCommandTerminalObservation.NeedsReview(MessageId));

        result.Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        result.Entry!.Status.ShouldBe(PendingCommandStatus.NeedsReview);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Rejected, MessageId, false);
        lifecycle.DidNotReceive().Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, Arg.Any<bool>());
    }

    [Fact]
    public void ResolveTerminal_LifecycleFailure_DuplicateConvergesFromImmutableStoredTruth() {
        CommandLifecycleState state = CommandLifecycleState.Idle;
        string? lifecycleMessageId = null;
        int attempts = 0;
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(CorrelationId).Returns(_ => state);
        lifecycle.GetMessageId(CorrelationId).Returns(_ => lifecycleMessageId);
        lifecycle
            .When(x => x.Transition(CorrelationId, Arg.Any<CommandLifecycleState>(), Arg.Any<string?>(), Arg.Any<bool>()))
            .Do(call => {
                attempts++;
                if (attempts == 1) {
                    throw new InvalidOperationException("transient lifecycle failure");
                }

                state = call.ArgAt<CommandLifecycleState>(1);
                lifecycleMessageId = call.ArgAt<string?>(2);
            });
        PendingCommandStateService sut = Create(lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult first = sut.ResolveTerminal(
            PendingCommandTerminalObservation.Confirmed(MessageId));
        PendingCommandResolutionResult duplicate = sut.ResolveTerminal(
            PendingCommandTerminalObservation.Rejected(MessageId, "late", "ignored"));

        first.Status.ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        duplicate.Status.ShouldBe(PendingCommandResolutionStatus.DuplicateIgnored);
        duplicate.Entry.ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        duplicate.Entry.DuplicateTerminalObservations.ShouldBe(1);
        state.ShouldBe(CommandLifecycleState.Confirmed);
        lifecycleMessageId.ShouldBe(MessageId);
        attempts.ShouldBe(2);
    }

    [Fact]
    public void TryConvergeLifecycle_OperationCanceledException_PropagatesWithoutRequeue() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns((string?)null);
        lifecycle
            .When(x => x.Transition(Arg.Any<string>(), Arg.Any<CommandLifecycleState>(), Arg.Any<string?>(), Arg.Any<bool>()))
            .Do(_ => throw new InvalidOperationException("lifecycle-failed"));
        CancelAfterRegisterTimeProvider time = new();
        PendingCommandStateService sut = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            userContext: null,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        time.ThrowCanceled = true;

        Should.Throw<OperationCanceledException>(() =>
            sut.ResolveTerminal(PendingCommandTerminalObservation.Rejected(MessageId, "late", "ignored")));
    }

    [Fact]
    public void LifecycleConvergence_CapacityOneEvictsOldestAndRetainsNewestWork() {
        const string secondMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        bool convergeSecond = false;
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
            .Do(call => {
                if (convergeSecond) {
                    values[SecondCorrelationId] = (
                        call.ArgAt<CommandLifecycleState>(1),
                        call.ArgAt<string?>(2));
                }
            });
        PendingCommandStateService sut = Create(maxEntries: 1, lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        sut.Register(Registration(
            correlationId: SecondCorrelationId,
            messageId: secondMessageId,
            entityKey: "counter-2"));
        sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(secondMessageId)).Status
            .ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        convergeSecond = true;

        (int attempts, int converged) = sut.ConvergeLifecycle(10);

        attempts.ShouldBe(1);
        converged.ShouldBe(1);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, false);
        lifecycle.Received(2).Transition(SecondCorrelationId, CommandLifecycleState.Confirmed, secondMessageId, false);
    }

    [Fact]
    public void ResolveTerminal_MaxValueClockSaturatesConvergenceDeadline() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.GetState(Arg.Any<string>()).Returns(CommandLifecycleState.Idle);
        lifecycle.GetMessageId(Arg.Any<string>()).Returns((string?)null);
        FakeTimeProvider time = new(DateTimeOffset.MaxValue);
        PendingCommandStateService sut = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions {
                MaxPendingCommandPollingDurationMs = 120_000,
            }),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult result = sut.ResolveTerminal(
            PendingCommandTerminalObservation.Confirmed(MessageId));
        (int attempts, int converged) = sut.ConvergeLifecycle(1);

        result.Status.ShouldBe(PendingCommandResolutionStatus.LifecycleDispatchFailed);
        attempts.ShouldBe(1);
        converged.ShouldBe(0);
        sut.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public void ResolveTerminal_UnknownMessageId_IsIgnoredWithoutLifecycleMutation() {
        ILifecycleStateService lifecycle = CreateLifecycle();
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
            correlationId: SecondCorrelationId,
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

    [Fact]
    public void Register_AcceptsLowercaseCorrelationId_NormalizesStoredEntryToUppercase() {
        PendingCommandStateService sut = Create();

        PendingCommandRegistrationResult result = sut.Register(Registration(correlationId: CorrelationId.ToLowerInvariant()));

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        result.Entry!.CorrelationId.ShouldBe(CorrelationId);
        sut.GetByMessageId(MessageId)!.CorrelationId.ShouldBe(CorrelationId);
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
        ILifecycleStateService lifecycle = CreateLifecycle();
        PendingCommandStateService sut = Create(lifecycle: lifecycle);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandResolutionResult first = sut.ResolveTerminal(PendingCommandTerminalObservation.Rejected(MessageId, "Save failed", "No data changed."));
        PendingCommandResolutionResult later = sut.ResolveTerminal(PendingCommandTerminalObservation.Confirmed(MessageId));

        first.Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        first.Entry!.Status.ShouldBe(PendingCommandStatus.Rejected);
        later.Status.ShouldBe(PendingCommandResolutionStatus.DuplicateIgnored);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Rejected, MessageId, false);
        lifecycle.DidNotReceive().Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, Arg.Any<bool>());
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
            correlationId: SecondCorrelationId,
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
        sut.Register(Registration(correlationId: SecondCorrelationId, messageId: "01BRZ3NDEKTSV4RRFFQ69G5FAV", entityKey: "counter-2"))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        sut.GetByMessageId(MessageId).ShouldBeNull();
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Rejected, MessageId);
    }

    [Fact]
    public void EnforceScopeBoundary_ConcurrentNewScopeRegistrationDuringTransitionSurvivesAtomicClear() {
        const string nestedMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        const string outerMessageId = "01CRZ3NDEKTSV4RRFFQ69G5FAV";
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns("tenant-a");
        accessor.UserId.Returns("user-1");
        CallbackLogger<PendingCommandStateService> logger = new();
        PendingCommandStateService sut = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            CreateLifecycle(),
            accessor,
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero)),
            logger);
        sut.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        PendingCommandRegistrationResult? nestedResult = null;
        accessor.TenantId.Returns("tenant-b");
        logger.Callback = () => nestedResult = sut.Register(Registration(
            correlationId: SecondCorrelationId,
            messageId: nestedMessageId,
            entityKey: "counter-2"));

        PendingCommandRegistrationResult outerResult = sut.Register(Registration(
            correlationId: "01EPZ3NDEKTSV4RRFFQ69G5FAV",
            messageId: outerMessageId,
            entityKey: "counter-3"));

        nestedResult.ShouldNotBeNull().Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        outerResult.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        sut.GetByMessageId(MessageId).ShouldBeNull();
        sut.GetByMessageId(nestedMessageId).ShouldNotBeNull();
        sut.GetByMessageId(outerMessageId).ShouldNotBeNull();
    }

    private static PendingCommandStateService Create(
        int maxEntries = 64,
        ILifecycleStateService? lifecycle = null) =>
        new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandEntries = maxEntries }),
            lifecycle ?? CreateLifecycle(),
            userContext: null,
            new FakeTimeProvider(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<PendingCommandStateService>.Instance);

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

    private static PendingCommandRegistration Registration(
        string correlationId = CorrelationId,
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

    private static CommandTargetSnapshot Target(
        DateTimeOffset capturedAt,
        string projectionTypeName = "Counter.Count",
        string viewKey = "counter-counts",
        string entityKey = "counter-1",
        CommandTargetChangeKind changeKind = CommandTargetChangeKind.Update,
        string? priorStatus = "Draft",
        string? expectedStatus = "Approved",
        string tenantId = "tenant-1",
        string userId = "user-1") =>
        new(
            projectionTypeName,
            viewKey,
            entityKey,
            changeKind,
            priorStatus,
            expectedStatus,
            tenantId,
            userId,
            capturedAt);

    private sealed class CancelAfterRegisterTimeProvider : TimeProvider {
        public bool ThrowCanceled { get; set; }

        public override DateTimeOffset GetUtcNow() {
            if (ThrowCanceled) {
                throw new OperationCanceledException();
            }

            return new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);
        }
    }

    private sealed class CallbackLogger<T> : ILogger<T> {
        private Action? _callback;

        public Action? Callback {
            get => Volatile.Read(ref _callback);
            set => Volatile.Write(ref _callback, value);
        }

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Interlocked.Exchange(ref _callback, null)?.Invoke();
    }
}
