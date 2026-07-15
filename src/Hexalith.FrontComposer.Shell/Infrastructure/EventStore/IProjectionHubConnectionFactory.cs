namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal interface IProjectionHubConnectionFactory {
    IProjectionHubConnection Create(Uri hubUri, Func<CancellationToken, ValueTask<string?>>? accessTokenProvider);
}
