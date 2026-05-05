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

    public FrontComposerMcpDescriptorRegistry(IOptions<FrontComposerMcpOptions> options)
        : this(options, corpusProviders: null) {
    }

    public FrontComposerMcpDescriptorRegistry(
        IOptions<FrontComposerMcpOptions> options,
        IEnumerable<ISkillCorpusFingerprintProvider>? corpusProviders) {
        ArgumentNullException.ThrowIfNull(options);

        List<McpManifest> manifests = [.. options.Value.Manifests];
        foreach (Assembly assembly in options.Value.ManifestAssemblies) {
            manifests.AddRange(LoadGeneratedManifests(assembly));
        }

        // 8-6a review H6: collect skill corpus fingerprints from registered providers so AC8's
        // "runtime aggregate manifest fingerprint includes corpus resource fingerprints" actually
        // holds at registration time. The build-time emitter has no visibility into the runtime
        // corpus and stamps `[]` for the corpus list; the runtime layer in registers the loaded
        // corpus's fingerprints here and recomputes the aggregate. Hosts without a corpus
        // provider get an empty list (which is valid for hosts that ship no skill corpus).
        // 8-6a re-review: defensive coalesce — a custom corpus provider that returns null
        // crashed registry construction and brought the host startup down. Treat null as empty.
        IReadOnlyList<SchemaFingerprint> corpusFingerprints = corpusProviders is null
            ? []
            : [.. corpusProviders.SelectMany(p => p.GetFingerprints() ?? [])];

        ValidateAggregateIntegrity(manifests, corpusFingerprints);
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
        // 8-6a re-review D6: each manifest's claimed Fingerprint represents the aggregate of ITS
        // OWN nested commands/resources (build-time emitter scope). The previous implementation
        // computed a single cross-manifest aggregate across ALL manifests + corpus and compared
        // every per-manifest fingerprint to it, which trips SchemaIntegrityMismatch in any
        // multi-manifest deployment. Per-manifest scope ≠ cross-manifest scope.
        //
        // The corpus argument is intentionally unused inside this loop because the build-time
        // emitter has no visibility into the runtime corpus (Story 8-6 P-5 / D22). The corpus
        // contributes to a separate runtime aggregate fingerprint surfaced via
        // FrontComposerMcpRuntimeManifestAggregator.Compute(...) when callers need the AC8
        // runtime-aggregate-with-corpus invariant.
        _ = corpusFingerprints;
        foreach (McpManifest manifest in manifests) {
            if (manifest.Fingerprint is null) {
                continue;
            }

            // 8-6a review H7: a manifest that ships a fingerprint but stamps a non-canonical-JSON
            // algorithm is an inconsistent claim — the canonical aggregate algorithm is fixed at
            // Sha256CanonicalJsonV1 by D17/AC7. Fail closed in that case.
            if (!string.Equals(manifest.Fingerprint.AlgorithmId, SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1, StringComparison.Ordinal)) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.SchemaIntegrityMismatch);
            }

            SchemaFingerprint perManifestComputed = FrontComposerMcpRuntimeManifestAggregator.Compute([manifest], []);
            if (!string.Equals(manifest.Fingerprint.Value, perManifestComputed.Value, StringComparison.Ordinal)) {
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
                    ["bounds.maxRows"] = options.MaxRowsPerResource.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["bounds.maxColumns"] = options.MaxFieldsPerResource.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ["bounds.maxCellCharacters"] = options.MaxProjectionCellCharacters.ToString(System.Globalization.CultureInfo.InvariantCulture),
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
