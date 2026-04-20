// ATDD RED PHASE — Story 3-2 Task 10.3 (D7, D8, D9; AC4, AC5)
// Fails at compile until Task 8 (FcHamburgerToggle component) lands.

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-2 Task 10.3 — FluentLayoutHamburger wrapper.
/// D8: Visible = CurrentViewport != Desktop.
/// D9 amended 2026-04-19: manual Desktop collapse dropped; Desktop has no visible toggle.
/// </summary>
public sealed class FcHamburgerToggleTests : LayoutComponentTestBase
{
    public FcHamburgerToggleTests()
    {
        EnsureStoreInitialized();
    }

    [Fact]
    public void VisibleFalseAtDesktopWhenNotCollapsed()
    {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));

        IRenderedComponent<FcHamburgerToggle> cut = Render<FcHamburgerToggle>();

        cut.WaitForAssertion(() =>
            cut.Instance.IsVisibleForTest.ShouldBeFalse(
                "Desktop + SidebarCollapsed=false → hamburger is HIDDEN (D8)"));
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
            cut.Instance.IsVisibleForTest.ShouldBeTrue(
                $"At {tier}, hamburger MUST be visible (D7 / D8)");

            // F5 — lock the e2e selector contract at the unit level.
            cut.Markup.ShouldContain("data-testid=\"fc-hamburger-toggle\"");
        });
    }

    [Fact]
    public void HiddenAtDesktopEvenWhenSidebarCollapsed()
    {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));
        dispatcher.Dispatch(new SidebarToggledAction("c1")); // collapse via hydrated state

        IRenderedComponent<FcHamburgerToggle> cut = Render<FcHamburgerToggle>();

        // D9 amended 2026-04-19: Desktop follows persisted SidebarCollapsed without a visible
        // toggle (AC3 literal: FluentLayoutHamburger.Visible = false at Desktop).
        cut.WaitForAssertion(() =>
            cut.Instance.IsVisibleForTest.ShouldBeFalse(
                "D9 amended 2026-04-19: hamburger toggle stays hidden at Desktop regardless of SidebarCollapsed."));
    }

    // REMOVED 2026-04-19 (code-review round 2): `ManualToggleAtDesktopDispatchesSidebarToggled`
    // asserted the Desktop-side OnHamburgerOpened dispatch path. D9 was narrowed via the
    // D9-2026-04-19 addendum to "manual toggle applies at CompactDesktop / Tablet / Phone only;
    // Desktop has no visible manual-collapse affordance". The Desktop dispatch branch is removed
    // from FcHamburgerToggle; this test is obsolete.

    // REMOVED 2026-04-19 (test-review F2): `ViewportDrivenVisibilityDoesNotDispatchToggle` was
    // asserting a guard behavior ("at non-Desktop tiers, OnHamburgerOpened must not dispatch
    // SidebarToggledAction") that is not stated in D9, D7, or AC4/AC5. Superseded by the D9
    // amendment above which removes Desktop dispatch entirely.
}
