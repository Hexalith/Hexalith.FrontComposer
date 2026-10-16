using Counter.Specimens.Domain;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Counter.Specimens;

[ProjectionTemplate(typeof(SpecimenStatusProjection), ProjectionTemplateContractVersion.Current)]
public partial class SpecimenStatusProjectionTemplate {
    /// <summary>Test specimen opt-in for the real generated status grid.</summary>
    [CascadingParameter(Name = "SpecimenGeneratedStatusGrid")]
    public bool UseGeneratedDefaultGrid { get; set; }

    [Parameter]
    [EditorRequired]
    public ProjectionTemplateContext<SpecimenStatusProjection> Context { get; set; } = default!;
}
