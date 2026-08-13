using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services;

public sealed class LegacyLifecycleObservationCommandServiceAdapterTests {
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";

    [Fact]
    public async Task AcceptedDispatch_DetachesCallerCancellationAndRetainedCallbackSurvivesDisposeSignal() {
        RetainedCallbackService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);
        using CancellationTokenSource formLifetime = new();
        List<CommandLifecycleObservation> observations = [];

        CommandResult result = await sut.DispatchAsync(
            new object(),
            observations.Add,
            formLifetime.Token);
        await formLifetime.CancelAsync();

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        inner.ObservedToken.IsCancellationRequested.ShouldBeFalse();
        inner.Callback.ShouldNotBeNull()(CommandLifecycleState.Confirmed, MessageId);
        observations.Single().State.ShouldBe(CommandLifecycleState.Confirmed);
        observations.Single().Materiality.ShouldBe(CommandMateriality.Unknown);
    }

    [Fact]
    public async Task PreAcceptCancellation_StillCancelsLegacyDispatch() {
        BlockingPreAcceptService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);
        using CancellationTokenSource caller = new();

        Task<CommandResult> dispatch = sut.DispatchAsync(
            new object(),
            _ => { },
            caller.Token);
        await inner.Started.Task.WaitAsync(TestContext.Current.CancellationToken);
        await caller.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(() => dispatch);
    }

    [Fact]
    public async Task SynchronousTerminalObserverFailure_DoesNotReplaceAcceptedDispatchResult() {
        SynchronousTerminalService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);

        CommandResult result = await sut.DispatchAsync(
            new object(),
            _ => throw new InvalidOperationException("observer-failed"),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        inner.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task SynchronousTerminalClockFailure_DoesNotReplaceAcceptedDispatchResult() {
        SynchronousTerminalService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, new ThrowingTimeProvider());
        int observations = 0;

        CommandResult result = await sut.DispatchAsync(
            new object(),
            _ => observations++,
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        inner.DispatchCount.ShouldBe(1);
        observations.ShouldBe(0);
    }

    private sealed class RetainedCallbackService : ICommandServiceWithLifecycle {
        public Action<CommandLifecycleState, string?>? Callback { get; private set; }

        public CancellationToken ObservedToken { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            Callback = onLifecycleChange;
            ObservedToken = cancellationToken;
            return Task.FromResult(new CommandResult(MessageId, CommandResultStatus.Accepted));
        }
    }

    private sealed class BlockingPreAcceptService : ICommandServiceWithLifecycle {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public async Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
            return new CommandResult(MessageId, CommandResultStatus.Accepted);
        }
    }

    private sealed class SynchronousTerminalService : ICommandServiceWithLifecycle {
        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            CancellationToken cancellationToken = default)
            where TCommand : class => DispatchAsync(command, null, cancellationToken);

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, MessageId);
            return Task.FromResult(new CommandResult(MessageId, CommandResultStatus.Accepted));
        }
    }

    private sealed class ThrowingTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => throw new InvalidOperationException("clock-failed");
    }
}
