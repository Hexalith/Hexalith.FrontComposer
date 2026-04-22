// ATDD RED PHASE — Story 3-2 Task 10.2 (D13; AC4)
// Fails at compile until Task 7 (FcCollapsedNavRail component) lands.

using System.Collections.Immutable;

using AngleSharp.Dom;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-2 Task 10.2 — 48 px icon rail rendered at CompactDesktop (and Desktop + manual collapse).
/// D13 — one FluentButton per manifest with FluentTooltip anchored by id, default Icons.Regular.Size20.Apps,
/// click dispatches SidebarExpandedAction(correlationId).
/// </summary>
public sealed class FcCollapsedNavRailTests : LayoutComponentTestBase {
    private readonly IFrontComposerRegistry _registry;
    private readonly IUlidFactory _ulidFactory;

    public FcCollapsedNavRailTests() {
        _registry = Substitute.For<IFrontComposerRegistry>();
        Services.Replace(ServiceDescriptor.Singleton(_registry));

        _ulidFactory = Substitute.For<IUlidFactory>();
        _ulidFactory.NewUlid().Returns("01J0TEST0000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(_ulidFactory));

        EnsureStoreInitialized();
    }

    [Fact]
    public void RendersOneButtonPerManifest() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
            new DomainManifest("Orders", "Orders", ["Orders.Domain.Projections.OrderList"], Commands: []),
        ]);

        IRenderedComponent<FcCollapsedNavRail> cut = Render<FcCollapsedNavRail>();

        cut.WaitForAssertion(() => {
            IReadOnlyList<AngleSharp.Dom.IElement> buttons = [.. cut.Nodes.QuerySelectorAll("fluent-button")];
            buttons.Count.ShouldBe(2);
            buttons[0].GetAttribute("id").ShouldBe("fc-rail-Counter");
            buttons[1].GetAttribute("id").ShouldBe("fc-rail-Orders");

            // F5 — lock the e2e selector contract at the unit level.
            cut.Markup.ShouldContain("data-testid=\"fc-collapsed-rail\"");
        });
    }

    [Fact]
    public void TooltipContainsBoundedContextName() {
        _registry.GetManifests().Returns([
            new DomainManifest(Name: "Counter Domain", BoundedContext: "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IRenderedComponent<FcCollapsedNavRail> cut = Render<FcCollapsedNavRail>();

        cut.WaitForAssertion(() => {
            IRenderedComponent<FluentTooltip> tooltip = cut.FindComponent<FluentTooltip>();
            tooltip.Instance.Anchor.ShouldBe("fc-rail-Counter");
            cut.Markup.ShouldContain("fc-rail-Counter");
            cut.Markup.ShouldContain("Counter Domain");
        });
    }

    [Fact]
    public void CommandsOnlyManifestDoesNotRenderRailButton() {
        _registry.GetManifests().Returns([
            new DomainManifest(
                Name: "CommandsOnly",
                BoundedContext: "CommandsOnly",
                Projections: [],
                Commands: ["CommandsOnly.Domain.Commands.RunThing"]),
        ]);

        IRenderedComponent<FcCollapsedNavRail> cut = Render<FcCollapsedNavRail>();

        cut.WaitForAssertion(() => {
            IReadOnlyList<AngleSharp.Dom.IElement> buttons = [.. cut.Nodes.QuerySelectorAll("fluent-button")];
            buttons.ShouldBeEmpty();
            cut.Markup.ShouldNotContain("CommandsOnly");
        });
    }

    [Fact]
    public async Task ClickDispatchesSidebarExpanded() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IRenderedComponent<FcCollapsedNavRail> cut = Render<FcCollapsedNavRail>();
        IState<FrontComposerCapabilityDiscoveryState> discoveryState =
            Services.GetRequiredService<IState<FrontComposerCapabilityDiscoveryState>>();
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();

        // Seed state as collapsed, so the click's SidebarExpandedAction produces an observable change.
        Services.GetRequiredService<IDispatcher>().Dispatch(new SidebarToggledAction("c-setup"));
        state.Value.SidebarCollapsed.ShouldBeTrue();

        // Click the first rail button. F7 — async/await instead of GetAwaiter().GetResult().
        AngleSharp.Dom.IElement firstButton = cut.Nodes.QuerySelectorAll("fluent-button").First();
        await cut.InvokeAsync(() => firstButton.Click());

        // Final state is the contract. F7 — dropped the brittle ULID call count assertion; the
        // observable state change already proves the SidebarExpandedAction pipeline fired.
        cut.WaitForAssertion(() => state.Value.SidebarCollapsed.ShouldBeFalse(
            "SidebarExpandedAction should flip SidebarCollapsed back to false (D13)"));
        discoveryState.Value.SeenCapabilities.ShouldContain("bc:Counter");
    }

    [Fact]
    public void RendersBadgeAndNewIndicator_WhenBoundedContextHasUrgency() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 3)));

        IRenderedComponent<FcCollapsedNavRail> cut = Render<FcCollapsedNavRail>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-rail-badge-Counter\"");
            cut.Markup.ShouldContain("data-testid=\"fc-rail-new-Counter\"");
        });
    }

    [Fact]
    public async Task CompactDesktopClickRequestsHamburgerDrawer() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        SpyHamburgerCoordinator coordinator = new();
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();
        Services.GetRequiredService<IDispatcher>().Dispatch(new ViewportTierChangedAction(ViewportTier.CompactDesktop));
        Services.GetRequiredService<IDispatcher>().Dispatch(new SidebarToggledAction("c-setup"));
        state.Value.SidebarCollapsed.ShouldBeTrue();

        var host = Render(builder => {
            builder.OpenComponent<CascadingValue<LayoutHamburgerCoordinator>>(0);
            builder.AddAttribute(1, nameof(CascadingValue<LayoutHamburgerCoordinator>.Value), coordinator);
            builder.AddAttribute(2, nameof(CascadingValue<LayoutHamburgerCoordinator>.IsFixed), true);
            builder.AddAttribute(3, nameof(CascadingValue<LayoutHamburgerCoordinator>.ChildContent), (RenderFragment)(child => {
                child.OpenComponent<FcCollapsedNavRail>(0);
                child.CloseComponent();
            }));
            builder.CloseComponent();
        });

        AngleSharp.Dom.IElement firstButton = host.Nodes.QuerySelectorAll("fluent-button").First();
        await host.InvokeAsync(() => firstButton.Click());

        coordinator.ShowCalls.ShouldBe(1, "CompactDesktop rail clicks should request the hamburger drawer.");
        state.Value.SidebarCollapsed.ShouldBeFalse();
    }

    private sealed class SpyHamburgerCoordinator : LayoutHamburgerCoordinator {
        public int ShowCalls { get; private set; }

        internal override Task ShowAsync() {
            ShowCalls++;
            return Task.CompletedTask;
        }
    }
}
