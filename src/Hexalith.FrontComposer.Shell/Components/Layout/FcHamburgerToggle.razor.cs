using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Always-visible navigation hamburger. Supersedes the earlier D9 "Desktop has no visible toggle"
/// decision (2026-06 UX request to match the Fluent reference shell). At <see cref="ViewportTier.Desktop"/>
/// it renders a subtle <c>FluentButton</c> whose click dispatches <see cref="SidebarToggledAction"/>,
/// flipping <c>SidebarCollapsed</c> — already wired to swap the 72px labeled rail with the 48px
/// icon-only rail and to persist via <c>NavigationEffects.HandleSidebarToggled</c>. At every
/// other tier it renders <c>FluentLayoutHamburger</c>, which opens the responsive navigation drawer.
/// </summary>
public partial class FcHamburgerToggle : FluxorComponent, IAsyncDisposable {
    private FluentLayoutHamburger? _hamburger;

    [Inject] private IState<FrontComposerNavigationState> NavState { get; set; } = default!;

    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    [Inject] private IUlidFactory UlidFactory { get; set; } = default!;

    [CascadingParameter] private LayoutHamburgerCoordinator? HamburgerCoordinator { get; set; }

    /// <summary>
    /// Whether the Desktop sidebar-toggle button (vs. the responsive drawer hamburger) renders.
    /// Internal hook for the bUnit assertions in <c>FcHamburgerToggleTests</c>; the toggle itself is
    /// rendered at every viewport tier now, so visibility is proven by the presence of the
    /// <c>data-testid="fc-hamburger-toggle"</c> marker in both branches.
    /// </summary>
    internal bool IsDesktop => NavState.Value.CurrentViewport == ViewportTier.Desktop;

    /// <inheritdoc />
    protected override void OnAfterRender(bool firstRender) {
        base.OnAfterRender(firstRender);
        if (firstRender && _hamburger is not null) {
            HamburgerCoordinator?.Register(_hamburger);
        }
    }

    /// <inheritdoc />
    public new async ValueTask DisposeAsync() {
        HamburgerCoordinator?.Register(null);
        await base.DisposeAsync().ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Desktop click handler — toggles <c>SidebarCollapsed</c> through the single-writer
    /// <see cref="SidebarToggledAction"/> (correlation id sourced from <see cref="IUlidFactory"/>, as
    /// the other shell controls do). The reducer flips the flag; the effect layer persists it.
    /// </summary>
    private void ToggleSidebar()
        => Dispatcher.Dispatch(new SidebarToggledAction(UlidFactory.NewUlid()));
}
