using System.Collections.Immutable;

using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Badges;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Code-behind for <see cref="FcCollapsedNavRail"/> (Story 3-2 D13 / AC4).
/// Click dispatches <see cref="SidebarExpandedAction"/> to restore the full sidebar.
/// </summary>
public partial class FcCollapsedNavRail : FluxorComponent {
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    [Inject] private IState<FrontComposerCapabilityDiscoveryState> DiscoveryState { get; set; } = default!;

    [Inject] private IState<FrontComposerNavigationState> NavState { get; set; } = default!;

    [Inject] private IUlidFactory UlidFactory { get; set; } = default!;

    [CascadingParameter] private LayoutHamburgerCoordinator? HamburgerCoordinator { get; set; }

    [Inject] private IStringLocalizerFactory LocalizerFactory { get; set; } = default!;

    private static int AggregateCount(DomainManifest manifest, ImmutableDictionary<Type, int> counts)
        => FrontComposerNavigation.AggregateBoundedContextCount(manifest, counts);

    /// <summary>
    /// Resolves the bounded-context display name to the request culture for the rail's accessible
    /// label and tooltip, falling back to the culture-invariant <see cref="DomainManifest.Name"/>.
    /// </summary>
    /// <param name="manifest">The manifest whose rail label is rendered.</param>
    /// <returns>The localized (or fallback) bounded-context name.</returns>
    private string LocalizeName(DomainManifest manifest)
        => FcNavLocalization.Resolve(LocalizerFactory, manifest.Resource, manifest.NameKey, manifest.Name);

    private async Task OnRailClicked(string boundedContext) {
        Dispatcher.Dispatch(new CapabilityVisitedAction(CapabilityIds.ForBoundedContext(boundedContext)));

        // At CompactDesktop the expansion is visible only as the hamburger drawer. If no
        // coordinator is cascaded (rail used outside FrontComposerShell), skip the dispatch
        // entirely so the UI does not end up with SidebarCollapsed=false but no drawer —
        // which would render a blank 48 px column until the next viewport change.
        bool isCompactDesktop = NavState.Value.CurrentViewport == ViewportTier.CompactDesktop;
        if (isCompactDesktop && HamburgerCoordinator is null) {
            return;
        }

        Dispatcher.Dispatch(new SidebarExpandedAction(UlidFactory.NewUlid()));

        if (isCompactDesktop && HamburgerCoordinator is not null) {
            await HamburgerCoordinator.ShowAsync().ConfigureAwait(false);
        }
    }
}
