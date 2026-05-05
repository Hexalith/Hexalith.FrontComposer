using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Schema;

public sealed class FrontComposerMcpRuntimeManifestAggregator {
    public static SchemaFingerprint Compute(
        IReadOnlyList<McpManifest> manifests,
        IReadOnlyList<SchemaFingerprint> corpusFingerprints) {
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentNullException.ThrowIfNull(corpusFingerprints);

        List<SchemaFieldContract> fields = [];
        foreach (SchemaFingerprint fingerprint in manifests
            .SelectMany(m => m.Commands.Select(c => c.Fingerprint).Concat(m.Resources.Select(r => r.Fingerprint)))
            .Where(f => f is not null)
            .Cast<SchemaFingerprint>()
            .Concat(corpusFingerprints)
            .OrderBy(f => f.AlgorithmId, StringComparer.Ordinal)
            .ThenBy(f => f.Value, StringComparer.Ordinal)) {
            fields.Add(new SchemaFieldContract(
                fingerprint.AlgorithmId + ":" + fingerprint.Value,
                "SchemaFingerprint",
                "string",
                true,
                false));
        }

        var document = new SchemaContractDocument(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.AggregateMcpManifest,
            "frontcomposer://mcp/runtime-manifest",
            "frontcomposer.mcp-manifest.aggregate.v1",
            null,
            null,
            "frontcomposer://mcp/runtime-manifest",
            fields,
            [new SchemaCollectionContract("fingerprints", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string> {
                ["corpusFingerprintCount"] = corpusFingerprints.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });
        return CanonicalSchemaMaterial.CreatePayload(document).Fingerprint;
    }
}
