using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

/// <summary>Throws a sensitive synthetic child-render failure inside a generated fallback.</summary>
internal sealed class ThrowingGeneratedFallback : ComponentBase
{
    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
        => throw new InvalidOperationException("SensitiveFallbackPayload token stack trace");
}
