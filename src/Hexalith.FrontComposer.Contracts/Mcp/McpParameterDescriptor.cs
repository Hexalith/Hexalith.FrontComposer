namespace Hexalith.FrontComposer.Contracts.Mcp;

/// <summary>
/// SDK-neutral descriptor for a user-editable command parameter or projection field.
/// </summary>
public sealed record McpParameterDescriptor(
    string Name,
    string TypeName,
    string JsonType,
    bool IsRequired,
    bool IsNullable,
    string Title,
    string? Description,
    IReadOnlyList<string> EnumValues,
    bool IsUnsupported);

