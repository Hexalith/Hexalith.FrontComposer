namespace Hexalith.FrontComposer.Mcp;

public sealed record McpToolResolutionResult(
    bool Accepted,
    FrontComposerMcpFailureCategory Category,
    string RequestedName,
    McpVisibleToolCatalogEntry? Tool,
    McpToolSuggestion? Suggestion,
    IReadOnlyList<McpVisibleToolCatalogEntry> VisibleTools,
    bool IsVisibleListTruncated) {
    public static McpToolResolutionResult Accept(McpVisibleToolCatalogEntry tool, McpVisibleToolCatalog catalog) {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(catalog);

        return new(true, FrontComposerMcpFailureCategory.None, tool.Name, tool, null, catalog.Tools, catalog.IsTruncated);
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
}
