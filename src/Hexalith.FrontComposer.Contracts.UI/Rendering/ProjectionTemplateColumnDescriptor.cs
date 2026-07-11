namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>Immutable layout-level descriptor for a generated column visible to a Level 2 template.</summary>
/// <param name="PropertyName">The case-sensitive property name on <c>TProjection</c>.</param>
/// <param name="Header">The localized column header.</param>
/// <param name="Priority">The explicitly declared column priority, or <see langword="null"/>.</param>
/// <param name="Description">The localized field description, or <see langword="null"/>.</param>
public sealed record ProjectionTemplateColumnDescriptor(
    string PropertyName,
    string Header,
    int? Priority,
    string? Description);
