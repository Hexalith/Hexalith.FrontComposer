namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Terminal observation type accepted by the pending-command resolver.</summary>
public enum PendingCommandTerminalOutcome {
    Confirmed,
    Rejected,
    IdempotentConfirmed,
    NeedsReview,
}
