namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Shared pending-command outcome resolver used by nudge, reconnect, polling, and status-query paths.</summary>
public interface IPendingCommandOutcomeResolver {
    PendingCommandOutcomeResolutionResult Resolve(PendingCommandOutcomeObservation observation);
}
