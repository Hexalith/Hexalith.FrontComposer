using System.IO.Pipelines;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

[Trait("Category", "Governance")]
public sealed class McpSecurityGovernanceTests {
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void ProductionMapping_RejectsAllowAllTenantGateBeforePublishingEndpoint(string environmentName) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        _ = builder.Services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = builder.Services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(Substitute.For<IFrontComposerMcpResourceVisibilityGate>());
        _ = builder.Services.AddFrontComposerMcp();
        using WebApplication app = builder.Build();

        InvalidOperationException error = Should.Throw<InvalidOperationException>(() => app.MapFrontComposerMcp());

        error.Message.ShouldContain(nameof(AllowAllMcpTenantToolGate));
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void ProductionMapping_RejectsAllowAllResourceGateBeforePublishingEndpoint(string environmentName) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        _ = builder.Services.AddSingleton<IFrontComposerMcpTenantToolGate>(Substitute.For<IFrontComposerMcpTenantToolGate>());
        _ = builder.Services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = builder.Services.AddFrontComposerMcp();
        using WebApplication app = builder.Build();

        InvalidOperationException error = Should.Throw<InvalidOperationException>(() => app.MapFrontComposerMcp());

        error.Message.ShouldContain(nameof(AllowAllResourceVisibilityGate));
        ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints).ShouldBeEmpty();
    }

    [Fact]
    public async Task ProductionMapping_RequiresAuthenticationBeforeSdkDispatch() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");
        _ = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.Events.OnRedirectToLogin = context => {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            });
        _ = builder.Services.AddAuthorization();
        _ = builder.Services.AddSingleton<IFrontComposerMcpTenantToolGate>(Substitute.For<IFrontComposerMcpTenantToolGate>());
        _ = builder.Services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(Substitute.For<IFrontComposerMcpResourceVisibilityGate>());
        _ = builder.Services.AddFrontComposerMcp();
        await using WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.MapGet("/login", (HttpContext context) => context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "audit-agent")],
                CookieAuthenticationDefaults.AuthenticationScheme))));
        _ = app.MapFrontComposerMcp();

        Endpoint[] endpoints = [.. ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText?.StartsWith("/mcp", StringComparison.Ordinal) == true)];
        endpoints.ShouldNotBeEmpty();
        endpoints.All(endpoint => endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null).ShouldBeTrue();
        endpoints.All(endpoint => endpoint.Metadata.GetMetadata<IAllowAnonymous>() is null).ShouldBeTrue();

        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        using HttpClient client = new();
        const string initialize = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"audit","version":"1"}}}""";
        using StringContent content = new(initialize, Encoding.UTF8, "application/json");
        using HttpRequestMessage request = new(HttpMethod.Post, address.TrimEnd('/') + "/mcp") { Content = content };
        request.Headers.Accept.ParseAdd("application/json, text/event-stream");
        using HttpResponseMessage response = await client.SendAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);

        using HttpClientHandler handler = new() { CookieContainer = new CookieContainer() };
        using HttpClient authenticatedClient = new(handler);
        using HttpResponseMessage login = await authenticatedClient.GetAsync(address.TrimEnd('/') + "/login", TestContext.Current.CancellationToken).ConfigureAwait(true);
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
        using StringContent authorizedContent = new(initialize, Encoding.UTF8, "application/json");
        using HttpRequestMessage authorizedRequest = new(HttpMethod.Post, address.TrimEnd('/') + "/mcp") { Content = authorizedContent };
        authorizedRequest.Headers.Accept.ParseAdd("application/json, text/event-stream");
        using HttpResponseMessage authorizedResponse = await authenticatedClient.SendAsync(authorizedRequest, TestContext.Current.CancellationToken).ConfigureAwait(true);
        authorizedResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SdkDispatch_UnregisteredResourceHasDistinctNotFoundError() {
        ServiceCollection services = new();
        McpAuditLoggerProvider auditLogs = new();
        _ = services.AddLogging(builder => builder.AddProvider(auditLogs));
        IFrontComposerMcpAgentContextAccessor accessor = Substitute.For<IFrontComposerMcpAgentContextAccessor>();
        accessor.GetContext().Returns(new FrontComposerMcpAgentContext("", "", new ClaimsPrincipal()));
        _ = services.AddSingleton(accessor);
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, ThrowingAuditTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(Substitute.For<IFrontComposerMcpResourceVisibilityGate>());
        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(new McpManifest(
            "frontcomposer.mcp.v1",
            [new McpCommandDescriptor("Billing.HiddenCommand.Execute", typeof(object).FullName!, "Billing", "Hidden command", null, null, [], [])],
            [new McpResourceDescriptor("frontcomposer://Billing/projections/HiddenProjection", "HiddenProjection", typeof(object).FullName!, "Billing", "Hidden projection", "Hidden projection", [])])));
        await using ServiceProvider provider = services.BuildServiceProvider();
        McpServerOptions options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        Pipe clientToServer = new();
        Pipe serverToClient = new();
        StreamServerTransport serverTransport = new(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream(), "audit");
        StreamClientTransport clientTransport = new(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream());
        McpServer server = McpServer.Create(serverTransport, options, serviceProvider: provider);
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        Task serverTask = server.RunAsync(timeout.Token);
        McpClient? client = null;
        try {
            client = await McpClient.CreateAsync(clientTransport, cancellationToken: timeout.Token).ConfigureAwait(true);

            ListToolsResult anonymousTools = await client.ListToolsAsync(new ListToolsRequestParams(), timeout.Token).ConfigureAwait(true);
            anonymousTools.Tools.ShouldBeEmpty();
            ListResourcesResult anonymousResources = await client.ListResourcesAsync(new ListResourcesRequestParams(), timeout.Token).ConfigureAwait(true);
            anonymousResources.Resources.Select(resource => resource.Uri).ShouldContain("frontcomposer://Billing/projections/HiddenProjection");
            anonymousResources.Resources.Select(resource => resource.Uri).ShouldContain("frontcomposer://skills/index");

            accessor.GetContext().Returns(new FrontComposerMcpAgentContext(
                "tenant-a", "agent-a", new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "audit"))));
            ListToolsResult admittedTools = await client.ListToolsAsync(new ListToolsRequestParams(), timeout.Token).ConfigureAwait(true);
            admittedTools.Tools.Select(tool => tool.Name).ShouldBe(["frontcomposer.lifecycle.subscribe"]);
            ListResourcesResult admittedResources = await client.ListResourcesAsync(new ListResourcesRequestParams(), timeout.Token).ConfigureAwait(true);
            JsonSerializer.Serialize(admittedResources.Resources).ShouldBe(JsonSerializer.Serialize(anonymousResources.Resources));

            CallToolResult hiddenTool = await client.CallToolAsync(new CallToolRequestParams { Name = "Billing.HiddenCommand.Execute" }, timeout.Token).ConfigureAwait(true);
            CallToolResult absentTool = await client.CallToolAsync(new CallToolRequestParams { Name = "Billing.AbsentCommand.Execute" }, timeout.Token).ConfigureAwait(true);
            JsonSerializer.Serialize(hiddenTool).ShouldBe(JsonSerializer.Serialize(absentTool));
            JsonSerializer.Serialize(hiddenTool).ShouldContain("unknown_tool");
            JsonSerializer.Serialize(hiddenTool).ShouldNotContain("Billing.HiddenCommand.Execute");
            JsonSerializer.Serialize(hiddenTool).ShouldNotContain("tenant-a");

            ReadResourceResult skill = await client.ReadResourceAsync("frontcomposer://skills/index", cancellationToken: timeout.Token).ConfigureAwait(true);
            skill.Contents.Single().ShouldBeOfType<TextResourceContents>().Text.ShouldContain("FrontComposer");
            ReadResourceResult hiddenProjection = await client.ReadResourceAsync("frontcomposer://Billing/projections/HiddenProjection", cancellationToken: timeout.Token).ConfigureAwait(true);
            string hiddenJson = JsonSerializer.Serialize(hiddenProjection);
            hiddenJson.ShouldContain("unknown_resource");
            hiddenJson.ShouldNotContain("tenant-a");

            accessor.GetContext().Returns(new FrontComposerMcpAgentContext("", "", new ClaimsPrincipal()));
            ReadResourceResult unauthorizedProjection = await client.ReadResourceAsync("frontcomposer://Billing/projections/HiddenProjection", cancellationToken: timeout.Token).ConfigureAwait(true);
            JsonSerializer.Serialize(unauthorizedProjection).ShouldBe(hiddenJson);

            McpException error = await Should.ThrowAsync<McpException>(async () => {
                _ = await client.ReadResourceAsync(
                    "frontcomposer://Billing/projections/never-registered", cancellationToken: timeout.Token).ConfigureAwait(true);
            }).ConfigureAwait(true);

            error.Message.ShouldContain("Unknown resource URI");
            error.Message.ShouldContain("frontcomposer://Billing/projections/never-registered");
            error.Message.ShouldNotContain("unknown_resource");
            error.Message.ShouldNotContain("tenant-a");
            error.Message.ShouldNotContain("fingerprint");

            string logs = string.Join('\n', auditLogs.Entries);
            logs.ShouldContain("MCP tenant gate failed closed");
            logs.ShouldNotContain("Billing.HiddenCommand.Execute");
            logs.ShouldNotContain("tenant-a");
            logs.ShouldNotContain("audit-internal-secret");
        }
        finally {
            timeout.Cancel();
            if (client is not null) {
                await client.DisposeAsync().ConfigureAwait(true);
            }

            try {
                await serverTask.ConfigureAwait(true);
            }
            catch (OperationCanceledException) {
            }

            await server.DisposeAsync().ConfigureAwait(true);
            await serverTransport.DisposeAsync().ConfigureAwait(true);
        }
    }
}
