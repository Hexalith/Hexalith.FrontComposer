namespace Hexalith.FrontComposer.Testing;

/// <summary>Expected field identity and label from adopter metadata, in complete declared field order.</summary>
/// <param name="FieldKey">Stable field identity independent of DOM IDs.</param>
/// <param name="VisibleLabel">Declared visible field label.</param>
/// <param name="GroupId">Declared owning group.</param>
public sealed record UxFieldDeclaration(string FieldKey, string VisibleLabel, string GroupId);
