using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Optional command-service extension that reports typed lifecycle observations.
/// </summary>
public interface ICommandServiceWithLifecycleObservations : ICommandService
{
    /// <summary>Dispatches a command and optionally reports typed lifecycle observations.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="onLifecycleObservation">Optional typed observation callback.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The initial dispatch acknowledgement.</returns>
    Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken = default)
        where TCommand : class;
}
