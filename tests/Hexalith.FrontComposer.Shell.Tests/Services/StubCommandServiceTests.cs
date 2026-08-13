using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public class StubCommandServiceTests {
    private static StubCommandService BuildService(StubCommandServiceOptions options) =>
        // Story 7-3 Pass 4 DN-7-3-4-2 — authorization is enforced by AuthorizingCommandServiceDecorator
        // at the DI seam; StubCommandService no longer accepts an optional gate, so its tests focus
        // on lifecycle / cancellation / rejection behaviour (gate behaviour is covered by
        // AuthorizingCommandServiceDecoratorTests).
        new(new OptionsSnapshotStub(options), new UlidFactory());

    private static StubCommandServiceOptions ZeroDelays() => new() {
        AcknowledgeDelayMs = 0,
        SyncingDelayMs = 0,
        ConfirmDelayMs = 0,
    };

    [Fact]
    public async Task DispatchAsync_Acknowledgement_ReturnsCommandResultWithNonEmptyMessageId() {
        StubCommandService service = BuildService(ZeroDelays());

        CommandResult result = await service.DispatchAsync(new object(), cancellationToken: TestContext.Current.CancellationToken);

        result.MessageId.ShouldNotBeNullOrEmpty();
        result.Status.ShouldBe("Accepted");
    }

    [Fact]
    public async Task DispatchAsync_Syncing_ThenConfirmed_CallbacksFireInOrder() {
        StubCommandService service = BuildService(ZeroDelays());
        List<CommandLifecycleState> observed = [];

        await service.DispatchAsync(
            new object(),
            onLifecycleChange: (state, _) => {
                lock (observed) {
                    observed.Add(state);
                }
            },
            cancellationToken: TestContext.Current.CancellationToken);

        SpinWait.SpinUntil(() => observed.Count == 2, TimeSpan.FromSeconds(2)).ShouldBeTrue();
        observed[0].ShouldBe(CommandLifecycleState.Syncing);
        observed[1].ShouldBe(CommandLifecycleState.Confirmed);
    }

    [Fact]
    public async Task DispatchAsync_Rejection_ThrowsCommandRejectedException() {
        StubCommandServiceOptions options = ZeroDelays();
        options.SimulateRejection = true;
        options.RejectionReason = "domain failure";
        options.RejectionResolution = "fix your input";
        options.RejectionErrorCode = "STUB-409";
        options.RejectionReasonCategory = "Stub";
        options.RejectionSuggestedAction = "Change the stub input";
        options.RejectionDocsCode = "FC-STUB-409";
        StubCommandService service = BuildService(options);

        CommandRejectedException? caught = null;
        try {
            _ = await service.DispatchAsync(new object(), cancellationToken: TestContext.Current.CancellationToken);
        }
        catch (CommandRejectedException ex) {
            caught = ex;
        }

        _ = caught.ShouldNotBeNull();
        caught.Message.ShouldBe("domain failure");
        caught.Resolution.ShouldBe("fix your input");
        caught.ErrorCode.ShouldBe("STUB-409");
        caught.ReasonCategory.ShouldBe("Stub");
        caught.SuggestedAction.ShouldBe("Change the stub input");
        caught.DocsCode.ShouldBe("FC-STUB-409");
    }

    [Fact]
    public async Task DispatchAsync_Rejection_DoesNotFireLifecycleCallbacks() {
        StubCommandServiceOptions options = ZeroDelays();
        options.SimulateRejection = true;
        StubCommandService service = BuildService(options);
        List<CommandLifecycleState> observed = [];

        try {
            _ = await service.DispatchAsync(
                new object(),
                onLifecycleChange: (state, _) => {
                    lock (observed) {
                        observed.Add(state);
                    }
                },
                cancellationToken: TestContext.Current.CancellationToken);
        }
        catch (CommandRejectedException) {
            // expected
        }

        // Wait a short time -- if a callback were going to fire, it would have by now.
        await Task.Delay(50, TestContext.Current.CancellationToken);
        observed.ShouldBeEmpty();
    }

    [Fact]
    public async Task LegacyDispatchAsync_AfterAcceptance_CallbackHonorsCallerCancellation() {
        StubCommandServiceOptions options = new() {
            AcknowledgeDelayMs = 0,
            SyncingDelayMs = 200,
            ConfirmDelayMs = 200,
        };
        StubCommandService service = BuildService(options);
        using CancellationTokenSource cts = new();
        List<CommandLifecycleState> observed = [];

        CommandResult result = await service.DispatchAsync(
            new object(),
            onLifecycleChange: (state, _) => {
                lock (observed) {
                    observed.Add(state);
                }
            },
            cancellationToken: cts.Token);

        result.MessageId.ShouldNotBeNullOrEmpty();
        cts.Cancel();
        await Task.Delay(500, TestContext.Current.CancellationToken);

        observed.ShouldBeEmpty();
    }

    [Fact]
    public async Task TypedDispatchAsync_AfterAcceptance_CallbackSurvivesCallerCancellation() {
        StubCommandServiceOptions options = new() {
            AcknowledgeDelayMs = 0,
            SyncingDelayMs = 100,
            ConfirmDelayMs = 100,
        };
        StubCommandService service = BuildService(options);
        using CancellationTokenSource cts = new();
        List<CommandLifecycleObservation> observed = [];

        CommandResult result = await ((ICommandServiceWithLifecycleObservations)service).DispatchAsync(
            new object(),
            observation => {
                lock (observed) {
                    observed.Add(observation);
                }
            },
            cts.Token);

        result.MessageId.ShouldNotBeNullOrEmpty();
        cts.Cancel();

        SpinWait.SpinUntil(
            () => observed.Any(item => item.State == CommandLifecycleState.Confirmed),
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_AcknowledgementDelayHonoursOptions() {
        StubCommandServiceOptions options = new() {
            AcknowledgeDelayMs = 100,
            SyncingDelayMs = 0,
            ConfirmDelayMs = 0,
        };
        StubCommandService service = BuildService(options);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        _ = await service.DispatchAsync(new object(), cancellationToken: TestContext.Current.CancellationToken);
        sw.Stop();

        sw.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(80);
    }

    [Fact]
    public async Task DispatchAsync_NullCommand_ThrowsArgumentNullException() {
        StubCommandService service = BuildService(ZeroDelays());

        ArgumentNullException? caught = null;
        try {
            _ = await service.DispatchAsync<object>(null!, cancellationToken: TestContext.Current.CancellationToken);
        }
        catch (ArgumentNullException ex) {
            caught = ex;
        }

        _ = caught.ShouldNotBeNull();
    }

    [Fact]
    public async Task TypedObservations_UseInjectedClockAndMaterialConfirmation() {
        DateTimeOffset now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider time = new(now);
        StubCommandService service = new(new OptionsSnapshotStub(ZeroDelays()), new UlidFactory(), logger: null, timeProvider: time);
        List<CommandLifecycleObservation> observations = [];

        _ = await ((ICommandServiceWithLifecycleObservations)service).DispatchAsync(
            new object(),
            observation => {
                lock (observations) {
                    observations.Add(observation);
                }
            },
            TestContext.Current.CancellationToken);

        SpinWait.SpinUntil(() => observations.Count == 2, TimeSpan.FromSeconds(2)).ShouldBeTrue();
        observations[0].Materiality.ShouldBe(CommandMateriality.Unknown);
        observations[1].Materiality.ShouldBe(CommandMateriality.Material);
        observations.ShouldAllBe(observation => observation.ObservedAt == now);
    }

    [Fact]
    public async Task TypedObservations_ThrowingSyncingCallbackDoesNotSuppressConfirmed() {
        StubCommandService service = BuildService(ZeroDelays());
        List<CommandLifecycleState> observed = [];

        CommandResult result = await ((ICommandServiceWithLifecycleObservations)service).DispatchAsync(
            new object(),
            observation => {
                if (observation.State == CommandLifecycleState.Syncing) {
                    throw new InvalidOperationException("sync observer unavailable");
                }

                lock (observed) {
                    observed.Add(observation.State);
                }
            },
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        SpinWait.SpinUntil(
            () => observed.Contains(CommandLifecycleState.Confirmed),
            TimeSpan.FromSeconds(2)).ShouldBeTrue();
        observed.ShouldBe([CommandLifecycleState.Confirmed]);
    }

    [Fact]
    public async Task ConcreteNullCallback_RemainsSourceCompatibleWithLegacyOverload() {
        StubCommandService service = BuildService(ZeroDelays());

        CommandResult result = await service.DispatchAsync(new object(), null, TestContext.Current.CancellationToken);

        result.Status.ShouldBe("Accepted");
    }

    private sealed class OptionsSnapshotStub : IOptionsSnapshot<StubCommandServiceOptions> {
        public OptionsSnapshotStub(StubCommandServiceOptions value) => Value = value;
        public StubCommandServiceOptions Value { get; }
        public StubCommandServiceOptions Get(string? name) => Value;
    }
}
