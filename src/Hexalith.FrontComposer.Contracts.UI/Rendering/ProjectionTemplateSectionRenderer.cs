using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>Renders a framework-owned section by name.</summary>
/// <param name="sectionName">The case-sensitive section name from <see cref="ProjectionTemplateSectionDescriptor.Name"/>.</param>
/// <returns>A <see cref="RenderFragment"/> for the requested section.</returns>
public delegate RenderFragment ProjectionTemplateSectionRenderer(string sectionName);
