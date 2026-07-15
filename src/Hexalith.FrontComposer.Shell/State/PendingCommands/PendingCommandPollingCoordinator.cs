using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

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

    public PendingCommandPollingCoordinator(
        IPendingCommandStateService pendingCommands,
        IPendingCommandOutcomeResolver resolver,
        IPendingCommandStatusQuery statusQuery,
        IOptions<FcShellOptions> options,
        ILogger<PendingCommandPollingCoordinator>? logger = null,
        TimeProvider? timeProvider = null) {
        _pendingCommands = pendingCommands ?? throw new ArgumentNullException(nameof(pendingCommands));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
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

        PendingCommandEntry[] pending = [.. _pendingCommands.Snapshot()
            .Where(static entry => entry.Status == PendingCommandStatus.Pending)
            .OrderBy(static entry => entry.SubmittedAt)
            .Take(budget)];

        int processed = 0;
        foreach (PendingCommandEntry entry in pending) {
            cancellationToken.ThrowIfCancellationRequested();

            // P16 — a live nudge or reconnect reconciliation may have resolved the entry between
            // snapshot and query; re-check the current state to avoid wasted HTTP load.
            PendingCommandEntry? current = _pendingCommands.GetByMessageId(entry.MessageId);
            if (current is null || current.Status != PendingCommandStatus.Pending) {
                continue;
            }

            if (IsExpired(current, _options.Value)) {
                PendingCommandOutcomeResolutionResult expiryResult = _resolver.Resolve(new PendingCommandOutcomeObservation(
                    PendingCommandOutcomeSource.FallbackPolling,
                    PendingCommandTerminalOutcome.NeedsReview,
                    MessageId: current.MessageId,
                    RejectionTitle: "Command needs review",
                    RejectionDetail: "Command status polling reached the configured maximum duration before a terminal EventStore status arrived."));
                if (expiryResult.Status == PendingCommandOutcomeResolutionStatus.Resolved) {
                    processed++;
                }

                continue;
            }

            try {
                PendingCommandOutcomeObservation? observation = await _statusQuery
                    .QueryAsync(current, cancellationToken)
                    .ConfigureAwait(false);

                if (observation is null) {
                    continue;
                }

                PendingCommandOutcomeResolutionResult result = _resolver.Resolve(observation);
                switch (result.Status) {
                    case PendingCommandOutcomeResolutionStatus.Resolved:
                        // P7 — count only successful terminal applications toward the success
                        // tally. Duplicate observations are healthy idempotency, not throughput.
                        processed++;
                        break;
                    case PendingCommandOutcomeResolutionStatus.DuplicateIgnored:
                        _logger.LogDebug(
                            "Pending command polling observed duplicate terminal. MessageId={MessageId}",
                            entry.MessageId);
                        break;
                    default:
                        _logger.LogWarning(
                            "Pending command polling produced non-resolved status. Status={Status} MessageId={MessageId}",
                            result.Status,
                            entry.MessageId);
                        break;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            }
            catch (Exception ex) {
                // Exception objects are intentionally not passed to the logger here; status-query
                // failures can echo transport or payload data through exception messages.
                FrontComposerLog.PendingCommandPollingFailed(_logger, ex.GetType().Name, entry.MessageId);
            }
        }

        return processed;
    }

    private bool IsExpired(PendingCommandEntry entry, FcShellOptions options) {
        TimeSpan elapsed = _timeProvider.GetUtcNow() - entry.SubmittedAt;
        return elapsed.TotalMilliseconds >= options.MaxPendingCommandPollingDurationMs;
    }
}
