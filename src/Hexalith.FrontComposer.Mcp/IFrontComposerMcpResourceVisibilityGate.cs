using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Per-resource visibility check invoked by the projection reader at admission, pre-query, and
/// pre-render. The gate must be safe to call multiple times per read.
/// </summary>
/// <remarks>
/// <para>
/// R2-P10: the <paramref name="descriptor"/> argument is NOT guaranteed to be a reference
/// to a registry-owned descriptor. The reader passes the live registry instance only on the
/// first (admission) call; subsequent pre-query and pre-render revalidation calls receive a
/// detached snapshot copy reconstructed via <c>FrontComposerMcpProjectionDescriptorSnapshot.ToDescriptor()</c>.
/// Implementations MUST therefore key any per-descriptor cache or comparison on stable values
/// such as <c>descriptor.Name</c> and <c>descriptor.BoundedContext</c>; reference-identity
/// comparisons against a registry-side descriptor instance will mis-cache.
/// </para>
/// </remarks>
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
