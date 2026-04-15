namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Command dispatch contract for the EventStore REST API.
/// </summary>
/// <remarks>
/// Rejection is signalled by throwing <see cref="CommandRejectedException"/> rather than via a
/// <see cref="CommandResult"/> status code, so the error path is explicit in the type system.
/// Implementations that can surface post-acknowledgement lifecycle transitions should also
/// implement <see cref="ICommandServiceWithLifecycle"/>. Callers can then use the
/// callback-aware overload exposed by <see cref="CommandServiceExtensions"/>.
/// </remarks>
public interface ICommandService {
    /// <summary>
    /// Dispatches a command for processing.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The command result containing the dispatched MessageId and acknowledgement status.</returns>
    /// <exception cref="CommandRejectedException">Thrown when domain validation rejects the command.</exception>
    Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class;
}
