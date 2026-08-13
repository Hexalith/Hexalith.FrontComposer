using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class PendingCommandPublicCompatibilityTests {
    private const string CorrelationId = "01CPZ3NDEKTSV4RRFFQ69G5FAV";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public void PreStory94ConstructorsAndDeconstructorsRemainSourceCompatible() {
        DateTimeOffset now = new(2026, 8, 13, 9, 0, 0, TimeSpan.Zero);

        // The untyped fourth positional null was a valid pre-Story-9.4 call and must stay unambiguous.
        PendingCommandRegistration registration = new(CorrelationId, MessageId, "Counter.Increment", null);
        registration.ProjectionTypeName.ShouldBeNull();

        PendingCommandEntry entry = new(
            CorrelationId,
            MessageId,
            "Counter.Increment",
            "Counter.Count",
            "counter-counts",
            "counter-1",
            "Approved",
            "Draft",
            now,
            PendingCommandStatus.Pending,
            null,
            null,
            null,
            null,
            0);
        var (
            correlationId,
            messageId,
            commandTypeName,
            projectionTypeName,
            laneKey,
            entityKey,
            expectedStatusSlot,
            priorStatusSlot,
            submittedAt,
            status,
            rejectionTitle,
            rejectionDetail,
            rejectionDataImpact,
            terminalAt,
            duplicateTerminalObservations) = entry;

        correlationId.ShouldBe(CorrelationId);
        messageId.ShouldBe(MessageId);
        commandTypeName.ShouldBe("Counter.Increment");
        projectionTypeName.ShouldBe("Counter.Count");
        laneKey.ShouldBe("counter-counts");
        entityKey.ShouldBe("counter-1");
        expectedStatusSlot.ShouldBe("Approved");
        priorStatusSlot.ShouldBe("Draft");
        submittedAt.ShouldBe(now);
        status.ShouldBe(PendingCommandStatus.Pending);
        rejectionTitle.ShouldBeNull();
        rejectionDetail.ShouldBeNull();
        rejectionDataImpact.ShouldBeNull();
        terminalAt.ShouldBeNull();
        duplicateTerminalObservations.ShouldBe(0);

        PendingCommandOutcomeObservation observation = new(
            PendingCommandOutcomeSource.ReconnectReconciliation,
            PendingCommandTerminalOutcome.Confirmed,
            MessageId,
            "Counter.Count",
            "counter-counts",
            "counter-1",
            "Approved",
            null,
            null,
            null,
            now);
        var (
            source,
            outcome,
            observedMessageId,
            observedProjectionTypeName,
            observedLaneKey,
            observedEntityKey,
            observedExpectedStatus,
            observedRejectionTitle,
            observedRejectionDetail,
            observedRejectionDataImpact,
            observedAt) = observation;

        source.ShouldBe(PendingCommandOutcomeSource.ReconnectReconciliation);
        outcome.ShouldBe(PendingCommandTerminalOutcome.Confirmed);
        observedMessageId.ShouldBe(MessageId);
        observedProjectionTypeName.ShouldBe("Counter.Count");
        observedLaneKey.ShouldBe("counter-counts");
        observedEntityKey.ShouldBe("counter-1");
        observedExpectedStatus.ShouldBe("Approved");
        observedRejectionTitle.ShouldBeNull();
        observedRejectionDetail.ShouldBeNull();
        observedRejectionDataImpact.ShouldBeNull();
        observedAt.ShouldBe(now);
    }

    [Fact]
    public void ExistingResolutionStatusNumericValuesRemainStable() {
        ((int)PendingCommandOutcomeResolutionStatus.Resolved).ShouldBe(0);
        ((int)PendingCommandOutcomeResolutionStatus.DuplicateIgnored).ShouldBe(1);
        ((int)PendingCommandOutcomeResolutionStatus.Unknown).ShouldBe(2);
        ((int)PendingCommandOutcomeResolutionStatus.InvalidMessageId).ShouldBe(3);
        ((int)PendingCommandOutcomeResolutionStatus.AmbiguousMatch).ShouldBe(4);
        ((int)PendingCommandOutcomeResolutionStatus.LifecycleDispatchFailed).ShouldBe(5);
        ((int)PendingCommandOutcomeResolutionStatus.Buffered).ShouldBe(6);
    }

    [Fact]
    public void ConcreteCommandServicesRetainUnambiguousLegacyNullCallbackCallShape() {
        typeof(StubCommandService).GetMethods()
            .Count(method => method.Name == nameof(StubCommandService.DispatchAsync) && method.GetParameters().Length == 3)
            .ShouldBe(1);
        typeof(EventStoreCommandClient).GetMethods()
            .Count(method => method.Name == nameof(EventStoreCommandClient.DispatchAsync) && method.GetParameters().Length == 3)
            .ShouldBe(1);

    }

    [Fact]
    public void BaselineResolverInterfaceRetainsSingleMethodAndCoordinatorDerivesFromIt() {
        System.Reflection.MethodInfo method = typeof(IPendingCommandOutcomeResolver).GetMethods().Single();
        method.Name.ShouldBe(nameof(IPendingCommandOutcomeResolver.Resolve));
        typeof(IPendingCommandOutcomeResolver).IsAssignableFrom(typeof(IPendingCommandOutcomeCoordinator)).ShouldBeTrue();
        typeof(IPendingCommandOutcomeCoordinator).GetMethod(nameof(IPendingCommandOutcomeCoordinator.BufferBeforeAccepted)).ShouldNotBeNull();
        typeof(IPendingCommandOutcomeCoordinator).GetMethod(nameof(IPendingCommandOutcomeCoordinator.AssociateAccepted)).ShouldNotBeNull();
        typeof(IPendingCommandOutcomeCoordinator).GetMethod(nameof(IPendingCommandOutcomeCoordinator.DiscardBuffered)).ShouldNotBeNull();
        typeof(IPendingCommandOutcomeCoordinator).GetMethod(nameof(IPendingCommandOutcomeCoordinator.DiscardBufferedByOwner)).ShouldNotBeNull();
    }

    [Fact]
    public void BaselinePublicConstructorSignaturesRemainAvailable() {
        Type[] stubSignature = [
            typeof(Microsoft.Extensions.Options.IOptionsSnapshot<StubCommandServiceOptions>),
            typeof(IUlidFactory),
            typeof(Microsoft.Extensions.Logging.ILogger<StubCommandService>),
        ];
        Type[] eventStoreSignature = [
            typeof(IHttpClientFactory),
            typeof(Microsoft.Extensions.Options.IOptions<EventStoreOptions>),
            typeof(IUlidFactory),
            typeof(Hexalith.FrontComposer.Contracts.Rendering.IUserContextAccessor),
            typeof(EventStoreResponseClassifier),
            typeof(Microsoft.Extensions.Logging.ILogger<EventStoreCommandClient>),
            typeof(Microsoft.Extensions.Options.IOptions<FcShellOptions>),
        ];
        Type[] resolverSignature = [
            typeof(IPendingCommandStateService),
            typeof(Microsoft.Extensions.Logging.ILogger<PendingCommandOutcomeResolver>),
            typeof(INewItemIndicatorStateService),
            typeof(TimeProvider),
        ];

        typeof(StubCommandService).GetConstructor(stubSignature).ShouldNotBeNull();
        typeof(EventStoreCommandClient).GetConstructor(eventStoreSignature).ShouldNotBeNull();
        typeof(PendingCommandOutcomeResolver).GetConstructor(resolverSignature).ShouldNotBeNull();
    }

    [Fact]
    public void CommandTargetSnapshotIsImmutableValidatedAndTimeIndependentForEquality() {
        DateTimeOffset firstTime = new(2026, 8, 13, 9, 0, 0, TimeSpan.Zero);
        CommandTargetSnapshot first = Snapshot(firstTime);
        CommandTargetSnapshot later = Snapshot(firstTime.AddHours(1));

        typeof(CommandTargetSnapshot).GetProperties()
            .All(property => property.SetMethod == null)
            .ShouldBeTrue();
        first.ShouldBe(later);
        first.GetHashCode().ShouldBe(later.GetHashCode());
        Should.Throw<ArgumentOutOfRangeException>(() => Snapshot(firstTime, (CommandTargetChangeKind)int.MaxValue));
        Should.Throw<ArgumentException>(() => new CommandTargetSnapshot(
            "Counter.Count",
            "counter-counts",
            "counter-1",
            CommandTargetChangeKind.StatusMove,
            null,
            "Approved",
            "tenant-1",
            "user-1",
            firstTime));
        Should.Throw<ArgumentException>(() => new CommandTargetSnapshot(
            "Counter.Count",
            "counter-counts",
            "counter-1",
            CommandTargetChangeKind.StatusMove,
            " Approved ",
            "Approved",
            "tenant-1",
            "user-1",
            firstTime));
    }

    private static Task<CommandResult> DispatchWithNullCallback(StubCommandService service) =>
        service.DispatchAsync(new object(), null, CancellationToken.None);

    private static Task<CommandResult> DispatchEventStoreWithNullCallback(EventStoreCommandClient service) =>
        service.DispatchAsync(new object(), null, CancellationToken.None);

    private static CommandTargetSnapshot Snapshot(
        DateTimeOffset capturedAt,
        CommandTargetChangeKind changeKind = CommandTargetChangeKind.Update) =>
        new(
            "Counter.Count",
            "counter-counts",
            "counter-1",
            changeKind,
            "Draft",
            "Approved",
            "tenant-1",
            "user-1",
            capturedAt);
}
