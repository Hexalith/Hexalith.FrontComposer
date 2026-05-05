using System.Reflection;

using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.Mcp.Schema;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp;

public sealed class FrontComposerMcpDescriptorRegistry : IFrontComposerMcpDescriptorEpochProvider {
    private readonly IReadOnlyDictionary<string, McpCommandDescriptor> _commands;
    private readonly IReadOnlyDictionary<string, McpResourceDescriptor> _resources;
    private readonly IReadOnlyList<McpCommandDescriptor> _orderedCommands;
    private readonly IReadOnlyList<McpResourceDescriptor> _orderedResources;
    private readonly IReadOnlyDictionary<string, string> _normalizedNames;
    private readonly IReadOnlyList<FrontComposerRenderContract> _renderContracts;

    public FrontComposerMcpDescriptorRegistry(IOptions<FrontComposerMcpOptions> options) {
        ArgumentNullException.ThrowIfNull(options);

        List<McpManifest> manifests = [.. options.Value.Manifests];
        foreach (Assembly assembly in options.Value.ManifestAssemblies) {
            manifests.AddRange(LoadGeneratedManifests(assembly));
        }

        ValidateAggregateIntegrity(manifests, []);
        _commands = BuildCommandMap(manifests);
        _resources = BuildResourceMap(manifests);
        _orderedCommands = [.. _commands.Values.OrderBy(c => c.ProtocolName, StringComparer.Ordinal)];
        _orderedResources = [.. _resources.Values.OrderBy(r => r.ProtocolUri, StringComparer.Ordinal)];
        _normalizedNames = BuildNormalizedNameMap(_orderedCommands);
        _renderContracts = BuildRenderContracts(_orderedResources, options.Value);
    }

    // DN-3: the in-memory manifest registry is immutable for the lifetime of the host (manifests
    // are loaded once at AddFrontComposerMcp time and never mutated). Static (1, 1) epochs are
    // therefore the correct baseline. Hosts adding hot-reload semantics must register a custom
    // IFrontComposerMcpDescriptorEpochProvider that increments on every catalog/descriptor
    // mutation; the snapshot/revalidation contract in FrontComposerMcpProjectionReader will
    // detect drift via the provider and surface StaleDescriptor without rendering partial output.
    public McpDescriptorEpochs GetEpochs()
        => new(DescriptorEpoch: 1, CatalogEpoch: 1);

    public IReadOnlyList<McpCommandDescriptor> Commands => _orderedCommands;

    public IReadOnlyList<McpResourceDescriptor> Resources => _orderedResources;

    public IReadOnlyList<FrontComposerRenderContract> GetRenderContracts() => _renderContracts;

    /// <summary>
    /// Returns the normalized matching key for the descriptor, computed once at registry construction
    /// time per spec T7 ("Precompute immutable normalized lookup keys at startup"). Returns
    /// <see cref="string.Empty"/> for descriptors whose protocol name produces an unsupported normalized form.
    /// </summary>
    public string GetNormalizedName(McpCommandDescriptor descriptor) {
        ArgumentNullException.ThrowIfNull(descriptor);
        return _normalizedNames.TryGetValue(descriptor.ProtocolName, out string? value) ? value : string.Empty;
    }

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

    private static IReadOnlyDictionary<string, string> BuildNormalizedNameMap(IEnumerable<McpCommandDescriptor> commands) {
        Dictionary<string, string> map = new(StringComparer.Ordinal);
        foreach (McpCommandDescriptor descriptor in commands) {
            string normalized = FrontComposerMcpToolAdmissionService.NormalizeForMatching(descriptor.ProtocolName, out bool unsupported);
            map[descriptor.ProtocolName] = unsupported ? string.Empty : normalized;
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

    private static void ValidateAggregateIntegrity(
        IReadOnlyList<McpManifest> manifests,
        IReadOnlyList<SchemaFingerprint> corpusFingerprints) {
        foreach (McpManifest manifest in manifests) {
            if (manifest.Fingerprint is null
                || !string.Equals(manifest.Fingerprint.AlgorithmId, SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, StringComparison.Ordinal)) {
                continue;
            }

            SchemaFingerprint computed = FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], corpusFingerprints);
            if (!string.Equals(manifest.Fingerprint.Value, computed.Value, StringComparison.Ordinal)) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.SchemaIntegrityMismatch);
            }
        }
    }

    private static IReadOnlyList<FrontComposerRenderContract> BuildRenderContracts(
        IReadOnlyList<McpResourceDescriptor> resources,
        FrontComposerMcpOptions options)
        => [.. resources.Select(resource => {
            SchemaContractDocument document = new(
                "frontcomposer.schema.contract.v1",
                SchemaContractFamily.MarkdownRendererContract,
                resource.ProtocolUri + "#renderer",
                "frontcomposer.renderer.markdown.v1",
                resource.BoundedContext,
                resource.ProjectionTypeName,
                resource.ProtocolUri,
                [],
                [new SchemaCollectionContract("fields", SchemaCollectionOrder.NonStructuralSorted, "name")],
                new Dictionary<string, string> {
                    ["bounds.maxCharacters"] = options.MaxProjectionMarkdownCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["bounds.maxFieldCharacters"] = options.MaxProjectionCellCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["outputContentType"] = "text/markdown",
                    ["renderStrategy"] = resource.RenderStrategy.ToString(),
                });
            SchemaFingerprint fingerprint = CanonicalSchemaMaterial.CreatePayload(document).Fingerprint;
            return new FrontComposerRenderContract(
                resource.ProtocolUri + "#renderer",
                "frontcomposer.renderer.markdown.v1",
                RenderSurfaceKind.McpMarkdown,
                "text/markdown",
                [
                    RenderCapability.ProjectionTable,
                    RenderCapability.EmptyState,
                    RenderCapability.BoundedMarkdown,
                    RenderCapability.SanitizedInertText,
                ],
                fingerprint,
                new RenderBounds(
                    options.MaxRowsPerResource,
                    options.MaxFieldsPerResource,
                    options.MaxProjectionMarkdownCharacters,
                    options.MaxProjectionCellCharacters));
        })];
}
