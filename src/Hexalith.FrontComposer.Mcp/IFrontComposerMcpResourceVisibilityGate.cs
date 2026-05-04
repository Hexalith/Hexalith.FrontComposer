using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp;

public interface IFrontComposerMcpResourceVisibilityGate {
    ValueTask<bool> IsVisibleAsync(
        McpResourceDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken);
}
