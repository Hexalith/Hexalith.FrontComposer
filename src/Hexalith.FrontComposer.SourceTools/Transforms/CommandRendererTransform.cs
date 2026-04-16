using System.Collections.Immutable;

using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Pure-function transform producing the IR consumed by
/// <c>CommandRendererEmitter</c>, <c>CommandPageEmitter</c>, and <c>LastUsedSubscriberEmitter</c>.
/// Story 2-2 Task 4.1.
/// </summary>
public static class CommandRendererTransform {
    /// <summary>Transforms a command IR into a renderer IR.</summary>
    public static CommandRendererModel Transform(CommandModel model, CommandFluxorModel fluxor) {
        if (model is null) {
            throw new ArgumentNullException(nameof(model));
        }

        if (fluxor is null) {
            throw new ArgumentNullException(nameof(fluxor));
        }

        string displayLabel = BuildDisplayLabel(model);
        string boundedContext = string.IsNullOrEmpty(model.BoundedContext) ? "Default" : model.BoundedContext!;
        // Story 2-2 code-review P40 — sanitize route segments to safe URL-path characters.
        string route = "/commands/" + SanitizeRouteSegment(boundedContext) + "/" + SanitizeRouteSegment(model.TypeName);

        ImmutableArray<string>.Builder nonDerivable = ImmutableArray.CreateBuilder<string>();
        foreach (PropertyModel p in model.NonDerivableProperties) {
            nonDerivable.Add(p.Name);
        }

        ImmutableArray<string>.Builder derivable = ImmutableArray.CreateBuilder<string>();
        foreach (PropertyModel p in model.DerivableProperties) {
            derivable.Add(p.Name);
        }

        return new CommandRendererModel(
            typeName: model.TypeName,
            @namespace: model.Namespace,
            boundedContext: model.BoundedContext,
            density: model.Density,
            iconName: model.IconName,
            displayLabel: displayLabel,
            fullPageRoute: route,
            commandFullyQualifiedName: string.IsNullOrEmpty(model.Namespace) ? model.TypeName : model.Namespace + "." + model.TypeName,
            nonDerivablePropertyNames: new EquatableArray<string>(nonDerivable.ToImmutable()),
            derivablePropertyNames: new EquatableArray<string>(derivable.ToImmutable()),
            formComponentName: model.TypeName + "Form",
            actionsWrapperName: fluxor.ActionsWrapperName,
            stateName: fluxor.StateName,
            subscriberTypeName: model.TypeName + "LastUsedSubscriber");
    }

    private static string BuildDisplayLabel(CommandModel model) {
        if (!string.IsNullOrEmpty(model.DisplayName)) {
            return StripTrailingCommand(model.DisplayName!);
        }

        string? humanized = CamelCaseHumanizer.Humanize(model.TypeName);
        string source = string.IsNullOrEmpty(humanized) ? model.TypeName : humanized!;
        return StripTrailingCommand(source);
    }

    // Story 2-2 code-review P40 — restrict route segments to URL-safe characters (letters, digits, '.', '-', '_').
    // Anything else collapses to '-'; empty segments become "_".
    private static string SanitizeRouteSegment(string segment) {
        if (string.IsNullOrEmpty(segment)) {
            return "_";
        }

        System.Text.StringBuilder sb = new(segment.Length);
        foreach (char c in segment) {
            _ = sb.Append(char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' ? c : '-');
        }

        return sb.ToString();
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
}
