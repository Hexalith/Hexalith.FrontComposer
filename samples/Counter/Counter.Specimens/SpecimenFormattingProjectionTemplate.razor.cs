using Counter.Specimens.Domain;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Counter.Specimens;

[ProjectionTemplate(typeof(SpecimenFormattingProjection), ProjectionTemplateContractVersion.Current)]
public partial class SpecimenFormattingProjectionTemplate {
    [Parameter]
    [EditorRequired]
    public ProjectionTemplateContext<SpecimenFormattingProjection> Context { get; set; } = default!;
}
