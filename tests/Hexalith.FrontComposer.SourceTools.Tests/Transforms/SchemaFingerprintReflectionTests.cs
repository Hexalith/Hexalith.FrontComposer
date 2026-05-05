using System.Reflection;

using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

/// <summary>
/// AC9 / T6 — derive lifecycle and renderer fingerprint material from runtime model structure
/// rather than literal field-list constants. After the 8-6a review pass the SourceTools side
/// maintains a deterministic catalog (the AppDomain.GetAssemblies() scan was removed for AC11
/// determinism), so these tests enforce the cross-package invariant by comparing the catalog's
/// field set against the runtime <c>McpLifecycleResult</c> properties at test time. Drift in
/// either direction surfaces here.
/// </summary>
public sealed class SchemaFingerprintReflectionTests {
    [Fact]
    public void LifecycleResultPayload_FieldsMatchRuntimeType() {
        Type? lifecycleResult = TryLoadLifecycleResultType();
        lifecycleResult.ShouldNotBeNull(
            "AC9 / T6 require McpLifecycleResult to be reachable from SourceTools test host.");

        IReadOnlyList<string> runtimeFieldNames = lifecycleResult!
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        GeneratedSchemaPayload payload = SchemaFingerprintTransform.CreateLifecycleResultPayload();
        IReadOnlyList<string> payloadFieldNames = ExtractFieldNames(payload);

        payloadFieldNames.ShouldBe(
            runtimeFieldNames,
            "AC9 cross-check: SourceTools lifecycle catalog must mirror McpLifecycleResult property set.");
    }

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
        first.Fingerprint.AlgorithmId.ShouldBeOneOf(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1);
    }

    private static IReadOnlyList<string> ExtractFieldNames(GeneratedSchemaPayload payload) {
        string canonical = payload.Json ?? string.Empty;
        return canonical.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("field=", StringComparison.Ordinal))
            .Select(line => line.Substring("field=".Length).Split('|', 2)[0])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
    }

    private static Type? TryLoadLifecycleResultType() {
        // SourceTools does not project-reference Mcp at build time; tests can probe Mcp via the
        // already-loaded assembly set after Mcp.Tests has wired it transitively.
        try {
            _ = Assembly.Load("Hexalith.FrontComposer.Mcp");
        }
        catch {
            // Probe loaded assemblies regardless; the cross-check assertion reports the contract miss.
        }

        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .FirstOrDefault(t => t.FullName == "Hexalith.FrontComposer.Mcp.Invocation.McpLifecycleResult");
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly) {
        try {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex) {
            return ex.Types.Where(t => t is not null)!;
        }
    }
}
