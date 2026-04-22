// ATDD RED PHASE — Story 3-2 Task 10.1 (D1, D2, D11, D16, D18; AC1, AC3, AC6)
// Fails at compile until Task 6 (FrontComposerNavigation component) lands.

using System.Collections.Immutable;

using AngleSharp.Dom;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
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
public sealed class FrontComposerNavigationTests : LayoutComponentTestBase {
    private readonly IFrontComposerRegistry _registry;
    private readonly IUlidFactory _ulidFactory;

    public FrontComposerNavigationTests() {
        _registry = Substitute.For<IFrontComposerRegistry>();
        Services.Replace(ServiceDescriptor.Singleton(_registry));

        _ulidFactory = Substitute.For<IUlidFactory>();
        _ulidFactory.NewUlid().Returns("01J0TEST0000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(_ulidFactory));

        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersOneCategoryPerManifest() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", Projections: ["Counter.Domain.Projections.CounterView"], Commands: []),
            new DomainManifest("Orders", "Orders", Projections: ["Orders.Domain.Projections.OrderList"], Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("id=\"Counter\"", Case.Sensitive);
            cut.Markup.ShouldContain("id=\"Orders\"", Case.Sensitive);

            // F5 — lock the e2e selector contract at the unit level.
            cut.Markup.ShouldContain("data-testid=\"fc-navigation-full\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-category-Counter\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-category-Orders\"");
        });
    }

    [Fact]
    public void RendersOneItemPerProjection() {
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

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("/counter/counter-view");
            cut.Markup.ShouldContain("/counter/counter-history");
            cut.Markup.ShouldContain("/counter/counter-audit");
        });
    }

    [Fact]
    public void ProjectionLabelsUseVerbatimTypeName() {
        FrontComposerNavigation.ProjectionLabel("Counter.Domain.Projections.CounterView")
            .ShouldBe("CounterView");

        FrontComposerNavigation.ProjectionLabel("CounterView")
            .ShouldBe("CounterView");
    }

    [Fact]
    public void DoesNotRenderCommandsAsNavItems() {
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

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("IncrementCommand");
            cut.Markup.ShouldNotContain("DecrementCommand");
            cut.Markup.ShouldNotContain("ResetCommand");
            cut.Markup.ShouldNotContain("/counter/increment-command");
        });
    }

    [Fact]
    public void HidesCategoryWhenProjectionsEmpty() {
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

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("id=\"CommandsOnly\"");
            cut.Markup.ShouldNotContain("CommandsOnly");
        });
    }

    [Fact]
    public void BuildRouteProducesExpectedHref() {
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
    public void ExpandedStateBindsToCollapsedGroups() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new NavGroupToggledAction("c-setup", "Counter", Collapsed: true));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        // F6 — assert the component property, not the rendered markup. FluentNavCategory may omit
        // the Expanded attribute when the value equals the default, making markup regex brittle.
        cut.WaitForAssertion(() => {
            IRenderedComponent<FluentNavCategory> category = cut
                .FindComponents<FluentNavCategory>()
                .Single(c => c.Instance.Id == "Counter");
            category.Instance.Expanded.ShouldBeFalse("Collapsed bounded context must render Expanded=false");
        });
    }

    [Fact]
    public void NavGroupCollapseDispatchesNavGroupToggledButDoesNotMarkCapabilitySeen() {
        // D11 — dispatches NavGroupToggledAction(correlationId, boundedContext, collapsed)
        // on FluentNavCategory.ExpandedChanged.
        // D13 (review 2026-04-22) — collapsing is decluttering, not engagement: seen-set MUST
        // NOT gain the bc:{BC} id on a collapse toggle.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        // Invoke the internal handler directly — the FluentNavCategory callback wires to this method.
        cut.Instance.OnGroupExpandedChangedForTest(boundedContext: "Counter", expanded: false);

        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();
        IState<FrontComposerCapabilityDiscoveryState> discoveryState =
            Services.GetRequiredService<IState<FrontComposerCapabilityDiscoveryState>>();
        state.Value.CollapsedGroups.ShouldContainKeyAndValue("Counter", true);
        discoveryState.Value.SeenCapabilities.ShouldNotContain("bc:Counter");
    }

    [Fact]
    public void NavGroupExpandMarksCapabilitySeen() {
        // D13 (review 2026-04-22) — an explicit expand signals category engagement and MUST
        // dispatch CapabilityVisitedAction(bc:{BC}), dismissing the BC-level "New" badge.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.Instance.OnGroupExpandedChangedForTest(boundedContext: "Counter", expanded: true);

        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();
        IState<FrontComposerCapabilityDiscoveryState> discoveryState =
            Services.GetRequiredService<IState<FrontComposerCapabilityDiscoveryState>>();
        // ReduceNavGroupToggled removes the key when Collapsed=false (expanded is the default).
        state.Value.CollapsedGroups.ShouldNotContainKey("Counter");
        discoveryState.Value.SeenCapabilities.ShouldContain("bc:Counter");
    }

    [Fact]
    public void NavItemsAreTabReachable() {
        // D16 / AC6 — FluentNav uses WAI-ARIA's roving-tabindex pattern: at most one nav item
        // carries tabindex="0" at a time, the rest get tabindex="-1" by design. The invariant worth
        // asserting at the framework seam is (a) nav items are rendered (visible in DOM), and
        // (b) at least one is currently tab-reachable, meaning the composite widget is entered
        // via the first item. Fluent UI's roving-focus logic owns the mid-widget transitions.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter",
                Projections: ["Counter.Domain.Projections.CounterView"],
                Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            IReadOnlyList<AngleSharp.Dom.IElement> focusableNavItems = [..
                cut.Nodes.QuerySelectorAll("a[href], button, [tabindex]:not([tabindex='-1'])")];
            focusableNavItems.ShouldNotBeEmpty(
                "The rendered navigation must expose at least one tab-reachable control at the framework seam.");
        });
    }

    [Fact]
    public void RendersRailAtCompactDesktop() {
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
        cut.WaitForAssertion(() => {
            _ = cut.FindComponent<FcCollapsedNavRail>();
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(
                () => cut.FindComponent<FluentNav>(),
                "At CompactDesktop the full FluentNav must NOT render — only the FcCollapsedNavRail.");
        });
    }

    [Fact]
    public void RendersFullNavAtDesktop() {
        // AC3 — Desktop tier renders full FluentNav (categories + items), not the rail.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("id=\"Counter\"");
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(() => cut.FindComponent<FcCollapsedNavRail>());
        });
    }
}
