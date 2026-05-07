using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.TrimAot;

/// <summary>
/// AC14 + AC15 / T6 — narrow trim/AOT reflection-catalog evidence diagnostic.
/// AC14: when <c>build_property.PublishTrimmed=true</c> AND no adopter-supplied
/// <c>IActionQueueProjectionCatalog</c> override is statically observable, emit a Warning
/// pointing to the source-generated catalog path. AC15: when adopter override evidence is
/// not statically knowable (no IActionQueueProjectionCatalog symbol visible to SourceTools),
/// stay silent — runtime validators are authoritative.
/// </summary>
public sealed class TrimAotReflectionCatalogDiagnosticTests {
    private const string SkipReason = "RED-PHASE: T6 — trim/AOT reflection-catalog evidence diagnostic not yet introduced.";

    [Fact()]
    public void PublishTrimmedTrue_AndNoOverrideEvidence_EmitsWarning_PointingAtCatalogPath() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, publishTrimmed: true);

        Diagnostic? trim = diagnostics.FirstOrDefault(d => d.Id.StartsWith("HFC10", StringComparison.Ordinal)
                                                        && d.GetMessage().Contains("trim", StringComparison.OrdinalIgnoreCase));
        trim.ShouldNotBeNull("AC14 — trim/AOT advisory must fire when PublishTrimmed=true with default reflection catalog.");
        trim!.Severity.ShouldBe(DiagnosticSeverity.Warning);
        trim.GetMessage().ShouldContain("IActionQueueProjectionCatalog", Case.Insensitive);
        trim.GetMessage().ShouldContain("source-generated", Case.Insensitive);
    }

    [Fact()]
    public void PublishTrimmedFalse_NoDiagnostic() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, publishTrimmed: false);

        diagnostics.Any(d => d.GetMessage().Contains("trim", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    }

    [Fact()]
    public void HostPolicyCatalogUnknownAtBuildTime_NoDiagnostic_RuntimeAuthoritative() {
        // AC15 — when there is NO statically visible IActionQueueProjectionCatalog override
        // candidate AND there is also no statically visible reflection-catalog reference, the
        // analyzer must be silent. Runtime validators are authoritative.
        const string sourceWithoutAnyCatalogReference = """
            namespace TestDomain;
            public class IsolatedDomainObject { public string Name { get; set; } = string.Empty; }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(sourceWithoutAnyCatalogReference, publishTrimmed: true);

        diagnostics.Any(d => d.GetMessage().Contains("trim", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse(
            "AC15 — no diagnostic when build-time evidence is insufficient.");
    }

    private static IReadOnlyList<Diagnostic> Run(string source, bool publishTrimmed) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AnalyzerConfigOptionsProvider options = new InMemoryOptions(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            ["build_property.PublishTrimmed"] = publishTrimmed ? "true" : "false",
        });

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private sealed class InMemoryOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptionsProvider {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new InMemory(values);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class InMemory(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions {
            public override bool TryGetValue(string key, out string value) {
                if (values.TryGetValue(key, out string? v)) { value = v; return true; }
                value = string.Empty; return false;
            }
        }
    }
}
