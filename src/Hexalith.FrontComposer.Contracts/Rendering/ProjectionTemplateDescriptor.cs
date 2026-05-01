#if NET10_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Immutable, type-only descriptor for a Level 2 projection-template registration emitted
/// by SourceTools at compile time (Story 6-2 T3 / T4 / D2 / D15). Holds the projection
/// type, the optional role-specific filter, the template component type, and the contract
/// version the template was authored against.
/// </summary>
/// <remarks>
/// <para>
/// <b>Cache-safety boundary (Story 6-2 D15 / AC15).</b> Descriptors carry <i>type metadata only</i>.
/// They MUST NOT capture per-render data: tenant identifiers, user identifiers, item collections,
/// rendered fragments, localized resolved strings, timestamps, or absolute paths. Generated
/// manifests pass the same boundary check at compile time so registry lookup never bleeds
/// per-tenant / per-user / per-culture state across renders.
/// </para>
/// <para>
/// <b>Equality.</b> Descriptors are compared structurally so generated manifests for the same
/// (projection, role, template) tuple deduplicate cleanly.
/// </para>
/// </remarks>
/// <param name="ProjectionType">The projection type the template targets — required.</param>
/// <param name="Role">The optional projection role this template applies to. <see langword="null"/>
/// matches every role of the projection (default behavior).</param>
/// <param name="TemplateType">The Razor component type that implements the template — required.</param>
/// <param name="ContractVersion">The Level 2 contract version the template was authored
/// against (<see cref="ProjectionTemplateContractVersion.Current"/> at the time of compilation).</param>
public sealed record ProjectionTemplateDescriptor(
    Type ProjectionType,
    Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole? Role,
#if NET10_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    Type TemplateType,
    int ContractVersion) {
    /// <summary>Validates non-null projection CLR type.</summary>
    public Type ProjectionType { get; } = ProjectionType ?? throw new ArgumentNullException(nameof(ProjectionType));

    /// <summary>Validates non-null template component type. Trim/AOT metadata flows through this property on .NET 10+.</summary>
#if NET10_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    public Type TemplateType { get; } = TemplateType ?? throw new ArgumentNullException(nameof(TemplateType));
}
