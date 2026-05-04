using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp.Invocation;

internal sealed record FrontComposerMcpProjectionReadSnapshot(
    string ProjectionKey,
    string ProtocolUriCategory,
    McpProjectionRenderStrategy RenderStrategy,
    string BoundedContext,
    long DescriptorEpoch,
    long CatalogEpoch,
    string QueryShapeCategory,
    string RequestId,
    CancellationToken CancellationToken,
    FrontComposerMcpProjectionDescriptorSnapshot Descriptor);
