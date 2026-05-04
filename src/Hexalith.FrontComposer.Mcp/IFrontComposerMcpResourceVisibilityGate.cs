using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp;

public interface IFrontComposerMcpResourceVisibilityGate {
    ValueTask<bool> IsVisibleAsync(
        McpResourceDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// Sample/dev visibility gate that returns true for any tenant-scoped context. Hosts that need
/// real per-tenant or per-policy visibility revalidation must implement
/// <see cref="IFrontComposerMcpResourceVisibilityGate"/> directly. Registered explicitly by the
/// host so a misconfiguration cannot accidentally ship unrestricted projection reads.
/// </summary>
public sealed class AllowAllResourceVisibilityGate : IFrontComposerMcpResourceVisibilityGate {
    public ValueTask<bool> IsVisibleAsync(
        McpResourceDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(context);

        return ValueTask.FromResult(!string.IsNullOrWhiteSpace(context.TenantId));
    }
}
