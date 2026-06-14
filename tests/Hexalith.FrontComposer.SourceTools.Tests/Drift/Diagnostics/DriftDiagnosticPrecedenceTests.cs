using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

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

    [Theory()]
    [InlineData("baseline-empty.json", "empty")]
    [InlineData("baseline-malformed.json", "malformed")]
    [InlineData("baseline-unsupported-schema.json", "schema version")]
    [InlineData("baseline-unsupported-algorithm.json", "algorithm")]
    [InlineData("baseline-oversized.json", "oversized")]
    [InlineData("baseline-duplicate-identity-within.json", "duplicate")]
    [InlineData("baseline-invariant-violation.json", "invariant")]
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

        // Story 9-1 review P2 + P10: the oversized fixture is a stable 520-byte artifact that
        // does not exceed the default 256 KB cap. Tighten MaxBaselineBytes for that one case
        // so the cap fires deterministically. (Production code no longer cheats via a
        // `_oversizedHint` sentinel substring.)
        int? maxBytes = string.Equals(fixtureFile, "baseline-oversized.json", StringComparison.Ordinal)
            ? 100
            : null;
        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline, maxBytes);

        diagnostics.Any(d => d.GetMessage().Contains(trustToken, StringComparison.OrdinalIgnoreCase)
                          && d.Severity == DiagnosticSeverity.Error)
            .ShouldBeTrue($"Trust failure '{trustToken}' must always emit an Error.");
        diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase)
                          || d.GetMessage().Contains("metadata drift", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse($"Trust failure '{trustToken}' must SUPPRESS lower-precedence drift comparison.");
    }

    [Fact()]
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

    [Fact()]
    public void ConfiguredBaselinePath_WhenNoMatchingAdditionalText_EmitsHFC1059_AndSuppressesDrift() {
        // Story 9-1 review CB-10: HFC1059 (InvalidBaselinePath) fires when
        // build_property.HfcDriftBaselinePath points at a path not matching any AdditionalText.
        // The configured-path branch was entirely uncovered before.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
                public string Added { get; set; } = string.Empty;
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunWithConfiguredPath(source, baseline, configuredPath: "this/path/does-not-exist.json");

        diagnostics.Any(d => d.Id == "HFC1059" && d.Severity == DiagnosticSeverity.Error)
            .ShouldBeTrue("CB-10 — configured baseline path that does not resolve must emit HFC1059 Error.");
        // Drift comparison must be suppressed for the affected configured baseline.
        diagnostics.Any(d => d.GetMessage().Contains("Added", StringComparison.Ordinal)
                          && d.Id == "HFC1065")
            .ShouldBeFalse("CB-10 — configured-path failure must suppress drift comparison for that baseline.");
    }

    [Fact()]
    public void ConfiguredBaselinePath_WhenMatchesViaSegmentAlignedSuffix_DoesNotEmitHFC1059() {
        // Story 9-1 review CB-10: the segment-aligned suffix match (P3 fix replacing the naive
        // `EndsWith` that allowed wrong-baseline-trusted via suffix collision) is exercised
        // here. A configured path of `frontcomposer.drift-baseline.json` should match an
        // AdditionalText with that exact filename component.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunWithConfiguredPath(source, baseline,
            configuredPath: "frontcomposer.drift-baseline.json");

        diagnostics.Any(d => d.Id == "HFC1059")
            .ShouldBeFalse("CB-10 — segment-aligned suffix match must satisfy the configured-path resolver.");
    }

    [Fact()]
    public void ConfiguredBaselinePath_WhenWhitespaceOnly_FallsBackToUnconfigured_NoHFC1059() {
        // Story 9-1 review CB-10 + P24: empty/whitespace-only `HfcDriftBaselinePath` is
        // treated as "not configured" — the loader must not fire HFC1059 for absence of an
        // unconfigured path.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunWithConfiguredPath(source, baseline, configuredPath: "   ");

        diagnostics.Any(d => d.Id == "HFC1059")
            .ShouldBeFalse("CB-10 + P24 — whitespace-only HfcDriftBaselinePath must NOT emit HFC1059.");
    }

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson, int? maxBaselineBytesOverride = null) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        Dictionary<string, string>? extra = maxBaselineBytesOverride is int max
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["build_property.HfcDriftMaxBaselineBytes"] = max.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }
            : null;
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: CompilationHelper.DriftEnabledOptions(extra));
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static IReadOnlyList<Diagnostic> RunWithConfiguredPath(string source, string baselineJson, string configuredPath) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        Dictionary<string, string> extra = new(StringComparer.OrdinalIgnoreCase) {
            ["build_property.HfcDriftBaselinePath"] = configuredPath,
        };
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: CompilationHelper.DriftEnabledOptions(extra));
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
