using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp.Rendering;

public sealed record McpProjectionRenderRequest(
    McpResourceDescriptor Descriptor,
    IReadOnlyList<object> Items,
    long TotalCount,
    string RowCountCategory = "visible",
    bool IsTruncated = false,
    string? RequestId = null,
    string? CorrelationId = null,
    IReadOnlyList<string>? SafeCommandSuggestions = null);

public sealed record McpProjectionRenderResult(
    bool IsSuccess,
    FrontComposerMcpFailureCategory Category,
    string ContentType,
    McpMarkdownProjectionDocument? Document) {
    public static McpProjectionRenderResult Success(McpMarkdownProjectionDocument document)
        => new(true, FrontComposerMcpFailureCategory.None, "text/markdown", document);

    public static McpProjectionRenderResult Failure(FrontComposerMcpFailureCategory category)
        => new(false, category, "text/plain", null);
}

public sealed record McpMarkdownProjectionDocument(
    string ProjectionIdentifier,
    string Role,
    string BoundedContext,
    string RowCountCategory,
    bool IsTruncated,
    string? RequestId,
    string? CorrelationId,
    string Text);

public sealed record McpMarkdownTable(
    IReadOnlyList<string> Headers,
    IReadOnlyList<IReadOnlyList<string>> Rows);

public sealed record McpMarkdownStatusSummary(
    string Title,
    IReadOnlyList<string> Lines);

public sealed record McpMarkdownTimeline(
    string Title,
    IReadOnlyList<string> Entries);
