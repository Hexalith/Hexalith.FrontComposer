namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>Immutable descriptor for a framework-owned render section exposed to Level 2 templates.</summary>
/// <param name="Name">The stable section name used with <see cref="ProjectionTemplateSectionRenderer"/>.</param>
/// <param name="DisplayName">The localized display name for tooling/dev-mode surfaces.</param>
/// <param name="Role">The semantic section role, such as <c>Body</c> or <c>Row</c>.</param>
public sealed record ProjectionTemplateSectionDescriptor(
    string Name,
    string DisplayName,
    string Role);
