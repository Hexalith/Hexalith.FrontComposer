using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Baseline;

/// <summary>
/// AC8 / T1 + T2 — missing baseline path. Two distinct diagnostics:
/// (a) baseline is absent / first run, (b) configured baseline path is invalid (analyzer config
/// points at a non-existent file). Each emits one actionable diagnostic and suppresses drift
/// comparison for the affected baseline. Both must reference the Story 9-2 CLI handoff text.
/// </summary>
public sealed class DriftBaselineMissingDiagnosticTests {
    private const string SkipReason = "RED-PHASE: T1 + T2 — baseline-missing diagnostic not yet introduced.";

    [Fact()]
    public void NoBaselineProvided_AndDriftDetectionOptedIn_EmitsFirstRunDiagnostic_Once() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunWithoutBaseline(source);

        Diagnostic[] missing = [.. diagnostics.Where(d => d.GetMessage().Contains("baseline", StringComparison.OrdinalIgnoreCase)
                                                       && (d.GetMessage().Contains("first run", StringComparison.OrdinalIgnoreCase)
                                                        || d.GetMessage().Contains("missing", StringComparison.OrdinalIgnoreCase)))];
        missing.Length.ShouldBe(1, "AC8 — exactly one first-run/missing-baseline diagnostic, regardless of declaration count.");
        missing[0].Severity.ShouldBe(DiagnosticSeverity.Warning);
        missing[0].GetMessage().ShouldContain("Story 9-2", Case.Insensitive);
    }

    [Fact()]
    public void ConfiguredPathDoesNotResolve_EmitsInvalidPathDiagnostic_DistinctFromFirstRun() {
        // Activation contract: when build_property.HfcDriftBaselinePath points at a path that
        // does not match any AdditionalText, the analyzer reports it as a *configuration* failure,
        // not a first-run diagnostic. The two MUST carry different IDs.
        // Story 9-1 review CB-32: positively assert distinct HFC IDs (HFC1058 vs HFC1059) —
        // wording-only differentiation is brittle.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> withoutBaseline = RunWithoutBaseline(source);
        IReadOnlyList<Diagnostic> invalidPath = RunWithMisconfiguredBaselinePath(source);

        Diagnostic? missing = withoutBaseline.FirstOrDefault(d => d.GetMessage().Contains("baseline", StringComparison.OrdinalIgnoreCase)
                                                               && (d.GetMessage().Contains("first run", StringComparison.OrdinalIgnoreCase)
                                                                || d.GetMessage().Contains("missing", StringComparison.OrdinalIgnoreCase)));
        Diagnostic? invalid = invalidPath.FirstOrDefault(d => d.GetMessage().Contains("baseline", StringComparison.OrdinalIgnoreCase)
                                                           && d.GetMessage().Contains("path", StringComparison.OrdinalIgnoreCase));

        missing.ShouldNotBeNull();
        invalid.ShouldNotBeNull();
        missing!.Id.ShouldNotBe(invalid!.Id, "AC8 — missing-baseline (HFC1058) and invalid-path (HFC1059) MUST carry distinct IDs.");
        missing.Id.ShouldBe("HFC1058", "AC8 — first-run/missing baseline pinned to HFC1058.");
        invalid.Id.ShouldBe("HFC1059", "AC8 — invalid-configured-path pinned to HFC1059.");

        invalidPath.Any(d => d.GetMessage().Contains("first run", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse(
            "AC8 — the invalid-path diagnostic must NOT reuse the first-run wording.");
    }

    [Fact()]
    public void MissingBaseline_SuppressesAllDriftComparisonDiagnostics_ForThatBaseline() {
        // Even though the source declares two projections, the missing-baseline diagnostic must
        // suppress structural-drift / metadata-drift comparison; only the missing-baseline
        // signal should be visible (plus unrelated existing generator diagnostics).
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class A { public string Id { get; set; } = string.Empty; }
            [BoundedContext("Orders")]
            [Projection]
            public partial class B { public string Id { get; set; } = string.Empty; }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunWithoutBaseline(source);

        diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase)
                          || d.GetMessage().Contains("metadata drift", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse("AC8 — missing baseline suppresses drift comparison.");
    }

    private static IReadOnlyList<Diagnostic> RunWithoutBaseline(string source) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        // Drift-detection opt-in is expected to be modelled either via a project-level analyzer
        // config (build_property.HfcDriftDetectionEnabled) or via the presence of a baseline
        // AdditionalText. The activation harness will swap this for the real option.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            optionsProvider: EnabledOptions());
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static IReadOnlyList<Diagnostic> RunWithMisconfiguredBaselinePath(string source) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AnalyzerConfigOptionsProvider options = EnabledOptions(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["build_property.HfcDriftBaselinePath"] = "does/not/exist.json",
        });
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static AnalyzerConfigOptionsProvider EnabledOptions(Dictionary<string, string>? extra = null) {
        Dictionary<string, string> values = extra is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(extra, StringComparer.OrdinalIgnoreCase);
        values["build_property.HfcDriftDetectionEnabled"] = "true";
        return new InMemoryOptions(values);
    }

    private sealed class InMemoryOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptionsProvider {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new InMemory(values);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class InMemory(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions {
            public override bool TryGetValue(string key, out string value) {
                if (values.TryGetValue(key, out string? v)) {
                    value = v;
                    return true;
                }

                value = string.Empty;
                return false;
            }
        }
    }
}
