using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

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
    public async Task PreAcceptCancellation_WinsWhenLegacyInnerIgnoresCanceledToken() {
        IgnoringPreAcceptCancellationService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);
        using CancellationTokenSource caller = new();
        Task<CommandResult> dispatch = sut.DispatchAsync(new object(), _ => { }, caller.Token);
        await inner.Started.Task.WaitAsync(TestContext.Current.CancellationToken);

        await caller.CancelAsync();
        inner.Release.TrySetResult();

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
        List<CommandLifecycleObservation> observations = [];

        CommandResult result = await sut.DispatchAsync(
            new object(),
            observations.Add,
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        inner.DispatchCount.ShouldBe(1);
        observations.Single().State.ShouldBe(CommandLifecycleState.Confirmed);
        observations.Single().ObservedAt.ShouldBeNull();
    }

    [Fact]
    public async Task RetainedCallback_FirstTerminalClosesFurtherDelivery() {
        RetainedCallbackService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);
        List<CommandLifecycleObservation> observations = [];
        _ = await sut.DispatchAsync(new object(), observations.Add, TestContext.Current.CancellationToken);

        inner.Callback.ShouldNotBeNull()(CommandLifecycleState.Confirmed, MessageId);
        inner.Callback(CommandLifecycleState.Rejected, MessageId);
        inner.Callback(CommandLifecycleState.Syncing, MessageId);

        observations.Select(observation => observation.State).ShouldBe([CommandLifecycleState.Confirmed]);
    }

    [Fact]
    public async Task PreAcceptMismatchedTerminal_DoesNotSuppressCanonicalRetainedTerminal() {
        MismatchThenRetainedCallbackService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);
        List<CommandLifecycleObservation> observations = [];

        CommandResult result = await sut.DispatchAsync(
            new object(),
            observations.Add,
            TestContext.Current.CancellationToken);
        inner.Callback.ShouldNotBeNull()(CommandLifecycleState.Confirmed, MessageId);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        observations.Select(static observation => observation.MessageId).ShouldBe([MessageId]);
    }

    [Fact]
    public async Task AcceptedDispatch_WithoutTerminal_ExpiresAndDisposesRetainedLifetime() {
        FakeTimeProvider time = new(new DateTimeOffset(2026, 8, 14, 10, 0, 0, TimeSpan.Zero));
        RetainedCallbackService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(
            inner,
            time,
            Microsoft.Extensions.Options.Options.Create(new FcShellOptions { MaxPendingCommandPollingDurationMs = 1_000 }));
        List<CommandLifecycleObservation> observations = [];

        _ = await sut.DispatchAsync(new object(), observations.Add, TestContext.Current.CancellationToken);
        time.Advance(TimeSpan.FromSeconds(1));
        inner.Callback.ShouldNotBeNull()(CommandLifecycleState.Confirmed, MessageId);

        observations.ShouldBeEmpty();
        Should.Throw<ObjectDisposedException>(() => _ = inner.ObservedToken.WaitHandle);
    }

    [Fact]
    public async Task FatalClockFailure_Propagates() {
        LegacyLifecycleObservationCommandServiceAdapter sut = new(
            new SynchronousTerminalService(),
            new FatalThrowingTimeProvider());

        _ = await Should.ThrowAsync<OutOfMemoryException>(() =>
            sut.DispatchAsync(new object(), _ => { }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task FatalObserverFailure_Propagates() {
        LegacyLifecycleObservationCommandServiceAdapter sut = new(
            new SynchronousTerminalService(),
            TimeProvider.System);

        _ = await Should.ThrowAsync<OutOfMemoryException>(() =>
            sut.DispatchAsync(
                new object(),
                _ => ThrowFatal<object?>(),
                TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task NonAcceptedDispatch_ClosesRetainedCallback() {
        RetainedCallbackService inner = new(CommandResultStatus.Rejected);
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);
        List<CommandLifecycleObservation> observations = [];

        CommandResult result = await sut.DispatchAsync(
            new object(),
            observations.Add,
            TestContext.Current.CancellationToken);
        inner.Callback.ShouldNotBeNull()(CommandLifecycleState.Rejected, MessageId);

        result.Status.ShouldBe(CommandResultStatus.Rejected);
        observations.ShouldBeEmpty();
    }

    [Fact]
    public async Task FaultedDispatch_ClosesRetainedCallback() {
        FaultedRetainedCallbackService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);
        List<CommandLifecycleObservation> observations = [];

        _ = await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.DispatchAsync(new object(), observations.Add, TestContext.Current.CancellationToken));
        inner.Callback.ShouldNotBeNull()(CommandLifecycleState.Rejected, MessageId);

        observations.ShouldBeEmpty();
    }

    [Fact]
    public async Task AcceptedDispatch_NullCallbackDisposesAdapterLifetime() {
        RetainedCallbackService inner = new();
        LegacyLifecycleObservationCommandServiceAdapter sut = new(inner, TimeProvider.System);

        _ = await sut.DispatchAsync(
            new object(),
            onLifecycleObservation: null,
            TestContext.Current.CancellationToken);

        Should.Throw<ObjectDisposedException>(() => _ = inner.ObservedToken.WaitHandle);
    }

    private sealed class RetainedCallbackService(string status = CommandResultStatus.Accepted) : ICommandServiceWithLifecycle {
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
            return Task.FromResult(new CommandResult(MessageId, status));
        }
    }

    private sealed class FaultedRetainedCallbackService : ICommandServiceWithLifecycle {
        public Action<CommandLifecycleState, string?>? Callback { get; private set; }

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
            return Task.FromException<CommandResult>(new InvalidOperationException("dispatch failed"));
        }
    }

    private sealed class MismatchThenRetainedCallbackService : ICommandServiceWithLifecycle {
        private const string MismatchedMessageId = "01BRZ3NDEKTSV4RRFFQ69G5FAV";

        public Action<CommandLifecycleState, string?>? Callback { get; private set; }

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
            onLifecycleChange?.Invoke(CommandLifecycleState.Rejected, MismatchedMessageId);
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

    private sealed class IgnoringPreAcceptCancellationService : ICommandServiceWithLifecycle {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            await Release.Task.ConfigureAwait(false);
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

    private sealed class FatalThrowingTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => ThrowFatal<DateTimeOffset>();
    }

    private static T ThrowFatal<T>() {
        System.Runtime.ExceptionServices.ExceptionDispatchInfo
            .Capture((Exception)Activator.CreateInstance(
                typeof(OutOfMemoryException),
                "fatal test exception")!)
            .Throw();
        return default!;
    }
}
