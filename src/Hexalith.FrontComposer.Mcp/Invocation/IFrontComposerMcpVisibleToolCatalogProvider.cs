namespace Hexalith.FrontComposer.Mcp.Invocation;

public interface IFrontComposerMcpVisibleToolCatalogProvider {
    ValueTask<McpVisibleToolCatalog> BuildVisibleCatalogAsync(CancellationToken cancellationToken = default);
}

