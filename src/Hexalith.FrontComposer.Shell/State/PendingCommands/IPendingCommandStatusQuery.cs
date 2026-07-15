namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Status-query seam for pending command fallback polling.</summary>
public interface IPendingCommandStatusQuery {
    ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
        PendingCommandEntry pendingCommand,
        CancellationToken cancellationToken = default);
}
