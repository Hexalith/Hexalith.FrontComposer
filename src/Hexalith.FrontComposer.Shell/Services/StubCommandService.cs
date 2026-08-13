using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

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
/// <see cref="CommandLifecycleState.Confirmed"/> callbacks are raised from a fire-and-forget task. Once dispatch
/// has been accepted, the continuation is owned by the command lifecycle rather than the form cancellation token
/// so navigation cannot discard the accepted outcome (Decisions D5, D6, D8, ADR-010).
/// Story 7-3 Pass 4 DN-7-3-4-2: authorization is wired via <c>AuthorizingCommandServiceDecorator</c>
/// at the DI seam; this concrete impl no longer takes a gate parameter so test factories cannot
/// silently bypass authorization by constructing the impl without the gate.
/// </remarks>
public sealed class StubCommandService : ICommandServiceWithLifecycle, ICommandServiceWithLifecycleObservations {
    private readonly IOptionsSnapshot<StubCommandServiceOptions> _options;
    private readonly IUlidFactory _ulidFactory;
    private readonly ILogger<StubCommandService> _logger;
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="StubCommandService"/> class.</summary>
    public StubCommandService(
        IOptionsSnapshot<StubCommandServiceOptions> options,
        IUlidFactory ulidFactory,
        ILogger<StubCommandService>? logger = null)
        : this(options, ulidFactory, logger, null)
    {
    }

    /// <summary>Initializes a new instance using the specified framework clock.</summary>
    public StubCommandService(
        IOptionsSnapshot<StubCommandServiceOptions> options,
        IUlidFactory ulidFactory,
        ILogger<StubCommandService>? logger,
        TimeProvider? timeProvider) {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _ulidFactory = ulidFactory ?? throw new ArgumentNullException(nameof(ulidFactory));
        _logger = logger ?? NullLogger<StubCommandService>.Instance;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : class
        => DispatchWithObservationsAsync(command, onLifecycleObservation: null, survivePostAcceptCancellation: false, cancellationToken);

    /// <inheritdoc />
    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleState, string?>? onLifecycleChange,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        return await DispatchWithObservationsAsync(
            command,
            observation => onLifecycleChange?.Invoke(observation.State, observation.MessageId),
            survivePostAcceptCancellation: false,
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    Task<CommandResult> ICommandServiceWithLifecycleObservations.DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken)
        where TCommand : class =>
        DispatchWithObservationsAsync(command, onLifecycleObservation, survivePostAcceptCancellation: true, cancellationToken);

    private async Task<CommandResult> DispatchWithObservationsAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        bool survivePostAcceptCancellation,
        CancellationToken cancellationToken)
        where TCommand : class {
        ArgumentNullException.ThrowIfNull(command);

        StubCommandServiceOptions opts = _options.Value;

        if (opts.AcknowledgeDelayMs > 0) {
            await Task.Delay(opts.AcknowledgeDelayMs, cancellationToken).ConfigureAwait(false);
        }

        if (opts.SimulateRejection) {
            string resolution = opts.RejectionResolution ?? "Adjust input and retry";
            throw new CommandRejectedException(
                opts.RejectionReason ?? "Simulated rejection",
                resolution,
                CommandRejectionDetails.FromOptional(
                    opts.RejectionErrorCode,
                    opts.RejectionReasonCategory,
                    opts.RejectionSuggestedAction,
                    opts.RejectionDocsCode,
                    resolution));
        }

        string messageId = _ulidFactory.NewUlid();
        CancellationToken lifecycleToken = survivePostAcceptCancellation
            ? CancellationToken.None
            : cancellationToken;

        // Fire-and-forget continuation. We observe the task via ContinueWith so an unhandled
        // exception inside the user-supplied onLifecycleChange (e.g., disposed Fluxor dispatcher)
        // does not escape as an unobserved task exception. (See code-review 2026-04-15, patch P9.)
        var continuation = Task.Run(
            async () => {
                try {
                    if (opts.SyncingDelayMs > 0) {
                        await Task.Delay(opts.SyncingDelayMs, lifecycleToken).ConfigureAwait(false);
                    }

                    lifecycleToken.ThrowIfCancellationRequested();

                    TryNotifyLifecycleObservation(
                        onLifecycleObservation,
                        CommandLifecycleState.Syncing,
                        CommandMateriality.Unknown,
                        messageId);

                    if (opts.ConfirmDelayMs > 0) {
                        await Task.Delay(opts.ConfirmDelayMs, lifecycleToken).ConfigureAwait(false);
                    }

                    lifecycleToken.ThrowIfCancellationRequested();

                    TryNotifyLifecycleObservation(
                        onLifecycleObservation,
                        CommandLifecycleState.Confirmed,
                        CommandMateriality.Material,
                        messageId);
                }
                catch (OperationCanceledException) {
                    // Form disposed during the callback sequence. Nothing to do.
                }
            },
            lifecycleToken);

        _ = continuation.ContinueWith(
            static (t, state) => {
                if (t.IsFaulted && t.Exception is not null) {
                    FrontComposerWarningLog.StubBackgroundTaskFaulted(
                        (ILogger)state!,
                        t.Exception.Flatten());
                }
            },
            _logger,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        return new CommandResult(messageId, CommandResultStatus.Accepted);
    }

    private void TryNotifyLifecycleObservation(
        Action<CommandLifecycleObservation>? observer,
        CommandLifecycleState state,
        CommandMateriality materiality,
        string messageId)
    {
        try
        {
            observer?.Invoke(new CommandLifecycleObservation(
                state,
                messageId,
                materiality,
                _timeProvider.GetUtcNow()));
        }
        catch (Exception ex)
        {
            FrontComposerWarningLog.StubLifecycleCallbackFailed(_logger, messageId, ex);
        }
    }
}
