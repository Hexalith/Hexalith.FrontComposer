namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Provisional renderer contract. Story 1.3 may add renderer-specific companion abstractions
/// while keeping this interface stable for existing implementers.
/// Implementations transform a model into a typed output (e.g., RenderFragment for Blazor,
/// string for MCP agents).
/// </summary>
/// <typeparam name="TModel">The model type to render.</typeparam>
/// <typeparam name="TOutput">The output type produced by the renderer.</typeparam>
public interface IRenderer<in TModel, out TOutput>
{
    /// <summary>
    /// Determines whether this renderer can handle the given model.
    /// </summary>
    /// <param name="model">The model to check.</param>
    /// <returns><c>true</c> if this renderer supports the model; otherwise <c>false</c>.</returns>
    bool CanRender(TModel model);

    /// <summary>
    /// Renders the model into the output type.
    /// </summary>
    /// <param name="model">The model to render.</param>
    /// <param name="context">The render context carrying tenant, user, and display settings.</param>
    /// <returns>The rendered output.</returns>
    TOutput Render(TModel model, RenderContext context);
}
