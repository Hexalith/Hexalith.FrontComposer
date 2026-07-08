using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
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
        return endpoints.MapMcp(route);
    }
}
