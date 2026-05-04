namespace Hexalith.FrontComposer.Mcp;

public readonly record struct McpDescriptorEpochs(long DescriptorEpoch, long CatalogEpoch);

public interface IFrontComposerMcpDescriptorEpochProvider {
    McpDescriptorEpochs GetEpochs();
}
