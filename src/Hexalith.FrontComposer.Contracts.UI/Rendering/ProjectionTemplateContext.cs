using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Typed render context passed to Level 2 projection-template Razor components.
/// </summary>
/// <remarks>
/// Only stable rendering inputs surface through this context. The context is constructed per render
/// and must not be cached across renders, tenants, users, or cultures.
/// </remarks>
/// <typeparam name="TProjection">The projection record/class type the template targets.</typeparam>
public sealed class ProjectionTemplateContext<TProjection> {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionTemplateContext{TProjection}"/> class.
    /// </summary>
    public ProjectionTemplateContext(
        Type projectionType,
        string? boundedContext,
        Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole? role,
        RenderContext? renderContext,
        IReadOnlyList<TProjection> items,
        IReadOnlyList<ProjectionTemplateColumnDescriptor> columns,
        IReadOnlyList<ProjectionTemplateSectionDescriptor> sections,
        RenderFragment defaultBody,
        ProjectionTemplateSectionRenderer sectionRenderer,
        ProjectionTemplateRowRenderer<TProjection> rowRenderer,
        ProjectionTemplateFieldRenderer<TProjection> fieldRenderer) {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentNullException.ThrowIfNull(defaultBody);
        ArgumentNullException.ThrowIfNull(sectionRenderer);
        ArgumentNullException.ThrowIfNull(rowRenderer);
        ArgumentNullException.ThrowIfNull(fieldRenderer);

        ProjectionType = projectionType;
        BoundedContext = boundedContext;
        Role = role;
        RenderContext = renderContext;
        Items = items;
        Columns = columns;
        Sections = sections;
        DefaultBody = defaultBody;
        SectionRenderer = sectionRenderer;
        RowRenderer = rowRenderer;
        FieldRenderer = fieldRenderer;
    }

    /// <summary>Gets the projection type metadata.</summary>
    public Type ProjectionType { get; }

    /// <summary>Gets the optional bounded-context name attached to the projection.</summary>
    public string? BoundedContext { get; }

    /// <summary>Gets the projection role; <see langword="null"/> for the Default role.</summary>
    public Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole? Role { get; }

    /// <summary>Gets the cascading render context.</summary>
    public RenderContext? RenderContext { get; }

    /// <summary>Gets the read-only collection of projection rows currently in scope.</summary>
    public IReadOnlyList<TProjection> Items { get; }

    /// <summary>Gets the immutable column descriptors for generated fields available to the template.</summary>
    public IReadOnlyList<ProjectionTemplateColumnDescriptor> Columns { get; }

    /// <summary>Gets immutable descriptors for framework-owned render sections.</summary>
    public IReadOnlyList<ProjectionTemplateSectionDescriptor> Sections { get; }

    /// <summary>Gets a render delegate emitting the framework's default body.</summary>
    public RenderFragment DefaultBody { get; }

    /// <summary>Gets a render delegate emitting a named generated section.</summary>
    public ProjectionTemplateSectionRenderer SectionRenderer { get; }

    /// <summary>Gets a render delegate emitting one generated row/item body.</summary>
    public ProjectionTemplateRowRenderer<TProjection> RowRenderer { get; }

    /// <summary>Gets a render delegate emitting a single generated field.</summary>
    public ProjectionTemplateFieldRenderer<TProjection> FieldRenderer { get; }
}
