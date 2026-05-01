#if NET10_0_OR_GREATER
using Hexalith.FrontComposer.Contracts.Attributes;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Typed per-render context passed to Level 4 full projection-view replacement components.
/// </summary>
/// <remarks>
/// A replacement owns only the generated projection body region. The surrounding shell,
/// lifecycle, loading/empty policy, navigation, authorization boundary, telemetry context,
/// and disposal hooks remain framework-owned. This context is constructed per render and
/// must not be cached across tenants, users, cultures, densities, themes, or item sets.
/// </remarks>
/// <typeparam name="TProjection">Projection record/class type being rendered.</typeparam>
public sealed class ProjectionViewContext<TProjection> {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionViewContext{TProjection}"/> class.
    /// </summary>
    public ProjectionViewContext(
        Type projectionType,
        string? boundedContext,
        ProjectionRole? role,
        IReadOnlyList<TProjection> items,
        RenderContext? renderContext,
        IReadOnlyList<ProjectionTemplateColumnDescriptor> columns,
        IReadOnlyList<ProjectionTemplateSectionDescriptor> sections,
        string lifecycleState,
        string entityLabel,
        string entityPluralLabel,
        RenderFragment defaultBody,
        ProjectionTemplateSectionRenderer sectionRenderer,
        ProjectionTemplateRowRenderer<TProjection> rowRenderer,
        ProjectionTemplateFieldRenderer<TProjection> fieldRenderer) {
        ArgumentNullException.ThrowIfNull(projectionType);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(sections);
        ArgumentException.ThrowIfNullOrWhiteSpace(lifecycleState);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityLabel);
        ArgumentException.ThrowIfNullOrWhiteSpace(entityPluralLabel);
        ArgumentNullException.ThrowIfNull(defaultBody);
        ArgumentNullException.ThrowIfNull(sectionRenderer);
        ArgumentNullException.ThrowIfNull(rowRenderer);
        ArgumentNullException.ThrowIfNull(fieldRenderer);

        ProjectionType = projectionType;
        BoundedContext = boundedContext;
        Role = role;
        Items = items;
        RenderContext = renderContext;
        Columns = columns;
        Sections = sections;
        LifecycleState = lifecycleState;
        EntityLabel = entityLabel;
        EntityPluralLabel = entityPluralLabel;
        DefaultBody = defaultBody;
        SectionRenderer = sectionRenderer;
        RowRenderer = rowRenderer;
        FieldRenderer = fieldRenderer;
    }

    /// <summary>Gets the projection type metadata.</summary>
    public Type ProjectionType { get; }

    /// <summary>Gets the bounded-context name declared for the projection, if any.</summary>
    public string? BoundedContext { get; }

    /// <summary>Gets the projection role; <see langword="null"/> means the default role.</summary>
    public ProjectionRole? Role { get; }

    /// <summary>Gets the current framework-owned render/query window, not an all-data grant.</summary>
    public IReadOnlyList<TProjection> Items { get; }

    /// <summary>Gets the current render context carrying tenant, user, render mode, density, and flags.</summary>
    public RenderContext? RenderContext { get; }

    /// <summary>Gets generated field descriptors available to starter templates and replacements.</summary>
    public IReadOnlyList<ProjectionTemplateColumnDescriptor> Columns { get; }

    /// <summary>Gets framework-owned section descriptors available to starter templates and replacements.</summary>
    public IReadOnlyList<ProjectionTemplateSectionDescriptor> Sections { get; }

    /// <summary>Gets the lifecycle summary available to the generated view body.</summary>
    public string LifecycleState { get; }

    /// <summary>Gets the localization-safe singular projection label.</summary>
    public string EntityLabel { get; }

    /// <summary>Gets the localization-safe plural projection label.</summary>
    public string EntityPluralLabel { get; }

    /// <summary>Gets the default generated body renderer. This bypasses the active Level 4 descriptor.</summary>
    public RenderFragment DefaultBody { get; }

    /// <summary>Gets a generated section renderer.</summary>
    public ProjectionTemplateSectionRenderer SectionRenderer { get; }

    /// <summary>Gets a generated row renderer.</summary>
    public ProjectionTemplateRowRenderer<TProjection> RowRenderer { get; }

    /// <summary>Gets a generated field renderer.</summary>
    public ProjectionTemplateFieldRenderer<TProjection> FieldRenderer { get; }

    /// <summary>Gets the effective density level for the current render.</summary>
    public DensityLevel DensityLevel => RenderContext?.DensityLevel ?? DensityLevel.Comfortable;

    /// <summary>Gets a value indicating whether this render should be treated as read-only.</summary>
    public bool IsReadOnly => RenderContext?.IsReadOnly == true;

    /// <summary>Gets a value indicating whether developer diagnostics affordances are active.</summary>
    public bool IsDevMode => RenderContext?.IsDevMode == true;
}
#endif
