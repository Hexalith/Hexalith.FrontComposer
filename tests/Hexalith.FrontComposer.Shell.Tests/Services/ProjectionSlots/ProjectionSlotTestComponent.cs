using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Tests.Services.ProjectionSlots;

/// <summary>Provides a compatible context for a registry test projection type.</summary>
/// <typeparam name="TProjection">Projection type supplied to the slot context.</typeparam>
internal sealed class ProjectionSlotTestComponent<TProjection> : ComponentBase
{
    /// <summary>Gets or sets the slot context.</summary>
    [Parameter]
    public FieldSlotContext<TProjection, int> Context { get; set; } = default!;
}
