// ATDD RED PHASE — Story 3-2 Task 10.1 (D1, D2, D11, D16, D18; AC1, AC3, AC6)
// Fails at compile until Task 6 (FrontComposerNavigation component) lands.

using System.Collections.Immutable;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-2 Task 10.1 — framework-owned sidebar component tests.
/// Covers D1 (one category per manifest; commands excluded; empty projections → hide category),
/// D2 (convention route), D11 (NavGroupToggledAction dispatch on expand change),
/// D16 (DOM tab reachability), D18 (auto-populate — tested via FrontComposerShellTests), AC1/AC3/AC6.
/// </summary>
public sealed class FrontComposerNavigationTests : LayoutComponentTestBase
{
    private readonly IFrontComposerRegistry _registry;
    private readonly IUlidFactory _ulidFactory;

    public FrontComposerNavigationTests()
    {
        _registry = Substitute.For<IFrontComposerRegistry>();
        Services.Replace(ServiceDescriptor.Singleton(_registry));

        _ulidFactory = Substitute.For<IUlidFactory>();
        _ulidFactory.NewUlid().Returns("01J0TEST0000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(_ulidFactory));
    }

    [Fact]
    public void RendersOneCategoryPerManifest()
    {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", Projections: ["Counter.Domain.Projections.CounterView"], Commands: []),
            new DomainManifest("Orders", "Orders", Projections: ["Orders.Domain.Projections.OrderList"], Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("id=\"Counter\"", Case.Sensitive);
            cut.Markup.ShouldContain("id=\"Orders\"", Case.Sensitive);

            // F5 — lock the e2e selector contract at the unit level.
            cut.Markup.ShouldContain("data-testid=\"fc-navigation-full\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-category-Counter\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-category-Orders\"");
        });
    }

    [Fact]
    public void RendersOneItemPerProjection()
    {
        _registry.GetManifests().Returns([
            new DomainManifest(
                Name: "Counter",
                BoundedContext: "Counter",
                Projections: [
                    "Counter.Domain.Projections.CounterView",
                    "Counter.Domain.Projections.CounterHistory",
                    "Counter.Domain.Projections.CounterAudit",
                ],
                Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("/counter/counter-view");
            cut.Markup.ShouldContain("/counter/counter-history");
            cut.Markup.ShouldContain("/counter/counter-audit");
        });
    }

    [Fact]
    public void DoesNotRenderCommandsAsNavItems()
    {
        _registry.GetManifests().Returns([
            new DomainManifest(
                Name: "Counter",
                BoundedContext: "Counter",
                Projections: ["Counter.Domain.Projections.CounterView"],
                Commands: [
                    "Counter.Domain.Commands.IncrementCommand",
                    "Counter.Domain.Commands.DecrementCommand",
                    "Counter.Domain.Commands.ResetCommand",
                ]),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldNotContain("IncrementCommand");
            cut.Markup.ShouldNotContain("DecrementCommand");
            cut.Markup.ShouldNotContain("ResetCommand");
            cut.Markup.ShouldNotContain("/counter/increment-command");
        });
    }

    [Fact]
    public void HidesCategoryWhenProjectionsEmpty()
    {
        // D1 clarification (2026-04-18): a manifest with commands-only (empty Projections)
        // produces NO FluentNavCategory. Empty category shells are noise-without-signal.
        _registry.GetManifests().Returns([
            new DomainManifest(
                Name: "CommandsOnly",
                BoundedContext: "CommandsOnly",
                Projections: [],
                Commands: ["Some.Namespace.SomeCommand"]),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldNotContain("id=\"CommandsOnly\"");
            cut.Markup.ShouldNotContain("CommandsOnly");
        });
    }

    [Fact]
    public void BuildRouteProducesExpectedHref()
    {
        // D2 — convention: /{boundedContext-lowercase}/{projectionTypeName-kebab-case}
        string route = FrontComposerNavigation.BuildRoute("Counter", "Counter.Domain.Projections.CounterView");
        route.ShouldBe("/counter/counter-view");

        // Multi-word type
        string route2 = FrontComposerNavigation.BuildRoute("Orders", "Orders.Domain.Projections.OrderLineItemView");
        route2.ShouldBe("/orders/order-line-item-view");

        // Short name (no namespace)
        string route3 = FrontComposerNavigation.BuildRoute("Counter", "CounterView");
        route3.ShouldBe("/counter/counter-view");
    }

    [Fact]
    public void ExpandedStateBindsToCollapsedGroups()
    {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new NavGroupToggledAction("c-setup", "Counter", Collapsed: true));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        // F6 — assert the component property, not the rendered markup. FluentNavCategory may omit
        // the Expanded attribute when the value equals the default, making markup regex brittle.
        cut.WaitForAssertion(() =>
        {
            IRenderedComponent<FluentNavCategory> category = cut
                .FindComponents<FluentNavCategory>()
                .Single(c => c.Instance.Id == "Counter");
            category.Instance.Expanded.ShouldBeFalse("Collapsed bounded context must render Expanded=false");
        });
    }

    [Fact]
    public void NavGroupToggledDispatchesOnExpandedChange()
    {
        // D11 — dispatches NavGroupToggledAction(correlationId, boundedContext, collapsed)
        // on FluentNavCategory.ExpandedChanged.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        // Invoke the internal handler directly — the FluentNavCategory callback wires to this method.
        cut.Instance.OnGroupExpandedChangedForTest(boundedContext: "Counter", expanded: false);

        // Assert dispatcher observed a NavGroupToggledAction with Collapsed=true (since expanded=false).
        // Using Fluxor's dispatch pipeline means the reducer has now run; we check reducer-observable state.
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();
        state.Value.CollapsedGroups.ShouldContainKeyAndValue("Counter", true);
    }

    [Fact]
    public void NavItemsAreTabReachable()
    {
        // D16 / AC6 — every rendered FluentNavItem is tab-reachable (no tabindex="-1" on focusables).
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter",
                Projections: ["Counter.Domain.Projections.CounterView"],
                Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() =>
        {
            IReadOnlyList<AngleSharp.Dom.IElement> focusables = [.. cut.Nodes.QuerySelectorAll("a, button")];
            focusables.ShouldNotBeEmpty();
            foreach (AngleSharp.Dom.IElement el in focusables)
            {
                string? tabindex = el.GetAttribute("tabindex");
                tabindex.ShouldNotBe("-1", "Every focusable nav control must be tab-reachable (WCAG 2.1 AA — D16)");
            }
        });
    }

    [Fact]
    public void RendersRailAtCompactDesktop()
    {
        // D7 / D9 — when CurrentViewport is CompactDesktop (or Desktop + SidebarCollapsed), the
        // navigation renders as FcCollapsedNavRail instead of the full FluentNav.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.CompactDesktop));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        // F3 — assert via typed component absence, not markup string. `<FluentNav` never appears
        // in rendered HTML (Razor components emit their DOM, not their tag name), so the previous
        // markup-string check was a tautology.
        cut.WaitForAssertion(() =>
        {
            _ = cut.FindComponent<FcCollapsedNavRail>();
            Should.Throw<Bunit.ComponentNotFoundException>(
                () => cut.FindComponent<FluentNav>(),
                "At CompactDesktop the full FluentNav must NOT render — only the FcCollapsedNavRail.");
        });
    }

    [Fact]
    public void RendersFullNavAtDesktop()
    {
        // AC3 — Desktop tier renders full FluentNav (categories + items), not the rail.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("id=\"Counter\"");
            Should.Throw<Bunit.ComponentNotFoundException>(() => cut.FindComponent<FcCollapsedNavRail>());
        });
    }
}
