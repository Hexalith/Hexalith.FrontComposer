namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>No-op provider used until an adopter/EventStore status endpoint is registered.</summary>
public sealed class NullPendingCommandStatusQuery : IPendingCommandStatusQuery {
    public ValueTask<PendingCommandOutcomeObservation?> QueryAsync(
        PendingCommandEntry pendingCommand,
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<PendingCommandOutcomeObservation?>(null);
}
