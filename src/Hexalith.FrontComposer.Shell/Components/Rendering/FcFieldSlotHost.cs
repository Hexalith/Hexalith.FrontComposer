using System.Diagnostics.CodeAnalysis;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Hosts a Level 3 field-slot component for one generated projection field.
/// </summary>
/// <typeparam name="TProjection">Projection type owning the field.</typeparam>
/// <typeparam name="TField">Selected field type.</typeparam>
public sealed class FcFieldSlotHost<TProjection, TField> : ComponentBase {
    /// <summary>Gets or sets the registry used to resolve slot descriptors.</summary>
    [Inject]
    public IProjectionSlotRegistry SlotRegistry { get; set; } = default!;

    /// <summary>Gets or sets the parent projection row.</summary>
    [Parameter]
    public TProjection Parent { get; set; } = default!;

    /// <summary>Gets or sets the selected field value.</summary>
    [Parameter]
    public TField? Value { get; set; }

    /// <summary>Gets or sets generated field metadata.</summary>
    [Parameter]
    public FieldDescriptor Field { get; set; } = default!;

    /// <summary>Gets or sets the current render context.</summary>
    [Parameter]
    public RenderContext RenderContext { get; set; } = default!;

    /// <summary>Gets or sets the projection role.</summary>
    [Parameter]
    public ProjectionRole? ProjectionRole { get; set; }

    /// <summary>Gets or sets the default generated field renderer. This bypasses slot lookup.</summary>
    [Parameter]
    public RenderFragment<FieldSlotContext<TProjection, TField>>? RenderDefault { get; set; }

    /// <inheritdoc />
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2072:DynamicallyAccessedMembers",
        Justification = "Slot component types are explicitly registered by the app; Story 6-6 owns richer trim diagnostics for incompatible custom components.")]
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);

        if (Parent is null || Field is null || RenderContext is null) {
            return;
        }

        FieldSlotContext<TProjection, TField> context = new(
            value: Value,
            parent: Parent,
            field: Field,
            renderContext: RenderContext,
            projectionRole: ProjectionRole,
            densityLevel: RenderContext.DensityLevel,
            isReadOnly: RenderContext.IsReadOnly || Field.IsReadOnly,
            isDevMode: RenderContext.IsDevMode,
            renderDefault: RenderDefault);

        ProjectionSlotDescriptor? descriptor = SlotRegistry.Resolve(
            typeof(TProjection),
            ProjectionRole,
            Field.Name);

        if (descriptor is null) {
            RenderDefault?.Invoke(context)(builder);
            return;
        }

        builder.OpenComponent(0, descriptor.ComponentType);
        builder.AddAttribute(1, "Context", context);
        builder.CloseComponent();
    }
}
