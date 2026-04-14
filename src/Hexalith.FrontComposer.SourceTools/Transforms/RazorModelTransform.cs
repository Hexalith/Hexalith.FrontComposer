
using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;
/// <summary>
/// Transforms a DomainModel IR into a RazorModel for DataGrid component generation.
/// </summary>
public static class RazorModelTransform {
    /// <summary>
    /// Transforms a parsed domain model into a Razor output model.
    /// </summary>
    /// <param name="model">The domain model IR from the Parse stage.</param>
    /// <returns>A RazorModel ready for the Emit stage.</returns>
    public static RazorModel Transform(DomainModel model) {
        ImmutableArray<ColumnModel>.Builder columnsBuilder = ImmutableArray.CreateBuilder<ColumnModel>();

        foreach (PropertyModel property in model.Properties) {
            if (property.IsUnsupported) {
                continue;
            }

            TypeCategory category = MapTypeCategory(property.TypeName);
            string? formatHint = GetFormatHint(property.TypeName, category);
            string header = ResolveHeader(property);

            columnsBuilder.Add(new ColumnModel(
                property.Name,
                header,
                category,
                formatHint,
                property.IsNullable,
                property.BadgeMappings));
        }

        return new RazorModel(
            model.TypeName,
            model.Namespace,
            model.BoundedContext,
            new EquatableArray<ColumnModel>(columnsBuilder.ToImmutable()));
    }

    private static TypeCategory MapTypeCategory(string typeName) => typeName switch {
        "String" or "Guid" => TypeCategory.Text,
        "Int32" or "Int64" or "Decimal" or "Double" or "Single" => TypeCategory.Numeric,
        "Boolean" => TypeCategory.Boolean,
        "DateTime" or "DateTimeOffset" or "DateOnly" or "TimeOnly" => TypeCategory.DateTime,
        "Enum" => TypeCategory.Enum,
        "Collection" => TypeCategory.Collection,
        _ => TypeCategory.Unsupported,
    };

    private static string? GetFormatHint(string typeName, TypeCategory category) => typeName switch {
        "Int32" or "Int64" => "N0",
        "Decimal" or "Double" or "Single" => "N2",
        "Boolean" => "Yes/No",
        "DateTime" or "DateTimeOffset" or "DateOnly" => "d",
        "TimeOnly" => "t",
        "Enum" => "Humanize:30",
        "Guid" => "Truncate:8",
        "Collection" => "Count",
        _ => null,
    };

    private static string ResolveHeader(PropertyModel property) {
        // Priority 1: [Display(Name)] attribute value
        if (!string.IsNullOrEmpty(property.DisplayName)) {
            return property.DisplayName!;
        }

        // Priority 2: Humanized CamelCase
        string? humanized = CamelCaseHumanizer.Humanize(property.Name);
        if (!string.IsNullOrEmpty(humanized)) {
            return humanized!;
        }

        // Priority 3: Raw property name (fallback)
        return property.Name;
    }
}
