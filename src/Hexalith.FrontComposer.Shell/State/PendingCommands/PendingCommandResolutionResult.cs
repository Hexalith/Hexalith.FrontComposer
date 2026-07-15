namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Terminal resolution result.</summary>
public sealed record PendingCommandResolutionResult(
    PendingCommandResolutionStatus Status,
    PendingCommandEntry? Entry = null) {
    public static PendingCommandResolutionResult Resolved(PendingCommandEntry entry) =>
        new(PendingCommandResolutionStatus.Resolved, entry);

    public static PendingCommandResolutionResult DuplicateIgnored(PendingCommandEntry entry) =>
        new(PendingCommandResolutionStatus.DuplicateIgnored, entry);

    public static PendingCommandResolutionResult InvalidMessageId() =>
        new(PendingCommandResolutionStatus.InvalidMessageId);

    public static PendingCommandResolutionResult UnknownMessageId() =>
        new(PendingCommandResolutionStatus.UnknownMessageId);

    public static PendingCommandResolutionResult LifecycleDispatchFailed(PendingCommandEntry entry) =>
        new(PendingCommandResolutionStatus.LifecycleDispatchFailed, entry);

    public static PendingCommandResolutionResult Disposed() =>
        new(PendingCommandResolutionStatus.Disposed);
}
