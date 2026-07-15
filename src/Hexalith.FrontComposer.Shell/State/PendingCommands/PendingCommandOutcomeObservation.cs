namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Transport-neutral terminal outcome metadata. Raw command payloads and form values must never be
/// used to populate this record.
/// </summary>
public sealed record PendingCommandOutcomeObservation(
    PendingCommandOutcomeSource Source,
    PendingCommandTerminalOutcome Outcome,
    string? MessageId = null,
    string? ProjectionTypeName = null,
    string? LaneKey = null,
    string? EntityKey = null,
    string? ExpectedStatusSlot = null,
    string? RejectionTitle = null,
    string? RejectionDetail = null,
    string? RejectionDataImpact = null,
    DateTimeOffset? ObservedAt = null);
