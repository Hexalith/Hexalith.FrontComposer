namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Result returned by the shared pending-command outcome resolver.</summary>
public sealed record PendingCommandOutcomeResolutionResult(
    PendingCommandOutcomeResolutionStatus Status,
    PendingCommandEntry? Entry = null) {
    public static PendingCommandOutcomeResolutionResult From(PendingCommandResolutionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch {
            PendingCommandResolutionStatus.Resolved => new(PendingCommandOutcomeResolutionStatus.Resolved, result.Entry),
            PendingCommandResolutionStatus.DuplicateIgnored => new(PendingCommandOutcomeResolutionStatus.DuplicateIgnored, result.Entry),
            PendingCommandResolutionStatus.InvalidMessageId => new(PendingCommandOutcomeResolutionStatus.InvalidMessageId),
            PendingCommandResolutionStatus.LifecycleDispatchFailed => new(PendingCommandOutcomeResolutionStatus.LifecycleDispatchFailed, result.Entry),
            _ => new(PendingCommandOutcomeResolutionStatus.Unknown),
        };
    }
}
