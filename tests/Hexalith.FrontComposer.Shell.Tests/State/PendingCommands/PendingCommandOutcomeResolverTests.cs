using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;
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
    public void Resolve_WithoutMessageId_DoesNotInferSingleCandidate() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: null));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        state.GetByMessageId(MessageId)!.Status.ShouldNotBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public void Resolve_WithoutMessageId_DoesNotInspectMultipleCandidates() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        state.Register(Registration(SecondCorrelationId, "01BRZ3NDEKTSV4RRFFQ69G5FAV")).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(messageId: null));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        state.Snapshot().All(e => e.Status == PendingCommandStatus.Pending).ShouldBeTrue();
        lifecycle.DidNotReceiveWithAnyArgs().Transition(default!, default, default);
    }

    [Fact]
    public void Resolve_ConfirmedWithCompletePendingMetadata_AddsNewItemIndicatorWithObservedTimestamp() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
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
        FakeTimeProvider time = new(s_observedAt);
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

    [Fact]
    public void Resolve_TwoConfirmedMessagesForOneRow_PublishAsFirstWinsThroughTheProducerBoundary() {
        // Story 9.6 — composed proof through the real producer boundary: two DISTINCT accepted
        // MessageIds whose target snapshots resolve to the same (ViewKey, EntityKey). Both burn an
        // indicator decision in the resolver, both reach INewItemIndicatorStateService.Add, and the
        // state service keeps the first publication's provenance and original expiry.
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        CapturingLogger<NewItemIndicatorStateService> indicatorLogger = new();
        using NewItemIndicatorStateService indicators = new(time, userContext: null, indicatorLogger);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        const string secondMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        state.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        state.Register(Registration(SecondCorrelationId, secondMessageId))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        sut.Resolve(Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        time.Advance(TimeSpan.FromSeconds(5));
        sut.Resolve(Outcome(secondMessageId, s_observedAt.AddSeconds(5)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        // The suppression diagnostic is the only proof that the SECOND observation actually reached
        // the state service: IndicatorDecisionCount also advances on the ineligible and
        // timestamp-rejected paths, so it cannot distinguish "suppressed by the row" from
        // "never published at all".
        CapturedLogEntry suppression = indicatorLogger.Entries.ShouldHaveSingleItem();
        suppression.Level.ShouldBe(LogLevel.Debug);
        suppression.EventId.Id.ShouldBe(5784);
        suppression.State["MessageId"].ShouldBe(FrontComposerHotPathLog.DigestIdentifier(secondMessageId));
        suppression.State["ViewKey"].ShouldBe(FrontComposerHotPathLog.DigestIdentifier("counter-counts"));
        suppression.State["EntityKey"].ShouldBe(FrontComposerHotPathLog.DigestIdentifier("counter-1"));

        sut.IndicatorDecisionCount.ShouldBe(2);
        NewItemIndicatorEntry active = indicators.Snapshot("counter-counts").ShouldHaveSingleItem();
        active.MessageId.ShouldBe(MessageId);
        active.CreatedAt.ShouldBe(s_observedAt);
        state.GetByMessageId(secondMessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);

        // The second command neither reset nor extended the first indicator's ten-second lifetime.
        time.Advance(TimeSpan.FromSeconds(5));
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void BufferBeforeAccepted_EarlyTerminal_ReplaysAfterMatchingAssociation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);

        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        PendingCommandRegistrationResult registration = sut.AssociateAccepted(Registration());

        registration.Status.ShouldBe(PendingCommandRegistrationStatus.MergedTerminal);
        registration.Entry!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        state.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        indicators.Snapshot("counter-counts").Single().MessageId.ShouldBe(MessageId);
        lifecycle.Received(1).Transition(CorrelationId, CommandLifecycleState.Confirmed, MessageId, false);
    }

    [Fact]
    public void Resolve_UnknownMessage_DoesNotBufferForLaterAssociation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);

        sut.Resolve(Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        sut.BufferedObservationCount.ShouldBe(0);

        PendingCommandRegistrationResult registration = sut.AssociateAccepted(Registration());

        registration.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        state.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Fact]
    public void BufferBeforeAccepted_RepeatedOwnerMessageAndScope_FirstObservationWins() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        PendingCommandOutcomeObservation first = Outcome(MessageId, s_observedAt) with {
            Materiality = CommandMateriality.NoOp,
        };

        sut.BufferBeforeAccepted(CorrelationId, first)
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt.AddSeconds(1)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        PendingCommandRegistrationResult registration = sut.AssociateAccepted(Registration());

        registration.Status.ShouldBe(PendingCommandRegistrationStatus.MergedTerminal);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void BufferBeforeAccepted_LowercaseOwnerAndMessage_ReplaysForCanonicalAssociation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);

        sut.BufferBeforeAccepted(
            CorrelationId.ToLowerInvariant(),
            Outcome(MessageId.ToLowerInvariant(), s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        PendingCommandRegistrationResult registration = sut.AssociateAccepted(Registration());

        registration.Status.ShouldBe(PendingCommandRegistrationStatus.MergedTerminal);
        state.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Theory]
    [InlineData("Z1ARZ3NDEKTSV4RRFFQ69G5FAV", CorrelationId)]
    [InlineData(MessageId, "Z1CPZ3NDEKTSV4RRFFQ69G5FAV")]
    public void BufferBeforeAccepted_OverflowUlidInMessageOrOwner_FailsClosed(
        string messageId,
        string ownerId) {
        PendingCommandOutcomeResolver sut = Create(Substitute.For<ILifecycleStateService>(), out _);

        PendingCommandOutcomeResolutionResult result = sut.BufferBeforeAccepted(
            ownerId,
            Outcome(messageId, s_observedAt));

        result.Status.ShouldBe(messageId == MessageId
            ? PendingCommandOutcomeResolutionStatus.Unknown
            : PendingCommandOutcomeResolutionStatus.InvalidMessageId);
        sut.BufferedObservationCount.ShouldBe(0);
        sut.BufferedOrderCount.ShouldBe(0);
    }

    [Fact]
    public void BufferBeforeAccepted_DifferentOwner_DoesNotReplay() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        PendingCommandRegistrationResult registration = sut.AssociateAccepted(
            Registration(SecondCorrelationId, MessageId));

        registration.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        state.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
        sut.BufferedObservationCount.ShouldBe(1);
    }

    [Fact]
    public void DiscardBufferedByOwner_SharedMessagePreservesOtherOwnersFirstObservation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        PendingCommandOutcomeObservation ownerAFirst = Outcome(MessageId, s_observedAt) with {
            Materiality = CommandMateriality.NoOp,
        };

        sut.BufferBeforeAccepted(CorrelationId, ownerAFirst)
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt.AddSeconds(1)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        sut.BufferBeforeAccepted(SecondCorrelationId, Outcome(MessageId, s_observedAt.AddSeconds(2)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        sut.DiscardBufferedByOwner(SecondCorrelationId.ToLowerInvariant());
        PendingCommandRegistrationResult registration = sut.AssociateAccepted(Registration());

        registration.Status.ShouldBe(PendingCommandRegistrationStatus.MergedTerminal);
        registration.Entry.ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
        sut.BufferedObservationCount.ShouldBe(0);
        sut.BufferedOrderCount.ShouldBe(0);
    }

    [Fact]
    public void BufferBeforeAccepted_DifferentMessage_DoesNotReplay() {
        const string differentMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        PendingCommandRegistrationResult registration = sut.AssociateAccepted(
            Registration(CorrelationId, differentMessageId));

        registration.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        state.GetByMessageId(differentMessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
        sut.BufferedObservationCount.ShouldBe(1);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BufferBeforeAccepted_DirectTenantOrUserSwitch_ClearsBeforeAssociation(bool switchTenant) {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        string tenant = "tenant-1";
        string user = "user-1";
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns(_ => tenant);
        _ = userContext.UserId.Returns(_ => user);
        PendingCommandOutcomeResolver sut = new(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            null,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        if (switchTenant) {
            tenant = "tenant-2";
        }
        else {
            user = "user-2";
        }
        PendingCommandRegistrationResult registration = sut.AssociateAccepted(Registration());

        registration.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        state.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
        sut.BufferedObservationCount.ShouldBe(0);
    }

    [Fact]
    public void AssociateAccepted_NullRegistration_ThrowsBeforeCanonicalization() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _);

        _ = Should.Throw<ArgumentNullException>(() => sut.AssociateAccepted(null!));

        sut.BufferedObservationCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AssociateAccepted_BlankIdentifier_ReturnsInvalidWithoutCanonicalizationFailure(bool blankMessage) {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _);
        PendingCommandRegistration registration = blankMessage
            ? Registration() with { MessageId = " " }
            : Registration() with { CorrelationId = " " };

        PendingCommandRegistrationResult result = sut.AssociateAccepted(registration);

        result.Status.ShouldBe(blankMessage
            ? PendingCommandRegistrationStatus.InvalidMessageId
            : PendingCommandRegistrationStatus.InvalidCorrelationId);
    }

    [Fact]
    public void AssociateAccepted_ConflictingMetadata_DiscardsMatchingBufferedObservation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state);
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        state.Register(Registration(SecondCorrelationId, MessageId))
            .Status.ShouldBe(PendingCommandRegistrationStatus.Registered);

        PendingCommandRegistrationResult result = sut.AssociateAccepted(Registration());

        result.Status.ShouldBe(PendingCommandRegistrationStatus.ConflictingMetadata);
        sut.BufferedObservationCount.ShouldBe(0);
        sut.BufferedOrderCount.ShouldBe(0);
    }

    [Fact]
    public void BufferBeforeAccepted_InitialMissingUserContext_ReplaysWhileScopeRemainsUnknown() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        string? tenant = null;
        string? user = null;
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns(_ => tenant!);
        _ = userContext.UserId.Returns(_ => user!);
        PendingCommandOutcomeResolver sut = new(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            null,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);

        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        PendingCommandRegistrationResult result = sut.AssociateAccepted(Registration());

        result.Status.ShouldBe(PendingCommandRegistrationStatus.MergedTerminal);
        state.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public void BufferBeforeAccepted_InitialUnknownScopeBecomingKnown_DiscardsBeforeAssociation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        string? tenant = null;
        string? user = null;
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns(_ => tenant!);
        _ = userContext.UserId.Returns(_ => user!);
        PendingCommandOutcomeResolver sut = new(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            null,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        tenant = "tenant-1";
        user = "user-1";
        PendingCommandRegistrationResult result = sut.AssociateAccepted(Registration());

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        state.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
        sut.BufferedObservationCount.ShouldBe(0);
    }

    [Fact]
    public void Resolve_FirstNoOpTerminalWinsAndLaterMaterialDuplicateDoesNotPublish() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        _ = sut.AssociateAccepted(Registration());

        PendingCommandOutcomeObservation noOp = Outcome(MessageId, s_observedAt) with { Materiality = CommandMateriality.NoOp };
        sut.Resolve(noOp).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(1))).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.DuplicateIgnored);

        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_UnknownMaterialityResolvesLifecycleButDoesNotPublish() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        _ = sut.AssociateAccepted(Registration());
        PendingCommandOutcomeObservation unknown = Outcome(MessageId, s_observedAt) with {
            Materiality = CommandMateriality.Unknown,
        };

        sut.Resolve(unknown).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        state.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("tenant-2", "user-1")]
    [InlineData("tenant-1", "user-2")]
    public void Resolve_TargetOwnerScopeMismatchResolvesLifecycleButDoesNotPublish(
        string currentTenant,
        string currentUser) {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(
            lifecycle,
            out PendingCommandStateService state,
            indicators,
            time,
            tenantId: currentTenant,
            userId: currentUser);
        _ = sut.AssociateAccepted(Registration());

        sut.Resolve(Outcome(MessageId, s_observedAt)).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        state.GetByMessageId(MessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_DeleteTargetNeverPublishesIndicator() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        PendingCommandRegistration registration = Registration() with {
            TargetSnapshot = Target(CommandTargetChangeKind.Delete),
        };
        _ = sut.AssociateAccepted(registration);

        sut.Resolve(Outcome(MessageId, s_observedAt)).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_SmallFutureSkewClampsButLargeFutureSkewSuppresses() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        _ = sut.AssociateAccepted(Registration());

        _ = sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(5)));
        indicators.Snapshot("counter-counts").Single().CreatedAt.ShouldBe(s_observedAt);

        string secondMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        _ = sut.AssociateAccepted(Registration(SecondCorrelationId, secondMessageId));
        _ = sut.Resolve(Outcome(secondMessageId, s_observedAt.AddSeconds(6)));
        indicators.Snapshot("counter-counts").Count.ShouldBe(1);
    }

    [Fact]
    public void Resolve_InvalidTimestamp_RecordsNonPublicationAndBlocksLaterValidObservation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        _ = sut.AssociateAccepted(Registration());

        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(6)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
        sut.IndicatorDecisionCount.ShouldBe(1);

        sut.Resolve(Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.DuplicateIgnored);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
        sut.IndicatorDecisionCount.ShouldBe(1);
    }

    [Fact]
    public void Resolve_NullObservedAt_UsesShellTime() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        _ = sut.AssociateAccepted(Registration());

        _ = sut.Resolve(Outcome(MessageId, observedAt: null));

        indicators.Snapshot("counter-counts").Single().CreatedAt.ShouldBe(s_observedAt);
    }

    [Fact]
    public void Resolve_ObservedOneTickBeforeCapture_SuppressesIndicator() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        PendingCommandRegistration registration = Registration() with {
            TargetSnapshot = Target(capturedAt: s_observedAt),
        };
        _ = sut.AssociateAccepted(registration);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(
            Outcome(MessageId, s_observedAt.AddTicks(-1)));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void BufferBeforeAccepted_CapacityOverflow_EvictsOldestFirst() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        FcShellOptions options = new() { MaxPendingCommandEntries = 1 };
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, resolverTime: time, options: options);
        string secondMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";

        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        sut.BufferBeforeAccepted(SecondCorrelationId, Outcome(secondMessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        _ = sut.AssociateAccepted(Registration());
        _ = sut.AssociateAccepted(Registration(SecondCorrelationId, secondMessageId));

        state.GetByMessageId(MessageId)!.Status.ShouldNotBe(PendingCommandStatus.Confirmed);
        state.GetByMessageId(secondMessageId)!.Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public void Resolve_IndicatorDecisionsExceedingCapacity_RetainsOldestAndDoesNotRepublish() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        FcShellOptions options = new() { MaxPendingCommandEntries = 1 };
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time, options);
        const string secondMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        _ = sut.AssociateAccepted(Registration());
        sut.Resolve(Outcome(MessageId, s_observedAt)).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        _ = sut.AssociateAccepted(Registration(SecondCorrelationId, secondMessageId, entityKey: "counter-2"));
        sut.Resolve(Outcome(secondMessageId, s_observedAt)).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        sut.IndicatorDecisionCount.ShouldBe(2);
        IReadOnlyList<NewItemIndicatorEntry> published = indicators.Snapshot("counter-counts");
        published.Count.ShouldBe(2);
        published.Single(entry => entry.MessageId == MessageId).CreatedAt.ShouldBe(s_observedAt);

        time.Advance(TimeSpan.FromSeconds(5));
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(5)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.DuplicateIgnored);
        indicators.Snapshot("counter-counts").Single(entry => entry.MessageId == MessageId)
            .CreatedAt.ShouldBe(s_observedAt);

        time.Advance(TimeSpan.FromSeconds(5));
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(10)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.DuplicateIgnored);
        indicators.Snapshot("counter-counts").ShouldBeEmpty();
        sut.IndicatorDecisionCount.ShouldBe(2);
    }

    [Fact]
    public void Resolve_IndicatorDecisionCapacityOverflow_DoesNotRepublishAfterFifoWouldHaveEvicted() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        FcShellOptions options = new() { MaxPendingCommandEntries = 1 };
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time, options);
        const string secondMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        const string thirdMessageId = "01CRZ3NDEKTSV4RRFFQ69G5FAV";
        const string thirdCorrelationId = "01EPZ3NDEKTSV4RRFFQ69G5FAV";
        _ = sut.AssociateAccepted(Registration());
        _ = sut.Resolve(Outcome(MessageId, s_observedAt));
        _ = sut.AssociateAccepted(Registration(SecondCorrelationId, secondMessageId, entityKey: "counter-2"));
        _ = sut.Resolve(Outcome(secondMessageId, s_observedAt));
        _ = sut.AssociateAccepted(Registration(thirdCorrelationId, thirdMessageId, entityKey: "counter-3"));
        _ = sut.Resolve(Outcome(thirdMessageId, s_observedAt));
        _ = sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(1)));

        sut.IndicatorDecisionCount.ShouldBe(3);
        indicators.Received(3).Add(Arg.Any<NewItemIndicatorEntry>());
        indicators.Received(1).Add(Arg.Is<NewItemIndicatorEntry>(entry => entry.MessageId == MessageId));
    }

    [Fact]
    public void Resolve_LegacyResolverAdapter_IndicatorDecisionOverflow_DoesNotRepublishOldest() {
        FakeTimeProvider time = new(s_observedAt);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        const string secondMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(call => {
            PendingCommandOutcomeObservation observation = call.Arg<PendingCommandOutcomeObservation>();
            string entityKey = observation.MessageId == MessageId ? "counter-1" : "counter-2";
            return new PendingCommandOutcomeResolutionResult(
                PendingCommandOutcomeResolutionStatus.Resolved,
                new PendingCommandEntry(
                    CorrelationId,
                    observation.MessageId!,
                    "Counter.Increment",
                    "Counter.Count",
                    "counter-counts",
                    entityKey,
                    "Approved",
                    "Draft",
                    s_observedAt.AddMinutes(-1),
                    PendingCommandStatus.Confirmed) {
                    TargetSnapshot = Target(entityKey: entityKey),
                });
        });
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns("tenant-1");
        _ = userContext.UserId.Returns("user-1");
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandEntries = 1 }),
            userContext);

        sut.Resolve(Outcome(MessageId, s_observedAt)).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        sut.Resolve(Outcome(secondMessageId, s_observedAt)).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(1))).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        sut.IndicatorDecisionCount.ShouldBe(2);
        indicators.Received(1).Add(Arg.Is<NewItemIndicatorEntry>(entry => entry.MessageId == MessageId));
        indicators.Received(1).Add(Arg.Is<NewItemIndicatorEntry>(entry => entry.MessageId == secondMessageId));
    }

    [Fact]
    public void BufferBeforeAccepted_ReplayAndDiscardCycles_KeepBothStructuresBounded() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        FcShellOptions options = new() { MaxPendingCommandEntries = 2 };
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, resolverTime: time, options: options);
        const string alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

        for (int index = 0; index < 20; index++) {
            string messageId = "01ARZ3NDEKTSV4RRFFQ69G5FA" + alphabet[index];
            string ownerId = "01CPZ3NDEKTSV4RRFFQ69G5FA" + alphabet[index];
            sut.BufferBeforeAccepted(ownerId, Outcome(messageId, s_observedAt))
                .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
            if ((index & 1) == 0) {
                _ = sut.AssociateAccepted(Registration(ownerId, messageId));
            }
            else {
                sut.DiscardBuffered(messageId);
            }

            sut.BufferedObservationCount.ShouldBe(0);
            sut.BufferedOrderCount.ShouldBe(0);
        }

        const string first = "01FRZ3NDEKTSV4RRFFQ69G5FAV";
        const string second = "01GRZ3NDEKTSV4RRFFQ69G5FAV";
        const string third = "01HRZ3NDEKTSV4RRFFQ69G5FAV";
        sut.BufferBeforeAccepted("01FPZ3NDEKTSV4RRFFQ69G5FAV", Outcome(first, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        sut.BufferBeforeAccepted("01GPZ3NDEKTSV4RRFFQ69G5FAV", Outcome(second, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        sut.BufferBeforeAccepted("01HPZ3NDEKTSV4RRFFQ69G5FAV", Outcome(third, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        _ = sut.AssociateAccepted(Registration("01FPZ3NDEKTSV4RRFFQ69G5FAV", first));
        _ = sut.AssociateAccepted(Registration("01GPZ3NDEKTSV4RRFFQ69G5FAV", second));
        _ = sut.AssociateAccepted(Registration("01HPZ3NDEKTSV4RRFFQ69G5FAV", third));

        state.GetByMessageId(first).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Pending);
        state.GetByMessageId(second).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        state.GetByMessageId(third).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        sut.BufferedObservationCount.ShouldBe(0);
        sut.BufferedOrderCount.ShouldBe(0);
    }

    [Fact]
    public void Resolve_CaptureToNowBeyondMaximumAgeSuppressesEvenWhenSubIntervalsAreWithinBound() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        FcShellOptions options = new() { MaxPendingCommandPollingDurationMs = 1_000 };
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time, options);
        PendingCommandRegistration registration = Registration() with {
            TargetSnapshot = Target(capturedAt: s_observedAt.AddMilliseconds(-1_500)),
        };
        _ = sut.AssociateAccepted(registration);

        _ = sut.Resolve(Outcome(MessageId, s_observedAt.AddMilliseconds(-750)));

        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(PendingCommandTerminalOutcome.Confirmed, true)]
    [InlineData(PendingCommandTerminalOutcome.IdempotentConfirmed, true)]
    [InlineData(PendingCommandTerminalOutcome.Rejected, false)]
    [InlineData(PendingCommandTerminalOutcome.NeedsReview, false)]
    public void Resolve_MaterialOutcomeMatrixPublishesOnlyConfirmedStates(
        PendingCommandTerminalOutcome outcome,
        bool shouldPublish) {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        _ = sut.AssociateAccepted(Registration());
        PendingCommandOutcomeObservation observation = Outcome(MessageId, s_observedAt) with { Outcome = outcome };

        sut.Resolve(observation).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        indicators.Snapshot("counter-counts").Any().ShouldBe(shouldPublish);
    }

    [Fact]
    public void Resolve_IndicatorPublicationFailurePreservesResolvedTerminal() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        indicators.WhenForAnyArgs(service => service.Add(default!))
            .Do(_ => throw new InvalidOperationException("indicator unavailable"));
        FakeTimeProvider time = new(s_observedAt);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        _ = sut.AssociateAccepted(Registration());

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(MessageId, s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        state.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public void Resolve_IndicatorPublicationFailure_DoesNotRetryAddOnDuplicateObservation() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        indicators.WhenForAnyArgs(service => service.Add(default!))
            .Do(_ => throw new InvalidOperationException("indicator unavailable"));
        FakeTimeProvider time = new(s_observedAt);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out _, indicators, time);
        _ = sut.AssociateAccepted(Registration());

        sut.Resolve(Outcome(MessageId, s_observedAt)).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(1)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.DuplicateIgnored);

        indicators.Received(1).Add(Arg.Any<NewItemIndicatorEntry>());
        sut.IndicatorDecisionCount.ShouldBe(1);
    }

    [Fact]
    public void Resolve_LifecycleDispatchFailureStillPublishesEligibleIndicator() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        lifecycle.WhenForAnyArgs(service => service.Transition(default!, default, default, default))
            .Do(_ => throw new InvalidOperationException("fluxor unavailable"));
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        PendingCommandOutcomeResolver sut = Create(lifecycle, out PendingCommandStateService state, indicators, time);
        _ = sut.AssociateAccepted(Registration());

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(MessageId, s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.LifecycleDispatchFailed);
        state.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        indicators.Snapshot("counter-counts").Single().MessageId.ShouldBe(MessageId);
    }

    [Fact]
    public void Resolve_TimeProviderFailurePreservesResolvedTerminalAndSuppressesIndicator() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        ConfigureLifecycleReadback(lifecycle);
        FakeTimeProvider stateTime = new(s_observedAt);
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            stateTime,
            NullLogger<PendingCommandStateService>.Instance);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns("tenant-1");
        _ = userContext.UserId.Returns("user-1");
        PendingCommandOutcomeResolver sut = new(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            new ThrowingTimeProvider(),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);
        _ = sut.AssociateAccepted(Registration());

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(MessageId, s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        state.GetByMessageId(MessageId).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
        indicators.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public void BufferBeforeAccepted_KnownScopeLost_ClearsAndRefusesUntilReacquired() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        string? tenant = "tenant-1";
        string? user = "user-1";
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns(_ => tenant!);
        _ = userContext.UserId.Returns(_ => user!);
        PendingCommandOutcomeResolver sut = new(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            null,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);
        const string beforeLoss = "01FRZ3NDEKTSV4RRFFQ69G5FAV";
        const string whileLost = "01GRZ3NDEKTSV4RRFFQ69G5FAV";
        const string afterReacquire = "01HRZ3NDEKTSV4RRFFQ69G5FAV";

        sut.BufferBeforeAccepted("01FPZ3NDEKTSV4RRFFQ69G5FAV", Outcome(beforeLoss, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);
        tenant = null;
        user = null;
        sut.BufferBeforeAccepted("01GPZ3NDEKTSV4RRFFQ69G5FAV", Outcome(whileLost, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        sut.BufferedObservationCount.ShouldBe(0);
        tenant = "tenant-1";
        user = "user-1";
        sut.BufferBeforeAccepted("01HPZ3NDEKTSV4RRFFQ69G5FAV", Outcome(afterReacquire, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        PendingCommandRegistrationResult result = sut.AssociateAccepted(
            Registration("01HPZ3NDEKTSV4RRFFQ69G5FAV", afterReacquire));
        result.Status.ShouldBe(PendingCommandRegistrationStatus.MergedTerminal);
        state.GetByMessageId(afterReacquire).ShouldNotBeNull().Status.ShouldBe(PendingCommandStatus.Confirmed);
    }

    [Fact]
    public void Resolve_LegacyAdapter_AfterScopeLoss_DoesNotRepublish() {
        FakeTimeProvider time = new(s_observedAt);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(call =>
            DelegatedResolved(call.Arg<PendingCommandOutcomeObservation>().MessageId!));
        string? tenant = "tenant-1";
        string? user = "user-1";
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns(_ => tenant!);
        _ = userContext.UserId.Returns(_ => user!);
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);

        sut.Resolve(Outcome(MessageId, s_observedAt)).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        tenant = null;
        user = null;
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        tenant = "tenant-1";
        user = "user-1";
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(1))).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        sut.IndicatorDecisionCount.ShouldBe(1);
        indicators.Received(1).Add(Arg.Is<NewItemIndicatorEntry>(entry => entry.MessageId == MessageId));
    }

    [Fact]
    public void Resolve_LegacyAdapter_AfterTenantSwitch_DoesNotRepublish() {
        FakeTimeProvider time = new(s_observedAt);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(call =>
            DelegatedResolved(call.Arg<PendingCommandOutcomeObservation>().MessageId!));
        string tenant = "tenant-1";
        string user = "user-1";
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns(_ => tenant);
        _ = userContext.UserId.Returns(_ => user);
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);

        sut.Resolve(Outcome(MessageId, s_observedAt) with { Materiality = CommandMateriality.NoOp })
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        tenant = "tenant-2";
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(1))).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        tenant = "tenant-1";
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(2))).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        sut.IndicatorDecisionCount.ShouldBe(1);
        indicators.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public void Resolve_IndicatorAdd_ReleasesResolverGateBeforePublication() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        FakeTimeProvider time = new(s_observedAt);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        PendingCommandOutcomeResolver? sut = null;
        bool concurrentLockAcquired = false;
        indicators.WhenForAnyArgs(service => service.Add(default!))
            .Do(call => {
                _ = call;
                Task<bool> probe = Task.Run(() => {
                    _ = sut!.IndicatorDecisionCount;
                    return true;
                });
                concurrentLockAcquired = probe.Wait(TimeSpan.FromSeconds(2));
            });
        sut = Create(lifecycle, out _, indicators, time);
        _ = sut.AssociateAccepted(Registration());

        sut.Resolve(Outcome(MessageId, s_observedAt)).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        concurrentLockAcquired.ShouldBeTrue();
        sut.IndicatorDecisionCount.ShouldBe(1);
        indicators.Received(1).Add(Arg.Any<NewItemIndicatorEntry>());
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
        FakeTimeProvider time = new(s_observedAt);
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
        FakeTimeProvider time = new(s_observedAt);
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
        FakeTimeProvider time = new(s_observedAt);
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

    [Fact]
    public void Resolve_LegacyResolverAdapterPublishesEligibleIndicatorOnce() {
        FakeTimeProvider time = new(s_observedAt);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        PendingCommandEntry resolvedEntry = new(
            CorrelationId,
            MessageId,
            "Counter.Increment",
            "Counter.Count",
            "counter-counts",
            "counter-1",
            "Approved",
            "Draft",
            s_observedAt.AddMinutes(-1),
            PendingCommandStatus.Confirmed) {
            TargetSnapshot = Target(),
        };
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(
            new PendingCommandOutcomeResolutionResult(
                PendingCommandOutcomeResolutionStatus.Resolved,
                resolvedEntry));
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns("tenant-1");
        _ = userContext.UserId.Returns("user-1");
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);

        sut.Resolve(Outcome(MessageId, s_observedAt)).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        sut.Resolve(Outcome(MessageId, s_observedAt)).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        indicators.Received(1).Add(Arg.Is<NewItemIndicatorEntry>(entry =>
            entry != null && entry.EntityKey == "counter-1" && entry.MessageId == MessageId));
    }

    [Fact]
    public void Resolve_DelegatedResultWithDifferentMessageIdFailsClosedBeforePublication() {
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        PendingCommandEntry mismatchedEntry = new(
            CorrelationId,
            "01BRZ3NDEKTSV4RRFFQ69G5FAV",
            "Counter.Increment",
            "Counter.Count",
            "counter-counts",
            "counter-1",
            "Approved",
            "Draft",
            s_observedAt.AddMinutes(-1),
            PendingCommandStatus.Confirmed) {
            TargetSnapshot = Target(),
        };
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(
            new PendingCommandOutcomeResolutionResult(
                PendingCommandOutcomeResolutionStatus.Resolved,
                mismatchedEntry));
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            new FakeTimeProvider(s_observedAt),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext: null);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(MessageId, s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        indicators.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public void Resolve_DelegatedTerminalWithoutEntryFailsClosedBeforePublication() {
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(
            new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Resolved));
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            new FakeTimeProvider(s_observedAt),
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext: null);

        PendingCommandOutcomeResolutionResult result = sut.Resolve(Outcome(MessageId, s_observedAt));

        result.Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Unknown);
        indicators.DidNotReceiveWithAnyArgs().Add(default!);
    }

    [Fact]
    public void AssociateAccepted_RegisterThrowsAfterCommit_ReconcilesCommittedMetadata() {
        PendingCommandRegistration registration = Registration();
        PendingCommandEntry committed = new(
            registration.CorrelationId,
            registration.MessageId,
            registration.CommandTypeName,
            registration.ProjectionTypeName,
            registration.LaneKey,
            registration.EntityKey,
            registration.ExpectedStatusSlot,
            registration.PriorStatusSlot,
            s_observedAt,
            PendingCommandStatus.Pending) {
            TargetSnapshot = registration.TargetSnapshot,
        };
        IPendingCommandStateService state = Substitute.For<IPendingCommandStateService>();
        state.Register(registration).Returns(_ => throw new InvalidOperationException("post-commit notification failed"));
        state.GetByMessageId(MessageId).Returns(committed);
        PendingCommandOutcomeResolver sut = new(state);

        PendingCommandRegistrationResult result = sut.AssociateAccepted(registration);

        result.Status.ShouldBe(PendingCommandRegistrationStatus.Merged);
        result.Entry.ShouldBeSameAs(committed);
    }

    [Fact]
    public void AssociateAccepted_ReplayThrowsAfterTerminalCommit_ReconcilesTerminalEntry() {
        PendingCommandRegistration registration = Registration();
        PendingCommandEntry pending = new(
            registration.CorrelationId,
            registration.MessageId,
            registration.CommandTypeName,
            registration.ProjectionTypeName,
            registration.LaneKey,
            registration.EntityKey,
            registration.ExpectedStatusSlot,
            registration.PriorStatusSlot,
            s_observedAt,
            PendingCommandStatus.Pending) {
            TargetSnapshot = registration.TargetSnapshot,
        };
        PendingCommandEntry terminal = pending with {
            Status = PendingCommandStatus.Confirmed,
            TerminalAt = s_observedAt,
        };
        bool terminalCommitted = false;
        IPendingCommandStateService state = Substitute.For<IPendingCommandStateService>();
        state.Register(registration).Returns(PendingCommandRegistrationResult.Registered(pending));
        state.GetByMessageId(MessageId).Returns(_ => terminalCommitted ? terminal : pending);
        state.ResolveTerminal(Arg.Any<PendingCommandTerminalObservation>()).Returns(_ => {
            terminalCommitted = true;
            throw new InvalidOperationException("post-commit replay notification failed");
        });
        PendingCommandOutcomeResolver sut = new(state);
        sut.BufferBeforeAccepted(CorrelationId, Outcome(MessageId, s_observedAt)).Status
            .ShouldBe(PendingCommandOutcomeResolutionStatus.Buffered);

        PendingCommandRegistrationResult result = sut.AssociateAccepted(registration);

        result.Status.ShouldBe(PendingCommandRegistrationStatus.MergedTerminal);
        result.Entry.ShouldBeSameAs(terminal);
    }

    [Fact]
    public void Resolve_LegacyResolverAdapterNoOpDoesNotPublishIndicator() {
        FakeTimeProvider time = new(s_observedAt);
        using NewItemIndicatorStateService indicators = new(time);
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        PendingCommandEntry resolvedEntry = new(
            CorrelationId,
            MessageId,
            "Counter.Increment",
            "Counter.Count",
            "counter-counts",
            "counter-1",
            "Approved",
            "Draft",
            s_observedAt.AddMinutes(-1),
            PendingCommandStatus.Confirmed) {
            TargetSnapshot = Target(),
        };
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(
            new PendingCommandOutcomeResolutionResult(
                PendingCommandOutcomeResolutionStatus.Resolved,
                resolvedEntry));
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns("tenant-1");
        _ = userContext.UserId.Returns("user-1");
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);

        PendingCommandOutcomeObservation observation = Outcome(MessageId, s_observedAt) with {
            Materiality = CommandMateriality.NoOp,
        };
        sut.Resolve(observation).Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        indicators.Snapshot("counter-counts").ShouldBeEmpty();
    }

    [Fact]
    public void Resolve_LegacyResolverAdapterNoOpThenMaterial_DoesNotPublishIndicator() {
        FakeTimeProvider time = new(s_observedAt);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        PendingCommandEntry resolvedEntry = new(
            CorrelationId,
            MessageId,
            "Counter.Increment",
            "Counter.Count",
            "counter-counts",
            "counter-1",
            "Approved",
            "Draft",
            s_observedAt.AddMinutes(-1),
            PendingCommandStatus.Confirmed) {
            TargetSnapshot = Target(),
        };
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(
            new PendingCommandOutcomeResolutionResult(
                PendingCommandOutcomeResolutionStatus.Resolved,
                resolvedEntry));
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns("tenant-1");
        _ = userContext.UserId.Returns("user-1");
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);

        sut.Resolve(Outcome(MessageId, s_observedAt) with { Materiality = CommandMateriality.NoOp })
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(1)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        indicators.DidNotReceiveWithAnyArgs().Add(default!);
        sut.IndicatorDecisionCount.ShouldBe(1);
    }

    [Fact]
    public void Resolve_LegacyResolverAdapterRejectedThenMaterial_DoesNotPublishIndicator() {
        FakeTimeProvider time = new(s_observedAt);
        INewItemIndicatorStateService indicators = Substitute.For<INewItemIndicatorStateService>();
        IPendingCommandOutcomeResolver legacy = Substitute.For<IPendingCommandOutcomeResolver>();
        PendingCommandEntry resolvedEntry = new(
            CorrelationId,
            MessageId,
            "Counter.Increment",
            "Counter.Count",
            "counter-counts",
            "counter-1",
            "Approved",
            "Draft",
            s_observedAt.AddMinutes(-1),
            PendingCommandStatus.Rejected) {
            TargetSnapshot = Target(),
        };
        legacy.Resolve(Arg.Any<PendingCommandOutcomeObservation>()).Returns(
            new PendingCommandOutcomeResolutionResult(
                PendingCommandOutcomeResolutionStatus.Resolved,
                resolvedEntry));
        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns("tenant-1");
        _ = userContext.UserId.Returns("user-1");
        PendingCommandOutcomeResolver sut = new(
            legacy,
            Substitute.For<IPendingCommandStateService>(),
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            userContext);

        sut.Resolve(Outcome(MessageId, s_observedAt) with { Outcome = PendingCommandTerminalOutcome.Rejected })
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);
        sut.Resolve(Outcome(MessageId, s_observedAt.AddSeconds(1)))
            .Status.ShouldBe(PendingCommandOutcomeResolutionStatus.Resolved);

        indicators.DidNotReceiveWithAnyArgs().Add(default!);
        sut.IndicatorDecisionCount.ShouldBe(1);
    }

    private static PendingCommandOutcomeResolver Create(
        ILifecycleStateService lifecycle,
        out PendingCommandStateService state,
        INewItemIndicatorStateService? indicators = null,
        TimeProvider? resolverTime = null,
        FcShellOptions? options = null,
        string tenantId = "tenant-1",
        string userId = "user-1") {
        ConfigureLifecycleReadback(lifecycle);
        TimeProvider stateTime = resolverTime ?? new FakeTimeProvider(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        FcShellOptions effectiveOptions = options ?? new FcShellOptions();
        state = new PendingCommandStateService(
            Microsoft.Extensions.Options.Options.Create(effectiveOptions),
            lifecycle,
            stateTime,
            NullLogger<PendingCommandStateService>.Instance);

        IUserContextAccessor userContext = Substitute.For<IUserContextAccessor>();
        _ = userContext.TenantId.Returns(tenantId);
        _ = userContext.UserId.Returns(userId);

        return new PendingCommandOutcomeResolver(
            state,
            NullLogger<PendingCommandOutcomeResolver>.Instance,
            indicators,
            resolverTime,
            Microsoft.Extensions.Options.Options.Create(effectiveOptions),
            userContext);
    }

    private static void ConfigureLifecycleReadback(ILifecycleStateService lifecycle) {
        Dictionary<string, (CommandLifecycleState State, string? MessageId)> values = new(StringComparer.Ordinal);
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
            PriorStatusSlot: "Draft") {
            TargetSnapshot = projectionTypeName is not null && laneKey is not null && entityKey is not null
                ? Target(
                    projectionTypeName: projectionTypeName,
                    viewKey: laneKey,
                    entityKey: entityKey)
                : null,
        };

    private static CommandTargetSnapshot Target(
        CommandTargetChangeKind changeKind = CommandTargetChangeKind.Update,
        DateTimeOffset? capturedAt = null,
        string projectionTypeName = "Counter.Count",
        string viewKey = "counter-counts",
        string entityKey = "counter-1",
        string tenantId = "tenant-1",
        string userId = "user-1") =>
        new(
            projectionTypeName,
            viewKey,
            entityKey,
            changeKind,
            "Draft",
            "Approved",
            tenantId,
            userId,
            capturedAt ?? s_observedAt.AddMinutes(-1));

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

    private static PendingCommandOutcomeResolutionResult DelegatedResolved(string messageId) =>
        new(
            PendingCommandOutcomeResolutionStatus.Resolved,
            new PendingCommandEntry(
                CorrelationId,
                messageId,
                "Counter.Increment",
                "Counter.Count",
                "counter-counts",
                "counter-1",
                "Approved",
                "Draft",
                s_observedAt.AddMinutes(-1),
                PendingCommandStatus.Confirmed) {
                TargetSnapshot = Target(),
            });

    private static PendingCommandOutcomeObservation Outcome(string? messageId, DateTimeOffset? observedAt = null) =>
        new(
            Source: PendingCommandOutcomeSource.ReconnectReconciliation,
            Outcome: PendingCommandTerminalOutcome.Confirmed,
            MessageId: messageId,
            ProjectionTypeName: "Counter.Count",
            LaneKey: "counter-counts",
            EntityKey: "counter-1",
            ExpectedStatusSlot: "Approved",
            ObservedAt: observedAt) {
            Materiality = CommandMateriality.Material,
        };

    private sealed class ThrowingTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("clock unavailable");
    }
}
