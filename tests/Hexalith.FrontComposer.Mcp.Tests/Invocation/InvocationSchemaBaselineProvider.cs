using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Mcp.Schema;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

/// <summary>Provides one canonical host-registered schema baseline for an invocation test.</summary>
internal sealed class InvocationSchemaBaselineProvider(SchemaContractDocument document) : ISchemaBaselineProvider
{
    private const string PackageOwner = "Hexalith.FrontComposer";
    private const string FixtureId = "baseline-known-v1";

    /// <summary>Gets the client fingerprint for the host-registered baseline.</summary>
    internal SchemaFingerprint Fingerprint => Snapshot.Fingerprint;

    private SchemaBaselineSnapshot Snapshot { get; } = CreateSnapshot(document);

    /// <inheritdoc />
    public bool TryResolve(SchemaContractFamily family, string packageOwner, string fixtureId, out SchemaBaselineSnapshot? snapshot)
    {
        snapshot = family == Snapshot.Provenance.ContractFamily && packageOwner == PackageOwner && fixtureId == FixtureId
            ? Snapshot
            : null;
        return snapshot is not null;
    }

    private static SchemaBaselineSnapshot CreateSnapshot(SchemaContractDocument document)
    {
        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(document);
        return new SchemaBaselineSnapshot(
            new SchemaBaselineProvenance(document.Family, document.ContractSchemaVersion,
                payload.Fingerprint.AlgorithmId, PackageOwner, FixtureId, requiresMigrationGuide: false),
            payload.Document,
            payload.Fingerprint);
    }
}
