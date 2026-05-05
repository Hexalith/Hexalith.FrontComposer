using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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

    [Fact(Skip = SkipReason)]
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

    [Fact(Skip = SkipReason)]
    public void ConfiguredPathDoesNotResolve_EmitsInvalidPathDiagnostic_DistinctFromFirstRun() {
        // Activation contract: when build_property.HfcDriftBaselinePath points at a path that
        // does not match any AdditionalText, the analyzer reports it as a *configuration* failure,
        // not a first-run diagnostic. The two MUST carry different IDs.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
            }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunWithMisconfiguredBaselinePath(source);

        Diagnostic[] invalid = [.. diagnostics.Where(d => d.GetMessage().Contains("baseline", StringComparison.OrdinalIgnoreCase)
                                                       && d.GetMessage().Contains("path", StringComparison.OrdinalIgnoreCase))];
        invalid.Length.ShouldBeGreaterThan(0);
        invalid.Any(d => d.GetMessage().Contains("first run", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse(
            "AC8 — the invalid-path diagnostic must NOT reuse the first-run wording.");
    }

    [Fact(Skip = SkipReason)]
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
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static IReadOnlyList<Diagnostic> RunWithMisconfiguredBaselinePath(string source) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        // The activation harness will plumb AnalyzerConfigOptionsProvider with
        // build_property.HfcDriftBaselinePath = "does/not/exist.json". Until then we rely on
        // the no-baseline pathway; the test asserts behavioral *distinction* will be observable
        // once T2 lands.
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
