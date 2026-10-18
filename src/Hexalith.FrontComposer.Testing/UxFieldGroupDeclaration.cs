namespace Hexalith.FrontComposer.Testing;

/// <summary>Expected group semantics and membership from adopter metadata, in complete declared group order.</summary>
/// <param name="GroupId">Declared group identity.</param>
/// <param name="VisibleLabel">Declared visible group label.</param>
/// <param name="Name">Declared programmatic name.</param>
/// <param name="Description">Declared programmatic description.</param>
/// <param name="FieldKeys">Complete ordered group membership.</param>
public sealed record UxFieldGroupDeclaration(string GroupId, string VisibleLabel, string Name, string Description,
    IReadOnlyList<string> FieldKeys);
