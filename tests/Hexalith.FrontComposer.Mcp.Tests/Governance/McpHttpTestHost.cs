using System.Net;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// Builds loopback ASP.NET Core hosts for MCP mapping tests and sends raw HTTP probes, so assertions observe the
/// effective authentication and authorization outcome instead of endpoint metadata.
/// </summary>
internal static class McpHttpTestHost {
    /// <summary>The MCP header that carries an API key by default.</summary>
    internal const string ApiKeyHeaderName = "X-FrontComposer-Mcp-Key";

    private const string InitializeRequest = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"audit","version":"1"}}}""";

    /// <summary>
    /// Creates a builder bound to an ephemeral loopback port, with restrictive substitute gates registered.
    /// </summary>
    /// <param name="environmentName">The host environment name.</param>
    /// <param name="logs">An optional provider that captures every log entry at every level.</param>
    /// <returns>The builder; the caller registers authentication, authorization and FrontComposer MCP.</returns>
    internal static WebApplicationBuilder CreateBuilder(string environmentName, McpAuditLoggerProvider? logs = null) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");
        if (logs is not null) {
            _ = builder.Logging.ClearProviders();
            _ = builder.Logging.AddProvider(logs);
            _ = builder.Logging.SetMinimumLevel(LogLevel.Trace);

            // A provider-specific rule outranks any category rule from host configuration, so capture stays at Trace.
            _ = builder.Logging.AddFilter<McpAuditLoggerProvider>(null, LogLevel.Trace);
        }

        _ = builder.Services.AddSingleton<IFrontComposerMcpTenantToolGate>(Substitute.For<IFrontComposerMcpTenantToolGate>());
        _ = builder.Services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(Substitute.For<IFrontComposerMcpResourceVisibilityGate>());
        return builder;
    }

    /// <summary>Returns the endpoints mapped under the MCP route.</summary>
    /// <param name="app">The application whose endpoints are inspected.</param>
    /// <returns>Every route endpoint whose pattern starts with <c>/mcp</c>.</returns>
    internal static RouteEndpoint[] McpEndpoints(WebApplication app)
        => [.. ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/mcp", StringComparison.Ordinal) == true)];

    /// <summary>Starts the application and returns its loopback base address without a trailing slash.</summary>
    /// <param name="app">The application to start.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The base address.</returns>
    internal static async Task<string> StartAsync(WebApplication app, CancellationToken cancellationToken) {
        await app.StartAsync(cancellationToken).ConfigureAwait(true);
        return app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
            .Addresses.Single().TrimEnd('/');
    }

    /// <summary>Posts an MCP <c>initialize</c> request with the supplied API-key header values.</summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="address">The host base address.</param>
    /// <param name="apiKeyValues">The API-key header values; <see langword="null"/> omits the header.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <param name="readBody">Whether to read the response body; only tests that assert on it should.</param>
    /// <returns>The status code, and the response body when <paramref name="readBody"/> is set.</returns>
    internal static async Task<(HttpStatusCode Status, string? Body)> PostInitializeAsync(
        HttpClient client,
        string address,
        string[]? apiKeyValues,
        CancellationToken cancellationToken,
        bool readBody = false) {
        using StringContent content = new(InitializeRequest, Encoding.UTF8, "application/json");
        using HttpRequestMessage request = new(HttpMethod.Post, address + "/mcp") { Content = content };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        if (apiKeyValues is not null) {
            request.Headers.TryAddWithoutValidation(ApiKeyHeaderName, apiKeyValues).ShouldBeTrue();
        }

        // Headers only: a regression that admits a streaming response must fail on its status, not hang on its body.
        using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(true);
        string? body = readBody ? await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(true) : null;
        return (response.StatusCode, body);
    }

    /// <summary>
    /// Sends an anonymous MCP <c>initialize</c> envelope to every mapped MCP endpoint, using each endpoint's own HTTP
    /// method, and asserts that authorization rejects each one with <c>401</c>.
    /// </summary>
    /// <param name="client">The HTTP client.</param>
    /// <param name="address">The host base address.</param>
    /// <param name="endpoints">The mapped MCP endpoints.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that completes when every endpoint was probed.</returns>
    internal static async Task AssertAnonymousRejectedEverywhereAsync(
        HttpClient client,
        string address,
        IReadOnlyCollection<RouteEndpoint> endpoints,
        CancellationToken cancellationToken) {
        endpoints.ShouldNotBeEmpty();
        List<string> probed = [];
        foreach (RouteEndpoint endpoint in endpoints) {
            IReadOnlyList<string> methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [HttpMethod.Post.Method];
            foreach (string method in methods) {
                using StringContent content = new(InitializeRequest, Encoding.UTF8, "application/json");
                using HttpRequestMessage request = new(new HttpMethod(method), address + "/" + endpoint.RoutePattern.RawText!.TrimStart('/')) {
                    Content = content,
                };
                request.Headers.Accept.ParseAdd("application/json, text/event-stream");
                using HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(true);
                response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized, $"{method} {endpoint.RoutePattern.RawText} must reject anonymous callers.");
                probed.Add(method);
            }
        }

        probed.ShouldContain(HttpMethod.Post.Method);
    }
}
