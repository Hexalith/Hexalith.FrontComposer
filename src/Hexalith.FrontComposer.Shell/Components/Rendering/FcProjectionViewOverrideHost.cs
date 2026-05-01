using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Components.Rendering;

/// <summary>
/// Hosts a Level 4 full projection-view replacement inside a narrow error boundary.
/// </summary>
/// <typeparam name="TProjection">Projection type being rendered.</typeparam>
public sealed class FcProjectionViewOverrideHost<TProjection> : ComponentBase {
    private ErrorBoundary? _boundary;
    private ProjectionViewOverrideDescriptor? _previousDescriptor;
    private object? _previousItems;
    private RenderContext? _previousRenderContext;

    /// <summary>Gets or sets the selected descriptor.</summary>
    [Parameter, EditorRequired]
    public ProjectionViewOverrideDescriptor Descriptor { get; set; } = default!;

    /// <summary>Gets or sets the per-render Level 4 context.</summary>
    [Parameter, EditorRequired]
    public ProjectionViewContext<TProjection> Context { get; set; } = default!;

    /// <summary>Gets or sets the logger.</summary>
    [Inject]
    public ILogger<FcProjectionViewOverrideHost<TProjection>> Logger { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet() {
        if (_boundary is not null
            && (!Equals(_previousDescriptor, Descriptor)
                || !ReferenceEquals(_previousItems, Context.Items)
                || !Equals(_previousRenderContext, Context.RenderContext))) {
            _boundary.Recover();
        }

        _previousDescriptor = Descriptor;
        _previousItems = Context.Items;
        _previousRenderContext = Context.RenderContext;
    }

    /// <inheritdoc />
    protected override void BuildRenderTree(RenderTreeBuilder builder) {
        ArgumentNullException.ThrowIfNull(builder);

        if (Descriptor is null || Context is null) {
            return;
        }

        builder.OpenComponent<ErrorBoundary>(0);
        builder.AddAttribute(1, "ChildContent", (RenderFragment)RenderReplacement);
        builder.AddAttribute(2, "ErrorContent", (RenderFragment<Exception>)RenderFailure);
        builder.AddComponentReferenceCapture(3, component => _boundary = (ErrorBoundary)component);
        builder.CloseComponent();
    }

    private void RenderReplacement(RenderTreeBuilder builder) {
        builder.OpenComponent(0, Descriptor.ComponentType);
        builder.AddAttribute(1, "Context", Context);
        builder.CloseComponent();
    }

    private RenderFragment RenderFailure(Exception exception)
        => builder => {
            string exceptionCategory = exception.GetType().Name;
            Logger.LogWarning(
                "{DiagnosticId}: Level 4 view replacement render fault isolated. Projection: {Projection}; Component: {Component}; Role: {Role}; ExceptionCategory: {ExceptionCategory}. Item payloads, field values, localized strings, raw exception messages, and render fragments are intentionally omitted.",
                FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault,
                typeof(TProjection).FullName,
                Descriptor.ComponentType.FullName,
                Context.Role?.ToString() ?? "<default>",
                exceptionCategory);

            builder.OpenElement(0, "div");
            builder.AddAttribute(1, "class", "fc-view-override-fault");
            builder.AddAttribute(2, "role", "alert");
            builder.AddAttribute(3, "data-fc-diagnostic", FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);
            builder.AddContent(4, FcDiagnosticIds.HFC2121_ProjectionViewOverrideRenderFault);
            builder.CloseElement();
        };
}
