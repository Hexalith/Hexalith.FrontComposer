using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Throws a synthetic Level 2 fault in generated Counter verification.</summary>
internal sealed class ThrowingCounterTemplate : ComponentBase
{
    /// <summary>Gets or sets the framework context supplied for this fixture render.</summary>
    [Parameter]
    public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        throw new InvalidOperationException("SensitiveTokenSentinel");
    }
}
