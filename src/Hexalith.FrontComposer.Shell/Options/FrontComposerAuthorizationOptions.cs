namespace Hexalith.FrontComposer.Shell.Options;

/// <summary>
/// Adopter-configurable settings for FrontComposer command authorization.
/// </summary>
public sealed class FrontComposerAuthorizationOptions {
    /// <summary>
    /// Gets or sets the catalog of host-registered authorization policy names. Used by
    /// <see cref="Hexalith.FrontComposer.Shell.Services.Authorization.FrontComposerAuthorizationPolicyCatalogValidator"/>
    /// at startup to surface generated command policies that the host has not registered.
    /// </summary>
    /// <remarks>
    /// Implemented as a settable <see cref="IList{T}"/> so the .NET <c>IConfiguration</c>
    /// binder can populate it from <c>appsettings.json</c>. The validator and registry
    /// compare ordinally; adopters must spell entries exactly as declared in
    /// <c>[RequiresPolicy]</c> attribute usage. Trailing/leading whitespace is trimmed by
    /// the validator before comparison.
    /// </remarks>
    public IList<string> KnownPolicies { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether missing policy catalog entries should fail host startup.
    /// When false (default), missing entries log a warning. When true, the catalog validator throws
    /// during <c>StartAsync</c> and aborts host startup.
    /// </summary>
    public bool StrictPolicyCatalogValidation { get; set; }
}
