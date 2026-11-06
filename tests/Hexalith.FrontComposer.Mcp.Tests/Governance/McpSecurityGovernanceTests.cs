using System.IO.Pipelines;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;
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
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    private const string AgentKey = "audit-agent-key-01";

    // E14R-AI-4 audit world. Tenant, user, fingerprints, gate exception text, and the API key are server-only.
    private const string AuditTenantId = "tenant-audit-5d1f";
    private const string AuditUserId = "user-audit-8c2e";
    private const string SiblingToolName = "Billing.PayInvoiceCommand.Execute";
    private const string HiddenToolName = "Billing.HiddenCommand.Execute";
    private const string PolicyToolName = "Billing.RestrictedCommand.Execute";
    private const string AbsentToolName = "Billing.AbsentCommand.Execute";
    private const string LifecycleToolName = "frontcomposer.lifecycle.subscribe";
    private const string AuditPolicyName = "AuditRestrictedPolicy";
    private const string SiblingFingerprint = "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08";
    private const string HiddenFingerprint = "b94d27b9934d3e08a52e52d7da7dabfac484efe37a5380ee9088f7ace2efcde9";
    private const string HiddenByTenantGate = "tenant-gate-false";
    private const string HiddenByThrowingGate = "tenant-gate-throws";
    private const string HiddenByPolicyGate = "policy-gate-denies";

    private static readonly string[] ServerOnlyValues = [
        AuditTenantId,
        AuditUserId,
        McpAuditIdentityTenantToolGate.ExceptionMarker,
        SiblingFingerprint,
        HiddenFingerprint,
        AgentKey,
        AuditPolicyName,
    ];

    // Derivable values a client tries to supply itself; the invoker must refuse them.
    private static readonly string[] SpoofedValues = [
        "tenant-spoof-41ab",
        "user-spoof-77cd",
        "01JZ0R5K9N8W4Y7V3Q2P6C1A0S",
        "01JZ0R5K9N8W4Y7V3Q2P6C1A0T",
    ];

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

        RouteEndpoint[] endpoints = McpHttpTestHost.McpEndpoints(app);
        await app.StartAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        string address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
        using HttpClient client = new();
        const string initialize = """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"audit","version":"1"}}}""";

        // Effective outcome, not metadata shape: every mapped MCP endpoint rejects an anonymous caller.
        await McpHttpTestHost.AssertAnonymousRejectedEverywhereAsync(
            client, address.TrimEnd('/'), endpoints, TestContext.Current.CancellationToken).ConfigureAwait(true);

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AppendedHostPolicyCannotRemoveTheAuthenticatedUserRequirement(bool appendNamedPolicy) {
        // E14R-AI-2 / F-2: an appended policy object used to replace the implicit default policy of an empty [Authorize].
        WebApplicationBuilder builder = McpHttpTestHost.CreateBuilder("Production");
        _ = builder.Services.AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey();
        _ = builder.Services.AddAuthorization(options => options.AddPolicy("HostAlwaysTrue", policy => policy.RequireAssertion(_ => true)));
        _ = builder.Services.AddFrontComposerMcp(options => options.ApiKeys[AgentKey] = new FrontComposerMcpApiKeyIdentity("tenant-a", "agent-a"));
        await using WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        IEndpointConventionBuilder mcp = app.MapFrontComposerMcp();
        _ = appendNamedPolicy
            ? mcp.RequireAuthorization("HostAlwaysTrue")
            : mcp.RequireAuthorization(new AuthorizationPolicyBuilder().RequireAssertion(_ => true).Build());

        RouteEndpoint[] endpoints = McpHttpTestHost.McpEndpoints(app);
        string address = await McpHttpTestHost.StartAsync(app, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using HttpClient client = new();

        await McpHttpTestHost.AssertAnonymousRejectedEverywhereAsync(client, address, endpoints, TestContext.Current.CancellationToken).ConfigureAwait(true);
        (await McpHttpTestHost.PostInitializeAsync(client, address, [AgentKey], TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task HostDefaultPolicyStillAppliesToFrameworkOnlyMapping() {
        const string OtherTenantKey = "audit-other-tenant-key-02";
        WebApplicationBuilder builder = McpHttpTestHost.CreateBuilder("Production");
        _ = builder.Services.AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey();
        _ = builder.Services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireClaim("TenantId", "tenant-a")
            .Build());
        _ = builder.Services.AddFrontComposerMcp(options => {
            options.ApiKeys[AgentKey] = new FrontComposerMcpApiKeyIdentity("tenant-a", "agent-a");
            options.ApiKeys[OtherTenantKey] = new FrontComposerMcpApiKeyIdentity("tenant-b", "agent-b");
        });
        await using WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.MapFrontComposerMcp();

        string address = await McpHttpTestHost.StartAsync(app, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using HttpClient client = new();

        (await McpHttpTestHost.PostInitializeAsync(client, address, null, TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await McpHttpTestHost.PostInitializeAsync(client, address, [OtherTenantKey], TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Status.ShouldBe(HttpStatusCode.Forbidden);
        (await McpHttpTestHost.PostInitializeAsync(client, address, [AgentKey], TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PermissiveHostDefaultPolicyStillRequiresAnAuthenticatedCaller() {
        // D1: the explicit RequireAuthenticatedUser() applies even when the host's default policy admits everyone.
        WebApplicationBuilder builder = McpHttpTestHost.CreateBuilder("Production");
        _ = builder.Services.AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey();
        _ = builder.Services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder()
            .RequireAssertion(_ => true)
            .Build());
        _ = builder.Services.AddFrontComposerMcp(options => options.ApiKeys[AgentKey] = new FrontComposerMcpApiKeyIdentity("tenant-a", "agent-a"));
        await using WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.MapFrontComposerMcp();

        RouteEndpoint[] endpoints = McpHttpTestHost.McpEndpoints(app);
        string address = await McpHttpTestHost.StartAsync(app, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using HttpClient client = new();

        await McpHttpTestHost.AssertAnonymousRejectedEverywhereAsync(client, address, endpoints, TestContext.Current.CancellationToken).ConfigureAwait(true);
        (await McpHttpTestHost.PostInitializeAsync(client, address, [AgentKey], TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Status.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task MixedSchemeHostAuthenticatesTheAgentKeyForMcp(bool scopeApiKeyToMcpEndpoints) {
        WebApplicationBuilder builder = McpHttpTestHost.CreateBuilder("Production");
        _ = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options => options.Events.OnRedirectToLogin = context => {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            })
            .AddFrontComposerMcpApiKey();
        _ = scopeApiKeyToMcpEndpoints
            ? builder.Services.AddAuthorization()
            : builder.Services.AddAuthorization(options => options.DefaultPolicy = new AuthorizationPolicyBuilder(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    FrontComposerMcpAuthenticationDefaults.ApiKeyScheme)
                .RequireAuthenticatedUser()
                .Build());
        _ = builder.Services.AddFrontComposerMcp(options => options.ApiKeys[AgentKey] = new FrontComposerMcpApiKeyIdentity("tenant-a", "agent-a"));
        await using WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        IEndpointConventionBuilder mcp = app.MapFrontComposerMcp();
        if (scopeApiKeyToMcpEndpoints) {
            _ = mcp.RequireAuthorization(new AuthorizationPolicyBuilder(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme)
                .RequireAuthenticatedUser()
                .Build());
        }

        RouteEndpoint[] endpoints = McpHttpTestHost.McpEndpoints(app);
        string address = await McpHttpTestHost.StartAsync(app, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using HttpClient client = new();

        (await McpHttpTestHost.PostInitializeAsync(client, address, [AgentKey], TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Status.ShouldBe(HttpStatusCode.OK);
        await McpHttpTestHost.AssertAnonymousRejectedEverywhereAsync(client, address, endpoints, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    [Fact]
    public async Task ApiKeyOnlyHostAuthenticatesConfiguredKeysBeforeAuthorization() {
        // E14R-AI-3 / F-3: FrontComposerMcpOptions.ApiKeys used to be matched only after authorization had already run.
        const string InvalidKey = "audit-unconfigured-key-03";
        McpAuditLoggerProvider logs = new();
        WebApplicationBuilder builder = McpHttpTestHost.CreateBuilder("Production", logs);
        _ = builder.Services.AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey();
        _ = builder.Services.AddAuthorization();
        _ = builder.Services.AddFrontComposerMcp(options => options.ApiKeys[AgentKey] = new FrontComposerMcpApiKeyIdentity("tenant-a", "agent-a"));
        await using WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.MapFrontComposerMcp();

        RouteEndpoint[] endpoints = McpHttpTestHost.McpEndpoints(app);
        string address = await McpHttpTestHost.StartAsync(app, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using HttpClient client = new();

        (await McpHttpTestHost.PostInitializeAsync(client, address, [AgentKey], TestContext.Current.CancellationToken).ConfigureAwait(true))
            .Status.ShouldBe(HttpStatusCode.OK);
        await McpHttpTestHost.AssertAnonymousRejectedEverywhereAsync(client, address, endpoints, TestContext.Current.CancellationToken).ConfigureAwait(true);
        foreach (string[] rejected in new[] {
            new[] { InvalidKey },
            new[] { "   " },
            new[] { AgentKey, AgentKey },
            new[] { AgentKey + " " + InvalidKey },
        }) {
            (HttpStatusCode status, string? responseBody) = await McpHttpTestHost.PostInitializeAsync(
                client, address, rejected, TestContext.Current.CancellationToken, readBody: true).ConfigureAwait(true);

            status.ShouldBe(HttpStatusCode.Unauthorized);
            string body = responseBody.ShouldNotBeNull();
            body.ShouldNotContain(AgentKey);
            body.ShouldNotContain(InvalidKey);
        }

        string logText = string.Join('\n', logs.Entries);
        logText.ShouldContain(FrontComposerMcpApiKeyAuthenticationHandler.FailureMessage);
        logText.ShouldNotContain(AgentKey);
        logText.ShouldNotContain(InvalidKey);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void MappingWithoutAuthenticationOrAuthorizationServicesNamesTheMissingCall(bool addAuthentication, bool addAuthorization) {
        WebApplicationBuilder builder = McpHttpTestHost.CreateBuilder("Production");
        if (addAuthentication) {
            _ = builder.Services.AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey();
        }

        if (addAuthorization) {
            _ = builder.Services.AddAuthorization();
        }

        _ = builder.Services.AddFrontComposerMcp();
        using WebApplication app = builder.Build();

        InvalidOperationException error = Should.Throw<InvalidOperationException>(() => app.MapFrontComposerMcp());

        if (addAuthentication) {
            error.Message.ShouldNotContain("AddAuthentication");
        }
        else {
            error.Message.ShouldContain("AddAuthentication");
        }

        if (addAuthorization) {
            error.Message.ShouldNotContain("AddAuthorization");
        }
        else {
            error.Message.ShouldContain("AddAuthorization");
        }

        McpHttpTestHost.McpEndpoints(app).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("development")]
    [InlineData("DEVELOPMENT")]
    [InlineData("Development")]
    public async Task DevelopmentEnvironmentInAnyCaseAdmitsAllowAllGates(string environmentName) {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environmentName });
        _ = builder.WebHost.UseUrls("http://127.0.0.1:0");
        _ = builder.Services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = builder.Services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, AllowAllResourceVisibilityGate>();
        _ = builder.Services.AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey();
        _ = builder.Services.AddAuthorization();
        _ = builder.Services.AddFrontComposerMcp();
        await using WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();

        _ = app.MapFrontComposerMcp();

        RouteEndpoint[] endpoints = McpHttpTestHost.McpEndpoints(app);
        string address = await McpHttpTestHost.StartAsync(app, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using HttpClient client = new();
        await McpHttpTestHost.AssertAnonymousRejectedEverywhereAsync(client, address, endpoints, TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    [Fact]
    public void ProductionMappingResolvesGatesInAnAsyncDisposedScope() {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
        _ = builder.Services.AddScoped<AsyncOnlyDisposableGateDependency>();
        _ = builder.Services.AddScoped<IFrontComposerMcpTenantToolGate, AsyncDisposableDependentTenantToolGate>();
        _ = builder.Services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(Substitute.For<IFrontComposerMcpResourceVisibilityGate>());
        _ = builder.Services.AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey();
        _ = builder.Services.AddAuthorization();
        _ = builder.Services.AddFrontComposerMcp();
        using WebApplication app = builder.Build();
        int disposalsBefore = AsyncOnlyDisposableGateDependency.Disposals;

        _ = app.MapFrontComposerMcp();

        AsyncOnlyDisposableGateDependency.Disposals.ShouldBe(disposalsBefore + 1);
        McpHttpTestHost.McpEndpoints(app).ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SdkDispatch_UnregisteredResourceHasDistinctNotFoundError() {
        ServiceCollection services = new();
        McpAuditLoggerProvider auditLogs = new();
        // F-6 (c): every category, the SDK's included, is captured at Trace.
        _ = services.AddLogging(builder => builder
            .AddProvider(auditLogs)
            .SetMinimumLevel(LogLevel.Trace)
            .AddFilter<McpAuditLoggerProvider>(null, LogLevel.Trace));
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
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        StreamServerTransport serverTransport = new(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream(), "audit", loggerFactory);
        StreamClientTransport clientTransport = new(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream());
        McpServer server = McpServer.Create(serverTransport, options, loggerFactory, provider);
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
                AuditTenantId, AuditUserId, new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "audit"))));
            ListToolsResult admittedTools = await client.ListToolsAsync(new ListToolsRequestParams(), timeout.Token).ConfigureAwait(true);
            admittedTools.Tools.Select(tool => tool.Name).ShouldBe(["frontcomposer.lifecycle.subscribe"]);
            ListResourcesResult admittedResources = await client.ListResourcesAsync(new ListResourcesRequestParams(), timeout.Token).ConfigureAwait(true);
            JsonSerializer.Serialize(admittedResources.Resources).ShouldBe(JsonSerializer.Serialize(anonymousResources.Resources));

            CallToolResult hiddenTool = await client.CallToolAsync(new CallToolRequestParams { Name = "Billing.HiddenCommand.Execute" }, timeout.Token).ConfigureAwait(true);
            CallToolResult absentTool = await client.CallToolAsync(new CallToolRequestParams { Name = "Billing.AbsentCommand.Execute" }, timeout.Token).ConfigureAwait(true);
            JsonSerializer.Serialize(hiddenTool).ShouldBe(JsonSerializer.Serialize(absentTool));
            JsonSerializer.Serialize(hiddenTool).ShouldContain("unknown_tool");
            JsonSerializer.Serialize(hiddenTool).ShouldNotContain("Billing.HiddenCommand.Execute");
            JsonSerializer.Serialize(hiddenTool).ShouldNotContain(AuditTenantId);

            ReadResourceResult skill = await client.ReadResourceAsync("frontcomposer://skills/index", cancellationToken: timeout.Token).ConfigureAwait(true);
            skill.Contents.Single().ShouldBeOfType<TextResourceContents>().Text.ShouldContain("FrontComposer");
            ReadResourceResult hiddenProjection = await client.ReadResourceAsync("frontcomposer://Billing/projections/HiddenProjection", cancellationToken: timeout.Token).ConfigureAwait(true);
            string hiddenJson = JsonSerializer.Serialize(hiddenProjection);
            hiddenJson.ShouldContain("unknown_resource");
            hiddenJson.ShouldNotContain(AuditTenantId);

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
            error.Message.ShouldNotContain(AuditTenantId);
            error.Message.ShouldNotContain("fingerprint");

            // D2: SDK categories may echo the tool name this client sent, so that assertion stays on FrontComposer
            // categories. Server-only values must be absent from every category.
            ShouldHaveCapturedSdkTraceLogs(auditLogs);
            string frontComposerLogs = JoinLogs(auditLogs, frontComposerOnly: true);
            frontComposerLogs.ShouldContain("MCP tenant gate failed closed");
            frontComposerLogs.ShouldNotContain("Billing.HiddenCommand.Execute");
            string logs = JoinLogs(auditLogs, frontComposerOnly: false);
            logs.ShouldNotContain(AuditTenantId);
            logs.ShouldNotContain(AuditUserId);
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

    [Theory]
    [InlineData("correlationId")]
    [InlineData("messageId")]
    public async Task SdkLifecycleSubscribeAnswersForeignPrincipalsAsNeverIssuedHandle(string handleName) {
        // E14R-AI-1 (AC 14.2 #4): every principal below is admitted to the same command tool. Only the
        // principal that created the lifecycle entry may read it; everyone else gets the never-issued shape.
        const string NeverIssuedHandle = "01JZ0R5K9N8W4Y7V3Q2P6C1A0Z";
        ServiceCollection services = new();
        McpAuditLoggerProvider auditLogs = new();
        // F-6 (c): every category, the SDK's included, is captured at Trace.
        _ = services.AddLogging(builder => builder
            .AddProvider(auditLogs)
            .SetMinimumLevel(LogLevel.Trace)
            .AddFilter<McpAuditLoggerProvider>(null, LogLevel.Trace));
        IFrontComposerMcpAgentContextAccessor accessor = Substitute.For<IFrontComposerMcpAgentContextAccessor>();
        _ = accessor.GetContext().Returns(AuditContext("tenant-a", "user-a"));
        _ = services.AddSingleton(accessor);
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate, AllowAllMcpTenantToolGate>();
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(Substitute.For<IFrontComposerMcpResourceVisibilityGate>());
        _ = services.AddSingleton<ICommandService, McpAuditCommandService>();
        _ = services.AddScoped(_ => Substitute.For<ILifecycleStateService>());
        _ = services.AddFrontComposerMcp(options => options.Manifests.Add(new McpManifest(
            "frontcomposer.mcp.v1",
            [new McpCommandDescriptor(
                "Billing.PayInvoice.Execute",
                typeof(McpAuditLifecycleCommand).FullName!,
                "Billing",
                "Pay invoice",
                null,
                null,
                [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
                ["TenantId", "UserId", "MessageId", "CorrelationId"])],
            [])));
        await using ServiceProvider provider = services.BuildServiceProvider();
        McpServerOptions options = provider.GetRequiredService<IOptions<McpServerOptions>>().Value;
        // Each SDK request runs in its own DI scope, so the owner's later poll crosses scopes.
        options.ScopeRequests.ShouldBeTrue();
        Pipe clientToServer = new();
        Pipe serverToClient = new();
        ILoggerFactory loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        StreamServerTransport serverTransport = new(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream(), "audit", loggerFactory);
        StreamClientTransport clientTransport = new(clientToServer.Writer.AsStream(), serverToClient.Reader.AsStream());
        McpServer server = McpServer.Create(serverTransport, options, loggerFactory, provider);
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        Task serverTask = server.RunAsync(timeout.Token);
        McpClient? client = null;
        try {
            client = await McpClient.CreateAsync(clientTransport, cancellationToken: timeout.Token).ConfigureAwait(true);

            CallToolResult acknowledgement = await client.CallToolAsync(
                new CallToolRequestParams {
                    Name = "Billing.PayInvoice.Execute",
                    Arguments = new Dictionary<string, JsonElement> { ["Amount"] = JsonSerializer.SerializeToElement(42) },
                },
                timeout.Token).ConfigureAwait(true);
            acknowledgement.IsError.GetValueOrDefault().ShouldBeFalse();
            JsonElement acknowledged = acknowledgement.StructuredContent.ShouldNotBeNull();
            string messageId = acknowledged.GetProperty("messageId").GetString().ShouldNotBeNull();
            string correlationId = acknowledged.GetProperty("correlationId").GetString().ShouldNotBeNull();
            string handle = handleName == "messageId" ? messageId : correlationId;
            NeverIssuedHandle.ShouldNotBe(messageId);
            NeverIssuedHandle.ShouldNotBe(correlationId);

            (string TenantId, string UserId)[] foreignPrincipals = [
                ("tenant-a", "user-b"),
                ("tenant-b", "user-b"),
                ("tenant-b", "user-a"),
                ("TENANT-A", "user-a"),
            ];
            foreach ((string tenantId, string userId) in foreignPrincipals) {
                _ = accessor.GetContext().Returns(AuditContext(tenantId, userId));
                string issued = JsonSerializer.Serialize(
                    await SubscribeAsync(client, handleName, handle, timeout.Token).ConfigureAwait(true));
                string neverIssued = JsonSerializer.Serialize(
                    await SubscribeAsync(client, handleName, NeverIssuedHandle, timeout.Token).ConfigureAwait(true));

                issued.ShouldBe(neverIssued, $"{tenantId}/{userId} must not be able to tell an issued handle from a never-issued one.");
                issued.ShouldContain("unknown_tool");
                issued.ShouldNotContain(messageId);
                issued.ShouldNotContain(correlationId);
                issued.ShouldNotContain(NeverIssuedHandle);
                issued.ShouldNotContain("tenant-", Case.Insensitive);
                issued.ShouldNotContain("user-", Case.Insensitive);
            }

            _ = accessor.GetContext().Returns(AuditContext("tenant-a", "user-a"));
            CallToolResult owner = await SubscribeAsync(client, handleName, handle, timeout.Token).ConfigureAwait(true);
            owner.IsError.GetValueOrDefault().ShouldBeFalse();
            JsonElement snapshot = owner.StructuredContent.ShouldNotBeNull();
            snapshot.GetProperty("messageId").GetString().ShouldBe(messageId);
            snapshot.GetProperty("correlationId").GetString().ShouldBe(correlationId);

            // D2: SDK wire traces carry the handles this client sent and received, so those assertions stay on
            // FrontComposer categories. The principals' identities must be absent from every category.
            ShouldHaveCapturedSdkTraceLogs(auditLogs);
            string frontComposerLogs = JoinLogs(auditLogs, frontComposerOnly: true);
            frontComposerLogs.ShouldNotContain(messageId);
            frontComposerLogs.ShouldNotContain(correlationId);
            frontComposerLogs.ShouldNotContain(NeverIssuedHandle);
            string logs = JoinLogs(auditLogs, frontComposerOnly: false);
            logs.ShouldNotContain("tenant-a");
            logs.ShouldNotContain("tenant-b");
            logs.ShouldNotContain("user-a");
            logs.ShouldNotContain("user-b");
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

    [Theory]
    [InlineData(HiddenByTenantGate, HiddenToolName, null)]
    [InlineData(HiddenByThrowingGate, HiddenToolName, null)]
    [InlineData(HiddenByPolicyGate, PolicyToolName, null)]
    [InlineData(HiddenByTenantGate, "billing.hiddencommand.execute", null)]
    [InlineData(HiddenByTenantGate, "Billing.HiddenComand.Execute", null)]
    [InlineData(HiddenByPolicyGate, "Billing.RestrictedComand.Execute", null)]
    [InlineData(HiddenByTenantGate, "Billing", SiblingToolName)]
    [InlineData(HiddenByThrowingGate, "Billing", SiblingToolName)]
    public async Task SdkUnknownToolAnswerIsIdenticalWhetherTheNameIsHiddenOrNeverRegistered(
        string hiddenBy,
        string requestedName,
        string? expectedSuggestion) {
        // E14R-AI-4 / F-6 (a)-(c): the same admitted caller and the same visible sibling in both worlds. One world
        // registers the hidden tool, the other never did, so byte-identity proves the answer is not an existence oracle.
        // The near-match rows would suggest the hidden tool if it reached the candidates: "Billing" ties it with the
        // sibling, and the ordinal tie-break would pick the hidden name.
        string hiddenName = hiddenBy == HiddenByPolicyGate ? PolicyToolName : HiddenToolName;
        McpAuditLoggerProvider registeredLogs = new();
        McpAuditLoggerProvider neverRegisteredLogs = new();
        McpRecordingAuditCommandService dispatcher = new();
        McpAuditIdentityTenantToolGate registeredGate = AuditTenantGate(hiddenBy);
        McpAuditIdentityTenantToolGate neverRegisteredGate = AuditTenantGate(hiddenBy);
        McpAuditDenyingCommandPolicyGate registeredPolicyGate = new();
        McpAuditDenyingCommandPolicyGate neverRegisteredPolicyGate = new();

        (ListToolsResult registeredList, CallToolResult[] registeredCalls) = await AskAuditWorldAsync(
            AdmittedHttpContext(), hiddenBy, registerHiddenTool: true, [requestedName], registeredLogs, dispatcher, registeredGate, registeredPolicyGate).ConfigureAwait(true);
        (ListToolsResult neverRegisteredList, CallToolResult[] neverRegisteredCalls) = await AskAuditWorldAsync(
            AdmittedHttpContext(), hiddenBy, registerHiddenTool: false, [requestedName], neverRegisteredLogs, dispatcher, neverRegisteredGate, neverRegisteredPolicyGate).ConfigureAwait(true);

        // Non-vacuity: in the registered world the hidden tool reached its gate and was denied there.
        if (hiddenBy == HiddenByPolicyGate) {
            registeredGate.Evaluated.ShouldContain(PolicyToolName);
            registeredGate.Denied.ShouldNotContain(PolicyToolName, "the tenant gate admits it, so only the policy gate hides it.");
            registeredPolicyGate.EvaluatedPolicies.ShouldContain(AuditPolicyName);
        }
        else {
            registeredGate.Evaluated.ShouldContain(HiddenToolName);
            registeredGate.Denied.ShouldContain(HiddenToolName);
        }

        neverRegisteredGate.Evaluated.ShouldNotContain(hiddenName);
        neverRegisteredPolicyGate.EvaluatedPolicies.ShouldBeEmpty();

        string listed = Wire(registeredList);
        listed.ShouldBe(Wire(neverRegisteredList), "tools/list must not depend on whether the hidden tool is registered.");
        registeredList.Tools.Select(tool => tool.Name).ShouldBe([SiblingToolName, LifecycleToolName]);
        CallToolResult answer = registeredCalls.ShouldHaveSingleItem();
        string answered = Wire(answer);
        answered.ShouldBe(Wire(neverRegisteredCalls.ShouldHaveSingleItem()), "tools/call must not depend on whether the hidden tool is registered.");

        answer.IsError.ShouldBe(true);
        answer.Content.ShouldHaveSingleItem().ShouldBeOfType<TextContentBlock>().Text.ShouldBe("Request failed.");
        JsonElement structured = answer.StructuredContent.ShouldNotBeNull();
        structured.GetProperty("category").GetString().ShouldBe("unknown_tool");
        structured.GetProperty("docsCode").GetString().ShouldBe("HFC-MCP-UNKNOWN-TOOL");
        structured.GetProperty("visibleTools").EnumerateArray()
            .Select(tool => tool.GetProperty("name").GetString())
            .ShouldBe([SiblingToolName]);
        JsonElement suggestion = structured.GetProperty("suggestion");
        string? suggested = suggestion.ValueKind == JsonValueKind.Null ? null : suggestion.GetString();
        suggested.ShouldBe(expectedSuggestion);
        suggested.ShouldNotBe(hiddenName);
        answered.ShouldNotContain(hiddenName, Case.Insensitive);
        ShouldNotCarryServerOnlyValues(listed + answered, "the tools/list and tools/call responses");
        dispatcher.Dispatched.ShouldBeEmpty();

        if (hiddenBy == HiddenByThrowingGate) {
            JoinLogs(registeredLogs, frontComposerOnly: true).ShouldContain("MCP tenant gate failed closed");
        }

        foreach (McpAuditLoggerProvider logs in new[] { registeredLogs, neverRegisteredLogs }) {
            ShouldHaveCapturedSdkTraceLogs(logs);
            ShouldNotCarryServerOnlyValues(JoinLogs(logs, frontComposerOnly: false), "any log category at Trace level");

            // D2: SDK wire traces may echo what this client sent; FrontComposer categories never echo a tool name.
            string frontComposerLogs = JoinLogs(logs, frontComposerOnly: true);
            frontComposerLogs.ShouldNotContain(hiddenName, Case.Insensitive);
            frontComposerLogs.ShouldNotContain(requestedName, Case.Insensitive);
            if (!string.Equals(requestedName, hiddenName, StringComparison.OrdinalIgnoreCase)) {
                // The client never sent the hidden name here, so no category may carry it.
                JoinLogs(logs, frontComposerOnly: false).ShouldNotContain(hiddenName, Case.Insensitive);
            }
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SdkCallerWithoutUsableIdentityCannotTellARegisteredToolFromAnAbsentOne(bool authenticatedWithoutTenant) {
        // E14R-AI-4 / F-6 (d): the production HttpFrontComposerMcpAgentContextAccessor runs over the SDK. An
        // authenticated principal without a tenant claim takes its TenantMissing path; a request without an
        // HttpContext takes its AuthFailed path. Neither may learn which tools exist or why it was refused.
        string expectedCategory = authenticatedWithoutTenant
            ? nameof(FrontComposerMcpFailureCategory.TenantMissing)
            : nameof(FrontComposerMcpFailureCategory.AuthFailed);
        string[] toolNames = [HiddenToolName, SiblingToolName, AbsentToolName, LifecycleToolName];
        McpAuditLoggerProvider registeredLogs = new();
        McpAuditLoggerProvider neverRegisteredLogs = new();
        McpRecordingAuditCommandService dispatcher = new();

        (ListToolsResult registeredList, CallToolResult[] registeredCalls) = await AskAuditWorldAsync(
            authenticatedWithoutTenant ? TenantlessHttpContext() : null,
            HiddenByTenantGate,
            registerHiddenTool: true,
            toolNames,
            registeredLogs,
            dispatcher,
            AuditTenantGate(HiddenByTenantGate),
            new McpAuditDenyingCommandPolicyGate()).ConfigureAwait(true);
        (ListToolsResult neverRegisteredList, CallToolResult[] neverRegisteredCalls) = await AskAuditWorldAsync(
            authenticatedWithoutTenant ? TenantlessHttpContext() : null,
            HiddenByTenantGate,
            registerHiddenTool: false,
            toolNames,
            neverRegisteredLogs,
            dispatcher,
            AuditTenantGate(HiddenByTenantGate),
            new McpAuditDenyingCommandPolicyGate()).ConfigureAwait(true);

        registeredList.Tools.ShouldBeEmpty();
        string listed = Wire(registeredList);
        listed.ShouldBe(Wire(neverRegisteredList));
        string[] answers = [.. registeredCalls.Select(Wire)];
        answers.ShouldBe([.. neverRegisteredCalls.Select(Wire)], "tools/call must not depend on whether a tool is registered.");
        answers.Distinct(StringComparer.Ordinal).ShouldHaveSingleItem("hidden, visible-to-others, absent, and lifecycle tools must answer alike.");

        CallToolResult answer = registeredCalls[0];
        answer.IsError.ShouldBe(true);
        answer.StructuredContent.ShouldBeNull();
        answer.Content.ShouldHaveSingleItem().ShouldBeOfType<TextContentBlock>().Text.ShouldBe("Request failed.");
        foreach (string disclosure in new[] { "auth_failed", "tenant_missing", "AuthFailed", "TenantMissing", "unknown_tool" }) {
            answers[0].ShouldNotContain(disclosure, Case.Insensitive);
        }

        ShouldNotCarryServerOnlyValues(listed + answers[0], "the tools/list and tools/call responses");
        dispatcher.Dispatched.ShouldBeEmpty();
        foreach (McpAuditLoggerProvider logs in new[] { registeredLogs, neverRegisteredLogs }) {
            // The failure category is server-side evidence that the production accessor path ran.
            string frontComposerLogs = JoinLogs(logs, frontComposerOnly: true);
            frontComposerLogs.ShouldContain($"MCP tools/list failed closed. Category={expectedCategory}.");
            frontComposerLogs.ShouldContain($"MCP command invocation failed with category {expectedCategory}.");
            frontComposerLogs.ShouldContain($"MCP lifecycle precheck failed closed. Category={expectedCategory}.");
            ShouldHaveCapturedSdkTraceLogs(logs);
            ShouldNotCarryServerOnlyValues(JoinLogs(logs, frontComposerOnly: false), "any log category at Trace level");
        }
    }

    [Fact]
    public async Task MappedHostListsAndInvokesTheToolAsTheApiKeyIdentity() {
        // E14R-AI-4 / F-6 (e): an authenticated SDK client lists and invokes a generated tool through the mapped
        // production host. The invoker injects the key's identity and fresh canonical handles; the agent receives
        // exactly those handles.
        McpAuditLoggerProvider logs = new();
        McpRecordingAuditCommandService dispatcher = new();
        WebApplicationBuilder builder = McpHttpTestHost.CreateBuilder("Production", logs);
        _ = builder.Services.Replace(ServiceDescriptor.Singleton<IFrontComposerMcpTenantToolGate>(
            new McpAuditIdentityTenantToolGate(AuditTenantId, AuditUserId)));
        _ = builder.Services.AddAuthentication(FrontComposerMcpAuthenticationDefaults.ApiKeyScheme).AddFrontComposerMcpApiKey();
        _ = builder.Services.AddAuthorization();
        _ = builder.Services.AddSingleton<ICommandService>(dispatcher);
        _ = builder.Services.AddScoped(_ => Substitute.For<ILifecycleStateService>());
        _ = builder.Services.AddFrontComposerMcp(options => {
            options.ApiKeys[AgentKey] = new FrontComposerMcpApiKeyIdentity(AuditTenantId, AuditUserId);
            options.Manifests.Add(AuditManifest(registeredHiddenBy: null));
        });
        await using WebApplication app = builder.Build();
        _ = app.UseAuthentication();
        _ = app.UseAuthorization();
        _ = app.MapFrontComposerMcp();

        string address = await McpHttpTestHost.StartAsync(app, TestContext.Current.CancellationToken).ConfigureAwait(true);
        using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        ListToolsResult tools;
        CallToolResult acknowledgement;
        CallToolResult spoofed;
        HttpClientTransport transport = new(new HttpClientTransportOptions {
            Endpoint = new Uri(address + "/mcp"),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>(StringComparer.Ordinal) {
                [McpHttpTestHost.ApiKeyHeaderName] = AgentKey,
            },
        });
        await using (transport.ConfigureAwait(true)) {
            McpClient client = await McpClient.CreateAsync(transport, cancellationToken: timeout.Token).ConfigureAwait(true);
            await using (client.ConfigureAwait(true)) {
                tools = await client.ListToolsAsync(new ListToolsRequestParams(), timeout.Token).ConfigureAwait(true);
                acknowledgement = await client.CallToolAsync(
                    new CallToolRequestParams { Name = SiblingToolName, Arguments = AmountArguments() },
                    timeout.Token).ConfigureAwait(true);
                spoofed = await client.CallToolAsync(
                    new CallToolRequestParams { Name = SiblingToolName, Arguments = SpoofedDerivableArguments() },
                    timeout.Token).ConfigureAwait(true);
            }
        }

        // Every request has completed and the host is stopped, so the captured logs are final.
        await app.StopAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        tools.Tools.Select(tool => tool.Name).ShouldBe([SiblingToolName, LifecycleToolName]);
        acknowledgement.IsError.GetValueOrDefault().ShouldBeFalse();
        JsonElement acknowledged = acknowledgement.StructuredContent.ShouldNotBeNull();
        string messageId = acknowledged.GetProperty("messageId").GetString().ShouldNotBeNull();
        string correlationId = acknowledged.GetProperty("correlationId").GetString().ShouldNotBeNull();
        McpAuditLifecycleCommand dispatched = dispatcher.Dispatched.ShouldHaveSingleItem();
        dispatched.TenantId.ShouldBe(AuditTenantId);
        dispatched.UserId.ShouldBe(AuditUserId);
        dispatched.Amount.ShouldBe(42);
        ShouldBeCanonicalUlid(dispatched.MessageId);
        ShouldBeCanonicalUlid(dispatched.CorrelationId);
        dispatched.CorrelationId.ShouldNotBe(dispatched.MessageId);
        dispatched.MessageId.ShouldBe(messageId);
        dispatched.CorrelationId.ShouldBe(correlationId);

        // A client that supplies the derivable values itself is refused before dispatch, and nothing is echoed.
        spoofed.IsError.ShouldBe(true);
        string spoofedAnswer = Wire(spoofed);
        foreach (string value in SpoofedValues) {
            spoofedAnswer.ShouldNotContain(value, Case.Insensitive);
        }

        dispatcher.Dispatched.Count.ShouldBe(1, "the spoofed call must not dispatch.");
        ShouldNotCarryServerOnlyValues(Wire(tools) + Wire(acknowledgement) + spoofedAnswer, "the mapped host responses");
        string allLogs = JoinLogs(logs, frontComposerOnly: false);
        allLogs.ShouldContain("was successfully authenticated");
        ShouldHaveCapturedSdkTraceLogs(logs);
        ShouldNotCarryServerOnlyValues(allLogs, "any log category at Trace level");
    }

    private static async Task<(ListToolsResult Tools, CallToolResult[] Calls)> AskAuditWorldAsync(
        HttpContext? httpContext,
        string hiddenBy,
        bool registerHiddenTool,
        IReadOnlyList<string> toolNames,
        McpAuditLoggerProvider logs,
        McpRecordingAuditCommandService dispatcher,
        McpAuditIdentityTenantToolGate tenantGate,
        McpAuditDenyingCommandPolicyGate policyGate) {
        ServiceCollection services = new();

        // AddFrontComposerMcp only tries to add an IHttpContextAccessor, so the production agent context accessor reads
        // this one. NSubstitute would otherwise return a substitute HttpContext for an unconfigured property.
        IHttpContextAccessor httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _ = httpContextAccessor.HttpContext.Returns(httpContext);
        _ = services.AddSingleton(httpContextAccessor);
        _ = services.AddSingleton<IFrontComposerMcpTenantToolGate>(tenantGate);
        _ = services.AddSingleton<IFrontComposerMcpResourceVisibilityGate>(Substitute.For<IFrontComposerMcpResourceVisibilityGate>());
        _ = services.AddSingleton<IFrontComposerMcpCommandPolicyGate>(policyGate);
        _ = services.AddSingleton<ICommandService>(dispatcher);
        _ = services.AddScoped(_ => Substitute.For<ILifecycleStateService>());
        _ = services.AddFrontComposerMcp(options => {
            options.ApiKeys[AgentKey] = new FrontComposerMcpApiKeyIdentity(AuditTenantId, AuditUserId);
            options.Manifests.Add(AuditManifest(registerHiddenTool ? hiddenBy : null));
        });

        McpSdkPipeSession session = await McpSdkPipeSession.StartAsync(services, logs, TestContext.Current.CancellationToken).ConfigureAwait(true);
        await using (session.ConfigureAwait(true)) {
            ListToolsResult tools = await session.Client.ListToolsAsync(new ListToolsRequestParams(), session.Token).ConfigureAwait(true);
            List<CallToolResult> calls = [];
            foreach (string toolName in toolNames) {
                calls.Add(await session.Client.CallToolAsync(
                    new CallToolRequestParams { Name = toolName, Arguments = AmountArguments() },
                    session.Token).ConfigureAwait(true));
            }

            return (tools, [.. calls]);
        }
    }

    private static McpAuditIdentityTenantToolGate AuditTenantGate(string hiddenBy)
        => new(
            AuditTenantId,
            AuditUserId,
            hiddenToolNames: hiddenBy == HiddenByTenantGate ? [HiddenToolName] : [],
            throwingToolNames: hiddenBy == HiddenByThrowingGate ? [HiddenToolName] : []);

    private static McpManifest AuditManifest(string? registeredHiddenBy) {
        List<McpCommandDescriptor> commands = [AuditCommand(SiblingToolName, "Pay invoice", null, SiblingFingerprint)];
        if (registeredHiddenBy == HiddenByPolicyGate) {
            commands.Add(AuditCommand(PolicyToolName, "Restricted command", AuditPolicyName, HiddenFingerprint));
        }
        else if (registeredHiddenBy is not null) {
            commands.Add(AuditCommand(HiddenToolName, "Hidden command", null, HiddenFingerprint));
        }

        return new McpManifest("frontcomposer.mcp.v1", commands, []);
    }

    private static McpCommandDescriptor AuditCommand(string protocolName, string title, string? policyName, string fingerprint)
        => new(
            protocolName,
            typeof(McpAuditLifecycleCommand).FullName!,
            "Billing",
            title,
            title + ".",
            policyName,
            [new McpParameterDescriptor("Amount", "Int32", "number", true, false, "Amount", null, [], false)],
            ["TenantId", "UserId", "MessageId", "CorrelationId"],
            new SchemaFingerprint(SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, fingerprint));

    private static Dictionary<string, JsonElement> AmountArguments()
        => new(StringComparer.Ordinal) { ["Amount"] = JsonSerializer.SerializeToElement(42) };

    private static Dictionary<string, JsonElement> SpoofedDerivableArguments()
        => new(StringComparer.Ordinal) {
            ["Amount"] = JsonSerializer.SerializeToElement(42),
            ["TenantId"] = JsonSerializer.SerializeToElement(SpoofedValues[0]),
            ["UserId"] = JsonSerializer.SerializeToElement(SpoofedValues[1]),
            ["MessageId"] = JsonSerializer.SerializeToElement(SpoofedValues[2]),
            ["CorrelationId"] = JsonSerializer.SerializeToElement(SpoofedValues[3]),
        };

    private static DefaultHttpContext AdmittedHttpContext()
        => new() {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("TenantId", AuditTenantId), new Claim("UserId", AuditUserId)],
                authenticationType: "audit")),
        };

    private static DefaultHttpContext TenantlessHttpContext()
        => new() {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("UserId", AuditUserId)],
                authenticationType: "audit")),
        };

    private static string Wire<T>(T value) => JsonSerializer.Serialize(value, McpJsonUtilities.DefaultOptions);

    private static string JoinLogs(McpAuditLoggerProvider logs, bool frontComposerOnly)
        => string.Join('\n', logs.Entries.Where(entry => !frontComposerOnly || entry.StartsWith("Hexalith.FrontComposer", StringComparison.Ordinal)));

    private static void ShouldHaveCapturedSdkTraceLogs(McpAuditLoggerProvider logs)
        => logs.Entries.ShouldContain(
            entry => entry.StartsWith("ModelContextProtocol", StringComparison.Ordinal) && entry.Contains(" [Trace]: ", StringComparison.Ordinal),
            "The MCP SDK must log through the captured factory at Trace level, or the leak assertions cover FrontComposer only.");

    private static void ShouldNotCarryServerOnlyValues(string text, string surface) {
        foreach (string value in ServerOnlyValues) {
            text.ShouldNotContain(value, Case.Insensitive, $"{surface} must not carry the server-only value '{value}'.");
        }
    }

    private static void ShouldBeCanonicalUlid(string value) {
        NUlid.Ulid.TryParse(value, out NUlid.Ulid parsed).ShouldBeTrue($"'{value}' must be a ULID.");
        parsed.ToString().ShouldBe(value, "the handle must be the canonical ULID encoding.");
    }

    private static FrontComposerMcpAgentContext AuditContext(string tenantId, string userId)
        => new(tenantId, userId, new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("TenantId", tenantId), new Claim("UserId", userId)],
            authenticationType: "audit")));

    private static async Task<CallToolResult> SubscribeAsync(
        McpClient client,
        string handleName,
        string handle,
        CancellationToken cancellationToken)
        => await client.CallToolAsync(
            new CallToolRequestParams {
                Name = "frontcomposer.lifecycle.subscribe",
                Arguments = new Dictionary<string, JsonElement> { [handleName] = JsonSerializer.SerializeToElement(handle) },
            },
            cancellationToken).ConfigureAwait(true);
}
