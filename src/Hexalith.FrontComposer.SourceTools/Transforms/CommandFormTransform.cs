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
            new EquatableArray<FormFieldModel>(fields.ToImmutable()),
            model.AuthorizationPolicyName);
    }

    private static FormFieldModel MapField(PropertyModel property) {
        FormFieldTypeCategory category = MapCategory(property);
        bool hasExplicitDisplay = !string.IsNullOrEmpty(property.DisplayName);
        string staticLabel = ResolveLabel(property);
        bool isRequired = !property.IsNullable && !property.IsUnsupported;

        return new FormFieldModel(
            property.Name,
            property.TypeName,
            category,
            staticLabel,
            property.IsNullable,
            isRequired,
            property.EnumFullyQualifiedName,
            hasExplicitDisplay);
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
            // Task 3B.1: TimeOnly renders as FluentTextInput with TextInputType.Time + HH:mm placeholder (patch 2026-04-16 P-06).
            "TimeOnly" => FormFieldTypeCategory.TimeInput,
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

    /// <summary>
    /// Story 2-2 Decision D23 — button label is the humanized command type-name with trailing
    /// " Command" stripped (display-only). Replaces the prior "Send X" prefix in ALL modes
    /// for UX consistency.
    /// </summary>
    private static string BuildButtonLabel(CommandModel model) {
        if (!string.IsNullOrEmpty(model.DisplayName)) {
            return StripTrailingCommand(model.DisplayName!);
        }

        string? humanized = CamelCaseHumanizer.Humanize(model.TypeName);
        string source = string.IsNullOrEmpty(humanized) ? model.TypeName : humanized!;
        return StripTrailingCommand(source);
    }

    // Story 2-2 code-review P41/P42 — case-insensitive suffix strip; fall back to original when result is empty.
    private static string StripTrailingCommand(string label) {
        const string suffix = " Command";
        if (!label.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) {
            return label;
        }

        string stripped = label.Substring(0, label.Length - suffix.Length);
        return string.IsNullOrWhiteSpace(stripped) ? label : stripped;
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
