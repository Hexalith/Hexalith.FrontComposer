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
    private const string ConfigDiagnosticId = "HFC1067";
    private const string SourceWithProjection = """
        using Hexalith.FrontComposer.Contracts.Attributes;
        namespace TestDomain;
        [BoundedContext("Orders")]
        [Projection]
        public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
        """;

    [Theory()]
    [InlineData("build_property.HfcDriftMaxDiagnostics", "-1")]
    [InlineData("build_property.HfcDriftMaxDiagnostics", "abc")]
    [InlineData("build_property.HfcDriftMaxBaselineBytes", "0")]
    [InlineData("build_property.HfcDriftMaxBaselineBytes", "999999999999999999999")]
    [InlineData("build_property.HfcDriftSeverity", "Catastrophic")]
    public void InvalidOption_EmitsConfigurationDiagnostic_AndDoesNotThrow(string optionKey, string optionValue) {
        IReadOnlyList<Diagnostic> diagnostics = RunWithOption(SourceWithProjection, optionKey, optionValue);

        // Story 9-1 review CB-34: pin the configuration diagnostic to HFC1067 (InvalidOption)
        // so a refactor that emits the message under a different ID is caught.
        diagnostics.Any(d => d.Id == ConfigDiagnosticId
                          && d.GetMessage().Contains(optionKey.Split('.').Last(), StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue($"AC9 — invalid {optionKey}={optionValue} must surface HFC1067 (InvalidOption) referencing the option name.");
    }

    [Theory()]
    // Story 9-1 review CB-12: positive coverage — empty/whitespace values are treated as
    // "unset" (TryReadString returns null), the generator falls back to the documented default
    // and MUST NOT emit HFC1067. Removing this case earlier left the P24 contract unverified.
    [InlineData("build_property.HfcDriftMaxDiagnostics", "")]
    [InlineData("build_property.HfcDriftMaxDiagnostics", "   ")]
    [InlineData("build_property.HfcDriftMaxBaselineBytes", "")]
    [InlineData("build_property.HfcDriftMaxBaselineBytes", "   ")]
    [InlineData("build_property.HfcDriftSeverity", "")]
    public void EmptyOrWhitespaceOption_TreatedAsUnset_NoConfigurationDiagnostic(string optionKey, string optionValue) {
        ArgumentNullException.ThrowIfNull(optionValue);
        IReadOnlyList<Diagnostic> diagnostics = RunWithOption(SourceWithProjection, optionKey, optionValue);

        diagnostics.Any(d => d.Id == ConfigDiagnosticId)
            .ShouldBeFalse($"P24 — {optionKey}={(optionValue.Length == 0 ? "<empty>" : "<whitespace>")} must be treated as unset, not as an explicit invalid value.");
    }

    [Theory()]
    // Story 9-1 review CB-13: HfcDriftSeverity must accept all production-supported values
    // case-insensitively. Production explicitly accepts Warning, Error, Info, Information
    // (TryReadString trims surrounding whitespace; comparisons are OrdinalIgnoreCase). Probe
    // each canonical token plus a casing variation and a trimmed-whitespace form.
    [InlineData("warning")]
    [InlineData("Warning")]
    [InlineData("WARNING")]
    [InlineData("error")]
    [InlineData("ERROR")]
    [InlineData("info")]
    [InlineData("Info")]
    [InlineData("Information")]
    [InlineData("  Warning  ")]
    public void HfcDriftSeverity_AcceptsCanonicalValuesCaseInsensitively_NoConfigurationDiagnostic(string value) {
        IReadOnlyList<Diagnostic> diagnostics = RunWithOption(SourceWithProjection, "build_property.HfcDriftSeverity", value);

        diagnostics.Any(d => d.Id == ConfigDiagnosticId
                          && d.GetMessage().Contains("HfcDriftSeverity", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse($"AC9 — HfcDriftSeverity='{value}' must be accepted (case-insensitive, trimmed).");
    }

    [Theory()]
    // Story 9-1 review CB-14: pin ReadPositiveInt boundary cases. Production accepts
    // NumberStyles.Integer (P25), so signed `+50` parses; `2147483648` overflows Int32 and
    // must be rejected; `0` is invalid (min is 1); `1` is the minimum valid value.
    [InlineData("1",            true)]
    [InlineData("+50",          true)]
    [InlineData("0",            false)]
    [InlineData("-1",           false)]
    [InlineData("2147483648",   false)]
    public void HfcDriftMaxDiagnostics_BoundaryParsing(string raw, bool expectAccepted) {
        IReadOnlyList<Diagnostic> diagnostics = RunWithOption(SourceWithProjection, "build_property.HfcDriftMaxDiagnostics", raw);

        bool emittedConfigDiagnostic = diagnostics.Any(d => d.Id == ConfigDiagnosticId
                                                         && d.GetMessage().Contains("HfcDriftMaxDiagnostics", StringComparison.OrdinalIgnoreCase));
        emittedConfigDiagnostic.ShouldBe(!expectAccepted,
            $"CB-14 — HfcDriftMaxDiagnostics={raw} expected accepted={expectAccepted}.");
    }

    [Theory()]
    // Story 9-1 review CB-15: HfcDriftDetectionEnabled boolean parsing. Only "true" was tested
    // before. Probe case-insensitivity ("True"/"TRUE"), the negative path ("false" disables
    // drift comparison), and a garbage value ("yes" should be parsed as false / disabled).
    [InlineData("true",  true)]
    [InlineData("True",  true)]
    [InlineData("TRUE",  true)]
    [InlineData("false", false)]
    [InlineData("yes",   false)] // Garbage values must not be silently treated as truthy.
    public void HfcDriftDetectionEnabled_BooleanParsing_DeterministicAcrossCasing(string value, bool expectEnabled) {
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
        IReadOnlyList<Diagnostic> diagnostics = RunWithEnabledOverrideAndBaseline(source, baseline, value);

        // When enabled, the "Added" property drift must surface. When disabled (or garbage),
        // no drift comparison runs, so no HFC1065 referencing "Added" should appear.
        bool driftFired = diagnostics.Any(d => d.Id == "HFC1065" && d.GetMessage().Contains("Added", StringComparison.Ordinal));
        driftFired.ShouldBe(expectEnabled, $"CB-15 — HfcDriftDetectionEnabled='{value}' expected enabled={expectEnabled}.");
    }

    [Fact()]
    public void FrontComposerDriftDetectionEnabledAlias_EnablesDriftComparison_WhenPrimaryOptionIsAbsent() {
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

        IReadOnlyList<Diagnostic> diagnostics = RunWithEnabledOverrideAndBaseline(
            source,
            baseline,
            "true",
            "build_property.FrontComposerDriftDetectionEnabled");

        diagnostics.Any(d => d.Id == "HFC1065" && d.GetMessage().Contains("Added", StringComparison.Ordinal))
            .ShouldBeTrue("AC1 — FrontComposerDriftDetectionEnabled=true must enable drift comparison when the primary HfcDriftDetectionEnabled option is absent.");
    }

    [Fact()]
    public void InvalidOption_FallsBackToDocumentedSafeDefault_NotSilentDisable() {
        // Story §"Validate analyzer-configured size/count/severity options before comparison;
        // invalid option values emit deterministic configuration diagnostics and fall back only
        // to documented safe defaults." Asserts: drift comparison still runs (visible drift
        // diagnostic is still emitted) when an unrelated option is invalid — the generator must
        // not silently disable drift detection on a single bad option.
        // Story 9-1 review CB-33: positively assert (a) HFC1067 fires for the bad option, AND
        // (b) drift comparison still surfaces the structural drift — proving the documented
        // default applied rather than a silent disable.
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

        diagnostics.Any(d => d.Id == ConfigDiagnosticId
                          && d.GetMessage().Contains("HfcDriftMaxDiagnostics", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue("CB-33 — HFC1067 must fire for the invalid option.");
        diagnostics.Any(d => d.Id == "HFC1065" && d.GetMessage().Contains("NewProp", StringComparison.Ordinal))
            .ShouldBeTrue("AC9 + CB-33 — drift detection must keep working under documented safe defaults; invalid options must not silently disable it.");
    }

    [Fact()]
    public void OptionParsing_IsCultureInvariant() {
        // AC19 — parsing "1,000" must NOT be accepted as 1000 under fr-FR. The binder must use
        // CultureInfo.InvariantCulture; the value should be rejected with the same configuration
        // diagnostic regardless of host culture.
        var prior = System.Globalization.CultureInfo.CurrentCulture;
        try {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("fr-FR");

            IReadOnlyList<Diagnostic> diagnostics = RunWithOption(SourceWithProjection, "build_property.HfcDriftMaxDiagnostics", "1,000");
            diagnostics.Any(d => d.Id == ConfigDiagnosticId).ShouldBeTrue(
                "AC19 — option parsing must be culture-invariant; 1,000 is not a valid integer.");
        }
        finally {
            System.Globalization.CultureInfo.CurrentCulture = prior;
        }
    }

    private static IReadOnlyList<Diagnostic> RunWithOption(string source, string optionKey, string optionValue) {
        // Story 9-1 review CB-35: keep modeling the production opt-in path explicitly. The
        // configuration-diagnostic check fires before the Enabled gate today, but routing
        // through DriftEnabledOptions guards against a future refactor that moves option
        // validation behind the gate.
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        Dictionary<string, string> extra = new(StringComparer.OrdinalIgnoreCase) { [optionKey] = optionValue };
        AnalyzerConfigOptionsProvider options = CompilationHelper.DriftEnabledOptions(extra);

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
        Dictionary<string, string> extra = new(StringComparer.OrdinalIgnoreCase) { [optionKey] = optionValue };
        AnalyzerConfigOptionsProvider options = CompilationHelper.DriftEnabledOptions(extra);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static IReadOnlyList<Diagnostic> RunWithEnabledOverrideAndBaseline(
        string source,
        string baselineJson,
        string enabledRaw,
        string optionKey = "build_property.HfcDriftDetectionEnabled") {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        // Bypass DriftEnabledOptions's auto-set so we can probe the exact raw value.
        AnalyzerConfigOptionsProvider options = new InMemoryRawOptionsProvider(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                [optionKey] = enabledRaw,
            });

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private sealed class InMemoryRawOptionsProvider(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptionsProvider {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new InMemoryRawOptions(values);
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        private sealed class InMemoryRawOptions(IReadOnlyDictionary<string, string> values) : AnalyzerConfigOptions {
            public override bool TryGetValue(string key, out string value) {
                if (values.TryGetValue(key, out string? v)) {
                    value = v;
                    return true;
                }
                value = null!;
                return false;
            }
        }
    }
}
