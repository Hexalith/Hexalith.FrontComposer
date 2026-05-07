using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

/// <summary>
/// AC9 / T6 — derive lifecycle and renderer fingerprint material from runtime model structure
/// rather than literal field-list constants. The cross-package check that compares the
/// SourceTools-side lifecycle catalog against the Mcp-side <c>McpLifecycleResult</c> runtime
/// type lives in <c>tests/Hexalith.FrontComposer.Mcp.Tests/Schema/SchemaFingerprintCrossPackageTests.cs</c>
/// (8-6a Group B D10 resolution) so the cross-check rides Mcp's existing reference seam to
/// SourceTools rather than piercing Mcp's internal boundary from this test project.
/// </summary>
public sealed class SchemaFingerprintReflectionTests {
    [Fact]
    public void RendererPayload_BoundsContributeToFingerprint() {
        // AC9 behavior test: replaces the prior source-walking test (M10 — source walks broke
        // under packaged test runs). Different bounds must produce different fingerprints; if
        // the renderer payload ever stops consuming the bounds parameters (e.g. reverts to magic
        // numbers), this fails.
        GeneratedSchemaPayload first = SchemaFingerprintTransform.CreateMarkdownRendererPayload(
            "frontcomposer.mcp.markdown", "Auto", maxCharacters: 50_000, maxFieldCharacters: 1_000);
        GeneratedSchemaPayload second = SchemaFingerprintTransform.CreateMarkdownRendererPayload(
            "frontcomposer.mcp.markdown", "Auto", maxCharacters: 80_000, maxFieldCharacters: 2_000);

        first.Fingerprint.Value.ShouldNotBe(
            second.Fingerprint.Value,
            "AC9: renderer bounds must drive the fingerprint material.");
    }

    [Fact]
    public void LifecyclePayload_FingerprintIsStable_AcrossInvocations() {
        GeneratedSchemaPayload first = SchemaFingerprintTransform.CreateLifecycleResultPayload();
        GeneratedSchemaPayload second = SchemaFingerprintTransform.CreateLifecycleResultPayload();

        first.Fingerprint.Value.ShouldBe(second.Fingerprint.Value);
        // CK4-P9: per D23 / chunk-2 C1, lifecycle payload runs through the SourceTools blob
        // canonicalizer at build time. Pin to Sha256SourceToolsBlobV1 so a regression that swaps
        // canonicalizer (e.g. accidental use of Sha256CanonicalJsonV1) fails this test instead of
        // silently passing the prior `ShouldBeOneOf(...)` either-or.
        first.Fingerprint.AlgorithmId.ShouldBe(
            SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1,
            "D23: lifecycle payload must be canonicalized via the SourceTools blob algorithm; the runtime cannot recompute it (Roslyn analyzer hosting constraint).");
    }
}
