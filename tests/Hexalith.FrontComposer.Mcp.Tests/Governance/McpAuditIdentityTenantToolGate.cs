using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.Mcp;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// Restrictive tenant gate for the SM-4 audit. It admits only one tenant and user, hides the named tools from that
/// caller, and throws an exception whose text carries server-only values for the named throwing tools. It records
/// every protocol name it evaluates and every one it denies, so a test can prove a hidden tool was actually gated.
/// </summary>
/// <param name="tenantId">The only tenant the gate admits.</param>
/// <param name="userId">The only user the gate admits.</param>
/// <param name="hiddenToolNames">Protocol names the gate answers <see langword="false"/> for.</param>
/// <param name="throwingToolNames">Protocol names the gate throws for.</param>
internal sealed class McpAuditIdentityTenantToolGate(
    string tenantId,
    string userId,
    IReadOnlyCollection<string>? hiddenToolNames = null,
    IReadOnlyCollection<string>? throwingToolNames = null) : IFrontComposerMcpTenantToolGate {
    /// <summary>The marker every exception thrown by this gate starts with.</summary>
    internal const string ExceptionMarker = "audit-gate-exception-3b7a";

    /// <summary>Gets the protocol names evaluated so far, in evaluation order.</summary>
    internal ConcurrentQueue<string> Evaluated { get; } = new();

    /// <summary>Gets the protocol names denied so far, by a <see langword="false"/> answer or a throw.</summary>
    internal ConcurrentQueue<string> Denied { get; } = new();

    /// <inheritdoc />
    public ValueTask<bool> IsVisibleAsync(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(context);
        Evaluated.Enqueue(descriptor.ProtocolName);
        if (throwingToolNames?.Contains(descriptor.ProtocolName) == true) {
            Denied.Enqueue(descriptor.ProtocolName);
            throw new InvalidOperationException($"{ExceptionMarker} {context.TenantId} {context.UserId} {descriptor.ProtocolName}");
        }

        bool visible = string.Equals(context.TenantId, tenantId, StringComparison.Ordinal)
            && string.Equals(context.UserId, userId, StringComparison.Ordinal)
            && hiddenToolNames?.Contains(descriptor.ProtocolName) != true;
        if (!visible) {
            Denied.Enqueue(descriptor.ProtocolName);
        }

        return ValueTask.FromResult(visible);
    }
}
