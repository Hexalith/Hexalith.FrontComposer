using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

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

        // Story 9-1 review P2 + P10: the oversized fixture is a stable 520-byte artifact and
        // does not exceed the default 256 KB cap on its own. Tighten MaxBaselineBytes to a
        // small value so the cap fires deterministically without checking in a multi-MB
        // fixture (the previous implementation cheated by looking for a `_oversizedHint`
        // sentinel substring in production code, which leaked test fixtures into the loader).
        int? maxBytesOverride = string.Equals(fixtureFileName, "baseline-oversized.json", StringComparison.Ordinal)
            ? 100
            : null;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, fixtureContent, maxBytesOverride);

        Diagnostic? trustFailure = diagnostics.FirstOrDefault(d =>
            d.Severity == DiagnosticSeverity.Error
            && d.GetMessage().Contains(expectedMessageToken, StringComparison.OrdinalIgnoreCase));
        trustFailure.ShouldNotBeNull(
            $"AC9 — fixture {fixtureFileName} must emit a deterministic Error diagnostic carrying the token '{expectedMessageToken}'.");

        diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase))
            .ShouldBeFalse($"AC9 — trust failure must suppress structural-drift comparison for {fixtureFileName}.");
    }

    [Fact()]
    public void OversizedCap_AtExactByteCap_DoesNotFire() {
        // Story 9-1 review CB-11 boundary: the loader uses `>` strictly when comparing byte
        // length to MaxBaselineBytes; a baseline at exactly the configured cap is allowed.
        // Build a minimal-valid baseline, measure its byte length, and pass that exact value
        // as the cap. The loader must NOT emit HFC1063 (BaselineBoundsExceeded) at the boundary.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;
        int exactBytes = System.Text.Encoding.UTF8.GetByteCount(baseline);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline, maxBaselineBytesOverride: exactBytes);

        diagnostics.Any(d => d.Id == "HFC1063")
            .ShouldBeFalse("CB-11 — oversize cap is `>` strict; baseline at exactly MaxBaselineBytes must pass.");
    }

    [Fact()]
    public void OversizedCap_AtCapPlusOne_FailsClosed() {
        // Companion to the at-cap test: cap-1 fires HFC1063, proving the boundary is between
        // exactCap (allowed) and exactCap-1 (rejected).
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;
        int exactBytes = System.Text.Encoding.UTF8.GetByteCount(baseline);

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline, maxBaselineBytesOverride: exactBytes - 1);

        diagnostics.Any(d => d.Id == "HFC1063" && d.Severity == DiagnosticSeverity.Error)
            .ShouldBeTrue("CB-11 — baseline strictly larger than MaxBaselineBytes must emit HFC1063 Error.");
    }

    [Fact()]
    public void OversizedCap_MeasuresBytesNotChars_ForMultiByteUtf8() {
        // Story 9-1 review P10: oversize check uses `Encoding.UTF8.GetByteCount` (not character
        // count). Build a baseline where multi-byte (CJK) characters drive byte length above
        // a configured cap that the char count would not exceed.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;
        // Each CJK character is 3 bytes in UTF-8 but 1 char. Build a description ~50 chars but
        // ~150+ bytes; the cap below (set just under the byte length) must reject by bytes.
        string padding = new string('漢', 60);
        string baseline = $$"""
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "displayName": "{{padding}}",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;
        int byteCount = System.Text.Encoding.UTF8.GetByteCount(baseline);
        int charCount = baseline.Length;
        // Sanity guard: this fixture is only meaningful if byteCount > charCount + slack.
        byteCount.ShouldBeGreaterThan(charCount + 100, "CB-11 fixture must contain enough multi-byte chars to differentiate.");

        // Cap at a value below byteCount but above charCount. A char-counting impl would let
        // this through; the byte-counting impl must reject.
        int cap = (charCount + byteCount) / 2;
        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline, maxBaselineBytesOverride: cap);

        diagnostics.Any(d => d.Id == "HFC1063" && d.Severity == DiagnosticSeverity.Error)
            .ShouldBeTrue("P10 — oversize comparison must use UTF-8 byte count, not char count.");
    }

    [Fact()]
    public void Utf8Bom_OnValidBaseline_ParsesAsValid_NoTrustFailure() {
        // Story 9-1 review P11: `JsonDocument.Parse` rejects UTF-8 BOM, so the loader strips it
        // before parsing. Prefix a valid baseline with U+FEFF and assert no HFC1060 fires.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;
        const string baselineBody = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [{ "name": "Id", "category": "String", "nullable": false }] }] }
            """;
        string bomBaseline = "﻿" + baselineBody;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, bomBaseline);

        diagnostics.Any(d => d.Id == "HFC1060" || d.Id == "HFC1061" || d.Id == "HFC1062")
            .ShouldBeFalse("P11 — UTF-8 BOM-prefixed valid baseline must parse cleanly (no malformed/schema/algorithm error).");
    }

    [Fact()]
    public void PropertyNameCaseCollision_FailsClosed_WithInvariantDiagnostic() {
        // Story 9-1 review P13: property dedupe uses `OrdinalIgnoreCase`. A baseline that
        // declares both `"Foo"` and `"foo"` on a single contract must fail the invariant check
        // (HFC1064) rather than silently throw or last-writer-wins-merge.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;
        const string baseline = """
            { "schemaVersion": "frontcomposer.generated-ui-baseline.v1",
              "algorithm": "frontcomposer-structural-v1",
              "contracts": [{ "family": "projection", "type": "TestDomain.OrderProjection", "boundedContext": "Orders",
                "properties": [
                  { "name": "Foo", "category": "String", "nullable": false },
                  { "name": "foo", "category": "String", "nullable": false }
                ] }] }
            """;

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baseline);

        diagnostics.Any(d => d.Id == "HFC1064" && d.Severity == DiagnosticSeverity.Error)
            .ShouldBeTrue("P13 — case-only property duplicates must emit HFC1064 (DuplicateOrInvariant) Error.");
    }

    [Fact()]
    public void DuplicateIdentityAcrossBaselineFiles_FailsClosed_NoLastWriterWins() {
        // Source matches the fixture identity (`Acme.Shipping.ShipmentProjection`) so this
        // exercises the realistic "duplicate poisons a real declaration" branch — chunk-B
        // CB-3 fix. The previous version used `TestDomain.ShipmentProjection`, which left
        // the duplicated identity disconnected from any compiled type.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Acme.Shipping;
            [BoundedContext("Shipping")]
            [Projection]
            public partial class ShipmentProjection {
                public string Priority { get; set; } = string.Empty;
            }
            """;
        string fileA = LoadFixture("baseline-duplicate-identity-across-a.json");
        string fileB = LoadFixture("baseline-duplicate-identity-across-b.json");

        // Story 9-1 review P5: IsCandidate now requires the documented baseline naming prefix
        // (`frontcomposer.drift-baseline*` / `frontcomposer.generated-ui-baseline*`). Stray
        // *.json AdditionalText files are no longer treated as drift baselines.
        IReadOnlyList<Diagnostic> diagnostics = RunWithMultipleBaselines(
            source,
            ("frontcomposer.drift-baseline-a.json", fileA),
            ("frontcomposer.drift-baseline-b.json", fileB));

        Diagnostic? duplicate = diagnostics.FirstOrDefault(d =>
            d.Severity == DiagnosticSeverity.Error
            && d.GetMessage().Contains("duplicate", StringComparison.OrdinalIgnoreCase)
            && d.GetMessage().Contains("Acme.Shipping.ShipmentProjection", StringComparison.Ordinal));
        duplicate.ShouldNotBeNull(
            "AC9 — duplicate identity across files MUST fail closed with an Error; no silent last-writer-wins merge.");

        // Drift comparison must be fully suppressed for that contract: neither structural drift
        // (Priority added vs file-B nullable) nor metadata drift (displayName diff between the
        // two files) may leak.
        diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
        diagnostics.Any(d => d.GetMessage().Contains("metadata drift", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    }

    [Fact()]
    public void DuplicateIdentityWithinSingleFile_FailsClosed() {
        // Source matches the fixture identity (`Acme.Shipping.ShipmentProjection`) — chunk-B
        // CB-3 fix.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Acme.Shipping;
            [BoundedContext("Shipping")]
            [Projection]
            public partial class ShipmentProjection { public string Id { get; set; } = string.Empty; }
            """;
        string fixtureContent = LoadFixture("baseline-duplicate-identity-within.json");

        IReadOnlyList<Diagnostic> diagnostics = Run(source, fixtureContent);

        diagnostics.Any(d => d.Id == "HFC1064"
                          && d.Severity == DiagnosticSeverity.Error
                          && d.GetMessage().Contains("duplicate", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue();
        // Even though the source defines the contract, drift comparison must be suppressed for it.
        diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
        diagnostics.Any(d => d.GetMessage().Contains("metadata drift", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    }

    [Fact()]
    public void DuplicateIdentityAcrossFiles_OrderAgnostic_BothOrderingsEmitDuplicate_NoDriftLeaks() {
        // Story 9-1 review CB-31: it isn't enough to assert that diagnostics compare equal under
        // forward and reverse orderings — the test must positively verify (a) the duplicate-id
        // diagnostic IS PRESENT in BOTH orderings (proving fail-closed ran in each), and
        // (b) no contract drift leaked under either ordering (proving the comparison was
        // actually suppressed, not silently reconciled).
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Acme.Shipping;
            [BoundedContext("Shipping")]
            [Projection]
            public partial class ShipmentProjection { public string Priority { get; set; } = string.Empty; }
            """;
        string a = LoadFixture("baseline-duplicate-identity-across-a.json");
        string b = LoadFixture("baseline-duplicate-identity-across-b.json");

        IReadOnlyList<Diagnostic> forward = RunWithMultipleBaselines(source, ("frontcomposer.drift-baseline-a.json", a), ("frontcomposer.drift-baseline-b.json", b));
        IReadOnlyList<Diagnostic> reverse = RunWithMultipleBaselines(source, ("frontcomposer.drift-baseline-b.json", b), ("frontcomposer.drift-baseline-a.json", a));

        foreach ((IReadOnlyList<Diagnostic> diagnostics, string label) in new[] { (forward, "forward"), (reverse, "reverse") }) {
            diagnostics.Any(d => d.Id == "HFC1064" && d.Severity == DiagnosticSeverity.Error)
                .ShouldBeTrue($"AC9 — {label} ordering must emit HFC1064 duplicate-identity Error.");
            diagnostics.Any(d => d.GetMessage().Contains("structural drift", StringComparison.OrdinalIgnoreCase)
                              || d.GetMessage().Contains("metadata drift", StringComparison.OrdinalIgnoreCase))
                .ShouldBeFalse($"AC9 — {label} ordering must suppress drift comparison for the duplicated identity.");
        }

        forward.Select(DiagnosticShape.From).OrderBy(s => s.SortKey, StringComparer.Ordinal)
            .ShouldBe(reverse.Select(DiagnosticShape.From).OrderBy(s => s.SortKey, StringComparer.Ordinal),
                "AC9 + AC18 — file enumeration order must not change diagnostics.");
    }

    [Fact()]
    public void LoadPhaseDiagnostics_AreCappedAndTruncated() {
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace Acme.Shipping;
            [BoundedContext("Shipping")]
            [Projection]
            public partial class ShipmentProjection { public string Id { get; set; } = string.Empty; }
            """;

        (string Path, string Content)[] baselines = Enumerable.Range(0, 5)
            .Select(i => (
                $"frontcomposer.drift-baseline-{i}.json",
                """{ "schemaVersion": "not-supported", "algorithm": "frontcomposer-structural-v1", "contracts": [] }"""))
            .ToArray();

        IReadOnlyList<Diagnostic> diagnostics = RunWithMultipleBaselines(source, 2, baselines);

        diagnostics.Count(d => d.Id == "HFC1061").ShouldBe(2,
            "AC7 — load-phase trust diagnostics must honor HfcDriftMaxDiagnostics.");
        diagnostics.Any(d => d.Id == "HFC1068" && d.GetMessage().Contains("3 omitted", StringComparison.Ordinal))
            .ShouldBeTrue("AC7 — capped load-phase diagnostics must emit a deterministic HFC1068 truncation fact.");
    }

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson, int? maxBaselineBytesOverride = null) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        // Story 9-1 review P4: drift detection is opt-in. Tests must explicitly set the flag.
        // Story 9-1 review CB-28: consolidate to use the shared helper.
        AnalyzerConfigOptionsProvider options = EnabledOptions(maxBaselineBytesOverride);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static IReadOnlyList<Diagnostic> RunWithMultipleBaselines(string source, params (string Path, string Content)[] baselines) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText[] texts = [.. baselines.Select(b => (AdditionalText)new InMemoryAdditionalText(b.Path, b.Content))];
        AnalyzerConfigOptionsProvider options = EnabledOptions();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: texts,
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static IReadOnlyList<Diagnostic> RunWithMultipleBaselines(
        string source,
        int maxDiagnostics,
        params (string Path, string Content)[] baselines) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText[] texts = [.. baselines.Select(b => (AdditionalText)new InMemoryAdditionalText(b.Path, b.Content))];
        AnalyzerConfigOptionsProvider options = CompilationHelper.DriftEnabledOptions(
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["build_property.HfcDriftMaxDiagnostics"] = maxDiagnostics.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: texts,
            optionsProvider: options);
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }

    private static AnalyzerConfigOptionsProvider EnabledOptions(int? maxBaselineBytes = null) {
        // Story 9-1 review CB-28: delegate to the canonical helper instead of duplicating the
        // provider/options classes inline.
        Dictionary<string, string>? extra = maxBaselineBytes is int max
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
                ["build_property.HfcDriftMaxBaselineBytes"] = max.ToString(System.Globalization.CultureInfo.InvariantCulture),
            }
            : null;
        return CompilationHelper.DriftEnabledOptions(extra);
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

    private sealed record DiagnosticShape(
        string Id,
        DiagnosticSeverity Severity,
        string Message,
        string Path,
        string Properties) {
        internal string SortKey { get; } = Id + "|" + Severity + "|" + Message + "|" + Path;

        internal static DiagnosticShape From(Diagnostic diagnostic) {
            string path = diagnostic.Location == Location.None
                ? "<none>"
                : diagnostic.Location.GetMappedLineSpan().Path;
            string properties = string.Join(
                "\n",
                diagnostic.Properties
                .OrderBy(p => p.Key, StringComparer.Ordinal)
                .Select(p => p.Key + "=" + (p.Value ?? "<null>")));
            return new DiagnosticShape(
                diagnostic.Id,
                diagnostic.Severity,
                diagnostic.GetMessage(),
                path,
                properties);
        }
    }
}
