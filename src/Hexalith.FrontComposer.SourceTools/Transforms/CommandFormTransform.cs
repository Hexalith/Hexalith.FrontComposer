using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Pure function that converts a <see cref="CommandModel"/> IR into a
/// <see cref="CommandFormModel"/> describing the fields to render.
/// </summary>
public static class CommandFormTransform {
    /// <summary>
    /// Maximum character length for humanized enum option labels.
    /// </summary>
    public const int EnumOptionLabelMaxLength = 30;

    /// <summary>
    /// Transforms a command IR into a form rendering model.
    /// </summary>
    /// <param name="model">The command IR from the Parse stage.</param>
    /// <returns>A ready-to-emit <see cref="CommandFormModel"/>.</returns>
    public static CommandFormModel Transform(CommandModel model) {
        ImmutableArray<FormFieldModel>.Builder fields = ImmutableArray.CreateBuilder<FormFieldModel>();
        foreach (PropertyModel property in model.NonDerivableProperties) {
            fields.Add(MapField(property));
        }

        string commandFqn = string.IsNullOrEmpty(model.Namespace)
            ? model.TypeName
            : model.Namespace + "." + model.TypeName;

        string buttonLabel = BuildButtonLabel(model);

        return new CommandFormModel(
            model.TypeName,
            model.Namespace,
            model.BoundedContext,
            commandFqn,
            buttonLabel,
            new EquatableArray<FormFieldModel>(fields.ToImmutable()));
    }

    private static FormFieldModel MapField(PropertyModel property) {
        FormFieldTypeCategory category = MapCategory(property);
        string staticLabel = ResolveLabel(property);
        bool isRequired = !property.IsNullable && !property.IsUnsupported;

        return new FormFieldModel(
            property.Name,
            property.TypeName,
            category,
            staticLabel,
            property.IsNullable,
            isRequired,
            property.EnumFullyQualifiedName);
    }

    private static FormFieldTypeCategory MapCategory(PropertyModel property) {
        if (property.IsUnsupported) {
            return FormFieldTypeCategory.Placeholder;
        }

        return property.TypeName switch {
            "String" => FormFieldTypeCategory.TextInput,
            "Int32" or "Int64" => FormFieldTypeCategory.NumberInput,
            "Decimal" or "Double" or "Single" => FormFieldTypeCategory.DecimalInput,
            "Boolean" => FormFieldTypeCategory.Switch,
            "DateTime" or "DateTimeOffset" or "DateOnly" => FormFieldTypeCategory.DatePicker,
            "TimeOnly" => FormFieldTypeCategory.TextInput,
            "Enum" => FormFieldTypeCategory.Select,
            "Guid" => FormFieldTypeCategory.MonospaceText,
            _ => FormFieldTypeCategory.Placeholder,
        };
    }

    private static string ResolveLabel(PropertyModel property) {
        if (!string.IsNullOrEmpty(property.DisplayName)) {
            return property.DisplayName!;
        }

        string? humanized = CamelCaseHumanizer.Humanize(property.Name);
        return string.IsNullOrEmpty(humanized) ? property.Name : humanized!;
    }

    private static string BuildButtonLabel(CommandModel model) {
        if (!string.IsNullOrEmpty(model.DisplayName)) {
            return "Send " + model.DisplayName;
        }

        string? humanized = CamelCaseHumanizer.Humanize(model.TypeName);
        return "Send " + (string.IsNullOrEmpty(humanized) ? model.TypeName : humanized);
    }

    /// <summary>
    /// Humanizes an enum member name and truncates it to <see cref="EnumOptionLabelMaxLength"/>.
    /// Exposed for tests and for the emitter's inline helper generation.
    /// </summary>
    public static string HumanizeAndTruncateEnumMember(string memberName) {
        string? humanized = CamelCaseHumanizer.Humanize(memberName);
        string label = string.IsNullOrEmpty(humanized) ? memberName : humanized!;
        if (label.Length <= EnumOptionLabelMaxLength) {
            return label;
        }

        return label.Substring(0, EnumOptionLabelMaxLength - 1) + "\u2026";
    }
}
