#if NET10_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Immutable descriptor for one Level 4 full projection-view replacement registration.
/// </summary>
/// <remarks>
/// Descriptors carry immutable registration metadata only. They must not capture projection
/// items, <see cref="RenderContext"/>, rendered fragments, scoped services, tenant/user data,
/// or localized strings. Runtime validation and duplicate policy live in the Shell registry.
/// </remarks>
/// <param name="ProjectionType">Projection CLR type whose generated body can be replaced.</param>
/// <param name="Role">Optional role-specific match; <see langword="null"/> is role-agnostic.</param>
/// <param name="ComponentType">Razor component type that receives <c>ProjectionViewContext&lt;TProjection&gt;</c>.</param>
/// <param name="ContractVersion">Level 4 contract version expected by the descriptor.</param>
/// <param name="RegistrationSource">Human-readable source for diagnostics.</param>
public sealed record ProjectionViewOverrideDescriptor(
    Type ProjectionType,
    ProjectionRole? Role,
#if NET10_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    Type ComponentType,
    int ContractVersion,
    string RegistrationSource) {
    /// <summary>Validates non-null projection CLR type.</summary>
    public Type ProjectionType { get; } = ProjectionType ?? throw new ArgumentNullException(nameof(ProjectionType));

    /// <summary>Validates non-null component CLR type. Trim/AOT metadata flows through this property on .NET 10+.</summary>
#if NET10_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    public Type ComponentType { get; } = ComponentType ?? throw new ArgumentNullException(nameof(ComponentType));

    /// <summary>Gets a human-readable registration source for diagnostics.</summary>
    public string RegistrationSource { get; } = string.IsNullOrWhiteSpace(RegistrationSource)
        ? "<unknown>"
        : RegistrationSource;
}
