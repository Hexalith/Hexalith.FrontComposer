using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

using Shouldly;
using Xunit;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Baseline;

/// <summary>
/// AC9 / T1 + T4 — analyzer-config option validation. Drift bounds (max diagnostics,
/// max baseline size, severity overrides) must be parsed culture-invariantly, fail closed on
/// invalid values, and never throw out of the generator. Invalid values produce a deterministic
/// configuration diagnostic; the generator falls back only to documented safe defaults.
/// </summary>
public sealed class DriftAnalyzerConfigOptionsTests {
    private const string SkipReason = "RED-PHASE: T1 + T4 — drift analyzer-config option binder not yet introduced.";

    [Theory()]
    [InlineData("build_property.HfcDriftMaxDiagnostics", "-1")]
    [InlineData("build_property.HfcDriftMaxDiagnostics", "abc")]
    [InlineData("build_property.HfcDriftMaxDiagnostics", "")]
    [InlineData("build_property.HfcDriftMaxBaselineBytes", "0")]
    [InlineData("build_property.HfcDriftMaxBaselineBytes", "999999999999999999999")]
    [InlineData("build_property.HfcDriftSeverity", "Catastrophic")]
    public void InvalidOption_EmitsConfigurationDiagnostic_AndDoesNotThrow(string optionKey, string optionValue) {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunWithOption(source, optionKey, optionValue);

        diagnostics.Any(d => d.GetMessage().Contains("invalid", StringComparison.OrdinalIgnoreCase)
                          && d.GetMessage().Contains(optionKey.Split('.').Last(), StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue($"AC9 — invalid {optionKey}={optionValue} must surface a deterministic configuration diagnostic.");
    }

    [Fact()]
    public void InvalidOption_FallsBackToDocumentedSafeDefault_NotSilentDisable() {
        // Story §"Validate analyzer-configured size/count/severity options before comparison;
        // invalid option values emit deterministic configuration diagnostics and fall back only
        // to documented safe defaults." Asserts: drift comparison still runs (visible drift
        // diagnostic is still emitted) when an unrelated option is invalid — the generator must
        // not silently disable drift detection on a single bad option.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection {
                public string Id { get; set; } = string.Empty;
                public string NewProp { get; set; } = string.Empty;
            }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = RunWithOptionAndBaseline(
            source, baseline, "build_property.HfcDriftMaxDiagnostics", "-1");

        diagnostics.Any(d => d.GetMessage().Contains("NewProp", StringComparison.Ordinal)).ShouldBeTrue(
            "AC9 — drift detection must keep working under documented safe defaults; invalid options must not silently disable it.");
    }

    [Fact()]
    public void OptionParsing_IsCultureInvariant() {
        // AC19 — parsing "1,000" must NOT be accepted as 1000 under fr-FR. The binder must use
        // CultureInfo.InvariantCulture; the value should be rejected with the same configuration
        // diagnostic regardless of host culture.
        var prior = System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");
            const string source = """
                using Hexalith.FrontComposer.Contracts.Attributes;
                namespace TestDomain;
                [BoundedContext("Orders")]
                [Projection]
                public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
                """;

            IReadOnlyList<Diagnostic> diagnostics = RunWithOption(source, "build_property.HfcDriftMaxDiagnostics", "1,000");
            diagnostics.Any(d => d.GetMessage().Contains("invalid", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue(
                "AC19 — option parsing must be culture-invariant; 1,000 is not a valid integer.");
        }
        finally {
            System.Globalization.CultureInfo.CurrentCulture = prior;
        }
    }

    private static IReadOnlyList<Diagnostic> RunWithOption(string source, string optionKey, string optionValue) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AnalyzerConfigOptionsProvider options = new InMemoryAnalyzerConfigOptionsProvider(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [optionKey] = optionValue });

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static IReadOnlyList<Diagnostic> RunWithOptionAndBaseline(string source, string baselineJson, string optionKey, string optionValue) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        AnalyzerConfigOptionsProvider options = new InMemoryAnalyzerConfigOptionsProvider(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [optionKey] = optionValue });

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private sealed class InMemoryAnalyzerConfigOptionsProvider(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptionsProvider {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new InMemoryAnalyzerConfigOptions(values);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class InMemoryAnalyzerConfigOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions {
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
