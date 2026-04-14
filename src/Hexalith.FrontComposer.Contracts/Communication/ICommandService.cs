namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Provisional command dispatch contract for the EventStore REST API.
/// Story 1.3 may extend command handling through companion abstractions while
/// keeping this interface stable for existing implementers.
/// </summary>
public interface ICommandService {
    /// <summary>
    /// Dispatches a command for processing.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The command result containing a message ID and status.</returns>
    Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class;
}
