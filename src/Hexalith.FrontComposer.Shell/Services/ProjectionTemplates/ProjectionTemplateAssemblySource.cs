using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Hexalith.FrontComposer.Contracts.Rendering;

namespace Hexalith.FrontComposer.Shell.Services.ProjectionTemplates;

/// <summary>
/// Story 6-2 T4 — DI-registered marker carrying the descriptors emitted by SourceTools for
/// a single consuming Razor/Web assembly. The runtime registry constructor enumerates every
/// registered <see cref="ProjectionTemplateAssemblySource"/> and feeds the descriptors into
/// the lookup table.
/// </summary>
/// <remarks>
/// The descriptors are resolved by reading the well-known generated type
/// <c>__FrontComposerProjectionTemplatesRegistration</c> from the supplied assembly. The
/// lookup is a single fixed-name <see cref="Type.GetType(string)"/> call — no broad assembly
/// scan (Story 6-2 D2 / AC11).
/// </remarks>
public sealed class ProjectionTemplateAssemblySource {
    public const string GeneratedTypeName = "__FrontComposerProjectionTemplatesRegistration";

    public ProjectionTemplateAssemblySource(IReadOnlyList<ProjectionTemplateDescriptor> descriptors) {
        ArgumentNullException.ThrowIfNull(descriptors);
        Descriptors = descriptors;
    }

    public IReadOnlyList<ProjectionTemplateDescriptor> Descriptors { get; }

    /// <summary>
    /// Reads the SourceTools-generated manifest type from the supplied assembly and returns
    /// the projected descriptors. Returns an empty list when the assembly does not declare
    /// the generated type — typical for domain-only assemblies that ship no Level 2 templates.
    /// </summary>
    /// <param name="assembly">The assembly to inspect.</param>
    /// <returns>The resolved descriptors, or an empty list when the assembly contains none.</returns>
    [RequiresUnreferencedCode("Reads the generated __FrontComposerProjectionTemplatesRegistration type via reflection.")]
    [UnconditionalSuppressMessage(
        "Trimming",
        "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicProperties'",
        Justification = "Generated type is preserved by the source generator with public static members; consumers explicitly opt in via AddHexalithProjectionTemplates which carries the same RequiresUnreferencedCode annotation.")]
    public static IReadOnlyList<ProjectionTemplateDescriptor> ResolveDescriptors(Assembly assembly) {
        ArgumentNullException.ThrowIfNull(assembly);

        Type? generated = assembly.GetType(GeneratedTypeName, throwOnError: false);
        if (generated is null) {
            return [];
        }

        PropertyInfo? property = generated.GetProperty(
            "Descriptors",
            BindingFlags.Public | BindingFlags.Static);
        if (property is null) {
            return [];
        }

        if (property.GetValue(null) is IReadOnlyList<ProjectionTemplateDescriptor> typed) {
            return typed;
        }

        if (property.GetValue(null) is IEnumerable<ProjectionTemplateDescriptor> enumerable) {
            return [.. enumerable];
        }

        return [];
    }
}
