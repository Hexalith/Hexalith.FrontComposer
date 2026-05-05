using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Schema;

public sealed class InMemorySchemaBaselineProvider : ISchemaBaselineProvider {
    private const string PackageOwner = "Hexalith.FrontComposer";
    private static readonly IReadOnlyDictionary<BaselineKey, SchemaBaselineSnapshot> Snapshots =
        new Dictionary<BaselineKey, SchemaBaselineSnapshot> {
            [new(SchemaContractFamily.ProjectionResource, PackageOwner, "baseline-known-v1")] =
                CreateSnapshot(SchemaContractFamily.ProjectionResource, "baseline-known-v1"),
            [new(SchemaContractFamily.CommandTool, PackageOwner, "baseline-known-v1")] =
                CreateSnapshot(SchemaContractFamily.CommandTool, "baseline-known-v1"),
            [new(SchemaContractFamily.MarkdownRendererContract, PackageOwner, "surface-metadata-only-renderer")] =
                CreateSnapshot(SchemaContractFamily.MarkdownRendererContract, "surface-metadata-only-renderer"),
        };

    public bool TryResolve(
        SchemaContractFamily family,
        string packageOwner,
        string fixtureId,
        out SchemaBaselineSnapshot? snapshot) {
        snapshot = null;
        if (!IsSafeIdentifier(packageOwner) || !IsSafeIdentifier(fixtureId)) {
            return false;
        }

        return Snapshots.TryGetValue(new BaselineKey(family, packageOwner, fixtureId), out snapshot);
    }

    private static bool IsSafeIdentifier(string value) {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128) {
            return false;
        }

        if (!char.IsLetterOrDigit(value[0])) {
            return false;
        }

        foreach (char ch in value) {
            if (!char.IsLetterOrDigit(ch) && ch is not '.' and not '_' and not '-') {
                return false;
            }
        }

        return true;
    }

    private static SchemaBaselineSnapshot CreateSnapshot(SchemaContractFamily family, string fixtureId) {
        var document = new SchemaContractDocument(
            "frontcomposer.schema.contract.v1",
            family,
            "frontcomposer://baseline/" + fixtureId,
            "frontcomposer." + family.ToString().ToLowerInvariant() + ".v1",
            "Hexalith",
            "Hexalith.FrontComposer." + fixtureId,
            "frontcomposer://baseline/" + fixtureId,
            [new SchemaFieldContract("Number", "String", "string", true, false)],
            [new SchemaCollectionContract("fields", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string> {
                ["fixtureId"] = fixtureId,
            });
        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(document);
        return new SchemaBaselineSnapshot(
            new SchemaBaselineProvenance(
                family,
                document.ContractSchemaVersion,
                SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
                PackageOwner,
                fixtureId,
                requiresMigrationGuide: false),
            payload.Document,
            payload.Fingerprint);
    }

    private sealed record BaselineKey(SchemaContractFamily Family, string PackageOwner, string FixtureId);
}
