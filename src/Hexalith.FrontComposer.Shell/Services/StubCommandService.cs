using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// In-memory <see cref="ICommandServiceWithLifecycle"/> that simulates the full 5-state lifecycle without contacting a real EventStore.
/// </summary>
/// <remarks>
/// The initial <see cref="Task.Delay(int, CancellationToken)"/> models the HTTP round-trip and returns a
/// <see cref="CommandResult"/>; subsequent <see cref="CommandLifecycleState.Syncing"/> and
/// <see cref="CommandLifecycleState.Confirmed"/> callbacks are raised from a fire-and-forget task that observes
/// the provided <see cref="CancellationToken"/> so the form can cancel in-flight callbacks on dispose
/// (Decisions D5, D6, D8, ADR-010).
/// </remarks>
public sealed class StubCommandService : ICommandServiceWithLifecycle {
    private readonly IOptionsSnapshot<StubCommandServiceOptions> _options;

    /// <summary>Initializes a new instance of the <see cref="StubCommandService"/> class.</summary>
    public StubCommandService(IOptionsSnapshot<StubCommandServiceOptions> options) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchAsync(command, onLifecycleChange: null, cancellationToken);

    /// <inheritdoc />
    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        ArgumentNullException.ThrowIfNull(command);

        StubCommandServiceOptions opts = _options.Value;

        if (opts.AcknowledgeDelayMs > 0) {
            await Task.Delay(opts.AcknowledgeDelayMs, cancellationToken).ConfigureAwait(false);
        }

        if (opts.SimulateRejection) {
            throw new CommandRejectedException(
                opts.RejectionReason ?? "Simulated rejection",
                opts.RejectionResolution ?? "Adjust input and retry");
        }

        string messageId = Guid.NewGuid().ToString();

        _ = Task.Run(
            async () => {
                try {
                    if (opts.SyncingDelayMs > 0) {
                        await Task.Delay(opts.SyncingDelayMs, cancellationToken).ConfigureAwait(false);
                    }

                    if (cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    onLifecycleChange?.Invoke(CommandLifecycleState.Syncing, messageId);

                    if (opts.ConfirmDelayMs > 0) {
                        await Task.Delay(opts.ConfirmDelayMs, cancellationToken).ConfigureAwait(false);
                    }

                    if (cancellationToken.IsCancellationRequested) {
                        return;
                    }

                    onLifecycleChange?.Invoke(CommandLifecycleState.Confirmed, messageId);
                }
                catch (OperationCanceledException) {
                    // Form disposed during the callback sequence. Nothing to do.
                }
            },
            cancellationToken);

        return new CommandResult(messageId, "Accepted");
    }
}
