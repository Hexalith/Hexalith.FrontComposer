using Counter.Specimens.Domain;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Counter.Specimens;

[ProjectionTemplate(typeof(SpecimenStatusProjection), ProjectionTemplateContractVersion.Current)]
public partial class SpecimenStatusProjectionTemplate {
    [Parameter]
    [EditorRequired]
    public ProjectionTemplateContext<SpecimenStatusProjection> Context { get; set; } = default!;
}
