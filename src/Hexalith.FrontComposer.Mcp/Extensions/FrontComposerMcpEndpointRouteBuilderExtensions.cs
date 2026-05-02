using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Extensions;

public static class FrontComposerMcpEndpointRouteBuilderExtensions {
    public static IEndpointConventionBuilder MapFrontComposerMcp(this IEndpointRouteBuilder endpoints, string? pattern = null) {
        ArgumentNullException.ThrowIfNull(endpoints);

        IOptions<FrontComposerMcpOptions>? options = endpoints.ServiceProvider.GetService<IOptions<FrontComposerMcpOptions>>();
        if (options is null) {
            throw new InvalidOperationException(
                "FrontComposer MCP services are not registered. Call IServiceCollection.AddFrontComposerMcp(...) before MapFrontComposerMcp(...).");
        }

        string route = pattern ?? options.Value.EndpointPattern;
        return endpoints.MapMcp(route);
    }
}
