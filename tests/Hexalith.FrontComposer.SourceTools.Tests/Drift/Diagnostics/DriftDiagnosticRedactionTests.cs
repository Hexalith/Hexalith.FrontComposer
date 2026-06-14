using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;

using static Hexalith.FrontComposer.SourceTools.Tests.Drift.Comparison.DriftClassifierProjectionPropertyTests;

using DriftBaselineTrustFailureTests = Hexalith.FrontComposer.SourceTools.Tests.Drift.Baseline.DriftBaselineTrustFailureTests;

namespace Hexalith.FrontComposer.SourceTools.Tests.Drift.Diagnostics;

/// <summary>
/// AC13 / T7 — sanitization. Tenant IDs, user IDs, claims, tokens, command payload values,
/// query rows, cache keys, ETags, local absolute paths, raw JSON fragments, generated source
/// snippets, and oversized arbitrary text MUST NEVER appear in any drift diagnostic message,
/// any diagnostic property, or any thrown exception text — even when the source / baseline
/// fixture contains those sentinels.
/// </summary>
public sealed class DriftDiagnosticRedactionTests {
    private const string SkipReason = "RED-PHASE: T7 — sanitization layer not yet introduced.";

    private static readonly string[] Sentinels = [
        "SENTINEL_TENANT_d4f1",
        "SENTINEL_USER_88aa",
        "SENTINEL_TOKEN_ey.eyJzdWIiOiIxMjM0NTYi.fake",
        "SENTINEL_PATH_C__Users_jane_secrets",
        "SENTINEL_PAYLOAD_{\"ssn\":\"000-00-0000\"}",
        "SENTINEL_ETAG_W/abc123",
        "SENTINEL_CACHE_KEY_tenant_42",
        "SENTINEL_ROW_select_*",
        "SENTINEL_GENERATED_SOURCE_///auto/",
        // Story 9-1 review CB-16: extend the sentinel list with Unicode and JWT-shape tokens.
        // AC13 enumerates "tokens", "raw JSON fragments", and "raw exception text"; zero-width
        // characters and JWT-shape strings are realistic redaction triggers that the previous
        // ASCII-only list did not exercise.
        "SENTINEL_ZW​SP_inside",                // U+200B zero-width space
        "SENTINEL_RTL‮Override_inside",         // U+202E right-to-left override
        "SENTINEL_JWT_eyJhbGciOi.eyJzdWIiOi.signedBlob",
    ];

    [Fact()]
    public void NoSentinelLeaksIntoDiagnosticMessages_OrPropertyValues() {
        // Source body deliberately drips sentinels into the *baseline* via attributes that the
        // baseline JSON references. Activation: the parser must reject or sanitize the sentinels
        // before they reach diagnostic messages or property values.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("SENTINEL_TENANT_d4f1")]
            [Projection]
            public partial class OrderProjection {
                [System.ComponentModel.DataAnnotations.Display(Name = "SENTINEL_PAYLOAD_{\"ssn\":\"000-00-0000\"}")]
                public string SecretField { get; set; } = "SENTINEL_TOKEN_ey.eyJzdWIiOiIxMjM0NTYi.fake";
            }
            """;
        string baselineJson = DriftBaselineTrustFailureTests.LoadFixture("baseline-redaction-sentinels.json");

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baselineJson);

        foreach (Diagnostic d in diagnostics) {
            string message = d.GetMessage();
            foreach (string sentinel in Sentinels) {
                message.ShouldNotContain(sentinel,
                    customMessage: $"AC13 — diagnostic {d.Id} leaked sentinel '{sentinel}' into its message.");
                foreach (KeyValuePair<string, string?> property in d.Properties) {
                    if (property.Value is null) {
                        continue;
                    }

                    property.Value.ShouldNotContain(sentinel,
                        customMessage: $"AC13 — diagnostic {d.Id} property '{property.Key}' leaked sentinel '{sentinel}'.");
                }
            }
        }
    }

    [Fact()]
    public void RedactionFailure_EmitsRedactionFallbackDiagnostic_AndSuppressesOriginal() {
        // Activation: when the sanitizer cannot prove a value is safe, it must emit a
        // dedicated redaction-fallback diagnostic and SUPPRESS the original diagnostic that
        // would have leaked. Story §"Diagnostic Contract" precedence row 11 = redaction failure.
        const string source = """
            using Hexalith.FrontComposer.Contracts.Attributes;
            namespace TestDomain;
            [BoundedContext("Orders")]
            [Projection]
            public partial class OrderProjection { public string Id { get; set; } = string.Empty; }
            """;
        string baselineJson = DriftBaselineTrustFailureTests.LoadFixture("baseline-redaction-sentinels.json");

        IReadOnlyList<Diagnostic> diagnostics = Run(source, baselineJson);

        // Story 9-1 review CB-23 (redaction): pin to the production HFC1069 RedactionSuppressed
        // ID instead of substring-matching "redact"/"sanitize", which can match incidental
        // docslink prose.
        diagnostics.Any(d => d.Id == "HFC1069" && d.Severity == DiagnosticSeverity.Error)
            .ShouldBeTrue("AC13 — RedactionSuppressed must emit HFC1069 Error.");

        // Story 9-1 review CB-23: the precedence-row-11 contract says redaction failure
        // SUPPRESSES the original would-have-leaked drift diagnostic. Assert no structural-
        // drift (HFC1065) or metadata-drift (HFC1066) leaks for the contracts whose values
        // the redaction layer flagged. The sentinels-fixture contract is
        // "Acme.SENTINEL_TENANT_d4f1.SENTINEL_USER_88aa.SecretProjection" — drift comparison
        // for that contract MUST be suppressed.
        diagnostics.Any(d => (d.Id == "HFC1065" || d.Id == "HFC1066")
                          && d.GetMessage().Contains("SecretProjection", StringComparison.Ordinal))
            .ShouldBeFalse("AC13 — redaction failure must SUPPRESS the would-have-leaked drift diagnostic for the affected contract.");
    }

    private static IReadOnlyList<Diagnostic> Run(string source, string baselineJson) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        CSharpCompilation compilation = CompilationHelper.CreateCompilation(source);
        FrontComposerGenerator generator = new();
        AdditionalText baselineText = new InMemoryAdditionalText("frontcomposer.drift-baseline.json", baselineJson);
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            additionalTexts: [baselineText],
            optionsProvider: CompilationHelper.DriftEnabledOptions());
        driver = driver.RunGenerators(compilation, ct);
        return driver.GetRunResult().Diagnostics;
    }
}
