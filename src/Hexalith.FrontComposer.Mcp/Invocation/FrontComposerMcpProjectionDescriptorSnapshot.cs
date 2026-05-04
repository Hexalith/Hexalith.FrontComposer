using Hexalith.FrontComposer.Contracts.Mcp;
using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Invocation;

internal sealed record FrontComposerMcpProjectionDescriptorSnapshot(
    string ProtocolUri,
    string Name,
    string ProjectionTypeName,
    string BoundedContext,
    string Title,
    string? Description,
    IReadOnlyList<McpParameterDescriptor> Fields,
    McpProjectionRenderStrategy RenderStrategy,
    string? EntityPluralLabel,
    string? EmptyStateCtaCommandName,
    SchemaFingerprint? Fingerprint) {
    public static FrontComposerMcpProjectionDescriptorSnapshot FromDescriptor(McpResourceDescriptor descriptor)
        => new(
            descriptor.ProtocolUri,
            descriptor.Name,
            descriptor.ProjectionTypeName,
            descriptor.BoundedContext,
            descriptor.Title,
            descriptor.Description,
            [.. descriptor.Fields.Select(CopyParameter)],
            descriptor.RenderStrategy,
            descriptor.EntityPluralLabel,
            descriptor.EmptyStateCtaCommandName,
            descriptor.Fingerprint);

    public McpResourceDescriptor ToDescriptor()
        => new(
            ProtocolUri,
            Name,
            ProjectionTypeName,
            BoundedContext,
            Title,
            Description,
            Fields,
            RenderStrategy,
            EntityPluralLabel,
            EmptyStateCtaCommandName,
            Fingerprint);

    private static McpParameterDescriptor CopyParameter(McpParameterDescriptor parameter)
        => parameter with {
            EnumValues = parameter.EnumValues.ToArray(),
            BadgeMappings = parameter.BadgeMappings is null
                ? null
                : new Dictionary<string, string>(parameter.BadgeMappings, StringComparer.Ordinal),
        };
}

