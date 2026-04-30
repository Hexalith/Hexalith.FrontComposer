#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Typed render context passed to Level 2 projection-template Razor components
/// (Story 6-2 T1 / D3 / AC1 / AC2). Carries stable, framework-owned rendering inputs
/// so a template can rearrange section-level layout while still delegating individual
/// field rendering to FrontComposer.
/// </summary>
/// <remarks>
/// <para>
/// <b>Allowed-member boundary (Story 6-2 D3 / AC1).</b> Only stable rendering inputs surface
/// through this context — projection metadata, the read-only item collection, the cascading
/// <see cref="Rendering.RenderContext"/>, immutable column descriptors, and generated render
/// delegates. Templates MUST NOT receive Shell services, Fluxor dispatch/state mutation APIs,
/// raw <c>RazorModel</c>, source-generator private method names, raw <c>RenderTreeBuilder</c>
/// helpers, runtime registry mutation APIs, or mutable collections owned by the generated view.
/// </para>
/// <para>
/// <b>Cache-safety boundary (Story 6-2 D15 / AC15).</b> The context is constructed per render
/// from current generated-view state. Generated registrations and the runtime registry MUST
/// NOT cache <c>ProjectionTemplateContext{TProjection}</c>, the contained item list, the
/// cascading <see cref="Rendering.RenderContext"/>, or any rendered <see cref="RenderFragment"/>
/// output across renders, tenants, users, or cultures.
/// </para>
/// </remarks>
/// <typeparam name="TProjection">The projection record/class type the template targets.</typeparam>
public sealed class ProjectionTemplateContext<TProjection> {
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectionTemplateContext{TProjection}"/> class.
    /// </summary>
    /// <param name="projectionType">The projection type metadata.</param>
    /// <param name="boundedContext">The bounded-context name when declared via
    /// <c>[BoundedContext]</c>; otherwise <see langword="null"/>.</param>
    /// <param name="role">The projection role driving role-specific layout decisions.
    /// <see langword="null"/> for the Default role.</param>
    /// <param name="renderContext">The cascading <see cref="Rendering.RenderContext"/> carrying
    /// tenant, user, mode, density, read-only and dev-mode flags. Templates inherit this
    /// state so wrapper-owned authorization, lifecycle, and degraded-state behavior is preserved.</param>
    /// <param name="items">The read-only collection of projection rows currently in scope.</param>
    /// <param name="columns">Immutable column/section descriptors describing the generated
    /// fields available to the template. Order is the generator-stable display order.</param>
    /// <param name="sections">Immutable section descriptors for framework-owned render regions
    /// that the template may place without bypassing the generated view contract.</param>
    /// <param name="defaultBody">A render delegate emitting the framework's default body for
    /// this projection role — useful when a template wants to wrap the default rendering
    /// rather than fully replace it.</param>
    /// <param name="sectionRenderer">A render delegate that emits a named generated section.</param>
    /// <param name="rowRenderer">A render delegate that emits one generated row/item body.</param>
    /// <param name="fieldRenderer">A render delegate that emits a single generated field for a
    /// given (row, columnPropertyName) pair using the framework's <c>ColumnEmitter</c> path.
    /// Templates rearrange layout by invoking this delegate; field rendering remains framework-owned.</param>
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

    /// <summary>Gets the cascading render context carrying tenant/user/mode/density/read-only/dev-mode
    /// state. Templates inherit this state to keep wrapper-owned behavior intact.</summary>
    public RenderContext? RenderContext { get; }

    /// <summary>Gets the read-only collection of projection rows currently in scope.</summary>
    public IReadOnlyList<TProjection> Items { get; }

    /// <summary>Gets the immutable column descriptors for generated fields available to the template.</summary>
    public IReadOnlyList<ProjectionTemplateColumnDescriptor> Columns { get; }

    /// <summary>Gets immutable descriptors for framework-owned render sections available to the template.</summary>
    public IReadOnlyList<ProjectionTemplateSectionDescriptor> Sections { get; }

    /// <summary>Gets a render delegate emitting the framework's default body for this projection.</summary>
    public RenderFragment DefaultBody { get; }

    /// <summary>Gets a render delegate emitting a named generated section.</summary>
    public ProjectionTemplateSectionRenderer SectionRenderer { get; }

    /// <summary>Gets a render delegate emitting one generated row/item body.</summary>
    public ProjectionTemplateRowRenderer<TProjection> RowRenderer { get; }

    /// <summary>Gets a render delegate emitting a single generated field for a given row + column.</summary>
    public ProjectionTemplateFieldRenderer<TProjection> FieldRenderer { get; }
}

/// <summary>
/// Renders a framework-owned section by name. Unknown section names return an empty fragment.
/// </summary>
/// <param name="sectionName">The case-sensitive section name from <see cref="ProjectionTemplateSectionDescriptor.Name"/>.</param>
/// <returns>A <see cref="RenderFragment"/> for the requested section.</returns>
public delegate RenderFragment ProjectionTemplateSectionRenderer(string sectionName);

/// <summary>
/// Renders one generated item/row body through FrontComposer-owned markup.
/// </summary>
/// <typeparam name="TProjection">The projection type owning the row.</typeparam>
/// <param name="row">The projection row to render.</param>
/// <returns>A <see cref="RenderFragment"/> for the row.</returns>
public delegate RenderFragment ProjectionTemplateRowRenderer<TProjection>(TProjection row);

/// <summary>
/// Renders a single generated field via the framework's column emitter path
/// (Story 6-2 T1 / D4). Templates invoke this delegate per (row, columnPropertyName)
/// so badge, description, unsupported placeholder, formatting, and accessibility
/// behavior remain owned by FrontComposer.
/// </summary>
/// <typeparam name="TProjection">The projection type owning the column.</typeparam>
/// <param name="row">The projection row whose field is being rendered.</param>
/// <param name="columnPropertyName">The case-sensitive property name of the column.</param>
/// <returns>A <see cref="RenderFragment"/> emitting the framework's default field rendering.
/// Returns an empty fragment when the column is unknown so an out-of-range lookup never throws.</returns>
public delegate RenderFragment ProjectionTemplateFieldRenderer<TProjection>(TProjection row, string columnPropertyName);

/// <summary>
/// Immutable layout-level descriptor for a generated column visible to a Level 2 template
/// (Story 6-2 T1 / D3). Carries display metadata only; no payload values.
/// </summary>
/// <param name="PropertyName">The case-sensitive property name on <c>TProjection</c>.</param>
/// <param name="Header">The localized column header (already resolved by the generated view).</param>
/// <param name="Priority">The column priority when explicitly declared via
/// <c>[ColumnPriority]</c>; <see langword="null"/> otherwise.</param>
/// <param name="Description">The localized field description when authored via
/// <c>[Description]</c> / <c>[Display(Description = ...)]</c>; <see langword="null"/> otherwise.</param>
public sealed record ProjectionTemplateColumnDescriptor(
    string PropertyName,
    string Header,
    int? Priority,
    string? Description);

/// <summary>
/// Immutable descriptor for a framework-owned render section exposed to Level 2 templates.
/// </summary>
/// <param name="Name">The stable section name used with <see cref="ProjectionTemplateSectionRenderer"/>.</param>
/// <param name="DisplayName">The localized display name for tooling/dev-mode surfaces.</param>
/// <param name="Role">The semantic section role, such as <c>Body</c> or <c>Row</c>.</param>
public sealed record ProjectionTemplateSectionDescriptor(
    string Name,
    string DisplayName,
    string Role);
#endif
