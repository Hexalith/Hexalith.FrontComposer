using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Optional extension for <see cref="ICommandService"/> implementations that can surface
/// post-acknowledgement lifecycle transitions.
/// </summary>
/// <remarks>
/// This preserves the original <see cref="ICommandService"/> contract for existing implementers
/// while allowing richer clients to observe <see cref="CommandLifecycleState.Syncing"/> and
/// <see cref="CommandLifecycleState.Confirmed"/> via
/// <see cref="CommandServiceExtensions.DispatchAsync{TCommand}(ICommandService, TCommand, Action{CommandLifecycleState, string?}?, CancellationToken)"/>.
/// </remarks>
public interface ICommandServiceWithLifecycle : ICommandService {
    /// <summary>
    /// Dispatches a command and optionally reports post-acknowledgement lifecycle transitions.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="onLifecycleChange">
    /// Optional callback invoked for post-acknowledgement lifecycle transitions together with the
    /// correlating MessageId. Implementations must stop invoking the callback when
    /// <paramref name="cancellationToken"/> is cancelled.
    /// </param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>
    /// The command result containing the dispatched MessageId and acknowledgement status
    /// once the initial HTTP round-trip completes.
    /// </returns>
    Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class;
}
