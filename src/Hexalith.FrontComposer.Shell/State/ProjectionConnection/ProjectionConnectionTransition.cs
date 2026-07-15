namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Connection-state transition produced by the EventStore hub wrapper/subscription service.</summary>
/// <param name="Status">New status.</param>
/// <param name="FailureCategory">Bounded non-sensitive category for logging and UI diagnostics.</param>
/// <param name="ReconnectAttempt">Reconnect attempt count, if known.</param>
public sealed record ProjectionConnectionTransition(
    ProjectionConnectionStatus Status,
    string? FailureCategory = null,
    int ReconnectAttempt = 0);
