using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
/// <summary>Creates child sinks for callbacks that request an unrelated form rerender.</summary>
internal sealed class TypedFieldSinkActivator : IComponentActivator
{
    /// <inheritdoc/>
    public IComponent CreateInstance(Type componentType) => new TypedFieldParameterSink();
}
