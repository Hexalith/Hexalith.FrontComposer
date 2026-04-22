namespace Hexalith.FrontComposer.Shell.State.Navigation;

/// <summary>
/// Serialisation shape for the navigation persistence blob (Story 3-2 D21 / Story 3-6 ADR-048).
/// Uses <see cref="Dictionary{TKey, TValue}"/> (not <see cref="System.Collections.Immutable.ImmutableDictionary{TKey, TValue}"/>)
/// for <see cref="System.Text.Json.JsonSerializer"/> compatibility without custom converters.
/// The schema is pinned via <c>NavigationPersistenceBlobSchemaLockedTests.BlobSchemaMatches</c>.
/// </summary>
/// <remarks>
/// Wire format (JSON): <c>{"sidebarCollapsed":bool, "collapsedGroups":{bc:bool, ...}, "lastActiveRoute":string?}</c>.
/// <c>CurrentViewport</c>, <c>CurrentBoundedContext</c>, <c>StorageReady</c>, and <c>HydrationState</c>
/// are NEVER serialised (ADR-037 / ADR-049 transient-fields invariant).
/// </remarks>
/// <param name="SidebarCollapsed">The persisted sidebar collapsed flag.</param>
/// <param name="CollapsedGroups">The per-bounded-context collapsed flags (sparse-by-default per D11).</param>
/// <param name="LastActiveRoute">
/// The last visited domain route (Story 3-6 ADR-048). Null on pre-3-6 stored blobs — feature
/// default (no auto-navigation) applies. Post-3-6 blobs carry this field. Consumers compare
/// <c>LastActiveRoute is not null &amp;&amp; LastActiveRoute.Length &gt; 0</c>; empty string is
/// invalid and never persisted.
/// </param>
public sealed record NavigationPersistenceBlob(
    bool SidebarCollapsed,
    Dictionary<string, bool> CollapsedGroups,
    string? LastActiveRoute = null);
