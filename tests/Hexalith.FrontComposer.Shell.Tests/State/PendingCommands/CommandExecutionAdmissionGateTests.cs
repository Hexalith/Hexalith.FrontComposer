using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class CommandExecutionAdmissionGateTests {
    private const string CommandTypeName = "Counter.Increment";
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01CPZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public void TryAcquire_WhenIdle_AdmitsFirstCommand() {
        CommandExecutionAdmissionGate sut = Create();

        using CommandExecutionAdmission admission = sut.TryAcquire(Request());

        admission.IsAdmitted.ShouldBeTrue();
        admission.DenialReason.ShouldBe(CommandExecutionAdmissionDenialReason.None);
    }

    [Fact]
    public void TryAcquire_WhenAdmissionAlreadyHeld_DeniesSecondCommand() {
        CommandExecutionAdmissionGate sut = Create();
        using CommandExecutionAdmission first = sut.TryAcquire(Request());

        using CommandExecutionAdmission second = sut.TryAcquire(Request("Counter.Reset"));

        first.IsAdmitted.ShouldBeTrue();
        second.IsAdmitted.ShouldBeFalse();
        second.DenialReason.ShouldBe(CommandExecutionAdmissionDenialReason.AdmissionAlreadyInProgress);
        second.BlockingCommandTypeName.ShouldBe(CommandTypeName);
    }

    [Fact]
    public void TryAcquire_WhenPendingSnapshotContainsPendingEntry_DeniesAdmission() {
        PendingCommandStateService pending = CreatePendingState();
        pending.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        CommandExecutionAdmissionGate sut = Create(pending);

        using CommandExecutionAdmission admission = sut.TryAcquire(Request("Counter.Reset"));

        admission.IsAdmitted.ShouldBeFalse();
        admission.DenialReason.ShouldBe(CommandExecutionAdmissionDenialReason.PendingCommandAlreadyExists);
        admission.BlockingMessageId.ShouldBe(MessageId);
    }

    [Theory]
    [InlineData(PendingCommandTerminalOutcome.Confirmed)]
    [InlineData(PendingCommandTerminalOutcome.Rejected)]
    [InlineData(PendingCommandTerminalOutcome.IdempotentConfirmed)]
    [InlineData(PendingCommandTerminalOutcome.NeedsReview)]
    public void TryAcquire_WhenOnlyTerminalEntriesExist_AdmitsCommand(PendingCommandTerminalOutcome outcome) {
        PendingCommandStateService pending = CreatePendingState();
        pending.Register(Registration()).Status.ShouldBe(PendingCommandRegistrationStatus.Registered);
        pending.ResolveTerminal(Observation(outcome)).Status.ShouldBe(PendingCommandResolutionStatus.Resolved);
        CommandExecutionAdmissionGate sut = Create(pending);

        using CommandExecutionAdmission admission = sut.TryAcquire(Request("Counter.Reset"));

        admission.IsAdmitted.ShouldBeTrue();
    }

    [Fact]
    public void Dispose_AfterExceptionalPath_ReleasesAdmissionFlag() {
        CommandExecutionAdmissionGate sut = Create();

        try {
            using CommandExecutionAdmission admission = sut.TryAcquire(Request());
            admission.IsAdmitted.ShouldBeTrue();
            throw new InvalidOperationException("simulated dispatch failure");
        }
        catch (InvalidOperationException) {
        }

        using CommandExecutionAdmission next = sut.TryAcquire(Request("Counter.Reset"));
        next.IsAdmitted.ShouldBeTrue();
    }

    private static CommandExecutionAdmissionGate Create(PendingCommandStateService? pending = null) =>
        new(pending ?? CreatePendingState(), new FakeTimeProvider(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero)));

    private static CommandExecutionAdmissionRequest Request(string commandTypeName = CommandTypeName) =>
        new(commandTypeName, "Submit command");

    private static PendingCommandStateService CreatePendingState() {
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        return new PendingCommandStateService(
            global::Microsoft.Extensions.Options.Options.Create(new FcShellOptions()),
            lifecycle,
            Substitute.For<IUserContextAccessor>(),
            new FakeTimeProvider(new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero)),
            NullLogger<PendingCommandStateService>.Instance);
    }

    private static PendingCommandRegistration Registration() =>
        new(
            CorrelationId,
            MessageId,
            CommandTypeName);

    private static PendingCommandTerminalObservation Observation(PendingCommandTerminalOutcome outcome) =>
        outcome switch {
            PendingCommandTerminalOutcome.Confirmed => PendingCommandTerminalObservation.Confirmed(MessageId),
            PendingCommandTerminalOutcome.Rejected => PendingCommandTerminalObservation.Rejected(MessageId, "Rejected", "No change was applied."),
            PendingCommandTerminalOutcome.IdempotentConfirmed => PendingCommandTerminalObservation.IdempotentConfirmed(MessageId),
            PendingCommandTerminalOutcome.NeedsReview => PendingCommandTerminalObservation.NeedsReview(MessageId),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
        };
}
