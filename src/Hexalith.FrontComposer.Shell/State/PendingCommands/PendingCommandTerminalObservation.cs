namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Terminal observation produced by live nudge refresh, reconnect reconciliation, polling, or status lookup.</summary>
public sealed record PendingCommandTerminalObservation(
    string MessageId,
    PendingCommandTerminalOutcome Outcome,
    string? RejectionTitle = null,
    string? RejectionDetail = null,
    string? RejectionDataImpact = null) {
    public static PendingCommandTerminalObservation Confirmed(string messageId) =>
        new(messageId, PendingCommandTerminalOutcome.Confirmed);

    public static PendingCommandTerminalObservation IdempotentConfirmed(string messageId) =>
        new(messageId, PendingCommandTerminalOutcome.IdempotentConfirmed);

    public static PendingCommandTerminalObservation Rejected(string messageId, string title, string detail, string? dataImpact = null) =>
        new(messageId, PendingCommandTerminalOutcome.Rejected, title, detail, dataImpact);

    public static PendingCommandTerminalObservation NeedsReview(string messageId) =>
        new(messageId, PendingCommandTerminalOutcome.NeedsReview);
}
