namespace Hexalith.FrontComposer.Mcp;

public sealed record McpToolResolutionResult(
    bool Accepted,
    FrontComposerMcpFailureCategory Category,
    string RequestedName,
    McpVisibleToolCatalogEntry? Tool,
    McpToolSuggestion? Suggestion,
    IReadOnlyList<McpVisibleToolCatalogEntry> VisibleTools,
    bool IsVisibleListTruncated,
    Schema.McpSchemaNegotiationResult? SchemaNegotiation = null) {
    public static McpToolResolutionResult Accept(
        McpVisibleToolCatalogEntry tool,
        McpVisibleToolCatalog catalog,
        Schema.McpSchemaNegotiationResult? schemaNegotiation = null) {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(catalog);

        return new(true, FrontComposerMcpFailureCategory.None, tool.Name, tool, null, catalog.Tools, catalog.IsTruncated, schemaNegotiation);
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
            catalog.IsTruncated);
    }

    public static McpToolResolutionResult Reject(
        string requestedName,
        FrontComposerMcpFailureCategory category,
        McpVisibleToolCatalog catalog) {
        ArgumentNullException.ThrowIfNull(catalog);

        return new(
            false,
            category,
            requestedName,
            null,
            null,
            catalog.Tools,
            catalog.IsTruncated);
    }
}
