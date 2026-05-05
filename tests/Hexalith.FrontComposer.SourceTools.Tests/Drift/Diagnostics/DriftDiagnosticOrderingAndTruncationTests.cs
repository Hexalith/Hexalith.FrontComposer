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

    [Fact(Skip = SkipReason)]
    public void First50_AreSortedByBoundedContextDeclarationKindNameMemberDriftKind_Ordinal() {
        (string source, string baseline) = BuildScenario(driftCount: 60);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        Diagnostic[] driftSorted = [.. diagnostics
            .Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                     && !d.GetMessage().Contains("truncat", StringComparison.OrdinalIgnoreCase))
            .Take(50)];

        string[] keys = [.. driftSorted.Select(SortKey)];
        keys.ShouldBe([.. keys.OrderBy(k => k, StringComparer.Ordinal)],
            "AC16 — first 50 diagnostics must be sorted by ordinal composite key before cap.");
    }

    [Fact(Skip = SkipReason)]
    public void TruncationSummary_FollowsFirst50_AndReportsOmittedCount() {
        const int totalDrifts = 70;
        (string source, string baseline) = BuildScenario(driftCount: totalDrifts);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        Diagnostic? truncation = diagnostics.FirstOrDefault(d => d.GetMessage().Contains("truncat", StringComparison.OrdinalIgnoreCase));
        truncation.ShouldNotBeNull("AC16 — exactly one truncation summary must follow the cap.");
        truncation!.GetMessage().ShouldContain((totalDrifts - 50).ToString(System.Globalization.CultureInfo.InvariantCulture),
            customMessage: "AC16 — truncation summary must report the omitted count.");
        diagnostics.Count(d => d.GetMessage().Contains("truncat", StringComparison.OrdinalIgnoreCase))
            .ShouldBe(1, "AC16 — exactly one truncation summary, no per-declaration repetition.");
    }

    [Fact(Skip = SkipReason)]
    public void NoTruncationSummary_WhenDriftCountIsAtOrBelowCap() {
        (string source, string baseline) = BuildScenario(driftCount: 50);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.GetMessage().Contains("truncat", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
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

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()], additionalTexts: [baselineText]);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
