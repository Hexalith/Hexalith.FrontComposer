namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// Describes a domain's projections and commands, emitted by the source generator
/// for runtime composition.
/// </summary>
/// <param name="Name">The domain display name.</param>
/// <param name="BoundedContext">The bounded context this domain belongs to.</param>
/// <param name="Projections">Fully qualified type names of projections in this domain.</param>
/// <param name="Commands">Fully qualified type names of commands in this domain.</param>
/// <param name="CommandPolicies">Optional command fully qualified type name to policy-name map.</param>
public record DomainManifest(
    string Name,
    string BoundedContext,
    IReadOnlyList<string> Projections,
    IReadOnlyList<string> Commands,
    IReadOnlyDictionary<string, string>? CommandPolicies = null) {
    private static readonly IReadOnlyDictionary<string, string> EmptyCommandPolicies =
        new Dictionary<string, string>(StringComparer.Ordinal);

    private readonly IReadOnlyDictionary<string, string> _commandPolicies =
        CommandPolicies ?? EmptyCommandPolicies;

    /// <summary>
    /// Gets command authorization policies keyed by command fully qualified type name.
    /// </summary>
    /// <remarks>
    /// The init accessor coerces null to an empty dictionary so <c>with { CommandPolicies = null }</c>
    /// expressions cannot reintroduce a null map and NRE downstream consumers
    /// (registry merge, catalog validator, palette filter, empty-state CTA resolver).
    /// </remarks>
    public IReadOnlyDictionary<string, string> CommandPolicies {
        get => _commandPolicies;
        init => _commandPolicies = value ?? EmptyCommandPolicies;
    }
}
