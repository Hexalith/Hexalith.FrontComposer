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

    private sealed class SignalRProjectionHubConnection : IProjectionHubConnection {
        private readonly HubConnection _connection;
        private readonly object _sync = new();
        private readonly List<Func<ProjectionHubConnectionStateChanged, Task>> _stateHandlers = [];

        public SignalRProjectionHubConnection(HubConnection connection) {
            _connection = connection;
            _connection.Reconnecting += exception => PublishAsync(new ProjectionHubConnectionStateChanged(
                ProjectionHubConnectionState.Reconnecting,
                exception));
            _connection.Reconnected += connectionId => PublishAsync(new ProjectionHubConnectionStateChanged(
                ProjectionHubConnectionState.Reconnected,
                ConnectionId: connectionId));
            _connection.Closed += exception => PublishAsync(new ProjectionHubConnectionStateChanged(
                ProjectionHubConnectionState.Closed,
                exception));
        }

        public bool IsConnected => _connection.State == HubConnectionState.Connected;

        public IDisposable OnProjectionChanged(Func<string, string, Task> handler)
            => _connection.On("ProjectionChanged", handler);

        public IDisposable OnConnectionStateChanged(Func<ProjectionHubConnectionStateChanged, Task> handler) {
            ArgumentNullException.ThrowIfNull(handler);
            lock (_sync) {
                _stateHandlers.Add(handler);
            }

            return new Registration(() => {
                lock (_sync) {
                    _ = _stateHandlers.Remove(handler);
                }
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken) {
            await _connection.StartAsync(cancellationToken).ConfigureAwait(false);
            await PublishAsync(new ProjectionHubConnectionStateChanged(ProjectionHubConnectionState.Connected)).ConfigureAwait(false);
        }

        public Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken)
            => _connection.InvokeAsync("JoinGroup", projectionType, tenantId, cancellationToken);

        public Task LeaveGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken)
            => _connection.InvokeAsync("LeaveGroup", projectionType, tenantId, cancellationToken);

        public Task StopAsync(CancellationToken cancellationToken)
            => _connection.StopAsync(cancellationToken);

        public ValueTask DisposeAsync()
            => _connection.DisposeAsync();

        private async Task PublishAsync(ProjectionHubConnectionStateChanged change) {
            Func<ProjectionHubConnectionStateChanged, Task>[] handlers;
            lock (_sync) {
                handlers = [.. _stateHandlers];
            }

            foreach (Func<ProjectionHubConnectionStateChanged, Task> handler in handlers) {
                await handler(change).ConfigureAwait(false);
            }
        }

        private sealed class Registration(Action dispose) : IDisposable {
            public void Dispose() => dispose();
        }
    }
}
