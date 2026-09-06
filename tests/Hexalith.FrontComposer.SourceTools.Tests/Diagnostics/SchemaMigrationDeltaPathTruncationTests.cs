using System.Diagnostics.CodeAnalysis;
using System.Reflection;

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
    private const string PathPrefix = "$.Fields.";
    private const string TruncationMarker = "...";

    // U+1F600 GRINNING FACE — an astral code point, i.e. a UTF-16 surrogate pair.
    private const string SurrogatePair = "\U0001F600";
    private static readonly int _maxPathLength = GetMaxPathLength();

    [Fact]
    public void Compare_RemovedFieldWithShortName_LeavesPathUntruncated() {
        string name = new('a', 32);

        string path = RemovedFieldPath(name);

        path.ShouldBe(PathPrefix + name);
        path.Length.ShouldBeLessThanOrEqualTo(_maxPathLength);
    }

    [Fact]
    public void Compare_RemovedFieldAtExactBoundary_LeavesPathUntruncated() {
        // TruncatePath returns early when the path is exactly the production length bound.
        string name = new('a', _maxPathLength - PathPrefix.Length);

        string path = RemovedFieldPath(name);

        path.ShouldBe(PathPrefix + name);
        path.Length.ShouldBe(_maxPathLength);
        path.ShouldNotEndWith(TruncationMarker);
    }

    [Theory]
    [InlineData(SchemaDeltaKind.RemovedField)]
    [InlineData(SchemaDeltaKind.AddedOptionalField)]
    [InlineData(SchemaDeltaKind.AddedRequiredField)]
    public void Compare_FieldDeltaWithAsciiNameOverBoundary_MatchesLegacySubstringOracle(SchemaDeltaKind expectedKind) {
        string name = new('a', _maxPathLength - PathPrefix.Length + 1);
        string untruncatedPath = PathPrefix + name;

        string path = FieldDeltaPath(name, expectedKind);

        path.ShouldBe(LegacySubstringOracle(untruncatedPath));
        path.Length.ShouldBe(_maxPathLength + TruncationMarker.Length);
    }

    [Theory]
    [InlineData(SchemaDeltaKind.RemovedField, -2)]
    [InlineData(SchemaDeltaKind.RemovedField, -1)]
    [InlineData(SchemaDeltaKind.RemovedField, 0)]
    [InlineData(SchemaDeltaKind.AddedOptionalField, -2)]
    [InlineData(SchemaDeltaKind.AddedOptionalField, -1)]
    [InlineData(SchemaDeltaKind.AddedOptionalField, 0)]
    [InlineData(SchemaDeltaKind.AddedRequiredField, -2)]
    [InlineData(SchemaDeltaKind.AddedRequiredField, -1)]
    [InlineData(SchemaDeltaKind.AddedRequiredField, 0)]
    public void Compare_FieldDeltaWithSurrogatePairAroundCut_MatchesLegacySubstringOracle(
        SchemaDeltaKind expectedKind,
        int pairStartOffsetFromCut) {
        int leading = _maxPathLength + pairStartOffsetFromCut - PathPrefix.Length;
        string name = new string('a', leading) + SurrogatePair + new string('b', 64);
        string untruncatedPath = PathPrefix + name;

        string path = FieldDeltaPath(name, expectedKind);

        path.ShouldBe(LegacySubstringOracle(untruncatedPath));
        if (pairStartOffsetFromCut == -2) {
            path.ShouldContain(SurrogatePair);
            path.Length.ShouldBe(_maxPathLength + TruncationMarker.Length);
        }
        else {
            path.ShouldNotContain(SurrogatePair);
            int expectedRetainedLength = pairStartOffsetFromCut == -1 ? _maxPathLength - 1 : _maxPathLength;
            path.Length.ShouldBe(expectedRetainedLength + TruncationMarker.Length);
        }

        HasUnpairedSurrogate(path).ShouldBeFalse("an unpaired surrogate would encode non-deterministically downstream.");
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
    private static string RemovedFieldPath(string fieldName)
        => FieldDeltaPath(fieldName, SchemaDeltaKind.RemovedField);

    private static string FieldDeltaPath(string fieldName, SchemaDeltaKind expectedKind) {
        var anchor = new SchemaFieldContract("Anchor", "String", "string", true, false);
        var changedField = new SchemaFieldContract(
            fieldName,
            "String",
            "string",
            expectedKind != SchemaDeltaKind.AddedOptionalField,
            expectedKind == SchemaDeltaKind.AddedOptionalField);

        SchemaBaselineSnapshot baseline;
        SchemaBaselineSnapshot current;
        switch (expectedKind) {
            case SchemaDeltaKind.RemovedField:
                baseline = Snapshot([anchor, changedField]);
                current = Snapshot([anchor]);
                break;
            case SchemaDeltaKind.AddedOptionalField:
            case SchemaDeltaKind.AddedRequiredField:
                baseline = Snapshot([anchor]);
                current = Snapshot([anchor, changedField]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(expectedKind), expectedKind, "Unsupported field delta kind.");
        }

        SchemaMigrationDeltaResult result = SchemaMigrationDeltaAnalyzer.Compare(baseline, current);

        SchemaDelta delta = result.Deltas.ShouldHaveSingleItem();
        delta.Kind.ShouldBe(expectedKind);
        return delta.Path;
    }

    [SuppressMessage("Performance", "CA1845:Use span-based 'string.Concat'", Justification = "The legacy Substring implementation is the independent regression oracle.")]
    [SuppressMessage("Style", "IDE0057:Use range operator", Justification = "The legacy Substring implementation is the independent regression oracle.")]
    private static string LegacySubstringOracle(string path) {
        if (path.Length <= _maxPathLength) {
            return path;
        }

        int cut = _maxPathLength;
        if (char.IsHighSurrogate(path[cut - 1])) {
            cut--;
        }

        return path.Substring(0, cut) + TruncationMarker;
    }

    private static int GetMaxPathLength()
        => typeof(SchemaMigrationDeltaAnalyzer)
            .GetField("_maxPathLength", BindingFlags.NonPublic | BindingFlags.Static)?
            .GetRawConstantValue() is int maxPathLength
                ? maxPathLength
                : throw new InvalidOperationException("SchemaMigrationDeltaAnalyzer._maxPathLength is unavailable.");

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
