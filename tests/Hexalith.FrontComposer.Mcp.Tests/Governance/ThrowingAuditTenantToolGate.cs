using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

internal sealed class ThrowingAuditTenantToolGate : IFrontComposerMcpTenantToolGate {
    public ValueTask<bool> IsVisibleAsync(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("audit-internal-secret tenant-a");
}
