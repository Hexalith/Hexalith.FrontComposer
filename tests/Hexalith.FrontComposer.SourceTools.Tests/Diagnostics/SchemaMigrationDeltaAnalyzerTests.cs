using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Schema.Diagnostics;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

public sealed class SchemaMigrationDeltaAnalyzerTests {
    [Fact]
    public void Compare_ClassifiesOptionalAndRequiredFields() {
        SchemaBaselineSnapshot baseline = Snapshot([
            new SchemaFieldContract("Number", "String", "string", true, false),
        ]);
        SchemaBaselineSnapshot current = Snapshot([
            new SchemaFieldContract("Number", "String", "string", true, false),
            new SchemaFieldContract("OptionalNote", "String", "string", false, true),
            new SchemaFieldContract("RequiredCode", "String", "string", true, false),
        ]);

        SchemaMigrationDeltaResult result = SchemaMigrationDeltaAnalyzer.Compare(baseline, current);

        result.Decision.ShouldBe(SchemaCompatibilityDecision.Breaking);
        result.Deltas.ShouldContain(d => d.Kind == SchemaDeltaKind.AddedOptionalField);
        result.Deltas.ShouldContain(d => d.Kind == SchemaDeltaKind.AddedRequiredField);
    }

    [Fact]
    public void Compare_RejectsMismatchedCanonicalizerMetadata() {
        SchemaBaselineSnapshot baseline = Snapshot([]);
        SchemaBaselineSnapshot current = Snapshot([]) with {
            Provenance = Snapshot([]).Provenance with { CanonicalizerVersion = "future" },
        };

        SchemaMigrationDeltaAnalyzer.Compare(baseline, current)
            .Decision.ShouldBe(SchemaCompatibilityDecision.Unknown);
    }

    private static SchemaBaselineSnapshot Snapshot(IReadOnlyList<SchemaFieldContract> fields) {
        var document = new SchemaContractDocument(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.ProjectionResource,
            "frontcomposer://Sales/projections/OrderQueue",
            "frontcomposer.projection-resource.v1",
            "Sales",
            "Orders.OrderQueueProjection",
            "frontcomposer://Sales/projections/OrderQueue",
            fields,
            [new SchemaCollectionContract("fields", SchemaCollectionOrder.NonStructuralSorted, "name")],
            new Dictionary<string, string>());
        SchemaCanonicalPayload payload = CanonicalSchemaMaterial.CreatePayload(document);
        return new SchemaBaselineSnapshot(
            new SchemaBaselineProvenance(
                SchemaContractFamily.ProjectionResource,
                "frontcomposer.projection-resource.v1",
                SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
                "Hexalith.FrontComposer",
                "baseline-known-v1",
                requiresMigrationGuide: false),
            payload.Document,
            payload.Fingerprint);
    }
}
