using System.Text.Json;
using System.Text.Json.Nodes;

using ModelContextProtocol.Protocol;

namespace Hexalith.FrontComposer.Mcp;

internal static class FrontComposerMcpProtocolMapper {
    public static Tool ToProtocolTool(McpVisibleToolCatalogEntry entry)
        => new() {
            Name = entry.Name,
            Title = entry.Title,
            Description = entry.Description,
            InputSchema = McpJsonSchemaBuilder.BuildInputSchema(entry.Descriptor.Parameters),
        };

    public static CallToolResult ToCallToolResult(FrontComposerMcpResult result)
        => new() {
            IsError = result.IsError,
            StructuredContent = result.StructuredContent?.Deserialize<JsonElement>(),
            Content = [new TextContentBlock { Text = result.Text }],
        };
}
