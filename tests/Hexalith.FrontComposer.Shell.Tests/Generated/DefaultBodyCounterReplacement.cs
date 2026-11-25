using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Level 4 fixture that delegates its body to the current generated renderer.</summary>
internal sealed class DefaultBodyCounterReplacement : ComponentBase
{
    /// <summary>Gets or sets the supplied Level 4 context.</summary>
    [Parameter]
    public ProjectionViewContext<CounterProjection> Context { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddContent(0, Context.DefaultBody);
    }
}
