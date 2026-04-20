namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Serialisation shape for the navigation persistence blob (Story 3-2 D21).
/// Uses <see cref="Dictionary{TKey, TValue}"/> (not <see cref="System.Collections.Immutable.ImmutableDictionary{TKey, TValue}"/>)
/// for <see cref="System.Text.Json.JsonSerializer"/> compatibility without custom converters.
/// The schema is pinned via <c>NavigationPersistenceSnapshotTests.BlobSchemaLocked</c>.
/// </summary>
/// <remarks>
/// Wire format (JSON): <c>{"SidebarCollapsed":bool, "CollapsedGroups":{bc:bool, ...}}</c>.
/// <c>CurrentViewport</c> is NEVER serialised (ADR-037).
/// </remarks>
/// <param name="SidebarCollapsed">The persisted sidebar collapsed flag.</param>
/// <param name="CollapsedGroups">The per-bounded-context collapsed flags (sparse-by-default per D11).</param>
public sealed record NavigationPersistenceBlob(
    bool SidebarCollapsed,
    Dictionary<string, bool> CollapsedGroups);
