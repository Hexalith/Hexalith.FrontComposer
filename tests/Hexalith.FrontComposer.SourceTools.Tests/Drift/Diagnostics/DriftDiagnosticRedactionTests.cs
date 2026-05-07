using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using Shouldly;
using Xunit;

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
                    if (property.Value is null) continue;
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

        diagnostics.Any(d => d.GetMessage().Contains("redact", StringComparison.OrdinalIgnoreCase)
                          || d.GetMessage().Contains("sanitize", StringComparison.OrdinalIgnoreCase))
            .ShouldBeTrue("AC13 — when sanitization cannot prove a value safe, the analyzer must emit an explicit redaction-fallback diagnostic.");
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
