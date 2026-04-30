namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Immutable descriptor for one Level 3 field-slot override registration.
/// </summary>
/// <remarks>
/// Descriptors carry type and field metadata only. They must not capture parent projection
/// instances, <see cref="RenderContext"/> values, rendered fragments, tenant/user data,
/// culture-specific strings, or scoped services.
/// </remarks>
/// <param name="ProjectionType">Projection CLR type that owns the field.</param>
/// <param name="FieldName">Canonical property name selected by the typed expression.</param>
/// <param name="FieldType">Selected field CLR type.</param>
/// <param name="Role">Optional role-specific match; <see langword="null"/> is role-agnostic.</param>
/// <param name="ComponentType">Razor component type used to render the slot.</param>
/// <param name="ContractVersion">Field-slot contract version expected by the descriptor.</param>
public sealed record ProjectionSlotDescriptor(
    Type ProjectionType,
    string FieldName,
    Type FieldType,
    Hexalith.FrontComposer.Contracts.Attributes.ProjectionRole? Role,
    Type ComponentType,
    int ContractVersion);
