using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.Schema.Diagnostics;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Diagnostics;

/// <summary>
/// Story 11.21 CA1845 regression cover for the P-45 surrogate-safe delta-path truncation in
/// <c>SchemaMigrationDeltaAnalyzer.TruncatePath</c>. The final <c>Substring(0, cut) + "..."</c> was
/// replaced with the span-based <c>string.Concat(path.AsSpan(0, cut), "...")</c> on net10.0. Both
/// forms must produce the identical string, and the surrogate step-back that keeps the cut off a
/// paired high surrogate must survive: an unpaired high surrogate at the boundary would make
/// downstream JSON/structured-log encoders emit U+FFFD non-deterministically.
/// </summary>
public sealed class SchemaMigrationDeltaPathTruncationTests {
    private const int MaxPathLength = 256;
    private const string PathPrefix = "$.Fields.";
    private const string TruncationMarker = "...";

    // U+1F600 GRINNING FACE — an astral code point, i.e. a UTF-16 surrogate pair.
    private const string SurrogatePair = "\U0001F600";

    [Fact]
    public void Compare_RemovedFieldWithShortName_LeavesPathUntruncated() {
        string name = new('a', 32);

        string path = RemovedFieldPath(name);

        path.ShouldBe(PathPrefix + name);
        path.Length.ShouldBeLessThanOrEqualTo(MaxPathLength);
    }

    [Fact]
    public void Compare_RemovedFieldAtExactBoundary_LeavesPathUntruncated() {
        // Exactly MaxPathLength characters: TruncatePath returns early on `path.Length <= 256`.
        string name = new('a', MaxPathLength - PathPrefix.Length);

        string path = RemovedFieldPath(name);

        path.Length.ShouldBe(MaxPathLength);
        path.ShouldNotEndWith(TruncationMarker);
    }

    [Fact]
    public void Compare_RemovedFieldWithAsciiNameOverBoundary_TruncatesAtExactlyMaxPathLength() {
        string name = new('a', 400);

        string path = RemovedFieldPath(name);

        path.ShouldBe(new string('a', MaxPathLength - PathPrefix.Length).Insert(0, PathPrefix) + TruncationMarker);
        path.Length.ShouldBe(MaxPathLength + TruncationMarker.Length);
    }

    [Fact]
    public void Compare_RemovedFieldWhoseCutSplitsSurrogatePair_StepsBackAndLeavesNoUnpairedSurrogate() {
        // Place the surrogate pair so the naive cut at index 256 would land between its two code
        // units: path[255] is the high surrogate and path[256] is the low surrogate.
        int leading = MaxPathLength - 1 - PathPrefix.Length;
        string name = new string('a', leading) + SurrogatePair + new string('b', 64);

        string path = RemovedFieldPath(name);

        // The step-back drops the whole pair rather than emitting a lone high surrogate.
        path.ShouldBe(PathPrefix + new string('a', leading) + TruncationMarker);
        path.Length.ShouldBe(MaxPathLength - 1 + TruncationMarker.Length);
        HasUnpairedSurrogate(path).ShouldBeFalse("an unpaired surrogate would encode non-deterministically downstream.");
    }

    [Fact]
    public void Compare_RemovedFieldWithSurrogatePairFullyInsideBudget_KeepsThePairIntact() {
        // The pair sits well before the cut, so it must survive verbatim.
        string name = new string('a', 32) + SurrogatePair + new string('b', 400);

        string path = RemovedFieldPath(name);

        path.ShouldContain(SurrogatePair);
        path.Length.ShouldBe(MaxPathLength + TruncationMarker.Length);
        HasUnpairedSurrogate(path).ShouldBeFalse();
    }

    private static bool HasUnpairedSurrogate(string value) {
        for (int i = 0; i < value.Length; i++) {
            char c = value[i];
            if (char.IsHighSurrogate(c)) {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1])) {
                    return true;
                }

                i++;
                continue;
            }

            if (char.IsLowSurrogate(c)) {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Drives the analyzer's public entry point so the truncation is observed through the real
    /// delta pipeline rather than a private helper: a field present in the baseline and absent from
    /// the current snapshot emits a RemovedField delta whose path is <c>$.Fields.{name}</c>.
    /// </summary>
    private static string RemovedFieldPath(string fieldName) {
        SchemaBaselineSnapshot baseline = Snapshot([
            new SchemaFieldContract("Anchor", "String", "string", true, false),
            new SchemaFieldContract(fieldName, "String", "string", true, false),
        ]);
        SchemaBaselineSnapshot current = Snapshot([
            new SchemaFieldContract("Anchor", "String", "string", true, false),
        ]);

        SchemaMigrationDeltaResult result = SchemaMigrationDeltaAnalyzer.Compare(baseline, current);

        SchemaDelta removed = result.Deltas.ShouldHaveSingleItem();
        removed.Kind.ShouldBe(SchemaDeltaKind.RemovedField);
        return removed.Path;
    }

    private static SchemaBaselineSnapshot Snapshot(IReadOnlyList<SchemaFieldContract> fields) {
        var document = new SchemaContractDocument(
            "frontcomposer.schema.contract.v1",
            SchemaContractFamily.ProjectionResource,
            "frontcomposer://Sales/projections/PathTruncation",
            "frontcomposer.projection-resource.v1",
            "Sales",
            "Sales.PathTruncationProjection",
            "frontcomposer://Sales/projections/PathTruncation",
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
                "story-11-21-path-truncation",
                requiresMigrationGuide: true),
            payload.Document,
            payload.Fingerprint);
    }
}
