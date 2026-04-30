using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Counter.Web.Components.Templates;

/// <summary>
/// Story 6-2 T9 / AC9 — Level 2 typed Razor template for <see cref="CounterProjection"/>.
/// Rearranges the projection into a card grid summary while letting the framework render
/// individual fields via <see cref="ProjectionTemplateContext{TProjection}.FieldRenderer"/>
/// so Level 1 annotations (the [RelativeTime] LastUpdated formatter and the Display(Name)
/// "Last changed" header) flow through unchanged.
/// </summary>
[ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
public partial class CounterCardLayoutTemplate {
    [Parameter, EditorRequired]
    public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
}
