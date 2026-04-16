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
        string route = "/commands/" + boundedContext + "/" + model.TypeName;

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

    private static string StripTrailingCommand(string label) {
        const string suffix = " Command";
        return label.EndsWith(suffix, StringComparison.Ordinal)
            ? label.Substring(0, label.Length - suffix.Length)
            : label;
    }
}
