using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp;

public interface IFrontComposerMcpTenantToolGate {
    ValueTask<bool> IsVisibleAsync(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken);
}

public sealed class AllowAllMcpTenantToolGate : IFrontComposerMcpTenantToolGate {
    public ValueTask<bool> IsVisibleAsync(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(context);

        return ValueTask.FromResult(!string.IsNullOrWhiteSpace(context.TenantId));
    }
}
