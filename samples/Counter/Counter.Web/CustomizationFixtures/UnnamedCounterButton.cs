#if COUNTER_ACCESSIBILITY_WARNING_FIXTURE
using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Extensions;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Counter.Web.CustomizationFixtures;

/// <summary>Compile-only fixture for the unnamed customization button diagnostic.</summary>
public sealed class UnnamedCounterButton : ComponentBase
{
    /// <summary>Gets or sets the Level 4 projection context.</summary>
    [Parameter, EditorRequired]
    public ProjectionViewContext<CounterProjection> Context { get; set; } = default!;

    /// <summary>Provides the typed registration for analyzer discovery during the fixture build.</summary>
    public static void Register(IServiceCollection services)
        => services.AddViewOverride<CounterProjection, UnnamedCounterButton>();

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.OpenElement(0, "button");
        builder.AddAttribute(1, "onclick", EventCallback.Factory.Create(this, static () => { }));
        builder.CloseElement();
    }
}
#endif
