using System.Text.Json.Nodes;

namespace Hexalith.FrontComposer.Mcp;

public sealed record FrontComposerMcpResult(
    bool IsError,
    FrontComposerMcpFailureCategory Category,
    string Text,
    JsonObject? StructuredContent = null) {
    private const string GenericFailureText = "Request failed.";

    public static FrontComposerMcpResult Success(string text, JsonObject? structuredContent = null)
        => new(false, FrontComposerMcpFailureCategory.None, text, structuredContent);

    public static FrontComposerMcpResult Failure(FrontComposerMcpFailureCategory category)
        => new(true, category, GenericFailureText);
}
