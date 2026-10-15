using Counter.Domain;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Counter.Specimens;

[ProjectionTemplate(typeof(CounterProjection), ProjectionTemplateContractVersion.Current)]
public partial class CounterProjectionTemplate {
    /// <summary>Existing specimen-route test seam for measuring the actual generated grid.</summary>
    [CascadingParameter(Name = "SpecimenGeneratedDefaultGrid")]
    public bool UseGeneratedDefaultGrid { get; set; }

    [Parameter]
    [EditorRequired]
    public ProjectionTemplateContext<CounterProjection> Context { get; set; } = default!;
}
