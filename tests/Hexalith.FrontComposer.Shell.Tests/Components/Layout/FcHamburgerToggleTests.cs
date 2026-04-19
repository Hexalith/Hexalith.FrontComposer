// ATDD RED PHASE — Story 3-2 Task 10.3 (D7, D8, D9; AC4, AC5)
// Fails at compile until Task 8 (FcHamburgerToggle component) lands.

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-2 Task 10.3 — FluentLayoutHamburger wrapper.
/// D8: Visible = CurrentViewport != Desktop || SidebarCollapsed (manual-collapse).
/// D9: Manual toggle at Desktop dispatches SidebarToggledAction.
/// </summary>
public sealed class FcHamburgerToggleTests : LayoutComponentTestBase
{
    private readonly IUlidFactory _ulidFactory;

    public FcHamburgerToggleTests()
    {
        _ulidFactory = Substitute.For<IUlidFactory>();
        _ulidFactory.NewUlid().Returns("01J0TEST0000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(_ulidFactory));
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
    public void VisibleAtDesktopWhenManuallyCollapsed()
    {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));
        dispatcher.Dispatch(new SidebarToggledAction("c1")); // collapse manually

        IRenderedComponent<FcHamburgerToggle> cut = Render<FcHamburgerToggle>();

        cut.WaitForAssertion(() =>
            cut.Instance.IsVisibleForTest.ShouldBeTrue(
                "Desktop + SidebarCollapsed=true → hamburger is SHOWN (D8 second disjunct)"));
    }

    [Fact]
    public void ManualToggleAtDesktopDispatchesSidebarToggled()
    {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();
        state.Value.SidebarCollapsed.ShouldBeFalse();

        IRenderedComponent<FcHamburgerToggle> cut = Render<FcHamburgerToggle>();

        // Simulate the FluentLayoutHamburger OnOpened(true) callback at Desktop — D9 says this
        // dispatches SidebarToggledAction only when user is physically acting on Desktop.
        cut.Instance.OnHamburgerOpenedForTest(opened: true);

        cut.WaitForAssertion(() =>
            state.Value.SidebarCollapsed.ShouldBeTrue(
                "Manual toggle at Desktop flips SidebarCollapsed (D9)"));
    }

    // REMOVED 2026-04-19 (test-review F2): `ViewportDrivenVisibilityDoesNotDispatchToggle` was
    // asserting a guard behavior ("at non-Desktop tiers, OnHamburgerOpened must not dispatch
    // SidebarToggledAction") that is not stated in D9, D7, or AC4/AC5. D9 describes the Desktop-side
    // dispatch only; the non-Desktop behavior of FluentLayoutHamburger.OnOpened is spec-ambiguous.
    // If the guard IS intended, raise a spec-change proposal to amend D9 with an explicit
    // "only when CurrentViewport == Desktop" clause, then re-add the test.
}
