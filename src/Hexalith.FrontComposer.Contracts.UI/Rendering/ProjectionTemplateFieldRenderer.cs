using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>Renders a single generated field through the framework's column-emitter path.</summary>
/// <typeparam name="TProjection">The projection type owning the column.</typeparam>
/// <param name="row">The projection row whose field is being rendered.</param>
/// <param name="columnPropertyName">The case-sensitive CLR property name of a generated column.</param>
/// <returns>
/// A <see cref="RenderFragment"/> that renders the generated field, including any active field slot.
/// The fragment is empty when the name is missing or does not match a generated column.
/// </returns>
public delegate RenderFragment ProjectionTemplateFieldRenderer<TProjection>(TProjection row, string columnPropertyName);
