using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>Adapts the retained lifecycle callback surface to typed fail-closed observations.</summary>
internal sealed class LegacyLifecycleObservationCommandServiceAdapter : ICommandServiceWithLifecycleObservations {
    private readonly ICommandServiceWithLifecycle _inner;
    private readonly TimeProvider _timeProvider;
    private readonly FcShellOptions _options;
    private readonly ILogger _logger;

    internal LegacyLifecycleObservationCommandServiceAdapter(
        ICommandServiceWithLifecycle inner,
        TimeProvider timeProvider,
        IOptions<FcShellOptions>? options = null,
        ILogger<LegacyLifecycleObservationCommandServiceAdapter>? logger = null) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _options = options?.Value ?? new FcShellOptions();
        _logger = logger ?? NullLogger<LegacyLifecycleObservationCommandServiceAdapter>.Instance;
    }

    /// <inheritdoc />
    public Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : class =>
        _inner.DispatchAsync(command, cancellationToken);

    /// <inheritdoc />
    public async Task<CommandResult> DispatchAsync<TCommand>(
        TCommand command,
        Action<CommandLifecycleObservation>? onLifecycleObservation,
        CancellationToken cancellationToken = default)
        where TCommand : class {
        object gate = new();
        CancellationTokenSource dispatchLifetime = new();
        var preAcceptTerminals = new Queue<CommandLifecycleObservation>();
        int terminalCapacity = Math.Max(1, _options.MaxPendingCommandEntries);
        bool accepted = false;
        string? acceptedMessageId = null;
        bool callbackClosed = false;
        bool preAcceptCancellation = false;
        bool lifetimeDisposed = false;
        ITimer? expiryTimer = null;
        CancellationTokenRegistration callerCancellation = default;

        static bool IsFatalCleanup(Exception exception) =>
            ExceptionGuard.IsFatal(exception)
            || exception is AggregateException aggregate
                && aggregate.Flatten().InnerExceptions.Any(ExceptionGuard.IsFatal);

        static void RunCleanup(Action cleanup) {
            try {
                cleanup();
            }
            catch (Exception ex) when (!IsFatalCleanup(ex)) {
                // Cleanup is best-effort after the adapter has already fixed its terminal state.
                // One failing token callback or disposable must not retain the remaining resources.
            }
        }

        void DisposeLifetime(bool cancel) {
            ITimer? timer;
            CancellationTokenRegistration registration;
            lock (gate) {
                if (lifetimeDisposed) {
                    return;
                }

                lifetimeDisposed = true;
                timer = expiryTimer;
                expiryTimer = null;
                registration = callerCancellation;
                callerCancellation = default;
            }

            if (cancel) {
                RunCleanup(dispatchLifetime.Cancel);
            }

            if (timer is not null) {
                RunCleanup(timer.Dispose);
            }

            RunCleanup(registration.Dispose);
            RunCleanup(dispatchLifetime.Dispose);
        }

        CommandLifecycleObservation CreateObservation(CommandLifecycleState state, string? messageId) {
            DateTimeOffset? observedAt;
            try {
                observedAt = _timeProvider.GetUtcNow();
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                observedAt = null;
            }

            return new CommandLifecycleObservation(
                state,
                messageId,
                CommandMateriality.Unknown,
                observedAt);
        }

        void Deliver(CommandLifecycleObservation observation) {
            try {
                onLifecycleObservation?.Invoke(observation);
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                // A legacy producer owns dispatch completion. A consumer observer failure must
                // not turn an otherwise accepted transport result into a dispatch failure.
            }
        }

        void ExpireAcceptedCallback() {
            lock (gate) {
                if (callbackClosed) {
                    return;
                }

                callbackClosed = true;
                preAcceptTerminals.Clear();
            }

            DisposeLifetime(cancel: true);
        }

        callerCancellation = cancellationToken.Register(() => {
            bool cancel;
            lock (gate) {
                cancel = !accepted && !callbackClosed;
                if (cancel) {
                    preAcceptCancellation = true;
                }
            }

            if (cancel) {
                RunCleanup(dispatchLifetime.Cancel);
            }
        });

        Action<CommandLifecycleState, string?>? callback = onLifecycleObservation is null
            ? null
            : (state, messageId) => {
                bool terminal = state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;
                CommandLifecycleObservation observation = CreateObservation(state, messageId);
                bool deliver;
                bool closeLifetime = false;
                lock (gate) {
                    if (callbackClosed) {
                        return;
                    }

                    if (!terminal) {
                        deliver = true;
                    }
                    else if (!accepted) {
                        while (preAcceptTerminals.Count >= terminalCapacity) {
                            _ = preAcceptTerminals.Dequeue();
                            FrontComposerHotPathLog.PendingOutcomeBufferOverflow(_logger);
                        }

                        preAcceptTerminals.Enqueue(observation);
                        return;
                    }
                    else if (!MessageIdsMatch(messageId, acceptedMessageId)) {
                        return;
                    }
                    else {
                        callbackClosed = true;
                        deliver = true;
                        closeLifetime = true;
                    }
                }

                try {
                    if (deliver) {
                        Deliver(observation);
                    }
                }
                finally {
                    if (closeLifetime) {
                        DisposeLifetime(cancel: false);
                    }
                }
            };

        CommandResult result;
        try {
            result = await _inner.DispatchAsync(
                command,
                callback,
                dispatchLifetime.Token).ConfigureAwait(false);
        }
        catch {
            lock (gate) {
                callbackClosed = true;
                preAcceptTerminals.Clear();
            }

            DisposeLifetime(cancel: true);
            throw;
        }

        CommandLifecycleObservation? acceptedTerminal = null;
        bool canceledBeforeAcceptance;
        bool disposeAfterReturn;
        lock (gate) {
            canceledBeforeAcceptance = preAcceptCancellation;
            accepted = !canceledBeforeAcceptance
                && string.Equals(result.Status, CommandResultStatus.Accepted, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(result.MessageId);
            acceptedMessageId = accepted ? result.MessageId : null;
            if (accepted) {
                while (preAcceptTerminals.TryDequeue(out CommandLifecycleObservation? candidate)) {
                    if (acceptedTerminal is null && MessageIdsMatch(candidate.MessageId, acceptedMessageId)) {
                        acceptedTerminal = candidate;
                    }
                }
            }
            else {
                preAcceptTerminals.Clear();
            }

            disposeAfterReturn = onLifecycleObservation is null || !accepted || acceptedTerminal is not null;
            if (disposeAfterReturn) {
                callbackClosed = true;
            }
        }

        // The caller owns cancellation only until acceptance. After Accepted, a retained legacy
        // callback may complete the lifecycle even if the form/navigation token is canceled.
        callerCancellation.Dispose();
        if (disposeAfterReturn) {
            try {
                if (acceptedTerminal is not null) {
                    Deliver(acceptedTerminal);
                }
            }
            finally {
                DisposeLifetime(cancel: !accepted);
            }
        }
        else {
            ITimer? timer = null;
            try {
                timer = _timeProvider.CreateTimer(
                    static state => ((Action)state!).Invoke(),
                    (Action)ExpireAcceptedCallback,
                    TimeSpan.FromMilliseconds(_options.MaxPendingCommandPollingDurationMs),
                    Timeout.InfiniteTimeSpan);
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                lock (gate) {
                    callbackClosed = true;
                }

                DisposeLifetime(cancel: true);
            }

            if (timer is not null) {
                bool disposeTimer;
                lock (gate) {
                    disposeTimer = callbackClosed || lifetimeDisposed;
                    if (!disposeTimer) {
                        expiryTimer = timer;
                    }
                }

                if (disposeTimer) {
                    RunCleanup(timer.Dispose);
                }
            }
        }

        if (canceledBeforeAcceptance) {
            cancellationToken.ThrowIfCancellationRequested();
        }

        return result;
    }

    private static bool MessageIdsMatch(string? observed, string? accepted) =>
        !string.IsNullOrWhiteSpace(observed)
        && !string.IsNullOrWhiteSpace(accepted)
        && string.Equals(observed.Trim(), accepted.Trim(), StringComparison.OrdinalIgnoreCase);
}
