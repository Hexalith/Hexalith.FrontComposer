using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Compatibility overloads for <see cref="ICommandService"/>.
/// </summary>
public static class CommandServiceExtensions {
    /// <summary>
    /// Dispatches a command and forwards lifecycle notifications when the implementation supports
    /// <see cref="ICommandServiceWithLifecycle"/>. Implementations that only support the original
    /// <see cref="ICommandService"/> contract still dispatch successfully; the callback is ignored.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="commandService">The command service.</param>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="onLifecycleChange">Optional lifecycle callback.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The acknowledgement result for the dispatch.</returns>
    public static Task<CommandResult> DispatchAsync<TCommand>(
        this ICommandService commandService,
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        if (commandService is null) {
            throw new ArgumentNullException(nameof(commandService));
        }

        return commandService is ICommandServiceWithLifecycle lifecycleAware
            ? lifecycleAware.DispatchAsync(command, onLifecycleChange, cancellationToken)
            : commandService.DispatchAsync(command, cancellationToken);
    }
}
