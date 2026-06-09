namespace Hexalith.FrontComposer.Contracts.Registration;

/// <summary>
/// A declarative left-navigation menu entry contributed by a domain module. Domains register
/// entries as plain data through <see cref="IFrontComposerNavEntryRegistry"/> (or the
/// <see cref="FrontComposerRegistryExtensions.AddNavEntry(IFrontComposerRegistry, FrontComposerNavEntry)"/>
/// helper); the shell owns all rendering. This keeps the domain stack decoupled from the technical
/// (rendering) stack — a module needs no Fluent UI or shell type to populate the global menu.
/// </summary>
/// <param name="BoundedContext">
/// The bounded context the entry belongs to. Entries are grouped under the navigation category of the
/// <see cref="DomainManifest"/> that shares this bounded context.
/// </param>
/// <param name="Title">The display label shown for the entry.</param>
/// <param name="Href">The route the entry links to (e.g. <c>/tenants</c>).</param>
/// <param name="Icon">
/// Optional icon key resolved by the shell to an inline-SVG glyph. <see langword="null"/> selects the
/// shell's default navigation glyph. The key is a string so domains stay free of any icon type.
/// </param>
/// <param name="Order">Sort order within the bounded-context category (ascending; ties broken by title).</param>
/// <param name="RequiredPolicy">
/// Optional authorization policy name. When set, the shell only renders the entry for users who
/// satisfy the named policy (evaluated via <c>AuthorizeView</c>); otherwise the entry is always shown.
/// </param>
/// <param name="Enabled">
/// When <see langword="false"/>, the entry renders as a non-navigable, disabled affordance (e.g. a
/// capability that is not yet reachable) rather than a link.
/// </param>
/// <param name="DisabledReason">Optional explanation shown beneath a disabled (<c>Enabled = false</c>) entry.</param>
public sealed record FrontComposerNavEntry(
    string BoundedContext,
    string Title,
    string Href,
    string? Icon = null,
    int Order = 0,
    string? RequiredPolicy = null,
    bool Enabled = true,
    string? DisabledReason = null);
