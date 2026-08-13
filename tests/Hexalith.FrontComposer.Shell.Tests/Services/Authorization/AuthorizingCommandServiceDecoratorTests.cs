using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Authorization;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Services.Authorization;

/// <summary>
/// Story 7-3 Pass 4 DN-7-3-4-2 — verifies the decorator gates direct dispatch BEFORE the inner
/// service runs, and propagates allow / deny / rejection / lifecycle behaviour transparently.
/// Replaces the per-impl gate tests previously in StubCommandServiceTests.
/// </summary>
public sealed class AuthorizingCommandServiceDecoratorTests {
    [Fact]
    public async Task DispatchAsync_GatesBeforeInnerDispatch() {
        ICommandServiceWithLifecycle inner = Substitute.For<ICommandServiceWithLifecycle>();
        ICommandDispatchAuthorizationGate gate = Substitute.For<ICommandDispatchAuthorizationGate>();
        AuthorizingCommandServiceDecorator sut = new(inner, gate);
        SampleCommand command = new();

        _ = await sut.DispatchAsync(command, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Received.InOrder(() => {
            gate.EnsureAuthorizedAsync(command, Arg.Any<CancellationToken>());
            inner.DispatchAsync(command, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task DispatchAsync_GateThrows_InnerNotCalledNoSideEffects() {
        ICommandServiceWithLifecycle inner = Substitute.For<ICommandServiceWithLifecycle>();
        ICommandDispatchAuthorizationGate gate = Substitute.For<ICommandDispatchAuthorizationGate>();
        gate.EnsureAuthorizedAsync(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new CommandWarningException(
                CommandWarningKind.Forbidden,
                new ProblemDetailsPayload(
                    Title: "denied",
                    Detail: "no",
                    Status: 403,
                    EntityLabel: null,
                    ValidationErrors: new Dictionary<string, IReadOnlyList<string>>(),
                    GlobalErrors: Array.Empty<string>()))));
        AuthorizingCommandServiceDecorator sut = new(inner, gate);

        await Should.ThrowAsync<CommandWarningException>(
            async () => await sut.DispatchAsync(new SampleCommand(), TestContext.Current.CancellationToken)
                .ConfigureAwait(true)).ConfigureAwait(true);

        await inner.DidNotReceive().DispatchAsync(
            Arg.Any<SampleCommand>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await inner.DidNotReceive().DispatchAsync(
            Arg.Any<SampleCommand>(),
            Arg.Any<Action<CommandLifecycleState, string?>>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task DispatchAsync_LifecycleOverload_GatesBeforeInner() {
        ICommandServiceWithLifecycle inner = Substitute.For<ICommandServiceWithLifecycle>();
        ICommandDispatchAuthorizationGate gate = Substitute.For<ICommandDispatchAuthorizationGate>();
        AuthorizingCommandServiceDecorator sut = new(inner, gate);
        SampleCommand command = new();
        static void callback(CommandLifecycleState _1, string? _2) { }

        _ = await sut.DispatchAsync(command, callback, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Received.InOrder(() => {
            gate.EnsureAuthorizedAsync(command, Arg.Any<CancellationToken>());
            inner.DispatchAsync(command, callback, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task DispatchAsync_NullCommand_ThrowsBeforeGate() {
        ICommandServiceWithLifecycle inner = Substitute.For<ICommandServiceWithLifecycle>();
        ICommandDispatchAuthorizationGate gate = Substitute.For<ICommandDispatchAuthorizationGate>();
        AuthorizingCommandServiceDecorator sut = new(inner, gate);

        await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.DispatchAsync<SampleCommand>(null!, TestContext.Current.CancellationToken)
                .ConfigureAwait(true)).ConfigureAwait(true);

        await gate.DidNotReceive().EnsureAuthorizedAsync(
            Arg.Any<SampleCommand>(),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task TypedObservations_FromStubSurviveDecoratorWithMaterialityAndTime() {
        DateTimeOffset now = new(2026, 8, 13, 12, 0, 0, TimeSpan.Zero);
        FakeTimeProvider time = new(now);
        StubCommandService inner = new(
            new OptionsSnapshotStub(new StubCommandServiceOptions {
                AcknowledgeDelayMs = 0,
                SyncingDelayMs = 0,
                ConfirmDelayMs = 0,
            }),
            new Hexalith.FrontComposer.Shell.Services.Lifecycle.UlidFactory(),
            logger: null,
            timeProvider: time);
        ICommandDispatchAuthorizationGate gate = Substitute.For<ICommandDispatchAuthorizationGate>();
        AuthorizingCommandServiceDecorator sut = new(inner, gate, time);
        List<CommandLifecycleObservation> observations = [];

        _ = await ((ICommandServiceWithLifecycleObservations)sut)
            .DispatchAsync(new SampleCommand(), observations.Add, TestContext.Current.CancellationToken);

        SpinWait.SpinUntil(() => observations.Count == 2, TimeSpan.FromSeconds(2)).ShouldBeTrue();
        observations[1].Materiality.ShouldBe(CommandMateriality.Material);
        observations.ShouldAllBe(observation => observation.ObservedAt == now);
    }

    [Fact]
    public async Task TypedDispatch_GatesBeforeInnerAndForwardsObservation() {
        ICommandServiceWithLifecycleObservations inner = Substitute.For<ICommandServiceWithLifecycleObservations>();
        ICommandDispatchAuthorizationGate gate = Substitute.For<ICommandDispatchAuthorizationGate>();
        AuthorizingCommandServiceDecorator sut = new(inner, gate);
        SampleCommand command = new();
        Action<CommandLifecycleObservation> callback = Substitute.For<Action<CommandLifecycleObservation>>();
        inner.DispatchAsync(command, callback, Arg.Any<CancellationToken>())
            .Returns(new CommandResult("01ARZ3NDEKTSV4RRFFQ69G5FAV", "Accepted"));

        _ = await ((ICommandServiceWithLifecycleObservations)sut)
            .DispatchAsync(command, callback, TestContext.Current.CancellationToken);

        Received.InOrder(() => {
            gate.EnsureAuthorizedAsync(command, Arg.Any<CancellationToken>());
            inner.DispatchAsync(command, callback, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task TypedDispatch_AuthorizationDeniedDoesNotDispatchOrObserve() {
        ICommandServiceWithLifecycleObservations inner = Substitute.For<ICommandServiceWithLifecycleObservations>();
        ICommandDispatchAuthorizationGate gate = Substitute.For<ICommandDispatchAuthorizationGate>();
        gate.EnsureAuthorizedAsync(Arg.Any<SampleCommand>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException(new CommandWarningException(
                CommandWarningKind.Forbidden,
                new ProblemDetailsPayload(
                    "denied",
                    "no",
                    403,
                    null,
                    new Dictionary<string, IReadOnlyList<string>>(),
                    Array.Empty<string>()))));
        AuthorizingCommandServiceDecorator sut = new(inner, gate);
        List<CommandLifecycleObservation> observations = [];

        await Should.ThrowAsync<CommandWarningException>(() =>
            ((ICommandServiceWithLifecycleObservations)sut).DispatchAsync(
                new SampleCommand(),
                observations.Add,
                TestContext.Current.CancellationToken));

        await inner.DidNotReceiveWithAnyArgs().DispatchAsync(
            Arg.Any<SampleCommand>(),
            Arg.Any<Action<CommandLifecycleObservation>?>(),
            Arg.Any<CancellationToken>());
        observations.ShouldBeEmpty();
    }

    private sealed class OptionsSnapshotStub(StubCommandServiceOptions value) : IOptionsSnapshot<StubCommandServiceOptions> {
        public StubCommandServiceOptions Value { get; } = value;

        public StubCommandServiceOptions Get(string? name) => Value;
    }

    private sealed class SampleCommand { }
}
