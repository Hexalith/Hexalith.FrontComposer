using System.Collections.Concurrent;

namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// Command policy gate that denies every policy, so every policy-protected command tool is hidden. It records every
/// policy name it evaluates, so a test can prove a policy-protected tool was actually gated.
/// </summary>
internal sealed class McpAuditDenyingCommandPolicyGate : IFrontComposerMcpCommandPolicyGate {
    /// <summary>Gets the policy names evaluated (and denied) so far, in evaluation order.</summary>
    internal ConcurrentQueue<string> EvaluatedPolicies { get; } = new();

    /// <inheritdoc />
    public ValueTask<bool> EvaluateAsync(
        string policyName,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken) {
        EvaluatedPolicies.Enqueue(policyName);
        return ValueTask.FromResult(false);
    }
}
