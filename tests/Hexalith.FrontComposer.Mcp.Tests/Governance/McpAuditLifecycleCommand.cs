namespace Hexalith.FrontComposer.Mcp.Tests.Governance;

/// <summary>
/// Command dispatched through the MCP SDK by the lifecycle ownership governance test. The invoker creates it by
/// reflection and injects the server-controlled tenant, user, and handle values.
/// </summary>
internal sealed class McpAuditLifecycleCommand {
    public string MessageId { get; set; } = "";

    public string CorrelationId { get; set; } = "";

    public string TenantId { get; set; } = "";

    public string UserId { get; set; } = "";

    public int Amount { get; set; }
}
