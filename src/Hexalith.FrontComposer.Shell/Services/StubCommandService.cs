using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
    private readonly IUlidFactory _ulidFactory;
    private readonly ILogger<StubCommandService> _logger;

    /// <summary>Initializes a new instance of the <see cref="StubCommandService"/> class.</summary>
    public StubCommandService(
        IOptionsSnapshot<StubCommandServiceOptions> options,
        IUlidFactory ulidFactory,
        ILogger<StubCommandService>? logger = null) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ulidFactory = ulidFactory ?? throw new ArgumentNullException(nameof(ulidFactory));
        _logger = logger ?? NullLogger<StubCommandService>.Instance;
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

        string messageId = _ulidFactory.NewUlid();

        // Fire-and-forget continuation. We observe the task via ContinueWith so an unhandled
        // exception inside the user-supplied onLifecycleChange (e.g., disposed Fluxor dispatcher)
        // does not escape as an unobserved task exception. (See code-review 2026-04-15, patch P9.)
        Task continuation = Task.Run(
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
                catch (Exception ex) {
                    _logger.LogError(
                        ex,
                        "Lifecycle callback threw for MessageId={MessageId}. Syncing/Confirmed notifications were skipped.",
                        messageId);
                }
            },
            cancellationToken);

        _ = continuation.ContinueWith(
            static (t, state) => {
                if (t.IsFaulted && t.Exception is not null) {
                    ((ILogger)state!).LogError(t.Exception.Flatten(), "StubCommandService background task faulted.");
                }
            },
            _logger,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return new CommandResult(messageId, "Accepted");
    }
}
