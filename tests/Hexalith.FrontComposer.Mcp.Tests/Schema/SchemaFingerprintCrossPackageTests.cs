using System.Reflection;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Mcp.Invocation;
using Hexalith.FrontComposer.SourceTools.Transforms;

using Shouldly;
using Xunit;

namespace Hexalith.FrontComposer.Mcp.Tests.Schema;

/// <summary>
/// AC9 / T6 cross-package check: SourceTools' deterministic lifecycle field catalog must mirror
/// <c>Hexalith.FrontComposer.Mcp.Invocation.McpLifecycleResult</c>'s runtime shape. Hosted in
/// Mcp.Tests (8-6a Group B D10 resolution) so the cross-check rides Mcp's existing
/// <c>InternalsVisibleTo</c> seam rather than piercing Mcp's internal boundary from a SourceTools
/// test project. The Mcp.Tests project already references SourceTools, so
/// <see cref="SchemaFingerprintTransform"/> is reachable here.
/// </summary>
public sealed class SchemaFingerprintCrossPackageTests {

    [Fact]
    public void LifecycleCatalog_FieldNames_MatchRuntimeProperties() {
        IReadOnlyList<string> runtimeFieldNames = typeof(McpLifecycleResult)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Select(p => p.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        IReadOnlyList<string> catalogFieldNames = ExtractCatalog()
            .Select(parts => parts[0])
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray();

        catalogFieldNames.ShouldBe(
            runtimeFieldNames,
            "AC9 cross-check: SourceTools lifecycle catalog field names must match McpLifecycleResult property set.");
    }

    [Fact]
    public void LifecycleCatalog_FieldTypes_MatchRuntimePropertyTypes() {
        // P-47 (8-6a Group B): the prior cross-check only validated field NAMES; a future change
        // that retypes a property (e.g. State from string to int) would slip past name checks but
        // break fingerprint determinism. This pins each catalog typename to the runtime property
        // type so type drift fails build, not runtime.
        Dictionary<string, string> runtimeTypes = typeof(McpLifecycleResult)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .ToDictionary(
                p => p.Name,
                p => MapClrTypeToCatalogType(p.PropertyType),
                StringComparer.Ordinal);

        foreach (string[] row in ExtractCatalog()) {
            string name = row[0];
            string typeName = row[2];
            runtimeTypes.ShouldContainKey(name, $"Catalog references unknown property '{name}'.");
            typeName.ShouldBe(
                runtimeTypes[name],
                $"AC9 cross-check: catalog typename for '{name}' must match the runtime property CLR type.");
        }
    }

    [Fact]
    public void LifecycleCatalog_StateEnumValues_MatchMcpLifecycleStateNames() {
        // 8-6a chunk-3 review (decision): the catalog's State enum-values cell is sourced from
        // Hexalith.FrontComposer.Contracts.Lifecycle.McpLifecycleStateNames.Canonical so the
        // SourceTools fingerprint catalog and any future MCP wire emitter share a single source
        // of truth (replaces the prior self-pinned ExpectedStateLine constant). Reordering,
        // casing changes, additions, or removals to the constant regenerate the lifecycle
        // fingerprint AND fail this cross-check test in the same build.
        IReadOnlyList<string[]> catalog = ExtractCatalog();
        string[] stateRow = catalog.Single(parts => parts[0] == "State");
        string actualLine = string.Join("|", stateRow);

        string expectedLine = "State|string|string|required|not-null|"
            + string.Join(",", McpLifecycleStateNames.Canonical);

        actualLine.ShouldBe(
            expectedLine,
            "Lifecycle State catalog line must match McpLifecycleStateNames.Canonical. If the canonical "
            + "wire-state set legitimately changes, update McpLifecycleStateNames.Canonical and document "
            + "the fingerprint regeneration in the story log.");
    }

    private static IReadOnlyList<string[]> ExtractCatalog() {
        GeneratedSchemaPayload payload = SchemaFingerprintTransform.CreateLifecycleResultPayload();
        string canonical = payload.Json ?? string.Empty;
        return canonical
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith("field=", StringComparison.Ordinal))
            .Select(line => line.Substring("field=".Length).Split('|'))
            .ToArray();
    }

    private static string MapClrTypeToCatalogType(Type clrType) {
        // The lifecycle surface is string-only today. If a property is re-typed to a non-string
        // CLR type, this throws so the cross-check escalates rather than silently producing a
        // bogus comparison. Extend the map when the lifecycle catalog legitimately gains
        // non-string columns.
        if (clrType == typeof(string)) {
            return "string";
        }

        throw new InvalidOperationException(
            $"P-47 cross-check has no mapping for CLR type '{clrType.FullName}'. "
            + "Either extend MapClrTypeToCatalogType or update SchemaFingerprintTransform's catalog format.");
    }
}
