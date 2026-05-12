using System.Security.Cryptography;
using System.Text;

namespace Hexalith.FrontComposer.Mcp;

public sealed record McpToolResolutionResult(
    bool Accepted,
    FrontComposerMcpFailureCategory Category,
    string RequestedName,
    McpVisibleToolCatalogEntry? Tool,
    McpToolSuggestion? Suggestion,
    IReadOnlyList<McpVisibleToolCatalogEntry> VisibleTools,
    bool IsVisibleListTruncated,
    Schema.McpSchemaNegotiationResult? SchemaNegotiation = null,
    string? InternalCorrelationKey = null) {
    public static McpToolResolutionResult Accept(
        McpVisibleToolCatalogEntry tool,
        McpVisibleToolCatalog catalog,
        Schema.McpSchemaNegotiationResult? schemaNegotiation = null) {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(catalog);

        return new(true, FrontComposerMcpFailureCategory.None, tool.Name, tool, null, catalog.Tools, catalog.IsTruncated, schemaNegotiation, InternalCorrelationKey: null);
    }

    public static McpToolResolutionResult Reject(
        string requestedName,
        McpToolSuggestion? suggestion,
        McpVisibleToolCatalog catalog) {
        ArgumentNullException.ThrowIfNull(catalog);

        return new(
            false,
            FrontComposerMcpFailureCategory.UnknownTool,
            requestedName,
            null,
            suggestion,
            catalog.Tools,
            catalog.IsTruncated,
            InternalCorrelationKey: null);
    }

    public static McpToolResolutionResult Reject(
        string requestedName,
        FrontComposerMcpFailureCategory category,
        McpVisibleToolCatalog catalog,
        McpVisibleToolCatalogEntry? tool = null) {
        ArgumentNullException.ThrowIfNull(catalog);

        // 11-5 review DN18 / D13: the schema-rejection path retains an opaque correlation
        // identifier derived from the resolved descriptor name instead of the full
        // McpVisibleToolCatalogEntry. Exposing the entry on a public record made the descriptor
        // observable to any caller that serialised the rejection result (telemetry, logs, wire
        // payloads), which contradicted D13's "public rejection payloads expose only stable
        // public codes, safe categories, docs/message keys, and opaque correlation identifiers".
        // The 16-character SHA256 prefix is non-reversible and safe to surface; the descriptor
        // itself is no longer carried on rejection results.
        string? correlationKey = tool is null ? null : ComputeOpaqueCorrelationKey(tool.Name);
        return new(
            false,
            category,
            requestedName,
            Tool: null,
            null,
            catalog.Tools,
            catalog.IsTruncated,
            InternalCorrelationKey: correlationKey);
    }

    private static string ComputeOpaqueCorrelationKey(string toolName) {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(toolName));
        StringBuilder builder = new(16);
        for (int i = 0; i < 8; i++) {
            _ = builder.Append(hash[i].ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        }

        return builder.ToString();
    }
}
