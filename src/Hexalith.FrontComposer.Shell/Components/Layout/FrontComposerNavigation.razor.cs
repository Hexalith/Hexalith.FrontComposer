using System.Collections.Immutable;
using System.Text;

using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.CommandPalette;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Framework-owned sidebar composing <c>FluentNavCategory</c> per registered <c>DomainManifest</c>
/// (Story 3-2 D1, D2, D9, D11, D16; AC1, AC3, AC6).
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
public partial class FrontComposerNavigation : FluxorComponent {
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    [Inject] private IState<FrontComposerNavigationState> NavState { get; set; } = default!;

    [Inject] private IState<FrontComposerCapabilityDiscoveryState> DiscoveryState { get; set; } = default!;

    [Inject] private IUlidFactory UlidFactory { get; set; } = default!;

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

    /// <summary>
    /// Test hook exposing the private handler so the nav-group expand/collapse tests
    /// can invoke the category callback without driving the FluentNavCategory event pipeline.
    /// </summary>
    /// <param name="boundedContext">The bounded context whose category changed.</param>
    /// <param name="expanded">The new expanded state reported by FluentNavCategory.</param>
    internal void OnGroupExpandedChangedForTest(string boundedContext, bool expanded)
        => OnGroupExpandedChanged(boundedContext, CapabilityIds.ForBoundedContext(boundedContext), expanded);

    private bool ShouldRenderCollapsedRail() {
        FrontComposerNavigationState snapshot = NavState.Value;
        return snapshot.CurrentViewport == ViewportTier.CompactDesktop
            || (snapshot.CurrentViewport == ViewportTier.Desktop && snapshot.SidebarCollapsed);
    }

    private bool IsGroupCollapsed(string boundedContext)
        => NavState.Value.CollapsedGroups.TryGetValue(boundedContext, out bool collapsed) && collapsed;

    private void OnGroupExpandedChanged(string boundedContext, string capabilityId, bool expanded) {
        // D13 (review 2026-04-22): only an explicit expand signals engagement with the category;
        // collapsing is decluttering and MUST NOT mark the capability as seen.
        if (expanded) {
            Dispatcher.Dispatch(new CapabilityVisitedAction(capabilityId));
        }

        Dispatcher.Dispatch(new NavGroupToggledAction(UlidFactory.NewUlid(), boundedContext, Collapsed: !expanded));
    }

    private void HandleNavItemClicked(string boundedContext, string capabilityId) {
        Dispatcher.Dispatch(new CapabilityVisitedAction(CapabilityIds.ForBoundedContext(boundedContext)));
        Dispatcher.Dispatch(new CapabilityVisitedAction(capabilityId));
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
                sb.Append('-');
            }

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }
}
