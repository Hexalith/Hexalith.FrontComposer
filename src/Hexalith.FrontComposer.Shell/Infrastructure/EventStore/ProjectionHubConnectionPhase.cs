namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal enum ProjectionHubConnectionPhase
{
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
}
