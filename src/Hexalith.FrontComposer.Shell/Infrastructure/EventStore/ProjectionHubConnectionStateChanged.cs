namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal sealed record ProjectionHubConnectionStateChanged(
    ProjectionHubConnectionState State,
    Exception? Exception = null,
    string? ConnectionId = null);
