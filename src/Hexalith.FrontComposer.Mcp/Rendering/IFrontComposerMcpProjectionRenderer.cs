namespace Hexalith.FrontComposer.Mcp.Rendering;

public interface IFrontComposerMcpProjectionRenderer {
    McpProjectionRenderResult Render(
        McpProjectionRenderRequest request,
        FrontComposerMcpOptions options,
        CancellationToken cancellationToken = default);
}

