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
    /// <summary>Stable identity for a disconnect and its recovery.</summary>
    public long Epoch { get; init; }

    /// <summary>Gets a value indicating whether the browser reports that it is offline.</summary>
    public bool BrowserOffline { get; init; }

    /// <summary>Gets a value indicating whether realtime projection nudges are unavailable.</summary>
    public bool IsDisconnected => BrowserOffline || Status is ProjectionConnectionStatus.Reconnecting or ProjectionConnectionStatus.Disconnected;
}
