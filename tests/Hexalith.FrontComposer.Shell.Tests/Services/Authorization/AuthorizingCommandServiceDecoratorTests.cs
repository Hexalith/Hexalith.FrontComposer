using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Services.Authorization;

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
        Action<CommandLifecycleState, string?> callback = (_, _) => { };

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

    private sealed class SampleCommand { }
}
