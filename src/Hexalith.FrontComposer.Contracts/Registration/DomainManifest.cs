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
/// <param name="Icon">
/// Optional icon key (e.g. <c>Regular.Size20.People</c>) the shell resolves to the bounded-context
/// glyph shown on the collapsed navigation rail. <see langword="null"/> selects the shell's default
/// rail glyph. The key is a plain string so domains stay free of any Fluent UI type.
/// </param>
/// <param name="NameKey">
/// Optional resource key for the navigation category title. When set together with
/// <paramref name="Resource"/>, the shell resolves the visible category label per the request culture;
/// <see cref="Name"/> stays the culture-invariant fallback used elsewhere (palette, diagnostics, merge).
/// </param>
/// <param name="Resource">
/// Optional resource marker type (e.g. a domain's <c>*Resources</c> class) whose assembly the shell
/// feeds to <c>IStringLocalizerFactory.Create(Type)</c> to resolve <paramref name="NameKey"/>. When
/// <see langword="null"/> the category title is not localized and <see cref="Name"/> renders verbatim.
/// </param>
public record DomainManifest(
    string Name,
    string BoundedContext,
    IReadOnlyList<string> Projections,
    IReadOnlyList<string> Commands,
    IReadOnlyDictionary<string, string>? CommandPolicies = null,
    string? Icon = null,
    string? NameKey = null,
    Type? Resource = null) {
    private static readonly IReadOnlyDictionary<string, string> EmptyCommandPolicies =
        new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets command authorization policies keyed by command fully qualified type name.
    /// </summary>
    /// <remarks>
    /// The init accessor coerces null to an empty dictionary so <c>with { CommandPolicies = null }</c>
    /// expressions cannot reintroduce a null map and NRE downstream consumers
    /// (registry merge, catalog validator, palette filter, empty-state CTA resolver).
    /// </remarks>
    public IReadOnlyDictionary<string, string> CommandPolicies {
        get;
        init => field = value ?? EmptyCommandPolicies;
    } = CommandPolicies ?? EmptyCommandPolicies;
}
