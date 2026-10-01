using System.Net;
using System.Security.Claims;

using Counter.Domain;
using Counter.Web;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp;
using Hexalith.FrontComposer.Mcp.Extensions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Samples;

public sealed class CounterMcpSampleAccessTests
{
    private static readonly string[] AdmittedCommands = [
        "Counter.BatchIncrementCommand.Execute", "Counter.ConfigureCounterCommand.Execute", "Counter.CreateCounterCommand.Execute",
        "Counter.UpdateCounterCommand.Execute", "Default.IncrementCommand.Execute",
    ];
    [Theory]
    [InlineData("demo-tenant", "demo-user", true, true)]
    [InlineData("demo-tenant", "demo-user", false, false)]
    [InlineData("other-tenant", "demo-user", true, false)]
    [InlineData("demo-tenant", "other-user", true, false)]
    [InlineData("", "demo-user", true, false)]
    public async Task SampleGates_RestrictGeneratedDescriptorsToTheAuthenticatedSampleIdentity(
        string tenantId, string userId, bool authenticated, bool admitted)
    {
        ServiceCollection services = [];
        services.AddSingleton<IFrontComposerMcpTenantToolGate, CounterMcpSampleTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, CounterMcpSampleResourceVisibilityGate>();
        services.AddFrontComposerMcp(options => options.ManifestAssemblies.Add(typeof(CounterDomain).Assembly));
        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpDescriptorRegistry registry = provider.GetRequiredService<FrontComposerMcpDescriptorRegistry>();
        ClaimsIdentity identity = new(
            [new Claim("TenantId", tenantId), new Claim("UserId", userId)],
            authenticated ? "FrontComposerMcp" : null);
        FrontComposerMcpAgentContext context = new(tenantId, userId, new ClaimsPrincipal(identity));

        McpResourceDescriptor resource = registry.Resources.Single(descriptor => descriptor.ProtocolUri == "frontcomposer://Counter/projections/CounterProjection");

        registry.Commands.Select(descriptor => descriptor.ProtocolName).Order().ShouldBe(AdmittedCommands);
        foreach (McpCommandDescriptor admittedCommand in registry.Commands)
        {
            (await new CounterMcpSampleTenantToolGate().IsVisibleAsync(admittedCommand, context, TestContext.Current.CancellationToken)).ShouldBe(admitted);
        }
        (await new CounterMcpSampleResourceVisibilityGate().IsVisibleAsync(resource, context, TestContext.Current.CancellationToken)).ShouldBe(admitted);
    }

    [Theory]
    [InlineData("127.0.0.1", true)]
    [InlineData("::1", true)]
    [InlineData("192.0.2.17", false)]
    [InlineData("2001:db8::17", false)]
    [InlineData(null, false)]
    public async Task TestTransportPolicy_AdmitsOnlyLoopbackAddresses(string? address, bool admitted)
    {
        ServiceCollection services = [];
        services.AddLogging();
        services.AddAuthorization();
        using ServiceProvider provider = services.BuildServiceProvider();
        DefaultHttpContext http = new();
        http.Connection.RemoteIpAddress = address is null ? null : IPAddress.Parse(address);
        AuthorizationPolicy policy = CounterMcpSampleAccessPolicy.CreateTestTransportPolicy();

        AuthorizationResult result = await provider.GetRequiredService<IAuthorizationService>()
            .AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()), http, policy);

        result.Succeeded.ShouldBe(admitted);
    }

    [Fact]
    public async Task SampleGates_RejectUnknownDescriptorsAndMismatchedPrincipalClaims()
    {
        ServiceCollection services = [];
        services.AddSingleton<IFrontComposerMcpTenantToolGate, CounterMcpSampleTenantToolGate>();
        services.AddSingleton<IFrontComposerMcpResourceVisibilityGate, CounterMcpSampleResourceVisibilityGate>();
        services.AddFrontComposerMcp(options => options.ManifestAssemblies.Add(typeof(CounterDomain).Assembly));
        using ServiceProvider provider = services.BuildServiceProvider();
        FrontComposerMcpDescriptorRegistry registry = provider.GetRequiredService<FrontComposerMcpDescriptorRegistry>();
        FrontComposerMcpAgentContext context = new("demo-tenant", "demo-user", new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("TenantId", "demo-tenant"), new Claim("UserId", "demo-user")], "FrontComposerMcp")));
        CounterMcpSampleTenantToolGate tools = new();
        CounterMcpSampleResourceVisibilityGate resources = new();
        McpCommandDescriptor command = registry.Commands.Single(descriptor => descriptor.ProtocolName == "Counter.BatchIncrementCommand.Execute");
        McpResourceDescriptor resource = registry.Resources.Single(descriptor => descriptor.ProtocolUri == "frontcomposer://Counter/projections/CounterProjection");

        foreach (McpCommandDescriptor denied in new[] {
            command with { BoundedContext = "Specimens" },
            command with { BoundedContext = "Counter", ProtocolName = "Default.IncrementCommand.Execute", CommandTypeName = "Counter.Domain.IncrementCommand" },
            command with { BoundedContext = "Default", ProtocolName = "Counter.IncrementCommand.Execute", CommandTypeName = "Counter.Domain.IncrementCommand" },
            command with { CommandTypeName = "Other.BatchIncrementCommand" },
            command with { ProtocolName = "Counter.UnknownCommand.Execute" },
        })
        {
            (await tools.IsVisibleAsync(denied, context, TestContext.Current.CancellationToken)).ShouldBeFalse();
        }

        foreach (McpResourceDescriptor denied in new[] {
            resource with { BoundedContext = "Specimens" },
            resource with { ProjectionTypeName = "Other.CounterProjection" },
            resource with { ProtocolUri = "frontcomposer://Counter/projections/UnknownProjection" },
        })
        {
            (await resources.IsVisibleAsync(denied, context, TestContext.Current.CancellationToken)).ShouldBeFalse();
        }

        FrontComposerMcpAgentContext wrongPrincipal = context with
        {
            Principal = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("TenantId", "other-tenant"), new Claim("UserId", "demo-user")], "FrontComposerMcp")),
        };
        (await tools.IsVisibleAsync(command, wrongPrincipal, TestContext.Current.CancellationToken)).ShouldBeFalse();
        (await resources.IsVisibleAsync(resource, wrongPrincipal, TestContext.Current.CancellationToken)).ShouldBeFalse();
    }
}
