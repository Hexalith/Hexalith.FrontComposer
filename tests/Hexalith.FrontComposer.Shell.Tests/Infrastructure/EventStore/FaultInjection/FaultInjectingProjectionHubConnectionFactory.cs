using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Factory adapter that hands out a single pre-configured
/// <see cref="FaultInjectingProjectionHubConnection"/> instance. Validates the requested hub URL
/// against the expected URL so tests cannot silently route through the wrong configuration.
/// </summary>
internal sealed class FaultInjectingProjectionHubConnectionFactory : IProjectionHubConnectionFactory {
    private readonly FaultInjectingProjectionHubConnection _connection;
    private readonly Uri? _expectedHubUri;

    public FaultInjectingProjectionHubConnectionFactory(
        FaultInjectingProjectionHubConnection connection,
        Uri? expectedHubUri = null) {
        ArgumentNullException.ThrowIfNull(connection);
        _connection = connection;
        _expectedHubUri = expectedHubUri;
    }

    public bool AccessTokenProviderRequested { get; private set; }

    public IProjectionHubConnection Create(Uri hubUri, Func<CancellationToken, ValueTask<string?>>? accessTokenProvider) {
        ArgumentNullException.ThrowIfNull(hubUri);
        if (_expectedHubUri is not null && _expectedHubUri != hubUri) {
            throw new InvalidOperationException(
                $"Hub URI mismatch. Expected '{_expectedHubUri}', got '{hubUri}'.");
        }

        AccessTokenProviderRequested = accessTokenProvider is not null;
        return _connection;
    }
}
