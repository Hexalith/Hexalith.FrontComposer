using System.Globalization;

using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

/// <summary>
/// AC11 / T8 — two clean generations of the same domain source must produce byte-for-byte
/// identical fingerprints across OS / culture / TZ / EOL / path-separator combinations. The
/// canonicalizer's normalizer (line endings, Unicode line separators, trim) and ordering
/// (StringComparer.Ordinal) is the contract under test.
/// </summary>
public sealed class SchemaFingerprintDeterminismTests {
    [Theory]
    [InlineData("en-US")]
    [InlineData("tr-TR")] // dotted-i / dotless-i lowercase quirk
    [InlineData("de-DE")]
    [InlineData("ja-JP")]
    public void LifecyclePayload_FingerprintIdenticalAcrossCultures(string cultureName) {
        // 8-6a review H13: compute the invariant-culture fingerprint OUTSIDE the test culture
        // scope so the comparison actually crosses culture boundaries. The prior test computed
        // both sides under the same culture and passed vacuously.
        CultureInfo previousCulture = CultureInfo.CurrentCulture;
        CultureInfo previousUiCulture = CultureInfo.CurrentUICulture;

        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        string invariantFingerprint = SchemaFingerprintTransform.CreateLifecycleResultPayload().Fingerprint.Value;

        try {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(cultureName);

            string testCultureFingerprint = SchemaFingerprintTransform.CreateLifecycleResultPayload().Fingerprint.Value;
            string repeated = SchemaFingerprintTransform.CreateLifecycleResultPayload().Fingerprint.Value;

            testCultureFingerprint.ShouldBe(repeated, $"culture {cultureName} produced drift across two clean generations.");
            testCultureFingerprint.ShouldBe(
                invariantFingerprint,
                $"AC11 cross-culture invariant: fingerprint under {cultureName} must match invariant culture.");
        }
        finally {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUiCulture;
        }
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    [InlineData("\u2028")] // Unicode LINE SEPARATOR
    [InlineData("\u2029")] // Unicode PARAGRAPH SEPARATOR
    public void RendererPayload_NormalizesEolInRendererId(string eol) {
        ArgumentNullException.ThrowIfNull(eol);
        // 8-6a review H12: actually inject the EOL into a string that flows through Normalize().
        // The prior test parameterized by `eol` but never used it in the payload inputs, so the
        // two computed fingerprints were trivially identical (same input, different parameter).
        // The renderer's `rendererId` is one of the strings that goes through Normalize, so a
        // multi-line value carrying any EOL flavor should canonicalize identically and produce
        // the same fingerprint.
        const string LfId = "frontcomposer.mcp.markdown\nextra";
        string variantId = "frontcomposer.mcp.markdown" + eol + "extra";

        string reference = SchemaFingerprintTransform.CreateMarkdownRendererPayload(
            LfId,
            "Default",
            maxCharacters: 64_000,
            maxFieldCharacters: 4_096).Fingerprint.Value;
        string variant = SchemaFingerprintTransform.CreateMarkdownRendererPayload(
            variantId,
            "Default",
            maxCharacters: 64_000,
            maxFieldCharacters: 4_096).Fingerprint.Value;

        variant.ShouldBe(
            reference,
            $"AC11: EOL '{eol.Replace("\r", "\\r").Replace("\n", "\\n")}' must canonicalize to the LF reference fingerprint.");
    }

    [Theory]
    [InlineData("\n")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    [InlineData("\u2028")] // Unicode LINE SEPARATOR
    [InlineData("\u2029")] // Unicode PARAGRAPH SEPARATOR
    public void CommandPayload_NormalizesEolInParameterDescription(string eol) {
        ArgumentNullException.ThrowIfNull(eol);
        // 8-6a chunk-3 review (Edge F1+F8): exercises the FieldLine cell path where Normalize
        // runs AFTER per-cell EscapeDelimited (`SchemaFingerprintTransform.cs:241`). Pre-fix, a
        // U+2028 in a parameter Description survived EscapeDelimited and was then collapsed to a
        // bare `\n` by Normalize, splitting the canonical-blob row mid-cell and synthesizing a
        // fake `field=...` line. Post-fix, EscapeDelimited maps U+2028/U+2029 to the same `\\n`
        // escape as `\n` so all line-break flavors hash identically AND cannot inject rows.
        const string LfDescription = "first line\nsecond line";
        string variantDescription = "first line" + eol + "second line";

        McpCommandDescriptorModel reference = BuildCommand(LfDescription);
        McpCommandDescriptorModel variant = BuildCommand(variantDescription);

        string referenceFingerprint = SchemaFingerprintTransform.CreateCommandPayload(reference).Fingerprint.Value;
        string variantFingerprint = SchemaFingerprintTransform.CreateCommandPayload(variant).Fingerprint.Value;

        variantFingerprint.ShouldBe(
            referenceFingerprint,
            $"AC11: EOL '{eol.Replace("\r", "\\r").Replace("\n", "\\n")}' inside a parameter Description must canonicalize to the LF reference fingerprint.");
    }

    private static McpCommandDescriptorModel BuildCommand(string parameterDescription)
        => new(
            protocolName: "frontcomposer.test.command",
            commandTypeName: "Hexalith.FrontComposer.Tests.TestCommand",
            boundedContext: "Test",
            title: "Test Command",
            description: "command description",
            authorizationPolicyName: null,
            parameters: [
                new McpParameterDescriptorModel(
                    name: "Subject",
                    typeName: "string",
                    jsonType: "string",
                    isRequired: true,
                    isNullable: false,
                    title: "Subject",
                    description: parameterDescription,
                    enumValues: [],
                    isUnsupported: false),
            ],
            derivablePropertyNames: []);
}
