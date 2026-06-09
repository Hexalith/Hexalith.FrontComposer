using Bunit;
using Bunit.TestDoubles;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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

        // bUnit forbids service registration after the provider is built; auth must be wired BEFORE
        // EnsureStoreInitialized triggers the first resolution. Default state is not-authorized.
        // bUnit's fake authorization grants AuthorizeView Policy checks via SetPolicies(...).
        _auth = AddAuthorization();

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
            cut.Markup.ShouldContain("data-testid=\"fc-nav-category-tenants\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-tenants-tenants\"");
            cut.Markup.ShouldContain("href=\"/tenants\"");
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
            // projection item (D2 route convention) and the explicit entry both render in the category
            cut.Markup.ShouldContain("/tenants/tenant-view");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-tenants-my-tenants\"");
            cut.Markup.ShouldContain("href=\"/tenants/my\"");
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
            cut.Markup.ShouldContain("href=\"/global-administrators\"");
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
            cut.Markup.ShouldContain("data-testid=\"fc-nav-category-standalone\"");
            cut.Markup.ShouldContain("data-testid=\"fc-nav-entry-standalone-standalone-page\"");
            cut.Markup.ShouldContain("href=\"/standalone\"");
        });
    }
}
