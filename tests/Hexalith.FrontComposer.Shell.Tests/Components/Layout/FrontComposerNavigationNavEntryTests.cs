using Bunit;
using Bunit.TestDoubles;

using AngleSharp.Dom;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Pins the declarative navigation-entry rendering added so domain modules can contribute their
/// left-menu items to the global shell nav as plain data (<see cref="FrontComposerNavEntry"/>),
/// rendered entirely by the framework. Covers: entry → FluentNavItem under its bounded-context
/// category, projection + entry coexistence, the empty-projections-but-has-entries category gap,
/// disabled affordance + reason, policy gating, and orphan (no-manifest) bounded contexts.
/// </summary>
public sealed class FrontComposerNavigationNavEntryTests : LayoutComponentTestBase {
    private const string TestPolicy = "fc.nav.test.policy";

    private readonly IFrontComposerRegistry _registry;
    private readonly IFrontComposerNavEntryRegistry _navRegistry;
    private readonly BunitAuthorizationContext _auth;

    public FrontComposerNavigationNavEntryTests() {
        _registry = Substitute.For<IFrontComposerRegistry, IFrontComposerNavEntryRegistry>();
        _navRegistry = (IFrontComposerNavEntryRegistry)_registry;
        _registry.GetManifests().Returns([]);
        _navRegistry.GetNavEntries().Returns([]);
        Services.Replace(ServiceDescriptor.Singleton(_registry));

        IUlidFactory ulidFactory = Substitute.For<IUlidFactory>();
        ulidFactory.NewUlid().Returns("01J0TEST0000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(ulidFactory));

        // Reuse the base's authorization context (wired before any provider build). Default state is
        // not-authorized; bUnit's fake authorization grants AuthorizeView Policy checks via SetPolicies(...).
        _auth = Authorization;

        EnsureStoreInitialized();
    }

    [Fact]
    public void RegisteredNavEntry_RendersAsNavItem_UnderItsBoundedContextCategory_EvenWithEmptyProjections() {
        _registry.GetManifests().Returns([
            new DomainManifest("Tenants", "tenants", Projections: [], Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns([
            new FrontComposerNavEntry("tenants", "Tenants", "/tenants"),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-nav-context-tenants\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-flyout-tenants\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-tenants-tenants\"");
            cut.Markup.ShouldContain("data-href=\"/tenants\"");
            cut.Markup.ShouldContain("Tenants");
        });
    }

    [Fact]
    public void NavEntries_RenderAlongsideProjectionItems_ForTheSameBoundedContext() {
        _registry.GetManifests().Returns([
            new DomainManifest(
                "Tenants",
                "tenants",
                Projections: ["Tenants.Domain.Projections.TenantView"],
                Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns([
            new FrontComposerNavEntry("tenants", "My tenants", "/tenants/my"),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            // Projection item (D2 route convention) and the explicit entry both render in the flyout.
            cut.Markup.ShouldContain("data-href=\"/tenants/tenant-view\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-tenants-my-tenants\"");
            cut.Markup.ShouldContain("data-href=\"/tenants/my\"");
        });
    }

    [Fact]
    public void DisabledNavEntry_RendersReason_AndDoesNotRenderItsRouteAsALink() {
        _registry.GetManifests().Returns([
            new DomainManifest("Tenants", "tenants", Projections: [], Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns([
            new FrontComposerNavEntry(
                "tenants",
                "Audit",
                "/should-not-link",
                Enabled: false,
                DisabledReason: "Pick a tenant first."),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-tenants-audit-reason\"");
            cut.Markup.ShouldContain("Pick a tenant first.");
            // disabled entries carry no navigable href
            cut.Markup.ShouldNotContain("href=\"/should-not-link\"");
        });
    }

    [Fact]
    public void GatedNavEntry_IsHidden_WhenTheUserDoesNotSatisfyThePolicy() {
        _auth.SetNotAuthorized();
        _registry.GetManifests().Returns([
            new DomainManifest("Tenants", "tenants", Projections: [], Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns([
            new FrontComposerNavEntry("tenants", "Global Administrators", "/global-administrators", RequiredPolicy: TestPolicy),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("data-testid=\"fc-nav-entry-tenants-global-administrators\"");
            cut.Markup.ShouldNotContain("/global-administrators");
        });
    }

    [Fact]
    public void GatedNavEntry_IsShown_WhenTheUserSatisfiesThePolicy() {
        _auth.SetAuthorized("operator");
        _auth.SetPolicies(TestPolicy);
        _registry.GetManifests().Returns([
            new DomainManifest("Tenants", "tenants", Projections: [], Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns([
            new FrontComposerNavEntry("tenants", "Global Administrators", "/global-administrators", RequiredPolicy: TestPolicy),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-tenants-global-administrators\"");
            cut.Markup.ShouldContain("data-href=\"/global-administrators\"");
        });
    }

    [Fact]
    public void OrphanBoundedContext_WithEntriesButNoManifest_RendersItsOwnCategory() {
        _registry.GetManifests().Returns([]);
        _navRegistry.GetNavEntries().Returns([
            new FrontComposerNavEntry("standalone", "Standalone page", "/standalone"),
        ]);

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-nav-context-standalone\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-flyout-standalone\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-standalone-standalone-page\"");
            cut.Markup.ShouldContain("data-href=\"/standalone\"");
        });
    }

    // ── Single-active highlighting (correct-course 2026-06-19) ───────────────────────────────────
    // Before the fix, FluentNavItem's default NavLinkMatch.Prefix lit BOTH "/tenants" (a prefix of
    // every sub-route) AND the exact sub-route — two active bars. The shell now gives only the
    // longest-prefix match (the current route) NavLinkMatch.Prefix and every other item
    // NavLinkMatch.All, so at most one item is ever active.

    [Fact]
    public void ActiveHighlight_MostSpecificWins_OnlyTheCurrentRouteIsActive() {
        _registry.GetManifests().Returns([
            new DomainManifest("Tenants", "tenants", Projections: [], Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns(TenantsNavEntries());

        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants/users");

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            IReadOnlyList<IElement> active = ActiveItems(cut);
            active.Count.ShouldBe(1, "exactly one nav item may carry the active (Prefix) match");
            active[0].GetAttribute("data-href").ShouldBe("/tenants/users");

            // The container route must no longer prefix-match its sub-routes.
            cut.Find("[data-href=\"/tenants\"]").HasAttribute("aria-current").ShouldBeFalse();
        });
    }

    [Fact]
    public void ActiveHighlight_DetailPage_KeepsItsSectionAncestorLit() {
        _registry.GetManifests().Returns([
            new DomainManifest("Tenants", "tenants", Projections: [], Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns(TenantsNavEntries());

        // A tenant detail page (/tenants/{id}) is not itself a nav entry.
        Services.GetRequiredService<NavigationManager>().NavigateTo("/tenants/01HTENANTID");

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => {
            IReadOnlyList<IElement> active = ActiveItems(cut);
            active.Count.ShouldBe(1, "a detail page keeps exactly its section ancestor lit");
            active[0].GetAttribute("data-href").ShouldBe("/tenants");
        });
    }

    [Fact]
    public void ActiveHighlight_UnrelatedRoute_LeavesNothingActive() {
        _registry.GetManifests().Returns([
            new DomainManifest("Tenants", "tenants", Projections: [], Commands: []),
        ]);
        _navRegistry.GetNavEntries().Returns(TenantsNavEntries());

        Services.GetRequiredService<NavigationManager>().NavigateTo("/settings");

        IRenderedComponent<FrontComposerNavigation> cut = Render<FrontComposerNavigation>();

        cut.WaitForAssertion(() => ActiveItems(cut).ShouldBeEmpty());
    }

    private static FrontComposerNavEntry[] TenantsNavEntries() => [
        new FrontComposerNavEntry("tenants", "All tenants", "/tenants"),
        new FrontComposerNavEntry("tenants", "My tenants", "/tenants/my"),
        new FrontComposerNavEntry("tenants", "User lookup", "/tenants/users"),
    ];

    private static IReadOnlyList<IElement> ActiveItems(IRenderedComponent<FrontComposerNavigation> cut)
        => [.. cut.Nodes.QuerySelectorAll("[data-href][aria-current='page']")];
}
