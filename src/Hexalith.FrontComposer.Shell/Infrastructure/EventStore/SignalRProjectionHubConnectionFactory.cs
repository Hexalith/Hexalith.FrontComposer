using Microsoft.AspNetCore.SignalR.Client;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal sealed class SignalRProjectionHubConnectionFactory : IProjectionHubConnectionFactory {
    public IProjectionHubConnection Create(Uri hubUri, Func<CancellationToken, ValueTask<string?>>? accessTokenProvider) {
        HubConnectionBuilder builder = new();
        _ = builder.WithUrl(hubUri, options => {
            if (accessTokenProvider is not null) {
                options.AccessTokenProvider = async () => await accessTokenProvider(CancellationToken.None).ConfigureAwait(false);
            }
        });
        _ = builder.WithAutomaticReconnect();
        return new SignalRProjectionHubConnection(builder.Build());
    }

    private sealed class SignalRProjectionHubConnection(HubConnection connection) : IProjectionHubConnection {
        public bool IsConnected => connection.State == HubConnectionState.Connected;

        public IDisposable OnProjectionChanged(Func<string, string, Task> handler)
            => connection.On("ProjectionChanged", handler);

        public Task StartAsync(CancellationToken cancellationToken)
            => connection.StartAsync(cancellationToken);

        public Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken)
            => connection.InvokeAsync("JoinGroup", projectionType, tenantId, cancellationToken);

        public Task LeaveGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken)
            => connection.InvokeAsync("LeaveGroup", projectionType, tenantId, cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken)
            => connection.StopAsync(cancellationToken);

        public ValueTask DisposeAsync()
            => connection.DisposeAsync();
    }
}
