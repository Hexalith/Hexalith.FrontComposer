using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <inheritdoc />
public sealed class PendingCommandPollingCoordinator : IPendingCommandPollingCoordinator {
    private readonly IPendingCommandStateService _pendingCommands;
    private readonly IPendingCommandOutcomeResolver _resolver;
    private readonly IPendingCommandStatusQuery _statusQuery;
    private readonly IOptions<FcShellOptions> _options;
    private readonly ILogger<PendingCommandPollingCoordinator> _logger;
    private readonly TimeProvider _timeProvider;
    private readonly object _queryGate = new();
    private readonly HashSet<PendingCommandEntry> _queriesInFlight = new(ReferenceEqualityComparer.Instance);
    private int _singleSlotTurn;

    public PendingCommandPollingCoordinator(
        IPendingCommandStateService pendingCommands,
        IPendingCommandOutcomeResolver resolver,
        IPendingCommandStatusQuery statusQuery,
        IOptions<FcShellOptions> options,
        ILogger<PendingCommandPollingCoordinator>? logger = null,
        TimeProvider? timeProvider = null) {
        _pendingCommands = pendingCommands ?? throw new ArgumentNullException(nameof(pendingCommands));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        if (_resolver is not IPendingCommandCapturedEntryResolver
            || _resolver is PendingCommandOutcomeResolver concrete && !concrete.SupportsCapturedEntryResolution) {
            throw new InvalidOperationException($"Polling requires {nameof(IPendingCommandCapturedEntryResolver)} so terminal outcomes can be applied to the captured pending entry.");
        }
        _statusQuery = statusQuery ?? throw new ArgumentNullException(nameof(statusQuery));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<PendingCommandPollingCoordinator>.Instance;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    /// <inheritdoc />
    public async Task<int> PollOnceAsync(CancellationToken cancellationToken = default) {
        int budget = Math.Max(0, _options.Value.MaxPendingCommandPollingPerTick);
        if (budget == 0) {
            return 0;
        }

        int processed = 0;
        bool hasPendingTransport = _pendingCommands.Snapshot()
            .Any(static entry => entry.Status == PendingCommandStatus.Pending);
        if (_pendingCommands is PendingCommandStateService concreteState) {
            // With one slot, reserving a transport slot on every tick strands
            // terminal lifecycle convergence. Alternate priority; an empty
            // convergence turn falls through to transport without wasting a slot.
            int convergenceBudget = hasPendingTransport
                ? budget == 1
                    ? (Interlocked.Increment(ref _singleSlotTurn) & 1) == 0 ? 1 : 0
                    : budget - 1
                : budget;
            (int attempts, int converged) = concreteState.ConvergeLifecycle(convergenceBudget);
            budget -= attempts;
            processed += converged;
            if (budget == 0) {
                return processed;
            }
        }

        PendingCommandEntry[] pending = [.. _pendingCommands.Snapshot()
            .Where(static entry => entry.Status == PendingCommandStatus.Pending)
            .Where(entry => !IsQueryInFlight(entry))
            .OrderBy(static entry => entry.SubmittedAt)
            .Take(budget)];

        // Independent bounded queries may settle in any order. A held oldest query must not
        // delay a later entry's eligible check or its own acceptance deadline.
        int[] outcomes = await Task.WhenAll(pending.Select(entry => PollEntryAsync(entry, cancellationToken)))
            .ConfigureAwait(false);
        return processed + outcomes.Sum();
    }

    private async Task<int> PollEntryAsync(PendingCommandEntry entry, CancellationToken cancellationToken) {
        lock (_queryGate) {
            if (!_queriesInFlight.Add(entry)) {
                return 0;
            }
        }
        try {
            return await PollOwnedEntryAsync(entry, cancellationToken).ConfigureAwait(false);
        }
        finally {
            lock (_queryGate) {
                _queriesInFlight.Remove(entry);
            }
        }
    }

    private async Task<int> PollOwnedEntryAsync(PendingCommandEntry entry, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();

        // The snapshot entry owns this poll. Reusing its MessageId after scope reset cannot
        // make an old query, timeout, or pre-query decision act on a replacement.
        if (!IsCurrentPending(entry)) {
            return 0;
        }

        if (IsExpired(entry, _options.Value)) {
            PendingCommandOutcomeResolutionResult expiryResult = ResolveForEntry(entry, new PendingCommandOutcomeObservation(
                    PendingCommandOutcomeSource.FallbackPolling,
                    PendingCommandTerminalOutcome.DegradedExhausted,
                    MessageId: entry.MessageId,
                    RejectionDetail: "Command status polling reached the configured maximum duration before a terminal EventStore status arrived."));
            return expiryResult.Status == PendingCommandOutcomeResolutionStatus.Resolved ? 1 : 0;
        }

        try {
            PendingCommandOutcomeObservation? observation = await QueryBeforeDeadlineAsync(entry, cancellationToken)
                .ConfigureAwait(false);

            if (!IsCurrentPending(entry)) {
                return 0;
            }
            if (IsExpired(entry, _options.Value)) {
                PendingCommandOutcomeResolutionResult exhausted = ResolveForEntry(entry, new PendingCommandOutcomeObservation(
                        PendingCommandOutcomeSource.FallbackPolling,
                        PendingCommandTerminalOutcome.DegradedExhausted,
                        MessageId: entry.MessageId));
                return exhausted.Status == PendingCommandOutcomeResolutionStatus.Resolved ? 1 : 0;
            }

            if (observation is null) {
                return 0;
            }

            PendingCommandOutcomeResolutionResult result = ResolveForEntry(entry, observation);
            switch (result.Status) {
                case PendingCommandOutcomeResolutionStatus.Resolved:
                    return 1;
                case PendingCommandOutcomeResolutionStatus.DuplicateIgnored:
                    FrontComposerHotPathLog.PendingPollDuplicateTerminal(_logger, entry.MessageId);
                    break;
                default:
                    FrontComposerHotPathLog.PendingPollNonResolved(_logger, result.Status, entry.MessageId);
                    break;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        }
        catch (TimeoutException) when (IsExpired(entry, _options.Value)) {
            if (IsCurrentPending(entry)) {
                PendingCommandOutcomeResolutionResult exhausted = ResolveForEntry(entry, new PendingCommandOutcomeObservation(
                    PendingCommandOutcomeSource.FallbackPolling,
                    PendingCommandTerminalOutcome.DegradedExhausted,
                    MessageId: entry.MessageId));
                return exhausted.Status == PendingCommandOutcomeResolutionStatus.Resolved ? 1 : 0;
            }
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            // Exception objects are intentionally not passed to the logger here; status-query
            // failures can echo transport or payload data through exception messages.
            FrontComposerLog.PendingCommandPollingFailed(_logger, ex.GetType().Name, entry.MessageId);
        }

        return 0;
    }

    private bool IsCurrentPending(PendingCommandEntry entry)
        => ReferenceEquals(_pendingCommands.GetByMessageId(entry.MessageId), entry)
            && entry.Status == PendingCommandStatus.Pending;

    private bool IsQueryInFlight(PendingCommandEntry entry) {
        lock (_queryGate) {
            return _queriesInFlight.Contains(entry);
        }
    }

    private PendingCommandOutcomeResolutionResult ResolveForEntry(
        PendingCommandEntry entry, PendingCommandOutcomeObservation observation) {
        if (!IsCurrentPending(entry)) {
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
        }

        return ((IPendingCommandCapturedEntryResolver)_resolver).ResolveForEntry(observation, entry);
    }

    private bool IsExpired(PendingCommandEntry entry, FcShellOptions options) {
        TimeSpan elapsed = _timeProvider.GetUtcNow() - entry.SubmittedAt;
        return elapsed.TotalMilliseconds >= options.MaxPendingCommandPollingDurationMs;
    }

    private async Task<PendingCommandOutcomeObservation?> QueryBeforeDeadlineAsync(
        PendingCommandEntry entry, CancellationToken cancellationToken) {
        CancellationTokenSource queryCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task<PendingCommandOutcomeObservation?>? query = null;
        try {
            query = _statusQuery.QueryAsync(entry, queryCancellation.Token).AsTask();
            TimeSpan remaining = TimeSpan.FromMilliseconds(_options.Value.MaxPendingCommandPollingDurationMs)
                - (_timeProvider.GetUtcNow() - entry.SubmittedAt);
            if (remaining <= TimeSpan.Zero) {
                throw new TimeoutException();
            }
            return await query.WaitAsync(remaining, _timeProvider, cancellationToken).ConfigureAwait(false);
        }
        finally {
            // Provider cancellation callbacks may block. End local polling at the deadline and
            // retire cleanup ownership after a bounded wait even if abandoned work never settles.
            Task cleanup = CompleteQueryCleanupAsync(queryCancellation, query);
            _ = cleanup.ContinueWith(static completed => { _ = completed.Exception; },
                CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
    }

    private async Task CompleteQueryCleanupAsync(CancellationTokenSource cancellation, Task? query) {
        ObserveLateFault(query);
        try {
            Task cancellationTask = cancellation.CancelAsync();
            ObserveLateFault(cancellationTask);
            try {
                await cancellationTask.WaitAsync(TimeSpan.FromSeconds(1), _timeProvider).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)
                && cancellationTask.Exception?.Flatten().InnerExceptions.Any(ExceptionGuard.IsFatal) != true) {
                // Abandoned transport evidence cannot replace a closed local outcome.
            }
        }
        finally {
            cancellation.Dispose();
        }
    }

    private static void ObserveLateFault(Task? task) {
        if (task is null) {
            return;
        }
        _ = task.ContinueWith(static completed => { _ = completed.Exception; },
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }
}
