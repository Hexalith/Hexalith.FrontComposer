namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal enum ProjectionHubConnectionState {
    Connected,
    Reconnecting,
    Reconnected,
    Closed,
}
