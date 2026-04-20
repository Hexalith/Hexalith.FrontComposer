using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Code-behind for <see cref="FcCollapsedNavRail"/> (Story 3-2 D13 / AC4).
/// Click dispatches <see cref="SidebarExpandedAction"/> to restore the full sidebar.
/// </summary>
public partial class FcCollapsedNavRail : ComponentBase {
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    [Inject] private IState<FrontComposerNavigationState> NavState { get; set; } = default!;

    [Inject] private IUlidFactory UlidFactory { get; set; } = default!;

    [CascadingParameter] private LayoutHamburgerCoordinator? HamburgerCoordinator { get; set; }

    private async Task OnRailClicked() {
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
