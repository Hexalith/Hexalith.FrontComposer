using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.PendingCommands;

public sealed class PendingCommandPollingCoordinatorTests {
    [Fact]
    public async Task PollOnce_ProcessesPendingCommandsOldestFirstWithinCap() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 4, 26, 12, 0, 0, TimeSpan.Zero));
        ILifecycleStateService lifecycle = Substitute.For<ILifecycleStateService>();
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 1 }),
            lifecycle,
            time,
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration("corr-1", "01ARZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));
        time.Advance(TimeSpan.FromSeconds(1));
        state.Register(Registration("corr-2", "01BRZ3NDEKTSV4RRFFQ69G5FAV", time.GetUtcNow()));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        query.QueryAsync(Arg.Any<PendingCommandEntry>(), Arg.Any<CancellationToken>())
            .Returns(call => ValueTask.FromResult<PendingCommandOutcomeObservation?>(new(
                PendingCommandOutcomeSource.FallbackPolling,
                PendingCommandTerminalOutcome.Confirmed,
                ((PendingCommandEntry)call[0]).MessageId)));

        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 1 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(1);
        state.GetByMessageId("01ARZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Confirmed);
        state.GetByMessageId("01BRZ3NDEKTSV4RRFFQ69G5FAV")!.Status.ShouldBe(PendingCommandStatus.Pending);
    }

    [Fact]
    public async Task PollOnce_ZeroCapDisablesPendingPolling() {
        PendingCommandStateService state = new(
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 0 }),
            Substitute.For<ILifecycleStateService>(),
            new FakeTimeProvider(),
            NullLogger<PendingCommandStateService>.Instance);
        state.Register(Registration("corr-1", "01ARZ3NDEKTSV4RRFFQ69G5FAV", DateTimeOffset.UtcNow));

        IPendingCommandStatusQuery query = Substitute.For<IPendingCommandStatusQuery>();
        PendingCommandPollingCoordinator sut = new(
            state,
            new PendingCommandOutcomeResolver(state),
            query,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingPerTick = 0 }),
            NullLogger<PendingCommandPollingCoordinator>.Instance);

        int processed = await sut.PollOnceAsync(TestContext.Current.CancellationToken);

        processed.ShouldBe(0);
        await query.DidNotReceiveWithAnyArgs().QueryAsync(default!, TestContext.Current.CancellationToken);
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
}
