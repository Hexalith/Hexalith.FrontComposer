using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Schema;

public sealed class FrontComposerMcpRuntimeManifestAggregator {
    public static SchemaFingerprint Compute(
        IReadOnlyList<McpManifest> manifests,
        IReadOnlyList<SchemaFingerprint> corpusFingerprints) {
        ArgumentNullException.ThrowIfNull(manifests);
        ArgumentNullException.ThrowIfNull(corpusFingerprints);

        SchemaFingerprint?[] nestedFingerprints = [.. manifests
            .SelectMany(m => m.Commands.Select(c => c.Fingerprint).Concat(m.Resources.Select(r => r.Fingerprint)))];
        if (nestedFingerprints.Any(f => f is null) && nestedFingerprints.Any(f => f is not null)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.SchemaIntegrityMismatch);
        }

        // 8-6a re-review: tuple-keyed dedup avoids the literal `:` collision between
        // (algorithmId="alg", value="A:B") and (algorithmId="alg:A", value="B").
        List<SchemaFingerprint> allFingerprints = [];
        HashSet<(string AlgorithmId, string Value)> seen = [];
        foreach (SchemaFingerprint fingerprint in nestedFingerprints
            .Where(f => f is not null)
            .Cast<SchemaFingerprint>()
            .Concat(corpusFingerprints)) {
            if (seen.Add((fingerprint.AlgorithmId, fingerprint.Value))) {
                allFingerprints.Add(fingerprint);
            }
        }

        List<SchemaFieldContract> fields = [];
        foreach (SchemaFingerprint fingerprint in allFingerprints
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
