namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Shared resolver status across live, reconnect, polling, and status-query inputs.</summary>
public enum PendingCommandOutcomeResolutionStatus {
    Resolved = 0,
    DuplicateIgnored = 1,
    Unknown = 2,
    InvalidMessageId = 3,
    AmbiguousMatch = 4,
    LifecycleDispatchFailed = 5,
    Buffered = 6,
}
