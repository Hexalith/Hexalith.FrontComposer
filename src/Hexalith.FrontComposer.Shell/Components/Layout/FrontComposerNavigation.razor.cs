using System.Collections.Immutable;
using System.Text;

using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.Components.Icons;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Framework-owned navigation rail composing one bounded-context tile per registered
/// <see cref="DomainManifest"/> or orphan navigation-entry context.
/// </summary>
/// <remarks>
/// <para>
/// Subscribes to <see cref="FrontComposerNavigationState"/> via
/// <see cref="FluxorComponent"/> so viewport-tier and collapsed-groups changes trigger
/// a minimal re-render.
/// </para>
/// <para>
/// Routes are derived via the convention in <see cref="BuildRoute"/>
/// (<c>/{boundedContextLowercase}/{projectionTypeKebabCase}</c>). Adopters needing a different
/// route replace the <c>FrontComposerShell.Navigation</c> slot (ADR-035 override path).
/// </para>
/// </remarks>
public partial class FrontComposerNavigation : FluxorComponent, IAsyncDisposable {
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    [Inject] private NavigationManager Navigation { get; set; } = default!;

    [Inject] private IState<FrontComposerNavigationState> NavState { get; set; } = default!;

    [Inject] private IState<FrontComposerCapabilityDiscoveryState> DiscoveryState { get; set; } = default!;

    [Inject] private IStringLocalizerFactory LocalizerFactory { get; set; } = default!;

    /// <summary>
    /// The single nav href currently treated as active — the registered route that is the longest
    /// segment-prefix of the current URL. Recomputed each render (and on every navigation, via the
    /// <see cref="NavigationManager.LocationChanged"/> subscription) so exactly one item highlights.
    /// </summary>
    private string? _activeNavHref;

    private string RailWidth => ShouldRenderIconOnlyRail() ? "48px" : "72px";

    private string RailWidthValue => ShouldRenderIconOnlyRail() ? "48" : "72";

    private string RailClass => ShouldRenderIconOnlyRail()
        ? "fc-navigation-rail fc-navigation-rail--icon-only"
        : "fc-navigation-rail fc-navigation-rail--labeled";

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();

        // Each FluentNavItem self-evaluates its NavLink match, so when the route changes we must
        // re-render to flip which item carries the Prefix (active) match — otherwise a client-side
        // navigation would leave the previous Prefix item lit alongside the new one (the original bug).
        Navigation.LocationChanged += HandleLocationChanged;
    }

    /// <summary>
    /// Re-renders on navigation so <see cref="RecomputeActiveNavHref"/> re-runs against the new URL.
    /// Mirrors the disposed-circuit guard in <c>FrontComposerShell.HandleLocationChanged</c>.
    /// </summary>
    /// <param name="sender">The navigation manager raising the event.</param>
    /// <param name="e">The location-changed payload (unused; the new URL is read from the manager).</param>
    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) {
        try {
            _ = InvokeAsync(StateHasChanged);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException) {
            // The circuit/store was torn down between the event firing and dispatch. Detach so a
            // future event does not re-enter a disposed scope.
            Navigation.LocationChanged -= HandleLocationChanged;
        }
    }

    /// <summary>
    /// Resolves the active nav href to the registered route that is the longest segment-prefix of the
    /// current URL, considering the same items the sidebar renders (visible projection routes + enabled
    /// nav-entry hrefs). The matching item then renders with <see cref="NavLinkMatch.Prefix"/> and every
    /// other item with <see cref="NavLinkMatch.All"/>, so at most one item is ever active.
    /// </summary>
    /// <param name="discovery">The capability-discovery state driving projection visibility.</param>
    /// <param name="navEntries">The registered navigation entries.</param>
    private void RecomputeActiveNavHref(
        FrontComposerCapabilityDiscoveryState discovery,
        IReadOnlyList<FrontComposerNavEntry> navEntries) {
        ArgumentNullException.ThrowIfNull(navEntries);
        List<string> hrefs = [];
        foreach (DomainManifest manifest in Registry.GetManifests()) {
            foreach (string projectionFqn in VisibleProjections(manifest, discovery.Counts)) {
                hrefs.Add(NormalizeHref(BuildRoute(manifest.BoundedContext, projectionFqn)));
            }
        }

        foreach (FrontComposerNavEntry entry in navEntries) {
            if (entry.Enabled && !string.IsNullOrWhiteSpace(entry.Href)) {
                hrefs.Add(NormalizeHref(entry.Href));
            }
        }

        _activeNavHref = LongestNavPrefix(NormalizeHref(Navigation.ToBaseRelativePath(Navigation.Uri)), hrefs);
    }

    /// <summary>
    /// Canonicalizes a route for comparison: drops the query/fragment, ensures a single leading slash,
    /// trims a trailing slash, and lowercases (Blazor routing is case-insensitive).
    /// </summary>
    /// <param name="href">The raw href or current relative path.</param>
    /// <returns>The canonical comparison form (always begins with <c>/</c>).</returns>
    internal static string NormalizeHref(string href) {
        ArgumentNullException.ThrowIfNull(href);
        string value = href;
        int cut = value.IndexOfAny(['?', '#']);
        if (cut >= 0) {
            value = value[..cut];
        }

        if (value.Length == 0 || value[0] != '/') {
            value = "/" + value;
        }

        if (value.Length > 1 && value[^1] == '/') {
            value = value[..^1];
        }

        return value.ToLowerInvariant();
    }

    /// <summary>
    /// Returns the candidate href that is the longest segment-prefix of <paramref name="currentPath"/>,
    /// or <see langword="null"/> when none matches. Inputs are expected to be <see cref="NormalizeHref"/>'d.
    /// </summary>
    /// <param name="currentPath">The normalized current path.</param>
    /// <param name="candidateHrefs">The normalized candidate nav hrefs.</param>
    /// <returns>The most specific matching href, or <see langword="null"/>.</returns>
    internal static string? LongestNavPrefix(string currentPath, IReadOnlyList<string> candidateHrefs) {
        ArgumentNullException.ThrowIfNull(currentPath);
        ArgumentNullException.ThrowIfNull(candidateHrefs);
        string? best = null;
        foreach (string href in candidateHrefs) {
            bool isPrefix = href.Length == 1 // "/" (root) is a prefix of everything
                || string.Equals(currentPath, href, StringComparison.Ordinal)
                || currentPath.StartsWith(href + "/", StringComparison.Ordinal);
            if (isPrefix && (best is null || href.Length > best.Length)) {
                best = href;
            }
        }

        return best;
    }

    /// <inheritdoc />
    public new async ValueTask DisposeAsync() {
        Navigation.LocationChanged -= HandleLocationChanged;
        await base.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Resolves the displayed category title for a manifest to the request culture, falling back to
    /// the culture-invariant <see cref="DomainManifest.Name"/> when the domain registered no
    /// localization pointer.
    /// </summary>
    /// <param name="manifest">The manifest whose category title is rendered.</param>
    /// <returns>The localized (or fallback) category title.</returns>
    internal string LocalizeManifestName(DomainManifest manifest) {
        ArgumentNullException.ThrowIfNull(manifest);
        return FcNavLocalization.Resolve(LocalizerFactory, manifest.Resource, manifest.NameKey, manifest.Name);
    }

    /// <summary>
    /// Resolves the displayed title for a navigation entry to the request culture, falling back to the
    /// culture-invariant <see cref="FrontComposerNavEntry.Title"/> when the entry is not localized.
    /// </summary>
    /// <param name="entry">The navigation entry whose title is rendered.</param>
    /// <returns>The localized (or fallback) entry title.</returns>
    internal string LocalizeEntryTitle(FrontComposerNavEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);
        return FcNavLocalization.Resolve(LocalizerFactory, entry.Resource, entry.TitleKey, entry.Title);
    }

    /// <summary>
    /// Resolves the disabled-entry reason to the request culture. The reason shares the entry's
    /// resource type; when the reason is treated as a literal (no resource) it renders verbatim.
    /// </summary>
    /// <param name="entry">The disabled navigation entry whose reason is rendered.</param>
    /// <returns>The localized (or verbatim) disabled reason, or <see langword="null"/> when none.</returns>
    internal string? LocalizeEntryReason(FrontComposerNavEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);
        return string.IsNullOrWhiteSpace(entry.DisabledReason)
            ? entry.DisabledReason
            : FcNavLocalization.Resolve(LocalizerFactory, entry.Resource, entry.DisabledReason, entry.DisabledReason);
    }

    /// <summary>
    /// Builds the nav-item <c>Href</c> from the D2 convention:
    /// <c>/{boundedContextLowercase}/{projectionTypeNameKebabCase}</c>.
    /// Projection type name = segment after the last <c>.</c> (or whole string if no dot).
    /// </summary>
    /// <param name="boundedContext">The bounded-context string (rendered lowercase in the route).</param>
    /// <param name="projectionFqn">The fully-qualified projection type name.</param>
    /// <returns>The conventional route.</returns>
    public static string BuildRoute(string boundedContext, string projectionFqn) {
        ArgumentException.ThrowIfNullOrEmpty(boundedContext);
        ArgumentException.ThrowIfNullOrEmpty(projectionFqn);
        string typeName = LastSegment(projectionFqn);
        return $"/{boundedContext.ToLowerInvariant()}/{ToKebab(typeName)}";
    }

    /// <summary>
    /// Gets the projection type-name segment for display — the substring after the last <c>.</c>
    /// in the fully-qualified name (or the whole string if no dot is present). Namespace prefixes
    /// are stripped; the exact casing of the final type name is preserved. Story 4-1 will replace
    /// this with projection-role-hint-driven friendly names.
    /// </summary>
    /// <param name="projectionFqn">The fully-qualified projection type name.</param>
    /// <returns>The projection type-name segment after the last dot.</returns>
    public static string ProjectionLabel(string projectionFqn) {
        ArgumentException.ThrowIfNullOrEmpty(projectionFqn);
        return LastSegment(projectionFqn);
    }

    /// <summary>
    /// Returns the registered navigation entries that belong to the given bounded context, preserving
    /// the registry's already-applied <see cref="FrontComposerNavEntry.Order"/> / title sort.
    /// </summary>
    /// <param name="entries">All registered navigation entries.</param>
    /// <param name="boundedContext">The bounded context to filter by.</param>
    /// <returns>The entries for the bounded context.</returns>
    internal static List<FrontComposerNavEntry> EntriesForContext(
        IReadOnlyList<FrontComposerNavEntry> entries,
        string boundedContext) {
        ArgumentNullException.ThrowIfNull(entries);
        List<FrontComposerNavEntry> list = [];
        foreach (FrontComposerNavEntry entry in entries) {
            if (string.Equals(entry.BoundedContext, boundedContext, StringComparison.Ordinal)) {
                list.Add(entry);
            }
        }

        return list;
    }

    /// <summary>
    /// Resolves a navigation entry icon key to an inline-SVG glyph, defaulting to the bounded-context
    /// glyph when the key is null or unknown. The icon contract stays a plain string so domains never
    /// reference a Fluent UI type.
    /// </summary>
    /// <param name="iconKey">The optional icon key (e.g. <c>Regular.Size20.Search</c>).</param>
    /// <returns>The resolved icon.</returns>
    internal static Icon ResolveNavEntryIcon(string? iconKey, bool filled = false)
        => FcFluentIcons.TryCreate(iconKey, filled ? IconVariant.Filled : IconVariant.Regular, out Icon? icon) && icon is not null
            ? icon
            : FcFluentIcons.Apps20(filled ? IconVariant.Filled : IconVariant.Regular);

    /// <summary>
    /// Builds the stable <c>data-testid</c> for a navigation entry item.
    /// </summary>
    /// <param name="entry">The navigation entry.</param>
    /// <returns>The test id.</returns>
    internal static string NavEntryTestId(FrontComposerNavEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);
        return $"fc-nav-entry-{entry.BoundedContext}-{Slug(entry.Title)}";
    }

    /// <summary>
    /// Returns the bounded contexts that have registered navigation entries but no matching
    /// <see cref="DomainManifest"/>, so those entries render as their own category instead of being
    /// silently dropped. First-seen order is preserved.
    /// </summary>
    /// <param name="entries">All registered navigation entries.</param>
    /// <returns>The orphan bounded-context names.</returns>
    private IReadOnlyList<string> OrphanEntryContexts(IReadOnlyList<FrontComposerNavEntry> entries) {
        ArgumentNullException.ThrowIfNull(entries);
        HashSet<string> manifestContexts = new(StringComparer.Ordinal);
        foreach (DomainManifest manifest in Registry.GetManifests()) {
            _ = manifestContexts.Add(manifest.BoundedContext);
        }

        List<string> orphans = [];
        HashSet<string> seen = new(StringComparer.Ordinal);
        foreach (FrontComposerNavEntry entry in entries) {
            if (!manifestContexts.Contains(entry.BoundedContext) && seen.Add(entry.BoundedContext)) {
                orphans.Add(entry.BoundedContext);
            }
        }

        return orphans;
    }

    private static string Slug(string value) {
        StringBuilder sb = new(value.Length);
        foreach (char c in value) {
            _ = sb.Append(char.IsWhiteSpace(c) ? '-' : char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    internal static List<string> RenderableProjections(DomainManifest manifest) {
        ArgumentNullException.ThrowIfNull(manifest);
        List<string> list = new(manifest.Projections.Count);
        foreach (string projection in manifest.Projections) {
            if (!string.IsNullOrWhiteSpace(projection)) {
                list.Add(projection);
            }
        }

        return list;
    }

    /// <summary>
    /// Returns the projections that should render as nav entries. Story 3-5 AC8 + Epic AC § 244 —
    /// projections with zero data stay invisible in the sidebar entirely. Until the badge counts
    /// are seeded (catalog enumerated + reader fan-out completed) the rendering falls back to the
    /// legacy <see cref="RenderableProjections(DomainManifest)"/> behavior so a fresh circuit does
    /// not flash an empty sidebar. Resolved projections that are missing from <paramref name="counts"/>
    /// also stay visible — that path represents a faulted or not-yet-snapshotted count, not proof
    /// that the projection has zero actionable items.
    /// </summary>
    /// <param name="manifest">The source <see cref="DomainManifest"/>.</param>
    /// <param name="counts">The current per-projection badge counts.</param>
    /// <returns>The projections to render in the sidebar.</returns>
    internal static List<string> VisibleProjections(
        DomainManifest manifest,
        ImmutableDictionary<Type, int> counts) {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(counts);

        // No counts seeded yet → preserve pre-Story-3-5 behavior (show all manifest projections).
        if (counts.IsEmpty) {
            return RenderableProjections(manifest);
        }

        List<string> list = new(manifest.Projections.Count);
        foreach (string projection in manifest.Projections) {
            if (string.IsNullOrWhiteSpace(projection)) {
                continue;
            }

            // AC8: only surface projections with count > 0 once counts are available. Projections
            // not present in the catalog (no badge contract) keep showing — they're not actionable
            // by the badge system.
            Type? resolved = ProjectionTypeResolver.Resolve(projection);
            if (resolved is null) {
                list.Add(projection);
                continue;
            }

            if (!counts.TryGetValue(resolved, out int count) || count > 0) {
                list.Add(projection);
            }
        }

        return list;
    }

    internal static int LookupCount(string projectionFqn, ImmutableDictionary<Type, int> counts) {
        ArgumentNullException.ThrowIfNull(counts);
        Type? resolved = ProjectionTypeResolver.Resolve(projectionFqn);
        return resolved is not null && counts.TryGetValue(resolved, out int count) ? count : 0;
    }

    internal static int AggregateBoundedContextCount(
        DomainManifest manifest,
        ImmutableDictionary<Type, int> counts) {
        ArgumentNullException.ThrowIfNull(manifest);
        int total = 0;
        foreach (string projection in manifest.Projections) {
            if (string.IsNullOrWhiteSpace(projection)) {
                continue;
            }

            total += LookupCount(projection, counts);
        }

        return total;
    }

    /// <summary>
    /// Test hook exposing the private nav-item click handler so tests can drive the dispatch
    /// without going through the FluentNavItem event pipeline.
    /// </summary>
    /// <param name="boundedContext">The bounded context owning the projection.</param>
    /// <param name="capabilityId">The seen-set capability id.</param>
    internal void HandleNavItemClickedForTest(string boundedContext, string capabilityId)
        => HandleNavItemClicked(boundedContext, capabilityId);

    private bool ShouldRenderIconOnlyRail() {
        FrontComposerNavigationState snapshot = NavState.Value;
        return snapshot.CurrentViewport == ViewportTier.CompactDesktop
            || (snapshot.CurrentViewport == ViewportTier.Desktop && snapshot.SidebarCollapsed);
    }

    private static string RailAnchorId(string boundedContext)
        => $"fc-rail-{boundedContext}";

    private static string ContextTileClass(bool active)
        => active
            ? "fc-navigation-rail__tile fc-navigation-rail__tile--active"
            : "fc-navigation-rail__tile";

    private bool IsHrefActive(string? href)
        => href is not null
            && string.Equals(NormalizeHref(href), _activeNavHref, StringComparison.Ordinal);

    private bool IsContextActive(
        string boundedContext,
        IReadOnlyList<string> projections,
        IReadOnlyList<FrontComposerNavEntry> entries) {
        ArgumentNullException.ThrowIfNull(projections);
        ArgumentNullException.ThrowIfNull(entries);

        if (_activeNavHref is null) {
            return false;
        }

        foreach (string projection in projections) {
            if (IsHrefActive(BuildRoute(boundedContext, projection))) {
                return true;
            }
        }

        foreach (FrontComposerNavEntry entry in entries) {
            if (entry.Enabled && IsHrefActive(entry.Href)) {
                return true;
            }
        }

        return false;
    }

    private void HandleNavItemClicked(string boundedContext, string capabilityId) {
        Dispatcher.Dispatch(new CapabilityVisitedAction(CapabilityIds.ForBoundedContext(boundedContext)));
        Dispatcher.Dispatch(new CapabilityVisitedAction(capabilityId));
    }

    private void HandleContextTileActivated(string boundedContext)
        => Dispatcher.Dispatch(new CapabilityVisitedAction(CapabilityIds.ForBoundedContext(boundedContext)));

    private void HandleProjectionMenuItemClicked(string boundedContext, string projectionFqn, string route) {
        HandleNavItemClicked(boundedContext, CapabilityIds.ForProjection(boundedContext, projectionFqn));
        Navigation.NavigateTo(route);
    }

    private void HandleNavEntryMenuItemClicked(FrontComposerNavEntry entry) {
        ArgumentNullException.ThrowIfNull(entry);
        if (!entry.Enabled || string.IsNullOrWhiteSpace(entry.Href)) {
            return;
        }

        Dispatcher.Dispatch(new CapabilityVisitedAction(CapabilityIds.ForBoundedContext(entry.BoundedContext)));
        Navigation.NavigateTo(entry.Href);
    }

    private static string LastSegment(string fqn) {
        int lastDot = fqn.LastIndexOf('.');
        return lastDot < 0 ? fqn : fqn[(lastDot + 1)..];
    }

    private static string ToKebab(string pascal) {
        if (string.IsNullOrEmpty(pascal)) {
            return pascal;
        }

        StringBuilder sb = new(pascal.Length + 4);
        for (int i = 0; i < pascal.Length; i++) {
            char c = pascal[i];
            if (i > 0 && char.IsUpper(c)) {
                _ = sb.Append('-');
            }

            _ = sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
