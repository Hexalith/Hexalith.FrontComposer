namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Runs bounded pending-command status polling from the existing projection fallback polling loop.</summary>
public interface IPendingCommandPollingCoordinator {
    Task<int> PollOnceAsync(CancellationToken cancellationToken = default);
}
