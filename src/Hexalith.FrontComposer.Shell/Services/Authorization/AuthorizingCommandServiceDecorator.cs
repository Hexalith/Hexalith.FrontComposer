using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Shell.Services.Authorization;

/// <summary>
/// Wraps an inner <see cref="ICommandServiceWithLifecycle"/> with the canonical Story 7-3 dispatch
/// authorization gate. Story 7-3 Pass 4 DN-7-3-4-2: every direct
/// <see cref="ICommandService.DispatchAsync{TCommand}(TCommand,CancellationToken)"/> caller — not
/// just the framework-shipped <c>StubCommandService</c> and <c>EventStoreCommandClient</c> — is
/// gated through <see cref="ICommandDispatchAuthorizationGate"/> before any side effect runs.
/// Adopters with a custom <see cref="ICommandService"/> impl get authorization for free as soon as
/// they wire it through <c>AddHexalithFrontComposer*</c>.
/// </summary>
internal sealed class AuthorizingCommandServiceDecorator(
    ICommandServiceWithLifecycle inner,
    ICommandDispatchAuthorizationGate gate) : ICommandServiceWithLifecycle {
    /// <inheritdoc />
    public async Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class {
        ArgumentNullException.ThrowIfNull(command);
        await gate.EnsureAuthorizedAsync(command, cancellationToken).ConfigureAwait(false);
        return await inner.DispatchAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        ArgumentNullException.ThrowIfNull(command);
        await gate.EnsureAuthorizedAsync(command, cancellationToken).ConfigureAwait(false);
        return await inner.DispatchAsync(command, onLifecycleChange, cancellationToken).ConfigureAwait(false);
    }
}
