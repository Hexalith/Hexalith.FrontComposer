using System.Collections.Immutable;
using System.Text;

using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Converts SourceTools command/projection IR into SDK-neutral MCP descriptors.
/// </summary>
public static class McpManifestTransform {
    public const string SchemaVersion = "frontcomposer.mcp.v1";

    public static McpCommandDescriptorModel TransformCommand(CommandModel model, bool disambiguateProtocolName = false) {
        string boundedContext = ResolveBoundedContext(model.BoundedContext);
        string title = CommandFormTransform.Transform(model).ButtonLabel;
        ImmutableArray<McpParameterDescriptorModel>.Builder parameters = ImmutableArray.CreateBuilder<McpParameterDescriptorModel>();
        foreach (PropertyModel property in model.NonDerivableProperties) {
            parameters.Add(MapParameter(property));
        }

        ImmutableArray<string>.Builder derivable = ImmutableArray.CreateBuilder<string>();
        foreach (PropertyModel property in model.DerivableProperties) {
            derivable.Add(property.Name);
        }

        // Description intentionally null when only a Display Name is present — Story 8-1 does not
        // yet propagate [Description]. See Known Gap KG-8-1-1 (FluentValidation/Description backlog).
        return new McpCommandDescriptorModel(
            BuildCommandProtocolName(boundedContext, model.Namespace, model.TypeName, disambiguateProtocolName),
            Fqn(model.Namespace, model.TypeName),
            boundedContext,
            title,
            description: null,
            model.AuthorizationPolicyName,
            parameters.ToImmutable(),
            derivable.ToImmutable());
    }

    public static McpResourceDescriptorModel TransformProjection(DomainModel model, bool disambiguateProtocolUri = false) {
        string boundedContext = ResolveBoundedContext(model.BoundedContext);
        RazorModel razor = RazorModelTransform.Transform(model);
        ImmutableArray<McpParameterDescriptorModel>.Builder fields = ImmutableArray.CreateBuilder<McpParameterDescriptorModel>();

        foreach (ColumnModel column in razor.Columns) {
            fields.Add(new McpParameterDescriptorModel(
                column.PropertyName,
                column.TypeCategory.ToString(),
                MapJsonType(column.TypeCategory.ToString()),
                !column.IsNullable && column.TypeCategory != TypeCategory.Unsupported,
                column.IsNullable,
                column.Header,
                column.Description,
                column.EnumMemberNames.ToImmutableArray(),
                column.TypeCategory == TypeCategory.Unsupported));
        }

        // Same Known Gap KG-8-1-1 applies — projection [Description] propagation is deferred.
        return new McpResourceDescriptorModel(
            BuildProjectionUri(boundedContext, model.Namespace, model.TypeName, disambiguateProtocolUri),
            SanitizeProtocolSegment(model.TypeName),
            Fqn(model.Namespace, model.TypeName),
            boundedContext,
            razor.EntityLabel ?? model.TypeName,
            description: null,
            fields.ToImmutable());
    }

    public static string BuildCommandProtocolName(string boundedContext, string @namespace, string typeName, bool disambiguate)
        => disambiguate && !string.IsNullOrWhiteSpace(@namespace)
            ? SanitizeProtocolSegment(boundedContext) + "." + SanitizeProtocolSegment(@namespace) + "." + SanitizeProtocolSegment(typeName) + ".Execute"
            : SanitizeProtocolSegment(boundedContext) + "." + SanitizeProtocolSegment(typeName) + ".Execute";

    public static string BuildProjectionUri(string boundedContext, string @namespace, string typeName, bool disambiguate)
        => disambiguate && !string.IsNullOrWhiteSpace(@namespace)
            ? "frontcomposer://" + SanitizeProtocolSegment(boundedContext) + "/projections/" + SanitizeProtocolSegment(@namespace) + "." + SanitizeProtocolSegment(typeName)
            : "frontcomposer://" + SanitizeProtocolSegment(boundedContext) + "/projections/" + SanitizeProtocolSegment(typeName);

    private static McpParameterDescriptorModel MapParameter(PropertyModel property)
        => new(
            property.Name,
            property.TypeName,
            MapJsonType(property.TypeName),
            !property.IsNullable && !property.IsUnsupported,
            property.IsNullable,
            ResolveLabel(property),
            property.Description,
            property.EnumMemberNames.ToImmutableArray(),
            property.IsUnsupported);

    private static string ResolveLabel(PropertyModel property) {
        if (!string.IsNullOrWhiteSpace(property.DisplayName)) {
            return property.DisplayName!;
        }

        string? humanized = CamelCaseHumanizer.Humanize(property.Name);
        return string.IsNullOrWhiteSpace(humanized) ? property.Name : humanized!;
    }

    private static string MapJsonType(string typeName) => typeName switch {
        "String" or "Guid" or "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" or "Enum" or nameof(TypeCategory.Text) or nameof(TypeCategory.DateTime) => "string",
        "Int32" or "Int64" or "Decimal" or "Double" or "Single" or nameof(TypeCategory.Numeric) => "number",
        "Boolean" or nameof(TypeCategory.Boolean) => "boolean",
        "Collection" or nameof(TypeCategory.Collection) => "array",
        _ => "object",
    };

    private static string Fqn(string @namespace, string typeName)
        => string.IsNullOrWhiteSpace(@namespace) ? typeName : @namespace + "." + typeName;

    private static string ResolveBoundedContext(string? boundedContext)
        => string.IsNullOrWhiteSpace(boundedContext) ? "Default" : boundedContext!.Trim();

    private static string SanitizeProtocolSegment(string value) {
        if (string.IsNullOrWhiteSpace(value)) {
            return "_";
        }

        var sb = new StringBuilder(value.Length);
        foreach (char c in value) {
            sb.Append(IsAsciiLetterOrDigit(c) || c is '.' or '_' or '-' ? c : '-');
        }

        return sb.Length == 0 ? "_" : sb.ToString();
    }

    private static bool IsAsciiLetterOrDigit(char c)
        => (c >= 'A' && c <= 'Z')
            || (c >= 'a' && c <= 'z')
            || (c >= '0' && c <= '9');
}

public sealed class McpCommandDescriptorModel {
    public McpCommandDescriptorModel(
        string protocolName,
        string commandTypeName,
        string boundedContext,
        string title,
        string? description,
        string? authorizationPolicyName,
        IReadOnlyList<McpParameterDescriptorModel> parameters,
        IReadOnlyList<string> derivablePropertyNames) {
        ProtocolName = protocolName;
        CommandTypeName = commandTypeName;
        BoundedContext = boundedContext;
        Title = title;
        Description = description;
        AuthorizationPolicyName = authorizationPolicyName;
        Parameters = parameters;
        DerivablePropertyNames = derivablePropertyNames;
    }

    public string ProtocolName { get; }

    public string CommandTypeName { get; }

    public string BoundedContext { get; }

    public string Title { get; }

    public string? Description { get; }

    public string? AuthorizationPolicyName { get; }

    public IReadOnlyList<McpParameterDescriptorModel> Parameters { get; }

    public IReadOnlyList<string> DerivablePropertyNames { get; }
}

public sealed class McpResourceDescriptorModel {
    public McpResourceDescriptorModel(
        string protocolUri,
        string name,
        string projectionTypeName,
        string boundedContext,
        string title,
        string? description,
        IReadOnlyList<McpParameterDescriptorModel> fields) {
        ProtocolUri = protocolUri;
        Name = name;
        ProjectionTypeName = projectionTypeName;
        BoundedContext = boundedContext;
        Title = title;
        Description = description;
        Fields = fields;
    }

    public string ProtocolUri { get; }

    public string Name { get; }

    public string ProjectionTypeName { get; }

    public string BoundedContext { get; }

    public string Title { get; }

    public string? Description { get; }

    public IReadOnlyList<McpParameterDescriptorModel> Fields { get; }
}

public sealed class McpParameterDescriptorModel {
    public McpParameterDescriptorModel(
        string name,
        string typeName,
        string jsonType,
        bool isRequired,
        bool isNullable,
        string title,
        string? description,
        IReadOnlyList<string> enumValues,
        bool isUnsupported) {
        Name = name;
        TypeName = typeName;
        JsonType = jsonType;
        IsRequired = isRequired;
        IsNullable = isNullable;
        Title = title;
        Description = description;
        EnumValues = enumValues;
        IsUnsupported = isUnsupported;
    }

    public string Name { get; }

    public string TypeName { get; }

    public string JsonType { get; }

    public bool IsRequired { get; }

    public bool IsNullable { get; }

    public string Title { get; }

    public string? Description { get; }

    public IReadOnlyList<string> EnumValues { get; }

    public bool IsUnsupported { get; }
}
