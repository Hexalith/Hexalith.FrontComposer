using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Hamburger toggle wrapping <c>FluentLayoutHamburger</c> (Story 3-2 D7, D8, D9 amended 2026-04-19; AC4, AC5).
/// Visibility derives from <see cref="FrontComposerNavigationState"/>: shown whenever the viewport is not
/// <see cref="ViewportTier.Desktop"/>. Desktop follows persisted collapse state without a visible toggle
/// (D9 addendum: manual Desktop collapse dropped — AC3 literal).
/// </summary>
public partial class FcHamburgerToggle : FluxorComponent, IAsyncDisposable {
    private FluentLayoutHamburger? _hamburger;

    [Inject] private IState<FrontComposerNavigationState> NavState { get; set; } = default!;

    [CascadingParameter] private LayoutHamburgerCoordinator? HamburgerCoordinator { get; set; }

    /// <summary>
    /// Computed visibility: shown when viewport is non-Desktop. Internal hook for bUnit assertions
    /// (<c>FcHamburgerToggleTests</c>).
    /// </summary>
    internal bool IsVisibleForTest => IsVisible;

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

    private bool IsVisible => NavState.Value.CurrentViewport != ViewportTier.Desktop;
}
