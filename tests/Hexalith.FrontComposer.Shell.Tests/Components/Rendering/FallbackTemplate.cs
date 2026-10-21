using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using ViewProjection = Hexalith.FrontComposer.Shell.Tests.Components.Rendering.FcProjectionViewOverrideHostTests.ViewProjection;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>Renders the selected Level 2 fallback in override-host tests.</summary>
internal sealed class FallbackTemplate : ComponentBase
{
    /// <summary>Gets or sets the framework context supplied for this fixture render.</summary>
    [Parameter]
    public ProjectionTemplateContext<ViewProjection> Context { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddContent(0, "Level 2 fallback");
    }
}
