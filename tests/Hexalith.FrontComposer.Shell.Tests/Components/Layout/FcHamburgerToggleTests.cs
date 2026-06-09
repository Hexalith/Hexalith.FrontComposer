// Story 3-2 Task 10.3 — FluentLayoutHamburger wrapper.
// 2026-06 (supersedes the D9 "no Desktop hamburger" decision): the hamburger is now visible at every
// viewport tier so it can toggle the navigation, matching the Fluent reference shell. At Desktop it is
// a FluentButton whose click dispatches SidebarToggledAction (full sidebar <-> collapsed rail); at every
// other tier it stays the FluentLayoutHamburger that opens the responsive drawer.

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-2 Task 10.3 — hamburger toggle. The 2026-06 UX direction makes the toggle visible at all
/// tiers; at Desktop a click toggles <c>SidebarCollapsed</c> via <see cref="SidebarToggledAction"/>.
/// </summary>
public sealed class FcHamburgerToggleTests : LayoutComponentTestBase
{
    public FcHamburgerToggleTests()
    {
        EnsureStoreInitialized();
    }

    [Fact]
    public void VisibleAtDesktopAsSidebarToggleButton()
    {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));

        IRenderedComponent<FcHamburgerToggle> cut = Render<FcHamburgerToggle>();

        cut.WaitForAssertion(() =>
        {
            // 2026-06 (supersedes D9): the hamburger is visible at Desktop too, as the in-layout
            // sidebar-toggle button branch.
            cut.Instance.IsDesktop.ShouldBeTrue("Desktop renders the in-layout sidebar-toggle button branch.");

            // F5 — lock the e2e selector contract at the unit level (present in both branches).
            cut.Markup.ShouldContain("data-testid=\"fc-hamburger-toggle\"");
        });
    }

    [Fact]
    public async Task ClickAtDesktopTogglesSidebarCollapsed()
    {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));

        IState<FrontComposerNavigationState> navState =
            Services.GetRequiredService<IState<FrontComposerNavigationState>>();
        bool before = navState.Value.SidebarCollapsed;

        IRenderedComponent<FcHamburgerToggle> cut = Render<FcHamburgerToggle>();

        await cut.InvokeAsync(() => cut.Find("[data-testid=\"fc-hamburger-toggle\"]").Click());

        cut.WaitForAssertion(() =>
            navState.Value.SidebarCollapsed.ShouldBe(
                !before,
                "Clicking the Desktop hamburger dispatches SidebarToggledAction, flipping SidebarCollapsed."));
    }

    [Theory]
    [InlineData(ViewportTier.CompactDesktop)]
    [InlineData(ViewportTier.Tablet)]
    [InlineData(ViewportTier.Phone)]
    public void VisibleAcrossNonDesktopTiers(ViewportTier tier)
    {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(tier));

        IRenderedComponent<FcHamburgerToggle> cut = Render<FcHamburgerToggle>();

        cut.WaitForAssertion(() =>
        {
            cut.Instance.IsDesktop.ShouldBeFalse($"At {tier} the responsive drawer hamburger renders, not the Desktop button.");

            // F5 — lock the e2e selector contract at the unit level (hamburger visible at every tier).
            cut.Markup.ShouldContain("data-testid=\"fc-hamburger-toggle\"");
        });
    }
}
