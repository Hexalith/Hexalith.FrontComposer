using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Adapts the retained lifecycle callback surface to typed fail-closed observations.</summary>
internal sealed class LegacyLifecycleObservationCommandServiceAdapter(
    ICommandServiceWithLifecycle inner,
    TimeProvider timeProvider) : ICommandServiceWithLifecycleObservations {
    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class =>
        inner.DispatchAsync(command, cancellationToken);

    /// <inheritdoc />
    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        object gate = new();
        CancellationTokenSource dispatchLifetime = new();
        bool accepted = false;
        bool terminalObserved = false;
        CancellationTokenRegistration callerCancellation = cancellationToken.Register(() => {
            bool cancel;
            lock (gate) {
                cancel = !accepted;
            }

            if (cancel) {
                dispatchLifetime.Cancel();
            }
        });

        Action<CommandLifecycleState, string?>? callback = onLifecycleObservation is null
            ? null
            : (state, messageId) => {
                try {
                    onLifecycleObservation(new CommandLifecycleObservation(
                        state,
                        messageId,
                        CommandMateriality.Unknown,
                        timeProvider.GetUtcNow()));
                }
                catch (Exception) {
                    // A legacy producer owns dispatch completion. A consumer observer or shell
                    // clock failure must not turn an otherwise accepted transport result into a
                    // dispatch failure.
                }
                finally {
                    if (state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected) {
                        bool disposeLifetime;
                        lock (gate) {
                            terminalObserved = true;
                            disposeLifetime = accepted;
                        }

                        if (disposeLifetime) {
                            dispatchLifetime.Dispose();
                        }
                    }
                }
            };

        CommandResult result;
        try {
            result = await inner.DispatchAsync(
                command,
                callback,
                dispatchLifetime.Token).ConfigureAwait(false);
        }
        catch {
            callerCancellation.Dispose();
            dispatchLifetime.Dispose();
            throw;
        }

        bool disposeAfterReturn;
        lock (gate) {
            accepted = string.Equals(result.Status, CommandResultStatus.Accepted, StringComparison.OrdinalIgnoreCase);
            disposeAfterReturn = !accepted || terminalObserved;
        }

        // The caller owns cancellation only until acceptance. After Accepted, a retained legacy
        // callback may complete the lifecycle even if the form/navigation token is canceled.
        callerCancellation.Dispose();
        if (!accepted) {
            dispatchLifetime.Cancel();
        }

        if (disposeAfterReturn) {
            dispatchLifetime.Dispose();
        }

        return result;
    }
}
