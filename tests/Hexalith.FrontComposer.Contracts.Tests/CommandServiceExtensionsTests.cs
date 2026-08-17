using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Contracts.Tests;

public class CommandServiceExtensionsTests {
    [Fact]
    public async Task DispatchAsync_CallbackOverload_Throws_WhenCallbackSuppliedAndLifecycleUnsupported() {
        // Loud-fail contract (ADR-010 amendment 2026-04-15): supplying onLifecycleChange to a
        // non-lifecycle-aware service must throw so Syncing/Confirmed cannot be silently dropped.
        BasicCommandService service = new();
        using CancellationTokenSource cts = new();

        NotSupportedException ex = await Should.ThrowAsync<NotSupportedException>(async () => await service.DispatchAsync(
            new object(),
            onLifecycleChange: (_, _) => { },
            cancellationToken: cts.Token).ConfigureAwait(true)).ConfigureAwait(true);

        ex.Message.ShouldContain(nameof(ICommandServiceWithLifecycle));
    }

    [Fact]
    public async Task DispatchAsync_CallbackOverload_FallsBackToBaseContract_WhenCallbackIsNull() {
        BasicCommandService service = new();
        using CancellationTokenSource cts = new();

        CommandResult result = await service.DispatchAsync(
            new object(),
            onLifecycleChange: null,
            cancellationToken: cts.Token);

        result.MessageId.ShouldBe("basic-message");
        service.ObservedToken.ShouldBe(cts.Token);
    }

    [Fact]
    public async Task DispatchAsync_CallbackOverload_UsesLifecycleAwareImplementation_WhenAvailable() {
        LifecycleAwareCommandService service = new();
        using CancellationTokenSource cts = new();
        List<CommandLifecycleState> observedStates = [];

        CommandResult result = await ((ICommandService)service).DispatchAsync(
            new object(),
            onLifecycleChange: (state, _) => observedStates.Add(state),
            cancellationToken: cts.Token);

        result.MessageId.ShouldBe("lifecycle-message");
        service.ObservedToken.ShouldBe(cts.Token);
        observedStates.ShouldBe([CommandLifecycleState.Syncing, CommandLifecycleState.Confirmed]);
    }

    [Fact]
    public async Task DispatchAsync_TypedObservationOverload_ThrowsWhenBasicServiceWouldLoseCallback() {
        BasicCommandService service = new();
        List<CommandLifecycleObservation> observations = [];

        NotSupportedException exception = await Should.ThrowAsync<NotSupportedException>(
            () => service.DispatchWithLifecycleObservationsAsync(
                new object(),
                onLifecycleObservation: observations.Add,
                cancellationToken: TestContext.Current.CancellationToken));

        exception.Message.ShouldContain(nameof(ICommandServiceWithLifecycleObservations));
        service.DispatchCount.ShouldBe(0);
        observations.ShouldBeEmpty();
    }

    [Fact]
    public async Task DispatchAsync_TypedObservationOverload_AllowsBasicServiceWhenCallbackIsNull() {
        BasicCommandService service = new();

        CommandResult result = await service.DispatchWithLifecycleObservationsAsync(
            new object(),
            onLifecycleObservation: null,
            cancellationToken: TestContext.Current.CancellationToken);

        result.MessageId.ShouldBe("basic-message");
        service.DispatchCount.ShouldBe(1);
    }

    [Fact]
    public async Task DispatchAsync_TypedObservationOverload_AdaptsLegacyLifecycleAsUnknownMateriality() {
        LifecycleAwareCommandService service = new();
        List<CommandLifecycleObservation> observations = [];

        _ = await ((ICommandService)service).DispatchWithLifecycleObservationsAsync(
            new object(),
            onLifecycleObservation: observations.Add,
            cancellationToken: TestContext.Current.CancellationToken);

        observations.Count.ShouldBe(2);
        observations.ShouldAllBe(observation => observation.Materiality == CommandMateriality.Unknown);
    }

    [Fact]
    public async Task DispatchAsync_TypedObservationOverload_IsolatesNonFatalLegacyObserverFailure() {
        LifecycleAwareCommandService service = new();
        int observationCount = 0;

        CommandResult result = await ((ICommandService)service).DispatchWithLifecycleObservationsAsync(
            new object(),
            onLifecycleObservation: _ => {
                observationCount++;
                throw new InvalidOperationException("observer-failed");
            },
            cancellationToken: TestContext.Current.CancellationToken);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        observationCount.ShouldBe(2);
    }

    [Fact]
    public async Task DispatchAsync_TypedObservationOverload_PropagatesFatalLegacyObserverFailure() {
        LifecycleAwareCommandService service = new();

        _ = await Should.ThrowAsync<OutOfMemoryException>(() =>
            ((ICommandService)service).DispatchWithLifecycleObservationsAsync(
                new object(),
                onLifecycleObservation: _ => ThrowFatal<object?>(),
                cancellationToken: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DispatchAsync_TypedObservationOverload_IsolatesNonFatalAggregateObserverFailure() {
        LifecycleAwareCommandService service = new();
        int observationCount = 0;

        CommandResult result = await ((ICommandService)service).DispatchWithLifecycleObservationsAsync(
            new object(),
            onLifecycleObservation: _ => {
                observationCount++;
                throw new AggregateException(new InvalidOperationException("observer-failed"));
            },
            cancellationToken: TestContext.Current.CancellationToken);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        observationCount.ShouldBe(2);
    }

    [Fact]
    public async Task DispatchAsync_TypedObservationOverload_PropagatesNestedFatalAggregateObserverFailure() {
        LifecycleAwareCommandService service = new();

        AggregateException exception = await Should.ThrowAsync<AggregateException>(() =>
            ((ICommandService)service).DispatchWithLifecycleObservationsAsync(
                new object(),
                onLifecycleObservation: _ => throw new AggregateException(
                    new AggregateException(CreateFatal())),
                cancellationToken: TestContext.Current.CancellationToken));

        exception.Flatten().InnerExceptions.ShouldContain(inner => inner is OutOfMemoryException);
    }

    [Fact]
    public async Task DispatchAsync_TypedObservationOverload_PropagatesMixedAggregateWhenAnyInnerIsFatal() {
        LifecycleAwareCommandService service = new();

        AggregateException exception = await Should.ThrowAsync<AggregateException>(() =>
            ((ICommandService)service).DispatchWithLifecycleObservationsAsync(
                new object(),
                onLifecycleObservation: _ => throw new AggregateException(
                    new InvalidOperationException("observer-failed"),
                    CreateFatal()),
                cancellationToken: TestContext.Current.CancellationToken));

        exception.InnerExceptions.ShouldContain(inner => inner is OutOfMemoryException);
    }

    private sealed class BasicCommandService : ICommandService {
        public CancellationToken ObservedToken { get; private set; }
        public int DispatchCount { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            DispatchCount++;
            ObservedToken = cancellationToken;
            return Task.FromResult(new CommandResult("basic-message", "Accepted"));
        }
    }

    private sealed class LifecycleAwareCommandService : ICommandServiceWithLifecycle {
        public CancellationToken ObservedToken { get; private set; }

        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : class {
            ObservedToken = cancellationToken;
            return Task.FromResult(new CommandResult("legacy-message", "Accepted"));
        }

        public Task<CommandResult> DispatchAsync<TCommand>(
            TCommand command,
            Action<CommandLifecycleState, string?>? onLifecycleChange,
            CancellationToken cancellationToken = default)
            where TCommand : class {
            ObservedToken = cancellationToken;
            onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, "lifecycle-message");
            onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, "lifecycle-message");
            return Task.FromResult(new CommandResult("lifecycle-message", "Accepted"));
        }
    }

    private static T ThrowFatal<T>() {
        System.Runtime.ExceptionServices.ExceptionDispatchInfo
            .Capture(CreateFatal())
            .Throw();
        return default!;
    }

    private static OutOfMemoryException CreateFatal() =>
        (OutOfMemoryException)Activator.CreateInstance(
            typeof(OutOfMemoryException),
            "fatal test exception")!;
}
