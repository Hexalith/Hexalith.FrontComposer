namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Shared resolver status across live, reconnect, polling, and status-query inputs.</summary>
public enum PendingCommandOutcomeResolutionStatus {
    Resolved,
    DuplicateIgnored,
    Unknown,
    InvalidMessageId,
    AmbiguousMatch,
    LifecycleDispatchFailed,
}
