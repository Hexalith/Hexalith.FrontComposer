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
    /// <para>
    /// Policy names appear in startup warnings and (under <see cref="StrictPolicyCatalogValidation"/>)
    /// in <see cref="System.InvalidOperationException"/> messages emitted by the validator. They must
    /// not embed personally-identifiable information. Use stable PascalCase identifiers
    /// (e.g., <c>"OrderApprover"</c>, <c>"InvoiceReader"</c>); never embed customer / tenant / user
    /// IDs in policy names.
    /// </para>
    /// <para>
    /// The validator snapshots this collection once during <c>StartAsync</c>. Mutations applied
    /// after host startup (e.g., via <c>IOptionsMonitor</c> reload or direct mutation of the bound
    /// instance) do not re-trigger validation. To change the catalog at runtime, restart the host.
    /// </para>
    /// </remarks>
    public IList<string> KnownPolicies { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether missing policy catalog entries should fail host startup.
    /// When false (default), missing entries log a warning. When true, the catalog validator throws
    /// during <c>StartAsync</c> and aborts host startup.
    /// </summary>
    public bool StrictPolicyCatalogValidation { get; set; }
}
