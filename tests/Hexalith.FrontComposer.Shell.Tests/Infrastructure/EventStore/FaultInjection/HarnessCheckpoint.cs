using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>Lifecycle operation surfaced by <see cref="FaultInjectingProjectionHubConnection"/>.</summary>
internal enum HarnessOperation : byte {
    Start,
    Stop,
    Dispose,
    Join,
    Leave,
    ConnectionState,
    Nudge,
    FallbackTrigger,
}

/// <summary>
/// Strongly-typed checkpoint identifier. Lifecycle checkpoints (<see cref="Start"/>,
/// <see cref="Stop"/>, <see cref="Dispose"/>) are global; <see cref="Join"/>/<see cref="Leave"/>
/// are per-group. The <see cref="Qualifier"/> is the validated <c>"{projectionType}:{tenantId}"</c>
/// segment for group checkpoints and <see langword="null"/> otherwise.
/// </summary>
internal readonly record struct HarnessCheckpoint {
    public HarnessOperation Operation { get; }

    public string? Qualifier { get; }

    private HarnessCheckpoint(HarnessOperation op, string? qualifier) {
        Operation = op;
        Qualifier = qualifier;
    }

    public static HarnessCheckpoint Start { get; } = new(HarnessOperation.Start, null);

    public static HarnessCheckpoint Stop { get; } = new(HarnessOperation.Stop, null);

    public static HarnessCheckpoint Dispose { get; } = new(HarnessOperation.Dispose, null);

    public static HarnessCheckpoint FallbackTrigger { get; } = new(HarnessOperation.FallbackTrigger, null);

    public static HarnessCheckpoint Join(string projectionType, string tenantId)
        => new(HarnessOperation.Join, FormatGroup(projectionType, tenantId));

    public static HarnessCheckpoint Leave(string projectionType, string tenantId)
        => new(HarnessOperation.Leave, FormatGroup(projectionType, tenantId));

    public static HarnessCheckpoint Nudge(string projectionType, string tenantId)
        => new(HarnessOperation.Nudge, FormatGroup(projectionType, tenantId));

    public static HarnessCheckpoint ConnectionState(ProjectionHubConnectionState state)
        => new(HarnessOperation.ConnectionState, state.ToString());

    public static HarnessCheckpoint FallbackTriggerFor(string projectionType, string tenantId)
        => new(HarnessOperation.FallbackTrigger, FormatGroup(projectionType, tenantId));

    public override string ToString()
        => Qualifier is null
            ? Operation.ToString()
            : Operation is HarnessOperation.Join
                or HarnessOperation.Leave
                or HarnessOperation.Nudge
                or HarnessOperation.FallbackTrigger
                    ? $"{Operation}(<group>)"
                    : $"{Operation}({Qualifier})";

    private static string FormatGroup(string projectionType, string tenantId) {
        // Validate fail-closed on blank/colon segments. Diagnostics that include the qualifier
        // remain bounded because both segments are required to be ":"-free, lowercase routing
        // identifiers (synthetic in tests).
        string projection = EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType));
        string tenant = EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId));
        return $"{projection}:{tenant}";
    }
}

/// <summary>
/// Connection-state event identifier used by tests when raising state changes through the
/// harness. Maps 1:1 to <see cref="ProjectionHubConnectionState"/>.
/// </summary>
internal static class HarnessConnectionStates {
    public static ProjectionHubConnectionStateChanged Connected(string? connectionId = null)
        => new(ProjectionHubConnectionState.Connected, ConnectionId: connectionId);

    public static ProjectionHubConnectionStateChanged Reconnecting(Exception? exception = null)
        => new(ProjectionHubConnectionState.Reconnecting, exception);

    public static ProjectionHubConnectionStateChanged Reconnected(string? connectionId = null)
        => new(ProjectionHubConnectionState.Reconnected, ConnectionId: connectionId);

    public static ProjectionHubConnectionStateChanged Closed(Exception? exception = null)
        => new(ProjectionHubConnectionState.Closed, exception);
}
