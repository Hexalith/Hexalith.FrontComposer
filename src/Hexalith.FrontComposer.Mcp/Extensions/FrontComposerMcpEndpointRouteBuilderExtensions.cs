using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Hexalith.FrontComposer.Mcp.Extensions;

public static class FrontComposerMcpEndpointRouteBuilderExtensions {
    public static IEndpointConventionBuilder MapFrontComposerMcp(this IEndpointRouteBuilder endpoints, string? pattern = null) {
        ArgumentNullException.ThrowIfNull(endpoints);

        IOptions<FrontComposerMcpOptions>? options = endpoints.ServiceProvider.GetService<IOptions<FrontComposerMcpOptions>>() ?? throw new InvalidOperationException(
                "FrontComposer MCP services are not registered. Call IServiceCollection.AddFrontComposerMcp(...) before MapFrontComposerMcp(...).");
        string route = pattern ?? options.Value.EndpointPattern;
        _ = endpoints.ServiceProvider.GetRequiredService<IOptions<McpServerOptions>>().Value.ResourceCollection;
        IHostEnvironment? environment = endpoints.ServiceProvider.GetService<IHostEnvironment>();
        if (!string.Equals(environment?.EnvironmentName, Environments.Development, StringComparison.Ordinal)) {
            using IServiceScope scope = endpoints.ServiceProvider.CreateScope();
            if (scope.ServiceProvider.GetRequiredService<IFrontComposerMcpTenantToolGate>() is AllowAllMcpTenantToolGate) {
                throw new InvalidOperationException("MapFrontComposerMcp requires a restrictive IFrontComposerMcpTenantToolGate outside Development; AllowAllMcpTenantToolGate is sample/dev only.");
            }

            if (scope.ServiceProvider.GetRequiredService<IFrontComposerMcpResourceVisibilityGate>() is AllowAllResourceVisibilityGate) {
                throw new InvalidOperationException("MapFrontComposerMcp requires a restrictive IFrontComposerMcpResourceVisibilityGate outside Development; AllowAllResourceVisibilityGate is sample/dev only.");
            }
        }

        // The SDK mapping carries authorization metadata even if the host forgets to add it.
        // Authentication schemes and policies remain host-owned.
        return endpoints.MapMcp(route).RequireAuthorization();
    }
}
