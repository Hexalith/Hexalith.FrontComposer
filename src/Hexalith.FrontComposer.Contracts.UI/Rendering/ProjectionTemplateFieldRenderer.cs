using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>Renders a single generated field through the framework's column-emitter path.</summary>
/// <typeparam name="TProjection">The projection type owning the column.</typeparam>
/// <param name="row">The projection row whose field is being rendered.</param>
/// <param name="columnPropertyName">The case-sensitive property name of the column.</param>
/// <returns>A <see cref="RenderFragment"/> emitting the framework's default field rendering.</returns>
public delegate RenderFragment ProjectionTemplateFieldRenderer<TProjection>(TProjection row, string columnPropertyName);
