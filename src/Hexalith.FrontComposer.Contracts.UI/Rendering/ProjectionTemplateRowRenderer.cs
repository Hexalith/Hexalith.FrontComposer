using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>Renders one generated item/row body through FrontComposer-owned markup.</summary>
/// <typeparam name="TProjection">The projection type owning the row.</typeparam>
/// <param name="row">The projection row to render.</param>
/// <returns>A <see cref="RenderFragment"/> for the row.</returns>
public delegate RenderFragment ProjectionTemplateRowRenderer<TProjection>(TProjection row);
