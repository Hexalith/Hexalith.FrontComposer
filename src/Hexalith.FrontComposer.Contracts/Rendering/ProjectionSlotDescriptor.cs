#if NET10_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

using Hexalith.FrontComposer.Contracts.Attributes;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Immutable descriptor for one Level 3 field-slot override registration.
/// </summary>
/// <remarks>
/// Descriptors carry type and field metadata only. They must not capture parent projection
/// instances, <see cref="RenderContext"/> values, rendered fragments, tenant/user data,
/// culture-specific strings, or scoped services. Component compatibility, contract-version
/// compatibility, duplicate detection, and registration-time policy live in the Shell
/// registry — not on this record.
/// </remarks>
/// <param name="ProjectionType">Projection CLR type that owns the field. Must not be null.</param>
/// <param name="FieldName">Canonical property name selected by the typed expression. Must not be null or empty.</param>
/// <param name="FieldType">Selected field CLR type. Must not be null.</param>
/// <param name="Role">Optional role-specific match; <see langword="null"/> is role-agnostic.</param>
/// <param name="ComponentType">Razor component type used to render the slot. Must not be null. Trim
/// metadata is preserved via <c>DynamicallyAccessedMembersAttribute</c> on .NET 10+ targets.</param>
/// <param name="ContractVersion">Field-slot contract version expected by the descriptor.</param>
public sealed record ProjectionSlotDescriptor(
    Type ProjectionType,
    string FieldName,
    Type FieldType,
    ProjectionRole? Role,
#if NET10_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    Type ComponentType,
    int ContractVersion) {
    /// <summary>Validates non-null reference inputs at construction.</summary>
    public Type ProjectionType { get; } = ProjectionType ?? throw new ArgumentNullException(nameof(ProjectionType));

    /// <summary>Validates the field name is provided and non-whitespace.</summary>
    public string FieldName { get; } = string.IsNullOrWhiteSpace(FieldName)
        ? throw new ArgumentException("Field name must be non-empty and non-whitespace.", nameof(FieldName))
        : FieldName;

    /// <summary>Validates non-null field CLR type.</summary>
    public Type FieldType { get; } = FieldType ?? throw new ArgumentNullException(nameof(FieldType));

    /// <summary>Validates non-null component type. Trim/AOT metadata flows through this property
    /// from the constructor parameter on .NET 10+ targets.</summary>
#if NET10_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    public Type ComponentType { get; } = ComponentType ?? throw new ArgumentNullException(nameof(ComponentType));
}
