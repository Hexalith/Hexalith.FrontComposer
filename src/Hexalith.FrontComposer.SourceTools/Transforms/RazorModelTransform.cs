#nullable enable

namespace Hexalith.FrontComposer.SourceTools.Transforms;

using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Transforms a DomainModel IR into a RazorModel for DataGrid component generation.
/// </summary>
public static class RazorModelTransform
{
    /// <summary>
    /// Transforms a parsed domain model into a Razor output model.
    /// </summary>
    /// <param name="model">The domain model IR from the Parse stage.</param>
    /// <returns>A RazorModel ready for the Emit stage.</returns>
    public static RazorModel Transform(DomainModel model)
    {
        ImmutableArray<ColumnModel>.Builder columnsBuilder = ImmutableArray.CreateBuilder<ColumnModel>();

        foreach (PropertyModel property in model.Properties)
        {
            if (property.IsUnsupported)
            {
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

    private static TypeCategory MapTypeCategory(string typeName)
    {
        switch (typeName)
        {
            case "String":
            case "Guid":
                return TypeCategory.Text;
            case "Int32":
            case "Int64":
            case "Decimal":
            case "Double":
            case "Single":
                return TypeCategory.Numeric;
            case "Boolean":
                return TypeCategory.Boolean;
            case "DateTime":
            case "DateTimeOffset":
            case "DateOnly":
            case "TimeOnly":
                return TypeCategory.DateTime;
            case "Enum":
                return TypeCategory.Enum;
            case "Collection":
                return TypeCategory.Collection;
            default:
                return TypeCategory.Unsupported;
        }
    }

    private static string? GetFormatHint(string typeName, TypeCategory category)
    {
        switch (typeName)
        {
            case "Int32":
            case "Int64":
                return "N0";
            case "Decimal":
            case "Double":
            case "Single":
                return "N2";
            case "Boolean":
                return "Yes/No";
            case "DateTime":
            case "DateTimeOffset":
            case "DateOnly":
                return "d";
            case "TimeOnly":
                return "t";
            case "Enum":
                return "Humanize:30";
            case "Guid":
                return "Truncate:8";
            case "Collection":
                return "Count";
            default:
                return null;
        }
    }

    private static string ResolveHeader(PropertyModel property)
    {
        // Priority 1: [Display(Name)] attribute value
        if (!string.IsNullOrEmpty(property.DisplayName))
        {
            return property.DisplayName!;
        }

        // Priority 2: Humanized CamelCase
        string? humanized = CamelCaseHumanizer.Humanize(property.Name);
        if (!string.IsNullOrEmpty(humanized))
        {
            return humanized!;
        }

        // Priority 3: Raw property name (fallback)
        return property.Name;
    }
}
