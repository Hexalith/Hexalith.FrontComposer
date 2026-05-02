namespace Hexalith.FrontComposer.Contracts.Mcp;

/// <summary>
/// SDK-neutral descriptor for an agent-callable command tool.
/// </summary>
public sealed record McpCommandDescriptor(
    string ProtocolName,
    string CommandTypeName,
    string BoundedContext,
    string Title,
    string? Description,
    string? AuthorizationPolicyName,
    IReadOnlyList<McpParameterDescriptor> Parameters,
    IReadOnlyList<string> DerivablePropertyNames);

