using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Services.Lifecycle;

using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public class StubCommandServiceTests {
    private static StubCommandService BuildService(StubCommandServiceOptions options) {
        return new StubCommandService(new OptionsSnapshotStub(options), new UlidFactory());
    }

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
    public async Task DispatchAsync_CancellationToken_StopsCallbackInvocation() {
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
        await Task.Delay(300, TestContext.Current.CancellationToken);

        observed.ShouldNotContain(CommandLifecycleState.Confirmed);
    }

    [Fact]
    public async Task DispatchAsync_AcknowledgementDelayHonoursOptions() {
        StubCommandServiceOptions options = new() {
            AcknowledgeDelayMs = 100,
            SyncingDelayMs = 0,
            ConfirmDelayMs = 0,
        };
        StubCommandService service = BuildService(options);

        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
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

    private sealed class OptionsSnapshotStub : IOptionsSnapshot<StubCommandServiceOptions> {
        public OptionsSnapshotStub(StubCommandServiceOptions value) => Value = value;
        public StubCommandServiceOptions Value { get; }
        public StubCommandServiceOptions Get(string? name) => Value;
    }
}
