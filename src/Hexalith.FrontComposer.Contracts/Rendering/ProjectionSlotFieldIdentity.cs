namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Canonical field identity parsed from a refactor-safe slot selector expression.
/// </summary>
/// <param name="Name">The selected direct projection property name.</param>
/// <param name="FieldType">The selected property CLR type.</param>
public sealed record ProjectionSlotFieldIdentity(string Name, Type FieldType);
