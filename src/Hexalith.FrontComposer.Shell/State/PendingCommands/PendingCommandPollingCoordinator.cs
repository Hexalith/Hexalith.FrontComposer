using Hexalith.FrontComposer.Contracts;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Status-query seam for pending command fallback polling.</summary>
public interface IPendingCommandStatusQuery {
    ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
        PendingCommandEntry pendingCommand,
        CancellationToken cancellationToken = default);
}

/// <summary>No-op provider used until an adopter/EventStore status endpoint is registered.</summary>
public sealed class NullPendingCommandStatusQuery : IPendingCommandStatusQuery {
    public ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
        PendingCommandEntry pendingCommand,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<PendingCommandOutcomeObservation?>(null);
}

/// <summary>Runs bounded pending-command status polling from the existing projection fallback polling loop.</summary>
public interface IPendingCommandPollingCoordinator {
    Task<int> PollOnceAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class PendingCommandPollingCoordinator : IPendingCommandPollingCoordinator {
    private readonly IPendingCommandStateService _pendingCommands;
    private readonly IPendingCommandOutcomeResolver _resolver;
    private readonly IPendingCommandStatusQuery _statusQuery;
    private readonly IOptions<FcShellOptions> _options;
    private readonly ILogger<PendingCommandPollingCoordinator> _logger;

    public PendingCommandPollingCoordinator(
        IPendingCommandStateService pendingCommands,
        IPendingCommandOutcomeResolver resolver,
        IPendingCommandStatusQuery statusQuery,
        IOptions<FcShellOptions> options,
        ILogger<PendingCommandPollingCoordinator>? logger = null) {
        _pendingCommands = pendingCommands ?? throw new ArgumentNullException(nameof(pendingCommands));
        _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        _statusQuery = statusQuery ?? throw new ArgumentNullException(nameof(statusQuery));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? NullLogger<PendingCommandPollingCoordinator>.Instance;
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
            try {
                PendingCommandOutcomeObservation? observation = await _statusQuery
                    .QueryAsync(entry, cancellationToken)
                    .ConfigureAwait(false);

                if (observation is null) {
                    continue;
                }

                PendingCommandOutcomeResolutionResult result = _resolver.Resolve(observation);
                if (result.Status is PendingCommandOutcomeResolutionStatus.Resolved
                    or PendingCommandOutcomeResolutionStatus.DuplicateIgnored) {
                    processed++;
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            }
            catch (Exception ex) when (ex is not OutOfMemoryException) {
                _logger.LogWarning(
                    "Pending command polling failed. FailureCategory={FailureCategory}",
                    ex.GetType().Name);
            }
        }

        return processed;
    }
}
