namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Provisional runtime composition bridge. Source generator emits RegisterDomain() calls
/// against this interface. Story 1.3 may extend composition through companion abstractions
/// while keeping this interface stable for existing implementers.
/// </summary>
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
