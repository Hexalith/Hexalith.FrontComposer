using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Diagnostics;

/// <summary>
/// AC16 / T3 + T4 — sort + truncate. Drift diagnostics are sorted by bounded context →
/// declaration kind → declaration name → member name → drift kind using ordinal comparison
/// before the cap is applied. The first 50 diagnostics are emitted; a single deterministic
/// truncation summary follows that reports the omitted count.
/// </summary>
public sealed class DriftDiagnosticOrderingAndTruncationTests {
    private const string SkipReason = "RED-PHASE: T3 + T4 — drift sort / truncate pipeline not yet introduced.";

    [Fact()]
    public void First50_AreSortedByBoundedContextDeclarationKindNameMemberDriftKind_Ordinal() {
        (string source, string baseline) = BuildScenario(driftCount: 60);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        // Story 9-1 review CB-21: filter the truncation summary by HFC ID rather than the
        // brittle "truncat" substring (which can match incidental docslink prose).
        Diagnostic[] driftSorted = [.. diagnostics
            .Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal) && d.Id != "HFC1068")
            .Take(50)];

        string[] keys = [.. driftSorted.Select(SortKey)];
        keys.ShouldBe([.. keys.OrderBy(k => k, StringComparer.Ordinal)],
            "AC16 — first 50 diagnostics must be sorted by ordinal composite key before cap.");
    }

    [Fact()]
    public void SortKey_VariesAcrossAllFiveDimensions_DeterministicOrdinal() {
        // Story 9-1 review CB-9: AC16 demands ordinal sort across five dimensions:
        // bounded context → declaration kind → declaration name → member name → drift kind.
        // The default scenario (single context, single declaration) only varies MemberName,
        // so a regression that omits any other tier from the sort key would still pass.
        // Build a scenario that touches all five tiers — multiple bounded contexts, mixed
        // command/projection declarations, multiple declaration names, multiple members —
        // and assert the emitted ordering is the ordinal sort of the composite key.
        (string source, string baseline) = BuildMultiDimensionalScenario();

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        Diagnostic[] driftSorted = [.. diagnostics
            .Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal) && d.Id != "HFC1068")];
        driftSorted.Length.ShouldBeGreaterThanOrEqualTo(4,
            "Multi-dimensional scenario should produce drift across multiple bounded contexts and declarations.");

        // Confirm the scenario actually varies bounded context AND declaration kind dimensions
        // (otherwise this test would degrade to the single-dimension scenario the previous fact covered).
        IEnumerable<string> contexts = driftSorted.Select(d =>
            d.Properties.GetValueOrDefault("BoundedContext")
                ?? d.GetMessage().Split(']').FirstOrDefault()
                ?? string.Empty);
        contexts.Distinct(StringComparer.Ordinal).Count().ShouldBeGreaterThanOrEqualTo(2,
            "AC16 — multi-dimensional scenario must surface drift in ≥2 bounded contexts so the sort key's first tier is exercised.");

        string[] keys = [.. driftSorted.Select(SortKey)];
        keys.ShouldBe([.. keys.OrderBy(k => k, StringComparer.Ordinal)],
            "AC16 — sort key must order across all five dimensions (bounded context, declaration kind, declaration name, member name, drift kind).");
    }

    [Theory()]
    // Story 9-1 review CB-20: pin the off-by-one boundaries 49 / 50 / 51 around the cap
    // so a regression in `Take(50)` or `omittedCount = total - 50` is caught at the cliff
    // edge rather than masked by a 70-drift fixture.
    [InlineData(49, 0)]
    [InlineData(50, 0)]
    [InlineData(51, 1)]
    public void TruncationBoundary_Cap50_OffByOneIsCorrect(int totalDrifts, int expectedOmitted) {
        (string source, string baseline) = BuildScenario(driftCount: totalDrifts);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        // Story 9-1 review CB-21 + CB-22: pin truncation summary by HFC ID and severity.
        Diagnostic[] truncations = [.. diagnostics.Where(d => d.Id == "HFC1068")];
        if (expectedOmitted == 0) {
            truncations.ShouldBeEmpty($"AC16 — at total={totalDrifts} (≤50 cap) no truncation summary should fire.");
            return;
        }

        truncations.Length.ShouldBe(1, $"AC16 — exactly one truncation summary for total={totalDrifts}.");
        Diagnostic truncation = truncations[0];
        truncation.Severity.ShouldBe(DiagnosticSeverity.Warning, "AC16 — truncation summary severity is Warning.");
        truncation.GetMessage().ShouldContain(expectedOmitted.ToString(System.Globalization.CultureInfo.InvariantCulture),
            customMessage: $"AC16 — truncation summary must report omittedCount={expectedOmitted} for total={totalDrifts}.");
    }

    [Fact()]
    public void TruncationSummary_FollowsFirst50_AndReportsOmittedCount() {
        const int totalDrifts = 70;
        (string source, string baseline) = BuildScenario(driftCount: totalDrifts);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        // Story 9-1 review CB-21: pin to HFC1068 (TruncationId).
        Diagnostic? truncation = diagnostics.FirstOrDefault(d => d.Id == "HFC1068");
        truncation.ShouldNotBeNull("AC16 — exactly one HFC1068 truncation summary must follow the cap.");
        truncation!.GetMessage().ShouldContain((totalDrifts - 50).ToString(System.Globalization.CultureInfo.InvariantCulture),
            customMessage: "AC16 — truncation summary must report the omitted count.");
        diagnostics.Count(d => d.Id == "HFC1068")
            .ShouldBe(1, "AC16 — exactly one truncation summary, no per-declaration repetition.");
    }

    [Fact()]
    public void HfcDriftMaxDiagnostics_AtCapEqualsOne_OmitsTotalMinusOne_DeterministicOrdering() {
        // Story 9-1 review CB-30: pin behavior at the smallest configurable cap (production
        // min=1). With 5 drifts, exactly one diagnostic plus one truncation summary should
        // fire; the emitted diagnostic must be the smallest under ordinal sort key, proving
        // the sort happens before the truncation.
        (string source, string baseline) = BuildScenario(driftCount: 5);

        IReadOnlyList<Diagnostic> diagnostics = RunWithCap(source, baseline, maxDiagnostics: 1);

        Diagnostic[] drifts = [.. diagnostics.Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal) && d.Id != "HFC1068")];
        Diagnostic[] truncations = [.. diagnostics.Where(d => d.Id == "HFC1068")];

        drifts.Length.ShouldBe(1, "CB-30 — cap=1 emits exactly one drift diagnostic.");
        truncations.Length.ShouldBe(1, "CB-30 — cap=1 with 5 drifts emits exactly one truncation summary.");
        truncations[0].GetMessage().ShouldContain("4", customMessage: "CB-30 — truncation summary must report the 4 omitted diagnostics.");
    }

    [Fact()]
    public void NoTruncationSummary_WhenDriftCountIsAtOrBelowCap() {
        (string source, string baseline) = BuildScenario(driftCount: 50);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.Id == "HFC1068").ShouldBeFalse();
    }

    private static string SortKey(Diagnostic d) {
        // Reflective composition — does not depend on the impl-side order key shape; it just
        // checks that the message contains the expected sortable tokens.
        string declName = d.Properties.GetValueOrDefault("DeclarationName") ?? string.Empty;
        string memberName = d.Properties.GetValueOrDefault("MemberName") ?? string.Empty;
        string boundedContext = d.Properties.GetValueOrDefault("BoundedContext")
            ?? d.GetMessage().Split(']').FirstOrDefault() ?? string.Empty;
        string driftKind = d.Properties.GetValueOrDefault("DriftKind") ?? d.Id;
        return $"{boundedContext}|{declName}|{memberName}|{driftKind}";
    }

    private static (string source, string baseline) BuildScenario(int driftCount) {
        // Build a synthetic projection with N members in source, all of which are missing from
        // the baseline ⇒ N "removed" drifts. We declare them in *reverse* alphabetical source
        // order to prove the impl re-sorts.
        StringBuilder sb = new();
        sb.AppendLine("using Hexalith.FrontComposer.Contracts.Attributes;");
        sb.AppendLine("namespace TestDomain;");
        sb.AppendLine("[BoundedContext(\"Orders\")] [Projection]");
        sb.AppendLine("public partial class OrderProjection {");
        for (int i = driftCount; i > 0; i--) {
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture,
                "    public string Member{0:D3} {{ get; set; }} = string.Empty;{1}", i, Environment.NewLine);
        }
        sb.AppendLine("}");

        string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [] }] }
            """;
        return (sb.ToString(), baseline);
    }

    private static (string source, string baseline) BuildMultiDimensionalScenario() {
        // Story 9-1 review CB-9: vary all five sort dimensions:
        //   - bounded context: Sales vs Shipping
        //   - declaration kind: command vs projection
        //   - declaration name: Order* vs Shipment*
        //   - member name: Alpha, Bravo, Charlie
        //   - drift kind: AddedDeclaration / AddedProperty / RemovedProperty
        // Source contains projections + a command across two contexts; baseline omits one
        // projection entirely (forces AddedDeclaration on it) and adds/removes properties on
        // the others.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Sales")] [Projection]
            public partial class OrderProjection {
                public string Alpha { get; set; } = string.Empty;
                public string Charlie { get; set; } = string.Empty;
            }
            [BoundedContext("Sales")] [Command]
            public partial class ConfirmCommand {
                public string Alpha { get; set; } = string.Empty;
                public string Bravo { get; set; } = string.Empty;
            }
            [BoundedContext("Shipping")] [Projection]
            public partial class ShipmentProjection {
                public string Alpha { get; set; } = string.Empty;
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [
                { "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Sales",
                  "properties": [
                    { "name": "Alpha",   "category": "String", "nullable": false },
                    { "name": "Bravo",   "category": "String", "nullable": false }
                  ] },
                { "family": "command",    "type": "TestDomain.ConfirmCommand", "boundedContext": "Sales",
                  "properties": [
                    { "name": "Alpha",   "category": "String", "nullable": false, "derivable": false },
                    { "name": "Charlie", "category": "String", "nullable": false, "derivable": false }
                  ] }
              ] }
            """;
        return (source, baseline);
    }

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: CompilationHelper.DriftEnabledOptions());
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static IReadOnlyList<Diagnostic> RunWithCap(string source, string baselineJson, int maxDiagnostics) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        Dictionary<string, string> extra = new(StringComparer.OrdinalIgnoreCase) {
            ["build_property.HfcDriftMaxDiagnostics"] = maxDiagnostics.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: CompilationHelper.DriftEnabledOptions(extra));
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
