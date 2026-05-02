using System.Reflection;

using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp;

public sealed class FrontComposerMcpDescriptorRegistry {
    private readonly IReadOnlyDictionary<string, McpCommandDescriptor> _commands;
    private readonly IReadOnlyDictionary<string, McpResourceDescriptor> _resources;
    private readonly IReadOnlyList<McpCommandDescriptor> _orderedCommands;
    private readonly IReadOnlyList<McpResourceDescriptor> _orderedResources;

    public FrontComposerMcpDescriptorRegistry(IOptions<FrontComposerMcpOptions> options) {
        ArgumentNullException.ThrowIfNull(options);

        List<McpManifest> manifests = [.. options.Value.Manifests];
        foreach (Assembly assembly in options.Value.ManifestAssemblies) {
            manifests.AddRange(LoadGeneratedManifests(assembly));
        }

        _commands = BuildCommandMap(manifests);
        _resources = BuildResourceMap(manifests);
        _orderedCommands = [.. _commands.Values.OrderBy(c => c.ProtocolName, StringComparer.Ordinal)];
        _orderedResources = [.. _resources.Values.OrderBy(r => r.ProtocolUri, StringComparer.Ordinal)];
    }

    public IReadOnlyList<McpCommandDescriptor> Commands => _orderedCommands;

    public IReadOnlyList<McpResourceDescriptor> Resources => _orderedResources;

    public bool TryGetCommand(string protocolName, out McpCommandDescriptor descriptor) {
        if (string.IsNullOrWhiteSpace(protocolName)) {
            descriptor = null!;
            return false;
        }

        return _commands.TryGetValue(protocolName, out descriptor!);
    }

    public bool TryGetResource(string uri, out McpResourceDescriptor descriptor) {
        if (string.IsNullOrWhiteSpace(uri)) {
            descriptor = null!;
            return false;
        }

        return _resources.TryGetValue(uri, out descriptor!);
    }

    private static IEnumerable<McpManifest> LoadGeneratedManifests(Assembly assembly) {
        IEnumerable<Type> types;
        try {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex) {
            // Continue with whatever types loaded successfully so a partial-load assembly does not
            // abort all manifest discovery.
            types = ex.Types.Where(t => t is not null)!;
        }

        foreach (Type type in types) {
            if (type is null || type.GetCustomAttribute<GeneratedManifestAttribute>() is null) {
                continue;
            }

            PropertyInfo? property = type.GetProperty("Manifest", BindingFlags.Public | BindingFlags.Static);
            if (property?.PropertyType == typeof(McpManifest)
                && property.GetValue(null) is McpManifest manifest) {
                yield return manifest;
            }
        }
    }

    private static IReadOnlyDictionary<string, McpCommandDescriptor> BuildCommandMap(IEnumerable<McpManifest> manifests) {
        // OrdinalIgnoreCase regardless of population — keeps lookup semantics stable across the
        // empty / populated state transition.
        Dictionary<string, McpCommandDescriptor> map = new(StringComparer.OrdinalIgnoreCase);
        foreach (McpCommandDescriptor descriptor in manifests.SelectMany(m => m.Commands)) {
            if (!map.TryAdd(descriptor.ProtocolName, descriptor)) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.DuplicateDescriptor);
            }
        }

        return map;
    }

    private static IReadOnlyDictionary<string, McpResourceDescriptor> BuildResourceMap(IEnumerable<McpManifest> manifests) {
        Dictionary<string, McpResourceDescriptor> map = new(StringComparer.OrdinalIgnoreCase);
        foreach (McpResourceDescriptor descriptor in manifests.SelectMany(m => m.Resources)) {
            if (!map.TryAdd(descriptor.ProtocolUri, descriptor)) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.DuplicateDescriptor);
            }
        }

        return map;
    }
}
