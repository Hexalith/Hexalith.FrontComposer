using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

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

    /// <summary>Gets or sets the logger used for runtime diagnostics (HFC2120 / HFC1039).</summary>
    [Inject]
    public ILogger<FcFieldSlotHost<TProjection, TField>> Logger { get; set; } = default!;

    /// <summary>Gets or sets the parent projection row.</summary>
    [Parameter, EditorRequired]
    public TProjection Parent { get; set; } = default!;

    /// <summary>Gets or sets the selected field value.</summary>
    [Parameter]
    public TField? Value { get; set; }

    /// <summary>Gets or sets generated field metadata.</summary>
    [Parameter, EditorRequired]
    public FieldDescriptor Field { get; set; } = default!;

    /// <summary>Gets or sets the current render context.</summary>
    [Parameter, EditorRequired]
    public RenderContext RenderContext { get; set; } = default!;

    /// <summary>Gets or sets the projection role.</summary>
    [Parameter]
    public ProjectionRole? ProjectionRole { get; set; }

    /// <summary>Gets or sets the default generated field renderer. This bypasses slot lookup.</summary>
    [Parameter]
    public RenderFragment<FieldSlotContext<TProjection, TField>>? RenderDefault { get; set; }

    /// <summary>Gets or sets the optional render-tree key used to anchor the slot host across
    /// virtualized row reuse (Story 6-3 D17). Generated emitters supply
    /// <c>(rowKey, fieldName)</c> so a re-rendered row does not leak slot component state to a
    /// neighbour row.</summary>
    [Parameter]
    public object? Key { get; set; }

    /// <inheritdoc />
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);

        // GB-P1 — surface generator/adopter wiring bugs that leave required parameters null instead
        // of silently rendering an empty cell. We cannot construct a FieldSlotContext without these
        // values so RenderDefault cannot be invoked either; the only safe fallback is to log and
        // render nothing.
        if (Parent is null || Field is null || RenderContext is null) {
            Logger.LogWarning(
                "{DiagnosticId}: FcFieldSlotHost<{Projection},{FieldType}> received null required parameter(s). ParentNull: {ParentNull}; FieldNull: {FieldNull}; RenderContextNull: {RenderContextNull}. Field renders nothing. Fix: ensure the calling generated emitter or adopter component supplies all required parameters.",
                FcDiagnosticIds.HFC2120_ProjectionSlotHostMissingParameter,
                typeof(TProjection).FullName,
                typeof(TField).FullName,
                Parent is null,
                Field is null,
                RenderContext is null);
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

        // GB-P14 — guard against descriptor type-arity drift (hand-built descriptor whose FieldType
        // is not the host's TField). Without this check the Blazor parameter cast would throw
        // InvalidCastException at OpenComponent time and abort the row render.
        if (descriptor is not null && descriptor.FieldType != typeof(TField)) {
            Logger.LogWarning(
                "{DiagnosticId}: Level 3 slot descriptor for projection {Projection} field {Field} has FieldType {DescriptorFieldType} which does not match the host's TField {HostFieldType}. Descriptor ignored; default rendering used. Fix: re-register the slot using the typed AddSlotOverride extension so FieldType is derived from the expression.",
                FcDiagnosticIds.HFC1039_ProjectionSlotComponentInvalid,
                typeof(TProjection).FullName,
                Field.Name,
                descriptor.FieldType.FullName,
                typeof(TField).FullName);
            descriptor = null;
        }

        if (descriptor is null) {
            RenderDefault?.Invoke(context)(builder);
            return;
        }

        builder.OpenComponent(0, descriptor.ComponentType);
        if (Key is not null) {
            builder.SetKey(Key);
        }

        builder.AddAttribute(1, "Context", context);
        builder.CloseComponent();
    }
}
