namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Result code for terminal command resolution.</summary>
public enum PendingCommandResolutionStatus {
    Resolved,
    DuplicateIgnored,
    InvalidMessageId,
    UnknownMessageId,
    LifecycleDispatchFailed,
    Disposed,
}
