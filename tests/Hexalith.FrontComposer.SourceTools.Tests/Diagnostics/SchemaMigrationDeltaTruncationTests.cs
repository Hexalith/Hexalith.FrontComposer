using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Schema.Diagnostics;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// AC12 / T8 — when more than 25 deltas exist, the analyzer truncates the delta list but the
/// aggregate <see cref="SchemaCompatibilityDecision"/> still reflects the FULL pre-truncation
/// worst-case category. This test inserts a Breaking delta past index 25 and asserts the
/// truncated result still classifies as Breaking.
/// </summary>
public sealed class SchemaMigrationDeltaTruncationTests {
    [Fact]
    public void Compare_BreakingDeltaPastIndex25_StillProducesBreakingAggregate() {
        // 30 fields total: indexes 0..27 are added optional (compatible), indexes 28..29 add a
        // type-changed required field (Breaking). Index 28 sits past the truncation boundary.
        SchemaBaselineSnapshot baseline = Snapshot([
            new SchemaFieldContract("Anchor", "String", "string", true, false),
        ]);

        SchemaFieldContract[] currentFields = new SchemaFieldContract[30];
        currentFields[0] = new SchemaFieldContract("Anchor", "String", "string", true, false);
        for (int i = 1; i < 28; i++) {
            currentFields[i] = new SchemaFieldContract("Optional" + i.ToString("D2"), "String", "string", false, true);
        }

        // Indexes 28 and 29 add type-changes that, sorted ordinal, push them past index 25 of the
        // truncated delta list while keeping their Breaking decision intact in the aggregate.
        currentFields[28] = new SchemaFieldContract("XBreakingTypeChange", "Int32", "number", true, false);
        currentFields[29] = new SchemaFieldContract("YBreakingTypeChange", "Int32", "number", true, false);

        SchemaBaselineSnapshot current = Snapshot(currentFields);

        SchemaMigrationDeltaResult result = SchemaMigrationDeltaAnalyzer.Compare(baseline, current);

        result.IsTruncated.ShouldBeTrue("AC12 anchor: more than 25 deltas should trigger truncation.");
        result.Decision.ShouldBe(
            SchemaCompatibilityDecision.Breaking,
            "AC12: truncation must not downgrade the aggregate decision; a Breaking delta past index 25 still wins.");
    }

    [Fact]
    public void Compare_OnlyAdditiveDeltasPast25_AggregatesToAdditiveCompatible() {
        // Counter-test: 30 added optional fields → all compatible-additive. The aggregate must
        // not over-correct to Breaking just because truncation occurred.
        SchemaBaselineSnapshot baseline = Snapshot([
            new SchemaFieldContract("Anchor", "String", "string", true, false),
        ]);

        SchemaFieldContract[] currentFields = new SchemaFieldContract[30];
        currentFields[0] = new SchemaFieldContract("Anchor", "String", "string", true, false);
        for (int i = 1; i < 30; i++) {
            currentFields[i] = new SchemaFieldContract("Optional" + i.ToString("D2"), "String", "string", false, true);
        }

        SchemaBaselineSnapshot current = Snapshot(currentFields);

        SchemaMigrationDeltaResult result = SchemaMigrationDeltaAnalyzer.Compare(baseline, current);

        result.IsTruncated.ShouldBeTrue();
        result.Decision.ShouldBe(SchemaCompatibilityDecision.AdditiveCompatible);
    }

    private static SchemaBaselineSnapshot Snapshot(IReadOnlyList<SchemaFieldContract> fields) {
        var document = new SchemaContractDocument(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.ProjectionResource,
            "frontcomposer://Sales/projections/Truncation",
            "frontcomposer.projection-resource.v1",
            "Sales",
            "Sales.TruncationProjection",
            "frontcomposer://Sales/projections/Truncation",
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
                "story-8-6a-truncation",
                requiresMigrationGuide: false),
            payload.Document,
            payload.Fingerprint);
    }
}
