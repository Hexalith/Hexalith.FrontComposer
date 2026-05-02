using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp;

public sealed record McpVisibleToolCatalog(
    McpToolVisibilityContext Context,
    IReadOnlyList<McpVisibleToolCatalogEntry> Tools,
    bool IsTruncated);

public sealed record McpVisibleToolCatalogEntry(
    string Name,
    string Title,
    string? Description,
    string BoundedContext,
    string InputSummary,
    McpCommandDescriptor Descriptor,
    string NormalizedName);
