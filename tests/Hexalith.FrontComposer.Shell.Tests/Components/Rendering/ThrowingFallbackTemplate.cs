using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

using ViewProjection = Hexalith.FrontComposer.Shell.Tests.Components.Rendering.FcProjectionViewOverrideHostTests.ViewProjection;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>Throws a synthetic render fault to verify bounded template fallback.</summary>
internal sealed class ThrowingFallbackTemplate : ComponentBase
{
    /// <summary>Gets or sets the framework context supplied for this fixture render.</summary>
    [Parameter]
    public ProjectionTemplateContext<ViewProjection> Context { get; set; } = default!;

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        throw new InvalidOperationException("SensitiveTokenSentinel");
    }
}
