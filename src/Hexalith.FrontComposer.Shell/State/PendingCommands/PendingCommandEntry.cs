namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Immutable snapshot of a circuit-local pending command.</summary>
public sealed record PendingCommandEntry(
    string CorrelationId,
    string MessageId,
    string CommandTypeName,
    string? ProjectionTypeName,
    string? LaneKey,
    string? EntityKey,
    string? ExpectedStatusSlot,
    string? PriorStatusSlot,
    DateTimeOffset SubmittedAt,
    PendingCommandStatus Status,
    string? RejectionTitle = null,
    string? RejectionDetail = null,
    string? RejectionDataImpact = null,
    DateTimeOffset? TerminalAt = null,
    int DuplicateTerminalObservations = 0) {
    internal bool HasSameFrameworkMetadata(PendingCommandRegistration registration) =>
        string.Equals(CorrelationId, registration.CorrelationId, StringComparison.Ordinal)
        && string.Equals(CommandTypeName, registration.CommandTypeName, StringComparison.Ordinal)
        && string.Equals(ProjectionTypeName, registration.ProjectionTypeName, StringComparison.Ordinal)
        && string.Equals(LaneKey, registration.LaneKey, StringComparison.Ordinal)
        && string.Equals(EntityKey, registration.EntityKey, StringComparison.Ordinal)
        && string.Equals(ExpectedStatusSlot, registration.ExpectedStatusSlot, StringComparison.Ordinal)
        && string.Equals(PriorStatusSlot, registration.PriorStatusSlot, StringComparison.Ordinal);
}
