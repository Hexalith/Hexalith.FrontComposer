namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Runtime state for a pending command entry.</summary>
public enum PendingCommandStatus {
    Pending,
    Confirmed,
    Rejected,
    IdempotentConfirmed,
    NeedsReview,
}
