namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>EventStore projection hub connectivity states surfaced to Shell components.</summary>
public enum ProjectionConnectionStatus {
    Connected,
    Reconnecting,
    Disconnected,
}
