namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Immutable public snapshot of the current EventStore projection connection state.</summary>
/// <param name="Status">Current connection status.</param>
/// <param name="LastTransitionAt">UTC timestamp of the latest transition.</param>
/// <param name="ReconnectAttempt">Current reconnect attempt count, if known.</param>
/// <param name="LastFailureCategory">Bounded non-sensitive failure category.</param>
public sealed record ProjectionConnectionSnapshot(
    ProjectionConnectionStatus Status,
    DateTimeOffset LastTransitionAt,
    int ReconnectAttempt,
    string? LastFailureCategory) {
    /// <summary>Gets a value indicating whether realtime projection nudges are unavailable.</summary>
    public bool IsDisconnected => Status is ProjectionConnectionStatus.Reconnecting or ProjectionConnectionStatus.Disconnected;
}
