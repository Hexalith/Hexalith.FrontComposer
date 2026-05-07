using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Baseline;

/// <summary>
/// AC9 / T1 + T2 + T4 — trust failure matrix. Every fail-closed scenario emits a deterministic
/// Error diagnostic and suppresses drift comparison for the affected baseline. There is no
/// "last writer wins" merge across baseline files — duplicate identity across files is itself
/// a trust failure.
/// </summary>
public sealed class DriftBaselineTrustFailureTests {
    private const string SkipReason = "RED-PHASE: T1 + T2 — baseline trust-failure pipeline not yet introduced.";

    public static TheoryData<string, string> TrustFailureFixtures() => new() {
        { "baseline-empty.json",                     "empty" },
        { "baseline-malformed.json",                 "malformed" },
        { "baseline-unsupported-schema.json",        "schema version" },
        { "baseline-unsupported-algorithm.json",     "algorithm" },
        { "baseline-oversized.json",                 "oversized" },
        { "baseline-duplicate-identity-within.json", "duplicate identity" },
        { "baseline-invariant-violation.json",       "invariant" },
    };

    [Theory()]
    [MemberData(nameof(TrustFailureFixtures))]
    public void Fixture_FailsClosed_WithErrorAndSuppressedComparison(string fixtureFileName, string expectedMessageToken) {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;
        string fixtureContent = LoadFixture(fixtureFileName);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, fixtureContent);

        Diagnostic? trustFailure = diagnostics.FirstOrDefault(d =>
            d.Severity == DiagnosticSeverity.Error
            && d.GetMessage().Contains(expectedMessageToken, StringComparison.OrdinalIgnoreCase));
        trustFailure.ShouldNotBeNull(
            $"AC9 — fixture {fixtureFileName} must emit a deterministic Error diagnostic carrying the token '{expectedMessageToken}'.");

        diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse($"AC9 — trust failure must suppress structural-drift comparison for {fixtureFileName}.");
    }

    [Fact()]
    public void DuplicateIdentityAcrossBaselineFiles_FailsClosed_NoLastWriterWins() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Shipping")]
            [Projection]
            public partial class ShipmentProjection {
                public string Priority { get; set; } = string.Empty;
            }
            """;
        string fileA = LoadFixture("baseline-duplicate-identity-across-a.json");
        string fileB = LoadFixture("baseline-duplicate-identity-across-b.json");

        IReadOnlyList<Diagnostic> diagnostics = RunWithMultipleBaselines(
            source,
            ("a.json", fileA),
            ("b.json", fileB));

        Diagnostic? duplicate = diagnostics.FirstOrDefault(d =>
            d.Severity == DiagnosticSeverity.Error
            && d.GetMessage().Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            && d.GetMessage().Contains("Acme.Shipping.ShipmentProjection", StringComparison.Ordinal));
        duplicate.ShouldNotBeNull(
            "AC9 — duplicate identity across files MUST fail closed with an Error; no silent last-writer-wins merge.");

        // Drift comparison must be fully suppressed for that contract.
        diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    }

    [Fact()]
    public void DuplicateIdentityWithinSingleFile_FailsClosed() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Shipping")]
            [Projection]
            public partial class ShipmentProjection { public string Id { get; set; } = string.Empty; }
            """;
        string fixtureContent = LoadFixture("baseline-duplicate-identity-within.json");

        IReadOnlyList<Diagnostic> diagnostics = Run(source, fixtureContent);

        diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error
                          && d.GetMessage().Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue();
    }

    [Fact()]
    public void DuplicateIdentityAcrossFiles_OrderAgnostic_SameDiagnostic() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Shipping")]
            [Projection]
            public partial class ShipmentProjection { public string Priority { get; set; } = string.Empty; }
            """;
        string a = LoadFixture("baseline-duplicate-identity-across-a.json");
        string b = LoadFixture("baseline-duplicate-identity-across-b.json");

        IReadOnlyList<Diagnostic> forward = RunWithMultipleBaselines(source, ("a.json", a), ("b.json", b));
        IReadOnlyList<Diagnostic> reverse = RunWithMultipleBaselines(source, ("b.json", b), ("a.json", a));

        forward.Select(d => d.Id + "|" + d.GetMessage()).OrderBy(s => s, StringComparer.Ordinal)
            .ShouldBe(reverse.Select(d => d.Id + "|" + d.GetMessage()).OrderBy(s => s, StringComparer.Ordinal),
                "AC9 + AC18 — file enumeration order must not change diagnostics.");
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

    private static IReadOnlyList<Diagnostic> RunWithMultipleBaselines(string source, params (string Path, string Content)[] baselines) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText[] texts = [.. baselines.Select(b => (AdditionalText)new InMemoryAdditionalText(b.Path, b.Content))];
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()], additionalTexts: texts);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    internal static string LoadFixture(string fileName) {
        string here = Path.GetDirectoryName(typeof(DriftBaselineTrustFailureTests).Assembly.Location)!;
        for (int i = 0; i < 8; i++) {
            string candidate = Path.Combine(here, "Drift", "Baseline", "Fixtures", fileName);
            if (File.Exists(candidate)) {
                return File.ReadAllText(candidate);
            }
            here = Path.GetDirectoryName(here)
                ?? throw new InvalidOperationException("Reached filesystem root without finding fixture directory.");
        }

        throw new FileNotFoundException($"Drift fixture '{fileName}' not found from {Assembly.GetExecutingAssembly().Location}.");
    }
}
