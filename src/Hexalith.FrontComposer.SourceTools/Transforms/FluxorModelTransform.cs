#nullable enable

namespace Hexalith.FrontComposer.SourceTools.Transforms;

using Hexalith.FrontComposer.SourceTools.Parsing;

/// <summary>
/// Transforms a DomainModel IR into a FluxorModel for state management generation.
/// </summary>
public static class FluxorModelTransform
{
    /// <summary>
    /// Transforms a parsed domain model into a Fluxor output model.
    /// </summary>
    /// <param name="model">The domain model IR from the Parse stage.</param>
    /// <returns>A FluxorModel ready for the Emit stage.</returns>
    public static FluxorModel Transform(DomainModel model)
    {
        return new FluxorModel(
            model.TypeName,
            model.Namespace,
            model.TypeName + "State",
            model.TypeName + "Feature");
    }
}
