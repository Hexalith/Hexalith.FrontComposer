using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;
using DriftBaselineTrustFailureTests = Hexalith.FrontComposer.SourceTools.Tests.Drift.Baseline.DriftBaselineTrustFailureTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Diagnostics;

/// <summary>
/// AC9 + AC12 + AC16 / T4 — diagnostic precedence table. Story §"Diagnostic Contract" defines
/// precedence: missing-baseline (1) → empty/malformed (2) → unsupported-schema (3) →
/// unsupported-algorithm (4) → oversized (5) → duplicate-id / invariant (6) → structural drift
/// (7) → metadata drift (8) → trim/AOT (9) → truncation (10). Higher-precedence trust failures
/// SUPPRESS lower-precedence drift comparison.
/// </summary>
public sealed class DriftDiagnosticPrecedenceTests {
    private const string SkipReason = "RED-PHASE: T4 — diagnostic precedence wiring not yet introduced.";

    [Theory(Skip = SkipReason)]
    [InlineData("baseline-empty.json",                 "empty")]
    [InlineData("baseline-malformed.json",             "malformed")]
    [InlineData("baseline-unsupported-schema.json",    "schema version")]
    [InlineData("baseline-unsupported-algorithm.json", "algorithm")]
    [InlineData("baseline-oversized.json",             "oversized")]
    [InlineData("baseline-duplicate-identity-within.json", "duplicate")]
    [InlineData("baseline-invariant-violation.json",   "invariant")]
    public void TrustFailure_SuppressesStructuralAndMetadataDrift_ForThatBaseline(string fixtureFile, string trustToken) {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
                public string ChangedShape { get; set; } = string.Empty;
                public string AlsoChanged { get; set; } = string.Empty;
            }
            """;
        string baseline = DriftBaselineTrustFailureTests.LoadFixture(fixtureFile);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.GetMessage().Contains(trustToken, StringComparison.OrdinalIgnoreCase)
                          && d.Severity == DiagnosticSeverity.Error)
            .ShouldBeTrue($"Trust failure '{trustToken}' must always emit an Error.");
        diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase)
                          || d.GetMessage().Contains("metadata drift", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse($"Trust failure '{trustToken}' must SUPPRESS lower-precedence drift comparison.");
    }

    [Fact(Skip = SkipReason)]
    public void StructuralDrift_TakesPrecedenceOverMetadataDrift_OnSameDeclaration() {
        // When BOTH a structural change (added/removed property) AND a metadata change (display
        // name) hit the same declaration, the structural diagnostic must come first; metadata
        // drift diagnostic still fires but is logically subordinate.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                [System.ComponentModel.DataAnnotations.Display(Name="NewLabel")]
                public string Id { get; set; } = string.Empty;
                public string Added { get; set; } = string.Empty;
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false, "displayName": "OldLabel" }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);
        Diagnostic[] orderingSensitive = [.. diagnostics
            .Where(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                     && (d.GetMessage().Contains("Added", StringComparison.Ordinal)
                      || d.GetMessage().Contains("display", StringComparison.OrdinalIgnoreCase)))];

        orderingSensitive.Length.ShouldBeGreaterThanOrEqualTo(2);
        orderingSensitive[0].GetMessage().ShouldContain("Added", Case.Insensitive,
            customMessage: "Structural drift takes precedence over metadata drift in emission order.");
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
