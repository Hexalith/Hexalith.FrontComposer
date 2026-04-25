namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal interface IProjectionHubConnection : IAsyncDisposable {
    bool IsConnected { get; }

    IDisposable OnProjectionChanged(Func<string, string, Task> handler);

    Task StartAsync(CancellationToken cancellationToken);

    Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken);

    Task LeaveGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken);

    Task StopAsync(CancellationToken cancellationToken);
}

internal interface IProjectionHubConnectionFactory {
    IProjectionHubConnection Create(Uri hubUri, Func<CancellationToken, ValueTask<string?>>? accessTokenProvider);
}
