namespace Hexalith.FrontComposer.Contracts.Mcp;

/// <summary>
/// SDK-neutral, generated MCP descriptor set for a FrontComposer domain assembly.
/// </summary>
/// <param name="SchemaVersion">Descriptor schema version emitted by SourceTools.</param>
/// <param name="Commands">Command tool descriptors.</param>
/// <param name="Resources">Projection resource descriptors.</param>
public sealed record McpManifest(
    string SchemaVersion,
    IReadOnlyList<McpCommandDescriptor> Commands,
    IReadOnlyList<McpResourceDescriptor> Resources);

