namespace Hexalith.FrontComposer.Mcp;

/// <summary>
/// Story 8-1 fail-closed gate for command tools that carry Story 7-3 <c>AuthorizationPolicyName</c>.
/// Hosts that wire FrontComposer Shell typically register a Shell-backed implementation that delegates
/// to the existing command authorization evaluator. Hosts that do not register a gate cause every
/// policy-protected command to fail-closed with <see cref="FrontComposerMcpFailureCategory.PolicyGateMissing"/>.
/// Full per-policy enforcement and tenant-scoped enumeration is owned by Story 8-2.
/// </summary>
public interface IFrontComposerMcpCommandPolicyGate {
    ValueTask<bool> EvaluateAsync(
        string policyName,
        FrontComposerMcpAgentContext context,
        CancellationToken cancellationToken);
}
