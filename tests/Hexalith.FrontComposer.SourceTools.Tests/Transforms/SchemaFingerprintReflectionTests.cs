using System.Reflection;

using Hexalith.FrontComposer.Contracts.Schema;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.SourceTools.Tests.Transforms;

/// <summary>
/// AC9 / T6 — derive lifecycle and renderer fingerprint material from runtime model structure
/// rather than literal field-list constants. The lifecycle payload must reflect the actual
/// <c>Hexalith.FrontComposer.Mcp.Invocation.McpLifecycleResult</c> field set; the renderer payload
/// must read its bounds from <c>FrontComposerMcpOptions</c> / <c>SkillResourceReadOptions</c>.
/// </summary>
public sealed class SchemaFingerprintReflectionTests {
    [Fact]
    public void LifecycleResultPayload_FieldsMatchRuntimeType_ReflectivelyDiscovered() {
        // T6 replaces the hardcoded literal field list with reflection-based discovery. After T6,
        // the canonical blob's `field=` lines must equal the McpLifecycleResult public property
        // names (sorted ordinal) — so any new property added to the runtime type drifts the
        // fingerprint automatically.
        Type? lifecycleResult = TryLoadLifecycleResultType();
        lifecycleResult.ShouldNotBeNull(
            "AC9 / T6 require McpLifecycleResult to be reachable from SourceTools — pull it via reflection or share the canonical type contract.");

        IReadOnlyList<string> runtimeFieldNames = lifecycleResult!
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => !RuntimeCorrelationName(p.Name))
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        GeneratedSchemaPayload payload = SchemaFingerprintTransform.CreateLifecycleResultPayload();
        IReadOnlyList<string> payloadFieldNames = ExtractFieldNames(payload);

        payloadFieldNames.ShouldBe(
            runtimeFieldNames,
            "AC9: lifecycle payload field list must derive from McpLifecycleResult, not from a literal constant.");
    }

    [Fact]
    public void LifecycleResultPayload_SourceDoesNotEmbedLiteralFieldArray() {
        // AC9 stricter form: the transform body must not contain the legacy literal field rows.
        string? source = LocateTransformSource();
        source.ShouldNotBeNull();

        // AC9: lifecycle payload must derive its field list from McpLifecycleResult reflection, not literal string rows.
        source!.ShouldNotContain("category|string|string|required|not-null");
        source!.ShouldNotContain("correlationId|string|string|required|not-null");
    }

    [Fact]
    public void RendererPayload_BoundsAreNotMagicNumbers_PulledFromOptions() {
        // T6 replaces the hardcoded 64_000 / 4_096 in CreateMarkdownRendererPayload with values
        // pulled from FrontComposerMcpOptions / SkillResourceReadOptions. Verify the transform
        // body no longer hosts the literal magic numbers.
        string? source = LocateTransformSource();
        source.ShouldNotBeNull("Unable to locate SchemaFingerprintTransform.cs for AC9 source verification.");

        // AC9: literal magic numbers must not appear in the transform body — pull from FrontComposerMcpOptions / SkillResourceReadOptions at the call site.
        source!.ShouldNotContain("64_000");
        source!.ShouldNotContain("4_096");
    }

    [Fact]
    public void LifecyclePayload_FingerprintIsStable_AcrossInvocations() {
        // Determinism counter-test. Even with reflection-based discovery, two consecutive calls
        // produce the same fingerprint (no environment / time / GUID drift).
        GeneratedSchemaPayload first = SchemaFingerprintTransform.CreateLifecycleResultPayload();
        GeneratedSchemaPayload second = SchemaFingerprintTransform.CreateLifecycleResultPayload();

        first.Fingerprint.Value.ShouldBe(second.Fingerprint.Value);
        first.Fingerprint.AlgorithmId.ShouldBeOneOf(
            SchemaFingerprintAlgorithm.Sha256CanonicalJsonV1,
            SchemaFingerprintAlgorithm.Sha256SourceToolsBlobV1);
    }

    private static bool RuntimeCorrelationName(string name)
        => name is "MessageId" or "TenantId" or "CorrelationId" or "UserId";

    private static IReadOnlyList<string> ExtractFieldNames(GeneratedSchemaPayload payload) {
        // Transform's canonical blob is newline-delimited key=value text. Field rows look like
        //   field=<name>|<type>|<jsonType>|<required>|<nullable>...
        // T6 may also keep the same wire shape; either way, the leading cell is the field name.
        string canonical = payload.Json ?? string.Empty;
        return canonical.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("field=", StringComparison.Ordinal))
            .Select(line => line.Substring("field=".Length).Split('|', 2)[0])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();
    }

    private static Type? TryLoadLifecycleResultType() {
        // SourceTools intentionally has no project reference to .Mcp (build-time tools only depend
        // on Contracts). Probe loaded assemblies first; fall back to file-system-side load if the
        // dev later wires a typed contract for the lifecycle field list.
        try {
            _ = Assembly.Load("Hexalith.FrontComposer.Mcp");
        }
        catch {
            // Fall through to loaded assemblies; the assertion below reports the contract miss.
        }

        Type? loaded = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .FirstOrDefault(t => t.FullName == "Hexalith.FrontComposer.Mcp.Invocation.McpLifecycleResult");
        return loaded;
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly) {
        try {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex) {
            return ex.Types.Where(t => t is not null)!;
        }
    }

    private static string? LocateTransformSource() {
        DirectoryInfo? dir = new(AppContext.BaseDirectory);
        for (int i = 0; i < 10 && dir is not null; i++, dir = dir.Parent) {
            string candidate = Path.Combine(dir.FullName, "src", "Hexalith.FrontComposer.SourceTools", "Transforms", "SchemaFingerprintTransform.cs");
            if (File.Exists(candidate)) {
                return File.ReadAllText(candidate);
            }
        }

        return null;
    }
}
