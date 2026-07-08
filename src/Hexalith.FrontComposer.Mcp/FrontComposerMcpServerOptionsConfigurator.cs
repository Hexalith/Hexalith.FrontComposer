using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Mcp.Skills;

using Microsoft.Extensions.Options;

using ModelContextProtocol.Server;

namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Adds FrontComposer projection and skill resources to MCP server options from the real DI provider.
/// </summary>
internal sealed class FrontComposerMcpServerOptionsConfigurator(
    FrontComposerMcpDescriptorRegistry registry,
    FrontComposerSkillResourceProvider skillProvider) : IConfigureOptions<McpServerOptions> {
    /// <inheritdoc/>
    public void Configure(McpServerOptions options) {
        ArgumentNullException.ThrowIfNull(options);

        EnsureNoSkillUriCollisions(registry.Resources);
        McpServerResourceCollection resources = options.ResourceCollection ?? new McpServerResourceCollection();
        HashSet<string> existingUris = new(
            resources
            .Select(r => r.ProtocolResource?.Uri)
            .OfType<string>(),
            StringComparer.Ordinal);
        foreach (McpResourceDescriptor descriptor in registry.Resources) {
            if (existingUris.Add(descriptor.ProtocolUri)) {
                resources.Add(new FrontComposerMcpResource(descriptor));
            }
        }

        foreach (FrontComposerSkillMcpResource resource in skillProvider.CreateMcpResources()) {
            string? uri = resource.ProtocolResource?.Uri;
            if (string.IsNullOrWhiteSpace(uri) || existingUris.Add(uri)) {
                resources.Add(resource);
            }
        }

        options.ResourceCollection = resources;
    }

    private static void EnsureNoSkillUriCollisions(IEnumerable<McpResourceDescriptor> descriptors) {
        foreach (McpResourceDescriptor descriptor in descriptors) {
            if (descriptor.ProtocolUri.StartsWith("frontcomposer://skills/", StringComparison.OrdinalIgnoreCase)) {
                throw new InvalidOperationException(
                    $"AddFrontComposerMcp detected a URI collision between a manifest projection resource and a skill resource ('{descriptor.ProtocolUri}'). " +
                    "Skill resource URIs are reserved under the 'frontcomposer://skills/' prefix; rename the colliding projection resource.");
            }
        }
    }
}
