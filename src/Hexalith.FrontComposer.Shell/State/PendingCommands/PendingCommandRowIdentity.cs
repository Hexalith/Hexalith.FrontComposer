namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Framework-controlled projection row identity cascaded to generated command forms rendered from
/// a generated projection row context.
/// </summary>
/// <remarks>
/// This type deliberately carries projection row metadata only. It must not be populated from raw
/// command payloads or user-editable form values.
/// </remarks>
public sealed record PendingCommandRowIdentity {
    public PendingCommandRowIdentity(
        string projectionTypeName,
        string laneKey,
        string entityKey,
        string? expectedStatusSlot = null,
        string? priorStatusSlot = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionTypeName);
        ArgumentException.ThrowIfNullOrWhiteSpace(laneKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityKey);

        ProjectionTypeName = projectionTypeName;
        LaneKey = laneKey;
        EntityKey = entityKey;
        ExpectedStatusSlot = expectedStatusSlot;
        PriorStatusSlot = priorStatusSlot;
    }

    public string ProjectionTypeName { get; init; }

    public string LaneKey { get; init; }

    public string EntityKey { get; init; }

    public string? ExpectedStatusSlot { get; init; }

    public string? PriorStatusSlot { get; init; }
}
