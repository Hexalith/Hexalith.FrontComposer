using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>Level 4 fixture that renders a known field alongside a requested field.</summary>
internal sealed class FieldRendererCounterReplacement : ComponentBase
{
    /// <summary>Gets or sets the supplied Level 4 context.</summary>
    [Parameter]
    public ProjectionViewContext<CounterProjection> Context { get; set; } = default!;

    private static readonly string[] s_unrecognizedFields = ["", "RemovedField", "count"];

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        CounterProjection row = Context.Items[0];

        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "data-known-field", string.Empty);
        builder.AddContent(2, Context.FieldRenderer(row, nameof(CounterProjection.Count)));
        builder.CloseElement();

        foreach (string fieldName in s_unrecognizedFields)
        {
            builder.OpenElement(3, "div");
            builder.AddAttribute(4, "data-requested-field", string.Empty);
            builder.AddContent(5, Context.FieldRenderer(row, fieldName));
            builder.CloseElement();
        }
    }
}
