using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.SourceTools.Tests.Emitters;
// Low-level render frames are intentional regression inputs for generated component parameters.
#pragma warning disable BL0006
/// <summary>Attaches the generated form while replacing unrelated child components with parameter sinks.</summary>
/// <param name="services">The private renderer service provider.</param>
internal sealed class TypedFieldFixtureRenderer(IServiceProvider services) : Renderer(services, NullLoggerFactory.Instance, new TypedFieldSinkActivator())
{
    /// <inheritdoc/>
    public override Microsoft.AspNetCore.Components.Dispatcher Dispatcher { get; } = Microsoft.AspNetCore.Components.Dispatcher.CreateDefault();

    /// <summary>Attaches the generated form so its real callbacks can request rendering.</summary>
    /// <param name="component">The generated component receiving actual edit callbacks.</param>
    internal void Attach(IComponent component) => AssignRootComponentId(component);
    /// <inheritdoc/>
    protected override void HandleException(Exception exception) => throw new InvalidOperationException("Generated callback render failed.", exception);
    /// <inheritdoc/>
    protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
}
#pragma warning restore BL0006
