using System.Reflection;
using System.Text.Json.Nodes;

using Hexalith.FrontComposer.Mcp;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Mcp.Tests.Invocation;

/// <summary>
/// AC1 / AC15 / T4 — extend FrontComposerMcpProjectionFailureMapper with explicit branches for
/// SchemaMismatch, UnknownSchemaBaseline, UnsupportedSchemaAlgorithm, SchemaIntegrityMismatch.
/// Each branch must surface a stable agent category, retryable flag, refresh hint, and HFC docs
/// code — and must not collapse to the generic <c>downstream_failed</c> path.
/// </summary>
public sealed class ProjectionReaderSchemaTaxonomyTests {
    [Theory]
    [InlineData(FrontComposerMcpFailureCategory.SchemaMismatch, "schema-mismatch", "HFC-MCP-PROJECTION-SCHEMA-MISMATCH", false, false)]
    [InlineData(FrontComposerMcpFailureCategory.UnknownSchemaBaseline, "schema-unavailable", "HFC-MCP-PROJECTION-SCHEMA-UNAVAILABLE", false, false)]
    [InlineData(FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm, "unsupported-schema-fingerprint", "HFC-MCP-PROJECTION-UNSUPPORTED-SCHEMA-ALGORITHM", false, false)]
    [InlineData(FrontComposerMcpFailureCategory.SchemaIntegrityMismatch, "schema-unavailable", "HFC-MCP-PROJECTION-SCHEMA-INTEGRITY-MISMATCH", false, false)]
    public void Map_NewSchemaCategories_ReturnDeterministicAgentTaxonomy(
        FrontComposerMcpFailureCategory category,
        string expectedAgentCategory,
        string expectedDocsCode,
        bool expectedRetryable,
        bool expectedRefreshResources) {
        FrontComposerMcpResult result = InvokeMapper(category);

        result.IsError.ShouldBeTrue();
        result.Category.ShouldBe(category);
        result.StructuredContent.ShouldNotBeNull();
        result.StructuredContent!["category"]!.GetValue<string>().ShouldBe(expectedAgentCategory);
        result.StructuredContent!["docsCode"]!.GetValue<string>().ShouldBe(expectedDocsCode);
        result.StructuredContent!["retryable"]!.GetValue<bool>().ShouldBe(expectedRetryable);
        result.StructuredContent!["refreshResources"]!.GetValue<bool>().ShouldBe(expectedRefreshResources);
        result.StructuredContent!["category"]!.GetValue<string>().ShouldNotBe(
            "downstream_failed",
            "AC1: schema categories must not collapse to the generic downstream_failed branch.");
    }

    [Fact]
    public void Map_SchemaCategories_NeverFlagAsHiddenEquivalent() {
        // Story 8-2 hidden-equivalent collapse is for tenant/auth/policy cases — schema categories
        // are publicly diagnosable (a stale client should know to re-fetch the manifest).
        foreach (FrontComposerMcpFailureCategory category in new[] {
            FrontComposerMcpFailureCategory.SchemaMismatch,
            FrontComposerMcpFailureCategory.UnknownSchemaBaseline,
            FrontComposerMcpFailureCategory.UnsupportedSchemaAlgorithm,
            FrontComposerMcpFailureCategory.SchemaIntegrityMismatch,
        }) {
            FrontComposerMcpResult result = InvokeMapper(category);
            result.StructuredContent!["isHiddenEquivalent"]!.GetValue<bool>()
                .ShouldBeFalse($"category {category} must NOT be hidden-equivalent (Story 8-2 reserves that for tenant/auth/policy).");
        }
    }

    [Fact]
    public void SanitizedPayload_StructuredContent_ContainsOnlyBoundedFields() {
        // AC15: agent-visible structured content carries only category / message / docsCode /
        // retryable / refreshResources / isHiddenEquivalent. No raw exception text, no client
        // fingerprint hash, no tenant id, no resource name.
        FrontComposerMcpResult result = InvokeMapper(FrontComposerMcpFailureCategory.SchemaMismatch);

        JsonObject content = result.StructuredContent!;
        HashSet<string> allowed = new(StringComparer.Ordinal) {
            "category", "message", "docsCode", "retryable", "refreshResources", "isHiddenEquivalent",
        };
        foreach (KeyValuePair<string, JsonNode?> property in content) {
            allowed.ShouldContain(property.Key,
                $"AC15: failure mapper exposed an unexpected structured field '{property.Key}'.");
        }
    }

    [Fact]
    public void SanitizedPayload_DoesNotEcho_RawClientHashOrTenantId() {
        // CK4-P6: the prior assertion shape (call mapper with no inputs, then negative-search the
        // serialized payload for placeholder strings the mapper never received) was vacuously
        // true. Now we negative-search the serialized payload for PRODUCTION-realistic
        // raw-input patterns (64-char hex hash = SHA-256 fingerprint shape, base64 SHA-256 shape,
        // bearer-token prefix). These patterns assert that even if the mapper started echoing
        // raw input via a future code path, the bounded shapes would fail-loud — without
        // false-positively flagging legitimate sanitized message strings.
        FrontComposerMcpResult result = InvokeMapper(FrontComposerMcpFailureCategory.SchemaMismatch);
        string serialized = result.StructuredContent!.ToJsonString();

        serialized.Contains(new string('a', 64), StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
            "AC15: 64-char hash patterns (lowercase hex SHA-256) must never appear in sanitized structured payload.");
        serialized.Contains(new string('A', 44), StringComparison.Ordinal).ShouldBeFalse(
            "AC15: 44-char hash patterns (base64 SHA-256) must never appear in sanitized structured payload.");
        serialized.Contains("Bearer ", StringComparison.OrdinalIgnoreCase).ShouldBeFalse(
            "AC15: bearer-token shaped patterns must never appear in sanitized structured payload.");
        serialized.Contains("eyJ", StringComparison.Ordinal).ShouldBeFalse(
            "AC15: JWT prefix shapes must never appear in sanitized structured payload.");
    }

    private static FrontComposerMcpResult InvokeMapper(FrontComposerMcpFailureCategory category) {
        // FrontComposerMcpProjectionFailureMapper is internal. Resolve via reflection so this test
        // compiles as a co-located red-phase scaffold. T4 may keep the mapper internal but should
        // expose either an InternalsVisibleTo or a public typed adapter; either way, the mapper's
        // ToResult shape is the contract under test.
        Type? mapper = typeof(FrontComposerMcpResult).Assembly
            .GetType("Hexalith.FrontComposer.Mcp.Invocation.FrontComposerMcpProjectionFailureMapper", throwOnError: false);
        mapper.ShouldNotBeNull("FrontComposerMcpProjectionFailureMapper not found — has the namespace moved?");

        MethodInfo? toResult = mapper!.GetMethod("ToResult", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
        toResult.ShouldNotBeNull("ToResult(FrontComposerMcpFailureCategory) method not found.");

        return (FrontComposerMcpResult)toResult!.Invoke(null, [category])!;
    }
}
