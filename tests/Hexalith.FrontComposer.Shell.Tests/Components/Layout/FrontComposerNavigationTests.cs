// ATDD RED PHASE — Story 3-2 Task 10.1 (D1, D2, D11, D16, D18; AC1, AC3, AC6)
// Fails at compile until Task 6 (FrontComposerNavigation component) lands.

using System.Collections.Immutable;

using AngleSharp.Dom;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
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
    private readonly IFrontComposerNavEntryRegistry _navRegistry;
    private readonly IUlidFactory _ulidFactory;

    public FrontComposerNavigationTests() {
        _registry = Substitute.For<IFrontComposerRegistry, IFrontComposerNavEntryRegistry>();
        _navRegistry = (IFrontComposerNavEntryRegistry)_registry;
        _navRegistry.GetNavEntries().Returns([]);
        Services.Replace(ServiceDescriptor.Singleton(_registry));

        _ulidFactory = Substitute.For<IUlidFactory>();
        _ulidFactory.NewUlid().Returns("01J0TEST0000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(_ulidFactory));

        EnsureStoreInitialized();
    }

    [Fact]
    public void DesktopExpanded_RendersUnifiedLabeledRail_WithoutLegacyFluentNavTree() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            IElement rail = cut.Find("[data-testid=\"fc-navigation-rail\"]");
            rail.GetAttribute("data-rail-width").ShouldBe("72");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-context-Counter\"");
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(
                () => cut.FindComponent<FluentNav>(),
                "Story 8.5 replaces the expanded FluentNav tree with the same rail path used by collapsed mode.");
        });
    }

    [Fact]
    public void ContextTile_RendersProjectionFlyoutMenu_WithProjectionAndExplicitEntries() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns([
            new FrontComposerNavEntry("Counter", "Counter audit", "/counter/audit"),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            IRenderedComponent<FluentMenu> menu = cut.FindComponents<FluentMenu>()
                .Single(m => m.Instance.Trigger == "fc-rail-Counter");
            menu.Instance.AdditionalAttributes.ShouldNotBeNull();
            menu.Instance.AdditionalAttributes!["data-testid"].ShouldBe("fc-nav-flyout-Counter");
            menu.Instance.AdditionalAttributes!["role"].ShouldBe("menu");

            cut.Markup.ShouldContain("data-testid=\"fc-nav-flyout-projection-Counter-CounterView\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-Counter-counter-audit\"");
        });
    }

    [Fact]
    public void RendersOneCategoryPerManifest() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", Projections: ["Counter.Domain.Projections.CounterView"], Commands: []),
            new DomainManifest("Orders", "Orders", Projections: ["Orders.Domain.Projections.OrderList"], Commands: []),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("id=\"fc-rail-Counter\"", Case.Sensitive);
            cut.Markup.ShouldContain("id=\"fc-rail-Orders\"", Case.Sensitive);

            // Story 8.5 — lock the e2e selector contract at the unit level.
            cut.Markup.ShouldContain("data-testid=\"fc-navigation-rail\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-context-Counter\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-context-Orders\"");
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
            cut.Markup.ShouldContain("data-href=\"/counter/counter-view\"");
            cut.Markup.ShouldContain("data-href=\"/counter/counter-history\"");
            cut.Markup.ShouldContain("data-href=\"/counter/counter-audit\"");
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

    [Theory]
    [InlineData("/Tenants/Users", "/tenants/users")] // case-insensitive routing → lowercased
    [InlineData("/tenants/", "/tenants")]             // trailing slash trimmed
    [InlineData("tenants", "/tenants")]               // leading slash ensured
    [InlineData("/tenants/users?userId=x", "/tenants/users")] // query stripped
    [InlineData("/tenants#frag", "/tenants")]         // fragment stripped
    [InlineData("", "/")]                             // home → root
    public void NormalizeHref_StripsQueryAndFragment_AndCanonicalizes(string input, string expected)
        => FrontComposerNavigation.NormalizeHref(input).ShouldBe(expected);

    [Theory]
    [InlineData("/tenants/users", "/tenants/users")]            // exact leaf wins
    [InlineData("/tenants/users?userId=abc", "/tenants/users")] // query stripped → leaf still wins
    [InlineData("/tenants/01HTENANTID", "/tenants")]            // detail page → section ancestor stays lit
    [InlineData("/tenants", "/tenants")]                        // container exact
    [InlineData("/tenants/my", "/tenants/my")]                  // sibling leaf
    [InlineData("/elsewhere", null)]                            // no registered route → nothing active
    public void LongestNavPrefix_PicksMostSpecificRegisteredRoute(string current, string? expected) {
        // The Tenants registration: /tenants (container) + /tenants/my + /tenants/users (leaves).
        IReadOnlyList<string> hrefs = [
            FrontComposerNavigation.NormalizeHref("/tenants"),
            FrontComposerNavigation.NormalizeHref("/tenants/my"),
            FrontComposerNavigation.NormalizeHref("/tenants/users"),
        ];

        FrontComposerNavigation.LongestNavPrefix(FrontComposerNavigation.NormalizeHref(current), hrefs)
            .ShouldBe(expected);
    }

    [Fact]
    public void CollapsedGroupsState_DoesNotHideUnifiedRailTile() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new NavGroupToggledAction("c-setup", "Counter", Collapsed: true));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-nav-context-Counter\"");
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(
                () => cut.FindComponent<FluentNavCategory>(),
                "Story 8.5 keeps CollapsedGroups in the persisted schema, but the rail no longer renders collapsible categories.");
        });
    }

    // Story 8.5 review (2026-06-25): the legacy NavGroupCollapse/Expand component tests were removed.
    // The Aspire-style rail/flyout has no collapsible nav categories, so the component-level
    // OnGroupExpandedChanged handler they drove was dead (no UI trigger) and was deleted. The persisted
    // CollapsedGroups schema and its D13 reducer/seen-set semantics stay covered by NavigationReducerTests
    // (ReduceNavGroupToggled), and CollapsedGroupsState_DoesNotHideUnifiedRailTile above pins that a
    // persisted collapsed group no longer hides a rail tile.

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
    public void RendersIconOnlyRailAtCompactDesktop() {
        // Story 8.5 — CompactDesktop uses the same unified rail path, in 48px icon-only mode.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.CompactDesktop));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Find("[data-testid=\"fc-navigation-rail\"]").GetAttribute("data-rail-width").ShouldBe("48");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-context-Counter\"");
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(
                () => cut.FindComponent<FluentNav>(),
                "At CompactDesktop the old full FluentNav must not render.");
        });
    }

    [Fact]
    public void RendersLabeledRailAtDesktop() {
        // Story 8.5 — Desktop tier renders the 72px labeled rail.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new ViewportTierChangedAction(ViewportTier.Desktop));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Find("[data-testid=\"fc-navigation-rail\"]").GetAttribute("data-rail-width").ShouldBe("72");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-context-Counter\"");
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(() => cut.FindComponent<FluentNav>());
        });
    }

    // ── Story 2.2 Task 1 (AC1) — count + projection-"New" badge RENDER pins ──────────────────
    // The tree/grouping/route/click/visibility rules are pinned above and in
    // FrontComposerNavigationCapabilityBadgeTests. What was NOT pinned before this story is the
    // actual badge MARKUP: the count `FluentBadge` (Filled/Brand, fc-nav-badge-{bc}-{label}) and the
    // projection-level "New" `FluentBadge` (Tint/Informative, fc-nav-new-{bc}-{label}). These pins
    // seed BadgeCountsSeededAction + SeenCapabilitiesHydratedAction and assert the rendered output.
    // Resolvable runtime types (System.String/System.Int32) are used so ProjectionTypeResolver maps
    // them to live counts — the same pattern the home + capability-badge suites use.

    private void SeedDiscovery(ImmutableHashSet<string> seen, ImmutableDictionary<Type, int> counts) {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(seen));
        dispatcher.Dispatch(new BadgeCountsSeededAction(counts));
    }

    [Fact]
    public void CountBadge_RendersBrandFilledBadgeWithValue_WhenProjectionCountPositive() {
        // AC1 — count > 0 → FluentBadge Filled/Brand carrying the count text, keyed fc-nav-badge-{bc}-{label}.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], Commands: []),
        ]);
        SeedDiscovery(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 7));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            IElement badge = cut.Find("[data-testid=\"fc-nav-badge-Counter-String\"]");
            badge.TextContent.Trim().ShouldBe("7");
        });
    }

    [Fact]
    public void CountBadge_AbsentAndProjectionHidden_WhenResolvedCountIsZero() {
        // AC1 — a resolved projection with count == 0 is filtered out by VisibleProjections once
        // counts are seeded, so neither the nav item nor its count badge render.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], Commands: []),
        ]);
        SeedDiscovery(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 0));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("data-testid=\"fc-nav-badge-Counter-String\"");
            cut.Markup.ShouldNotContain("data-testid=\"fc-nav-category-Counter\"");
        });
    }

    [Fact]
    public void CountBadge_Absent_WhenProjectionVisibleButHasNoResolvedCount() {
        // AC1 — exercises the `@if (count > 0)` markup guard with the nav item still visible: an
        // unresolved projection FQN stays visible (VisibleProjections keeps unresolved types) while
        // LookupCount yields 0, so the item renders with NO count badge.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        // counts non-empty (seeded mode) but the manifest's projection does not resolve.
        SeedDiscovery(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 3));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-href=\"/counter/counter-view\"");
            cut.Markup.ShouldNotContain("data-testid=\"fc-nav-badge-Counter-CounterView\"");
        });
    }

    [Theory]
    [InlineData(true, 3, false)]   // seen + count > 0 → no projection "New"
    [InlineData(false, 3, true)]   // unseen + count > 0 → projection "New" visible
    [InlineData(true, 0, false)]   // seen + count 0 → projection hidden → no "New"
    [InlineData(false, 0, false)]  // unseen + count 0 → projection hidden → no "New"
    public void ProjectionNewBadge_Matrix(bool alreadySeen, int count, bool expectNewBadge) {
        // AC1 — projShowsNew = !seen.Contains(proj:{bc}:{type}) && count > 0, keyed fc-nav-new-{bc}-{label}.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], Commands: []),
        ]);
        ImmutableHashSet<string> seen = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);
        if (alreadySeen) {
            seen = seen.Add($"proj:Counter:{typeof(string).FullName}");
        }

        SeedDiscovery(seen, ImmutableDictionary<Type, int>.Empty.Add(typeof(string), count));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            bool isRendered = cut.Markup.Contains(
                "data-testid=\"fc-nav-new-Counter-String\"",
                StringComparison.Ordinal);
            isRendered.ShouldBe(expectNewBadge);
        });
    }

    [Fact]
    public void MultiProjectionManifest_RendersPerProjectionCountBadges_AndAggregateBcNew() {
        // AC1 — multi-projection manifest exercises LookupCount per item AND
        // AggregateBoundedContextCount for the BC-level "New" badge (bcShowsNew = unseen && Σcount > 0).
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!, typeof(int).FullName!], Commands: []),
        ]);
        SeedDiscovery(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 2).Add(typeof(int), 3));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Find("[data-testid=\"fc-nav-badge-Counter-String\"]").TextContent.Trim().ShouldBe("2");
            cut.Find("[data-testid=\"fc-nav-badge-Counter-Int32\"]").TextContent.Trim().ShouldBe("3");
            // aggregate 5 > 0 and bc:Counter unseen → BC-level "New" renders.
            cut.Markup.ShouldContain("data-testid=\"fc-rail-new-Counter\"");
        });
    }

    [Fact]
    public void BcNewBadge_Absent_WhenBoundedContextAlreadySeen() {
        // AC1 — once bc:{BC} is in the seen-set the BC-level "New" badge is suppressed even though
        // the aggregate count is positive.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!, typeof(int).FullName!], Commands: []),
        ]);
        SeedDiscovery(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal).Add("bc:Counter"),
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 2).Add(typeof(int), 3));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            // count badges still render…
            cut.Markup.ShouldContain("data-testid=\"fc-nav-badge-Counter-String\"");
            // …but the BC-level "New" is gone.
            cut.Markup.ShouldNotContain("data-testid=\"fc-rail-new-Counter\"");
        });
    }

    // ── Story 2.2 QA gap-pins (AC1) — aria-label render + badge appearance/colour contract ────
    // The dev-story pass pinned the badge TESTIDS + text and claimed the FluentNav aria-label and
    // the Filled/Brand · Tint/Informative appearances were "already pinned", but no assertion
    // covered them: the nav could lose its accessible name or have its badges restyled and every
    // existing pin would still pass. These three pins close that gap.

    [Fact]
    public void Rail_RendersLocalizedNavMenuAriaLabel() {
        // AC1 / FC-A11Y in-scope pin — the navigation rail exposes its accessible name bound to the
        // localized NavMenuAriaLabel resource (not a hardcoded/empty string).
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);

        string expected = Services
            .GetRequiredService<IStringLocalizer<FcShellResources>>()["NavMenuAriaLabel"].Value;

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            expected.ShouldNotBeNullOrWhiteSpace();
            cut.Markup.ShouldContain($"aria-label=\"{expected}\"");
        });
    }

    [Fact]
    public void CountBadge_UsesFilledBrandAppearance_AsShippedContract() {
        // AC1 / Task 1 "do not restyle" — the count badge is FluentBadge Filled/Brand. Both
        // bc:{BC} and proj:{...} are pre-seeded into the seen-set so the ONLY badge rendered is the
        // count badge, making the appearance assertion unambiguous.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], Commands: []),
        ]);
        SeedDiscovery(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)
                .Add("bc:Counter")
                .Add($"proj:Counter:{typeof(string).FullName}"),
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 7));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            IRenderedComponent<FluentBadge> badge = FindBadgeByTestId(cut, "fc-nav-badge-Counter-String");
            badge.Instance.Appearance.ShouldBe(BadgeAppearance.Filled);
            badge.Instance.Color.ShouldBe(BadgeColor.Brand);
        });
    }

    [Fact]
    public void ProjectionNewBadge_UsesTintInformativeAppearance_AsShippedContract() {
        // AC1 / Task 1 "do not restyle" — the projection-level "New" badge is FluentBadge
        // Tint/Informative. Filtering by the exact fc-nav-new-* testid isolates it from the
        // co-rendered count and BC-level "New" badges.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], Commands: []),
        ]);
        SeedDiscovery(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal),
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 4));

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            IRenderedComponent<FluentBadge> badge = FindBadgeByTestId(cut, "fc-nav-new-Counter-String");
            badge.Instance.Appearance.ShouldBe(BadgeAppearance.Tint);
            badge.Instance.Color.ShouldBe(BadgeColor.Informative);
        });
    }

    private static IRenderedComponent<FluentBadge> FindBadgeByTestId(
        IRenderedComponent<FrontComposerNavigation> cut,
        string testId)
        => cut.FindComponents<FluentBadge>().Single(b =>
            b.Instance.AdditionalAttributes is not null
            && b.Instance.AdditionalAttributes.TryGetValue("data-testid", out object? value)
            && value is string s
            && string.Equals(s, testId, StringComparison.Ordinal));
}
