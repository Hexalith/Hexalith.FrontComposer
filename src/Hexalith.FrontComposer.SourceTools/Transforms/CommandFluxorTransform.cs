using Hexalith.FrontComposer.SourceTools.Parsing;

namespace Hexalith.FrontComposer.SourceTools.Transforms;

/// <summary>
/// Pure function that converts a <see cref="CommandModel"/> into a
/// <see cref="CommandFluxorModel"/> describing its lifecycle feature.
/// </summary>
public static class CommandFluxorTransform {
    /// <summary>
    /// Transforms the command IR into Fluxor generation metadata.
    /// </summary>
    public static CommandFluxorModel Transform(CommandModel model) {
        string stateName = model.TypeName + "LifecycleState";
        string featureName = model.TypeName + "LifecycleFeature";
        string actionsWrapper = model.TypeName + "Actions";
        string reducers = model.TypeName + "Reducers";
        string commandFqn = string.IsNullOrEmpty(model.Namespace)
            ? model.TypeName
            : model.Namespace + "." + model.TypeName;
        string featureQualified = string.IsNullOrEmpty(model.Namespace)
            ? stateName
            : model.Namespace + "." + stateName;

        return new CommandFluxorModel(
            model.TypeName,
            model.Namespace,
            stateName,
            featureName,
            actionsWrapper,
            reducers,
            commandFqn,
            featureQualified);
    }
}
