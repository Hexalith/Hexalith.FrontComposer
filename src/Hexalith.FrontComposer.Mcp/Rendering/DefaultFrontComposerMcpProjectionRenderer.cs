namespace Hexalith.FrontComposer.Mcp.Rendering;

public sealed class DefaultFrontComposerMcpProjectionRenderer : IFrontComposerMcpProjectionRenderer {
    public McpProjectionRenderResult Render(
        McpProjectionRenderRequest request,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken = default)
        => McpMarkdownProjectionRenderer.Render(request, options, cancellationToken);
}

