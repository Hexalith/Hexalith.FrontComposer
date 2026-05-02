namespace Hexalith.FrontComposer.Contracts.Mcp;

/// <summary>
/// SDK-neutral descriptor for an agent-readable projection resource.
/// </summary>
public sealed record McpResourceDescriptor(
    string ProtocolUri,
    string Name,
    string ProjectionTypeName,
    string BoundedContext,
    string Title,
    string? Description,
    IReadOnlyList<McpParameterDescriptor> Fields);

