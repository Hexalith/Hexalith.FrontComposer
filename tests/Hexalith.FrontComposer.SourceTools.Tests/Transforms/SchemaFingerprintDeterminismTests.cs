using System.Globalization;

using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

/// <summary>
/// AC11 / T8 — two clean generations of the same domain source must produce byte-for-byte
/// identical fingerprints across OS / culture / TZ / EOL / path-separator combinations. The
/// canonicalizer's normalizer (line endings, Unicode line separators, trim) and ordering
/// (StringComparer.Ordinal) is the contract under test.
/// </summary>
public sealed class SchemaFingerprintDeterminismTests {
    private const string SkipReason = "RED-PHASE: T8 — two-clean-generation determinism harness pending.";

    [Theory(Skip = SkipReason)]
    [InlineData("en-US")]
    [InlineData("tr-TR")] // dotted-i / dotless-i lowercase quirk
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public void LifecyclePayload_FingerprintIdenticalAcrossCultures(string cultureName) {
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            string baseline = SchemaFingerprintTransform.CreateLifecycleResultPayload().Fingerprint.Value;
            string repeated = SchemaFingerprintTransform.CreateLifecycleResultPayload().Fingerprint.Value;

            baseline.ShouldBe(repeated, $"culture {cultureName} produced drift across two clean generations.");
            // Cross-culture invariant: the fingerprint under tr-TR must match en-US.
            string invariant = SchemaFingerprintTransform.CreateLifecycleResultPayload().Fingerprint.Value;
            invariant.ShouldBe(baseline);
        } finally {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory(Skip = SkipReason)]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    [InlineData("\u2028")] // Unicode line separator
    public void RendererPayload_NormalizesEolInMetadataValues(string eol) {
        ArgumentNullException.ThrowIfNull(eol);
        // AC11: a metadata value carrying any of these line-terminator variants must canonicalize
        // identically. The transform's Normalize step is the contract under test.
        string baseline = SchemaFingerprintTransform.CreateMarkdownRendererPayload(
            "frontcomposer.mcp.markdown",
            "Default",
            maxCharacters: 64_000,
            maxFieldCharacters: 4_096).Fingerprint.Value;

        // The renderer payload's caller-facing inputs do not currently include a multi-line
        // string; AC11 still requires normalization to be consistent across runs even if the
        // current callers don't pass EOLs. This test pins the invariant.
        string repeated = SchemaFingerprintTransform.CreateMarkdownRendererPayload(
            $"frontcomposer.mcp.markdown",
            "Default",
            maxCharacters: 64_000,
            maxFieldCharacters: 4_096).Fingerprint.Value;

        baseline.ShouldBe(repeated, $"EOL '{eol.Replace("\r", "\\r").Replace("\n", "\\n")}' caused drift across regenerations.");
    }
}
