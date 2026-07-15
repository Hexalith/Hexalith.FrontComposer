namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Registration metadata for an accepted command. This intentionally excludes raw command payloads,
/// form values, tenant IDs, user IDs, and validation messages.
/// </summary>
/// <remarks>
/// P11 — primary-constructor validation: <c>CorrelationId</c>, <c>MessageId</c>, and
/// <c>CommandTypeName</c> are required non-null/non-whitespace. Failure surfaces at registration
/// rather than deeper in the resolver.
/// </remarks>
public sealed record PendingCommandRegistration {
    public PendingCommandRegistration(
        string CorrelationId,
        string MessageId,
        string CommandTypeName,
        string? ProjectionTypeName = null,
        string? LaneKey = null,
        string? EntityKey = null,
        string? ExpectedStatusSlot = null,
        string? PriorStatusSlot = null,
        DateTimeOffset? SubmittedAt = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(CorrelationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(MessageId);
        ArgumentException.ThrowIfNullOrWhiteSpace(CommandTypeName);
        this.CorrelationId = CorrelationId;
        this.MessageId = MessageId;
        this.CommandTypeName = CommandTypeName;
        this.ProjectionTypeName = ProjectionTypeName;
        this.LaneKey = LaneKey;
        this.EntityKey = EntityKey;
        this.ExpectedStatusSlot = ExpectedStatusSlot;
        this.PriorStatusSlot = PriorStatusSlot;
        this.SubmittedAt = SubmittedAt;
    }

    public string CorrelationId { get; init; }

    public string MessageId { get; init; }

    public string CommandTypeName { get; init; }

    public string? ProjectionTypeName { get; init; }

    public string? LaneKey { get; init; }

    public string? EntityKey { get; init; }

    public string? ExpectedStatusSlot { get; init; }

    public string? PriorStatusSlot { get; init; }

    public DateTimeOffset? SubmittedAt { get; init; }
}
