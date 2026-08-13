using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Compatibility overloads for <see cref="ICommandService"/>.
/// </summary>
public static class CommandServiceExtensions {
    /// <summary>
    /// Dispatches a command and forwards typed lifecycle observations when the implementation
    /// supports <see cref="ICommandServiceWithLifecycleObservations"/>.
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="commandService">The command service.</param>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="onLifecycleObservation">Optional typed lifecycle observation callback.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The acknowledgement result for the dispatch.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when a non-null callback is supplied but the service cannot report typed observations.
    /// </exception>
    public static Task<CommandResult> DispatchWithLifecycleObservationsAsync<TCommand>(
        this ICommandService commandService,
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken = default)
        where TCommand : class {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(commandService);
#else
        if (commandService is null) {
            throw new ArgumentNullException(nameof(commandService));
        }
#endif

        if (commandService is ICommandServiceWithLifecycleObservations observationAware) {
            return observationAware.DispatchAsync(command, onLifecycleObservation, cancellationToken);
        }

        if (commandService is ICommandServiceWithLifecycle lifecycleAware) {
            return lifecycleAware.DispatchAsync(
                command,
                onLifecycleObservation is null
                    ? null
                    : (state, messageId) => onLifecycleObservation(new CommandLifecycleObservation(
                        state,
                        messageId,
                        CommandMateriality.Unknown)),
                cancellationToken);
        }

        if (onLifecycleObservation is not null) {
            throw new NotSupportedException(
                $"The registered {nameof(ICommandService)} implementation '{commandService.GetType().FullName}' "
                + $"does not implement {nameof(ICommandServiceWithLifecycle)} or {nameof(ICommandServiceWithLifecycleObservations)}; "
                + "typed lifecycle observations cannot be forwarded.");
        }

        return commandService.DispatchAsync(command, cancellationToken);
    }

    /// <summary>
    /// Dispatches a command and forwards lifecycle notifications when the implementation supports
    /// <see cref="ICommandServiceWithLifecycle"/>. When <paramref name="onLifecycleChange"/> is
    /// <see langword="null"/>, any <see cref="ICommandService"/> implementation is accepted.
    /// When a callback is supplied but the implementation does not implement
    /// <see cref="ICommandServiceWithLifecycle"/>, this method throws to prevent silently losing
    /// Syncing/Confirmed notifications (ADR-010 loud-fail contract).
    /// </summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <param name="commandService">The command service.</param>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="onLifecycleChange">Optional lifecycle callback.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The acknowledgement result for the dispatch.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown when a non-null <paramref name="onLifecycleChange"/> is supplied but
    /// <paramref name="commandService"/> does not implement <see cref="ICommandServiceWithLifecycle"/>.
    /// </exception>
    public static Task<CommandResult> DispatchAsync<TCommand>(
        this ICommandService commandService,
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
#if NET10_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(commandService);
#else
        if (commandService is null) {
            throw new ArgumentNullException(nameof(commandService));
        }
#endif

        if (commandService is ICommandServiceWithLifecycle lifecycleAware) {
            return lifecycleAware.DispatchAsync(command, onLifecycleChange, cancellationToken);
        }

        if (onLifecycleChange is not null) {
            throw new NotSupportedException(
                $"The registered {nameof(ICommandService)} implementation '{commandService.GetType().FullName}' "
                + $"does not implement {nameof(ICommandServiceWithLifecycle)}; "
                + "lifecycle callbacks cannot be forwarded. Register an implementation that supports lifecycle "
                + "callbacks or invoke DispatchAsync without the onLifecycleChange argument.");
        }

        return commandService.DispatchAsync(command, cancellationToken);
    }
}
