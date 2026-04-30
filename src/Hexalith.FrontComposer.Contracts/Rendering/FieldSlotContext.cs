#if NET10_0_OR_GREATER
using Hexalith.FrontComposer.Contracts.Attributes;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Typed context passed to a Level 3 custom field-slot component.
/// </summary>
/// <remarks>
/// Slot components should treat <see cref="Parent"/> and <see cref="RenderContext"/> as
/// read-only render inputs. Do not mutate projection objects or cache this context,
/// tenant/user values, localized text, or rendered fallback output across renders.
/// </remarks>
/// <typeparam name="TProjection">Projection type owning the field.</typeparam>
/// <typeparam name="TField">Selected field type.</typeparam>
public sealed class FieldSlotContext<TProjection, TField> {
    /// <summary>
    /// Initializes a new instance of the <see cref="FieldSlotContext{TProjection,TField}"/> class.
    /// </summary>
    public FieldSlotContext(
        TField? value,
        TProjection parent,
        FieldDescriptor field,
        RenderContext renderContext,
        ProjectionRole? projectionRole,
        DensityLevel densityLevel,
        bool isReadOnly,
        bool isDevMode,
        RenderFragment<FieldSlotContext<TProjection, TField>>? renderDefault) {
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(field);
        ArgumentNullException.ThrowIfNull(renderContext);

        Value = value;
        Parent = parent;
        Field = field;
        RenderContext = renderContext;
        ProjectionRole = projectionRole;
        DensityLevel = densityLevel;
        IsReadOnly = isReadOnly;
        IsDevMode = isDevMode;
        RenderDefault = renderDefault;
    }

    /// <summary>Gets the selected field value for this render.</summary>
    public TField? Value { get; }

    /// <summary>Gets the parent projection item for this render.</summary>
    public TProjection Parent { get; }

    /// <summary>Gets generated field metadata, including label, description, format, order, and hints.</summary>
    public FieldDescriptor Field { get; }

    /// <summary>Gets the current tenant/user/render-mode context.</summary>
    public RenderContext RenderContext { get; }

    /// <summary>Gets the projection role, or <see langword="null"/> for the default role.</summary>
    public ProjectionRole? ProjectionRole { get; }

    /// <summary>Gets the active shell density.</summary>
    public DensityLevel DensityLevel { get; }

    /// <summary>Gets a value indicating whether the field should render read-only.</summary>
    public bool IsReadOnly { get; }

    /// <summary>Gets a value indicating whether developer diagnostics are active.</summary>
    public bool IsDevMode { get; }

    /// <summary>
    /// Gets a generated-default renderer that bypasses the active Level 3 slot for this same
    /// projection/role/field, preventing recursive slot resolution.
    /// </summary>
    public RenderFragment<FieldSlotContext<TProjection, TField>>? RenderDefault { get; }
}
#endif
