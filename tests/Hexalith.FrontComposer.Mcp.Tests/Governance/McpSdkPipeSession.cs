using System.IO.Pipelines;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// Runs the SDK server built from a FrontComposer service collection over in-memory pipes, with an SDK client
/// connected to it. The server and its transport log through the same Trace-level factory as FrontComposer, so a
/// leak assertion covers SDK categories as well.
/// </summary>
internal sealed class McpSdkPipeSession : IAsyncDisposable {
    private readonly ServiceProvider _provider;
    private readonly StreamServerTransport _serverTransport;
    private readonly McpServer _server;
    private readonly CancellationTokenSource _timeout;
    private readonly Task _serverTask;
    private McpClient? _client;

    private McpSdkPipeSession(
        ServiceProvider provider,
        StreamServerTransport serverTransport,
        McpServer server,
        CancellationTokenSource timeout) {
        _provider = provider;
        _serverTransport = serverTransport;
        _server = server;
        _timeout = timeout;
        _serverTask = server.RunAsync(timeout.Token);
    }

    /// <summary>Gets the connected SDK client.</summary>
    internal McpClient Client => _client ?? throw new InvalidOperationException("The client is not connected.");

    /// <summary>Gets the token that bounds every request of this session.</summary>
    internal CancellationToken Token => _timeout.Token;

    /// <summary>Builds the provider, starts the server, and connects the client.</summary>
    /// <param name="services">The FrontComposer service collection; logging is added here.</param>
    /// <param name="logs">The provider that captures every log entry at Trace level.</param>
    /// <param name="cancellationToken">The test cancellation token.</param>
    /// <returns>The connected session.</returns>
    internal static async Task<McpSdkPipeSession> StartAsync(
        IServiceCollection services,
        McpAuditLoggerProvider logs,
        CancellationToken cancellationToken) {
        _ = services.AddLogging(builder => builder
            .AddProvider(logs)
            .SetMinimumLevel(LogLevel.Trace)
            .AddFilter<McpAuditLoggerProvider>(null, LogLevel.Trace));
        ServiceProvider provider = services.BuildServiceProvider();
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        McpServerOptions options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        Pipe clientToServer = new();
        Pipe serverToClient = new();
        StreamServerTransport serverTransport = new(
            clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream(), "audit", loggerFactory);
        StreamClientTransport clientTransport = new(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream());
        McpServer server = McpServer.Create(serverTransport, options, loggerFactory, provider);
        CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        McpSdkPipeSession session = new(provider, serverTransport, server, timeout);
        try {
            session._client = await McpClient.CreateAsync(clientTransport, cancellationToken: timeout.Token).ConfigureAwait(true);
            return session;
        }
        catch {
            await session.DisposeAsync().ConfigureAwait(true);
            throw;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        await _timeout.CancelAsync().ConfigureAwait(true);
        if (_client is not null) {
            await _client.DisposeAsync().ConfigureAwait(true);
        }

        try {
            await _serverTask.ConfigureAwait(true);
        }
        catch (OperationCanceledException) {
        }

        await _server.DisposeAsync().ConfigureAwait(true);
        await _serverTransport.DisposeAsync().ConfigureAwait(true);
        await _provider.DisposeAsync().ConfigureAwait(true);
        _timeout.Dispose();
    }
}
