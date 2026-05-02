using Hexalith.FrontComposer.Mcp.Invocation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using ModelContextProtocol.AspNetCore;

namespace Hexalith.FrontComposer.Mcp.Extensions;

public static class FrontComposerMcpServiceCollectionExtensions {
    public static IServiceCollection AddFrontComposerMcp(
        this IServiceCollection services,
        Action<FrontComposerMcpOptions>? configure = null) {
        ArgumentNullException.ThrowIfNull(services);

        if (configure is not null) {
            services.Configure(configure);
        }

        services.AddSingleton<IValidateOptions<FrontComposerMcpOptions>, FrontComposerMcpOptionsValidator>();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<FrontComposerMcpDescriptorRegistry>();
        services.TryAddScoped<IFrontComposerMcpAgentContextAccessor, HttpFrontComposerMcpAgentContextAccessor>();
        services.TryAddScoped<FrontComposerMcpCommandInvoker>();
        services.TryAddScoped<FrontComposerMcpProjectionReader>();

        // The MCP SDK's WithTools/WithResources takes a static enumerable, so the descriptor list
        // is materialized once at AddFrontComposerMcp time. Adopters MUST call AddFrontComposerMcp
        // AFTER all options Configure(...) calls; later mutations to FrontComposerMcpOptions are
        // not reflected in the SDK-side tool catalog. The IValidateOptions guard above runs at
        // probe time so misconfiguration fails fast with a deterministic message.
        using ServiceProvider probe = services.BuildServiceProvider();
        FrontComposerMcpDescriptorRegistry registry = probe.GetRequiredService<FrontComposerMcpDescriptorRegistry>();
        IEnumerable<ModelContextProtocol.Server.McpServerTool> tools = registry.Commands
            .Select(c => new FrontComposerMcpTool(c))
            .ToArray();
        IEnumerable<ModelContextProtocol.Server.McpServerResource> resources = registry.Resources
            .Select(r => new FrontComposerMcpResource(r))
            .ToArray();

        services.AddMcpServer()
            .WithHttpTransport()
            .WithTools(tools)
            .WithResources(resources);

        return services;
    }
}

internal sealed class FrontComposerMcpOptionsValidator : IValidateOptions<FrontComposerMcpOptions> {
    public ValidateOptionsResult Validate(string? name, FrontComposerMcpOptions options) {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(options.EndpointPattern) || !options.EndpointPattern.StartsWith('/')) {
            errors.Add($"{nameof(FrontComposerMcpOptions.EndpointPattern)} must be a non-empty path starting with '/'.");
        }

        if (string.IsNullOrWhiteSpace(options.ApiKeyHeaderName)) {
            errors.Add($"{nameof(FrontComposerMcpOptions.ApiKeyHeaderName)} must be a non-empty header name.");
        }

        if (options.MaxArgumentBytes <= 0) {
            errors.Add($"{nameof(FrontComposerMcpOptions.MaxArgumentBytes)} must be positive.");
        }

        if (options.DefaultResourceTake <= 0 || options.MaxResourceTake <= 0) {
            errors.Add("Resource take limits must be positive.");
        }

        if (options.DefaultResourceTake > options.MaxResourceTake) {
            errors.Add($"{nameof(FrontComposerMcpOptions.DefaultResourceTake)} must not exceed {nameof(FrontComposerMcpOptions.MaxResourceTake)}.");
        }

        if (options.MaxRowsPerResource <= 0 || options.MaxFieldsPerResource <= 0) {
            errors.Add("Per-resource render limits must be positive.");
        }

        foreach (KeyValuePair<string, FrontComposerMcpApiKeyIdentity> entry in options.ApiKeys) {
            if (string.IsNullOrWhiteSpace(entry.Key)) {
                errors.Add("API key entries must not have empty/whitespace keys.");
            }

            if (entry.Value is null
                || string.IsNullOrWhiteSpace(entry.Value.TenantId)
                || string.IsNullOrWhiteSpace(entry.Value.UserId)) {
                errors.Add("API key identities must declare non-empty TenantId and UserId.");
            }
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
