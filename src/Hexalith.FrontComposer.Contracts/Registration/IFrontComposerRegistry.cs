namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Provisional runtime composition bridge. Source generator emits RegisterDomain() calls
/// against this interface. Story 1.3 may extend composition through companion abstractions
/// while keeping this interface stable for existing implementers.
/// </summary>
/// <remarks>
/// <para>
/// <b>Companion-interface opt-in for route reachability (Story 3-4 D21 ratified via DN1 2026-04-21):</b>
/// This interface intentionally does <em>not</em> declare <c>HasFullPageRoute</c>. Consumers that
/// know which of their commands carry generated FullPage pages should implement the companion
/// <see cref="IFrontComposerFullPageRouteRegistry"/> so the palette can filter unreachable
/// commands. Adopters that do NOT implement the companion get the permissive
/// <see cref="FrontComposerRegistryExtensions.HasFullPageRoute(IFrontComposerRegistry, string)"/>
/// fallback (<c>true</c> for every command). Story 9-4 adds a build-time analyzer that enforces
/// companion implementation for any registry type that registers commands.
/// </para>
/// </remarks>
public interface IFrontComposerRegistry {
    /// <summary>
    /// Adds a navigation group entry for a bounded context.
    /// </summary>
    /// <param name="name">The navigation group display name.</param>
    /// <param name="boundedContext">The bounded context name.</param>
    void AddNavGroup(string name, string boundedContext);

    /// <summary>
    /// Returns all registered domain manifests.
    /// </summary>
    /// <returns>A read-only list of registered manifests.</returns>
    IReadOnlyList<DomainManifest> GetManifests();

    /// <summary>
    /// Registers a domain with its projections and commands.
    /// </summary>
    /// <param name="manifest">The domain manifest describing the domain.</param>
    void RegisterDomain(DomainManifest manifest);

}
