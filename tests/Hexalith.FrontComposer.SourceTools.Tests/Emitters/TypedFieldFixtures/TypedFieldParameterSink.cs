using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
/// <summary>Accepts unrelated child parameters without browser or shell services.</summary>
internal sealed class TypedFieldParameterSink : IComponent
{
    /// <inheritdoc/>
    public void Attach(RenderHandle renderHandle)
    {
    }

    /// <inheritdoc/>
    public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
}
