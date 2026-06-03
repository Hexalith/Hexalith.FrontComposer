using System.Collections.Immutable;
using System.Globalization;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Home;
using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Home;

/// <summary>
/// Story 3-5 Task 7.6 — bUnit tests for <see cref="FcHomeDirectory"/>. Covers AC3 / AC4a / AC4b /
/// AC5 / AC6 / AC7 / D19 anonymous-fallback. State substitution flows through the Fluxor store via
/// dispatched seed actions.
/// </summary>
public sealed class FcHomeDirectoryTests : LayoutComponentTestBase {
    private readonly IFrontComposerRegistry _registry;
    private readonly IUlidFactory _ulidFactory;

    public FcHomeDirectoryTests() {
        // Pin culture to English so resource-key assertions match the expected EN strings.
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");

        _registry = Substitute.For<IFrontComposerRegistry>();
        Services.Replace(ServiceDescriptor.Singleton(_registry));

        _ulidFactory = Substitute.For<IUlidFactory>();
        _ulidFactory.NewUlid().Returns("01J0HOME0000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(_ulidFactory));

        EnsureStoreInitialized();
    }

    private void SeedHydration(CapabilityDiscoveryHydrationState hydration, ImmutableHashSet<string>? seenSet = null) {
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            seenSet ?? ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        if (hydration == CapabilityDiscoveryHydrationState.Seeding) {
            return;
        }

        if (hydration == CapabilityDiscoveryHydrationState.Seeded) {
            dispatcher.Dispatch(new BadgeCountsSeededAction(ImmutableDictionary<Type, int>.Empty));
        }
    }

    [Fact]
    public void RendersNoMicroservicesEmptyState_WhenNoManifestsRegistered() {
        _registry.GetManifests().Returns([]);
        SeedHydration(CapabilityDiscoveryHydrationState.Seeded);

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-empty-no-microservices\"");
            cut.Markup.ShouldContain("No microservices registered.");
            cut.Markup.ShouldContain($"href=\"{FcHomeDirectory.GettingStartedGuideUrl}\"");
        });
    }

    [Fact]
    public void RendersSkeletons_WhileHydrationIsIdle() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.CounterProjection"], []),
        ]);
        // Do NOT dispatch BadgeCountsSeededAction → state remains Idle.

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-skeletons\"");
            cut.Markup.ShouldContain("data-testid=\"fc-home-skeleton-Counter\"");
            cut.Markup.ShouldContain("aria-busy=\"true\"");
        });
    }

    [Fact]
    public void RendersFirstVisitText_WhenSeenSetEmpty_AndZeroItems() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.CounterProjection"], []),
        ]);
        SeedHydration(CapabilityDiscoveryHydrationState.Seeded);

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-first-visit\"");
            cut.Markup.ShouldContain("You're all set up.");
            cut.Markup.ShouldNotContain("All caught up.");
        });
    }

    [Fact]
    public void RendersAllCaughtUp_WhenSeenSetNonEmpty_AndZeroItems() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.CounterProjection"], []),
        ]);
        SeedHydration(
            CapabilityDiscoveryHydrationState.Seeded,
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal).Add("bc:Counter"));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-all-caught-up\"");
            cut.Markup.ShouldContain("All caught up.");
            cut.Markup.ShouldNotContain("You're all set up.");
        });
    }

    [Fact]
    public void RendersMainRoleAndSortedByUrgencyAriaDescription() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.CounterProjection"], []),
        ]);
        SeedHydration(CapabilityDiscoveryHydrationState.Seeded);

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("role=\"main\"");
            cut.Markup.ShouldContain("aria-description=\"Sorted by urgency\"");
        });
    }

    [Fact]
    public void WelcomeSubtitle_AnonymousUser_RendersHomeWelcomeAnonymous_NeverRendersCommaDot() {
        // D19 — "Welcome back, ." MUST NOT be producible. AuthenticationStateTask is null in tests
        // (no auth provider wired); component falls back to HomeWelcomeAnonymous.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.CounterProjection"], []),
        ]);
        SeedHydration(CapabilityDiscoveryHydrationState.Seeded);

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Welcome back.");
            cut.Markup.ShouldNotContain("Welcome back, .");
            cut.Markup.ShouldNotContain("Welcome back, , ");
        });
    }

    [Fact]
    public void RendersProgressiveCardsWhileHydrationIsSeeding() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], []),
            new DomainManifest("Orders", "Orders", [typeof(int).FullName!], []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountChangedAction(typeof(string), 4));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-cards-progressive\"");
            cut.Markup.ShouldContain("data-testid=\"fc-home-card-Counter\"");
            cut.Markup.ShouldContain("data-testid=\"fc-home-skeleton-Orders\"");
            cut.Markup.ShouldContain("data-testid=\"fc-home-card-badge-Counter\"");
        });
    }

    [Fact]
    public void SeededZeroCountProjection_RendersProjectionRowWithoutBadge() {
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 0)));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-card-projection-Counter-String\"");
            cut.Markup.ShouldNotContain("data-testid=\"fc-home-card-projection-badge-Counter-String\"");
        });
    }

    [Fact]
    public void RendersUrgencySortedCards_WhenTotalActionableItemsExceedsZero() {
        // AC3 — urgency cards sort by aggregate count descending, ties broken by name ascending.
        _registry.GetManifests().Returns([
            new DomainManifest("Alpha", "Alpha", [typeof(string).FullName!], []),
            new DomainManifest("Beta", "Beta", [typeof(int).FullName!], []),
            new DomainManifest("Gamma", "Gamma", [typeof(double).FullName!], []),
        ]);
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty
                .Add(typeof(string), 2)   // Alpha
                .Add(typeof(int), 9)      // Beta (most urgent)
                .Add(typeof(double), 5))); // Gamma

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            int betaIdx = markup.IndexOf("data-testid=\"fc-home-card-Beta\"", StringComparison.Ordinal);
            int gammaIdx = markup.IndexOf("data-testid=\"fc-home-card-Gamma\"", StringComparison.Ordinal);
            int alphaIdx = markup.IndexOf("data-testid=\"fc-home-card-Alpha\"", StringComparison.Ordinal);
            betaIdx.ShouldBeGreaterThan(-1);
            gammaIdx.ShouldBeGreaterThan(-1);
            alphaIdx.ShouldBeGreaterThan(-1);
            betaIdx.ShouldBeLessThan(gammaIdx);
            gammaIdx.ShouldBeLessThan(alphaIdx);
        });
    }

    [Fact]
    public void CollapsesZeroUrgencyContexts_IntoOtherAreasAccordion() {
        // AC3 / D17 — contexts with aggregate count 0 render inside "Other areas" accordion
        // when at least one other context IS urgent.
        _registry.GetManifests().Returns([
            new DomainManifest("UrgentBc", "UrgentBc", [typeof(string).FullName!], []),
            new DomainManifest("QuietBc", "QuietBc", [typeof(int).FullName!], []),
        ]);
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty
                .Add(typeof(string), 3)
                .Add(typeof(int), 0)));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-other-areas\"");
            cut.Markup.ShouldContain("data-testid=\"fc-home-cards-urgent\"");
        });
    }

    [Fact]
    public void SkipsOtherAreasAccordion_WhenAllContextsHaveUrgency() {
        // D17 — accordion does NOT render when no zero-urgency contexts exist.
        _registry.GetManifests().Returns([
            new DomainManifest("A", "A", [typeof(string).FullName!], []),
            new DomainManifest("B", "B", [typeof(int).FullName!], []),
        ]);
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty
                .Add(typeof(string), 2)
                .Add(typeof(int), 1)));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("data-testid=\"fc-home-cards-urgent\"");
            cut.Markup.ShouldNotContain("data-testid=\"fc-home-other-areas\"");
        });
    }

    [Fact]
    public async Task ActivatesCardViaEnterKey_DispatchesVisitedAction_ThenNavigates() {
        // AC7 / Task 3.5 — Enter on a card dispatches CapabilityVisitedAction(bc:{BC}) and
        // navigates to the first projection route. Event wiring goes through the FcHomeCard
        // child component (extracted during review to escape the RenderFragment lambda bug that
        // silently dropped @onclick / @onkeydown).
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.CounterProjection"], []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        await cut.InvokeAsync(() =>
            cut.Find("button.fc-home-card-button")
               .KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" }));

        IState<FrontComposerCapabilityDiscoveryState> state =
            Services.GetRequiredService<IState<FrontComposerCapabilityDiscoveryState>>();
        state.Value.SeenCapabilities.ShouldContain("bc:Counter");

        NavigationManager nav = Services.GetRequiredService<NavigationManager>();
        nav.Uri.ShouldEndWith("/counter/counter-projection");
    }

    [Theory]
    [InlineData(true, 0, false)]   // seen + zero count → no New
    [InlineData(true, 3, false)]   // seen + non-zero → no New
    [InlineData(false, 0, false)]  // not seen + zero → no New (empty projections stay invisible)
    [InlineData(false, 3, true)]   // not seen + non-zero → New badge visible
    public void NewBadge_Matrix(bool alreadySeen, int count, bool expectNewBadge) {
        // D12 — four-cell matrix for projection-level "New" visibility.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], []),
        ]);
        ImmutableHashSet<string> seen = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal);
        if (alreadySeen) {
            seen = seen.Add($"proj:Counter:{typeof(string).FullName}");
        }

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(seen));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), count)));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            bool isRendered = cut.Markup.Contains(
                "data-testid=\"fc-home-card-projection-new-Counter-String\"",
                StringComparison.Ordinal);
            isRendered.ShouldBe(expectNewBadge);
        });
    }

    // ── Story 2.2 Task 2 (AC2) — progressive / flat / accordion sort-ordering pins ───────────
    // The urgent-variant sort is pinned by RendersUrgencySortedCards_… above. The three remaining
    // orderings (progressive, seeded-flat, accordion-zeros) were implemented but unpinned and could
    // silently regress on a careless refactor. Expectations use StringComparer.Ordinal to mirror
    // the component (culture stays "en" from the ctor for resource-key stability).

    [Fact]
    public void RendersProgressiveCards_ReadyFirstThenCountDescThenNameOrdinal() {
        // AC2 — progressive (Seeding) order: OrderByDescending(IsReady)
        //   .ThenByDescending(AggregateCount).ThenBy(Name, Ordinal).
        // Ready = at least one projection count has arrived; not-ready cards render as skeletons last.
        _registry.GetManifests().Returns([
            new DomainManifest("Zulu", "Zulu", [typeof(string).FullName!], []),    // ready, count 3
            new DomainManifest("Mike", "Mike", [typeof(double).FullName!], []),    // NOT seeded → not ready
            new DomainManifest("Alpha", "Alpha", [typeof(int).FullName!], []),     // ready, count 8
            new DomainManifest("Bravo", "Bravo", [typeof(long).FullName!], []),    // ready, count 8 (ties Alpha)
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        // BadgeCountChangedAction keeps hydration at Seeding (progressive), never Seeded.
        dispatcher.Dispatch(new BadgeCountChangedAction(typeof(string), 3));
        dispatcher.Dispatch(new BadgeCountChangedAction(typeof(int), 8));
        dispatcher.Dispatch(new BadgeCountChangedAction(typeof(long), 8));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            markup.ShouldContain("data-testid=\"fc-home-cards-progressive\"");
            int alpha = markup.IndexOf("data-testid=\"fc-home-card-Alpha\"", StringComparison.Ordinal);
            int bravo = markup.IndexOf("data-testid=\"fc-home-card-Bravo\"", StringComparison.Ordinal);
            int zulu = markup.IndexOf("data-testid=\"fc-home-card-Zulu\"", StringComparison.Ordinal);
            int mikeSkeleton = markup.IndexOf("data-testid=\"fc-home-card-skeleton-Mike\"", StringComparison.Ordinal);
            alpha.ShouldBeGreaterThan(-1);
            bravo.ShouldBeGreaterThan(-1);
            zulu.ShouldBeGreaterThan(-1);
            mikeSkeleton.ShouldBeGreaterThan(-1);
            // ready group: count-desc, then name-ordinal for the 8/8 tie (Alpha before Bravo), then Zulu(3).
            alpha.ShouldBeLessThan(bravo);
            bravo.ShouldBeLessThan(zulu);
            // not-ready card renders last.
            zulu.ShouldBeLessThan(mikeSkeleton);
        });
    }

    [Fact]
    public void RendersSeededFlatCards_InNameOrdinalOrder_WhenAllCountsZero() {
        // AC2 — seeded + totalActionable == 0 → flat list ordered OrderBy(Name, Ordinal).
        _registry.GetManifests().Returns([
            new DomainManifest("Yankee", "Yankee", [typeof(string).FullName!], []),
            new DomainManifest("Alpha", "Alpha", [typeof(int).FullName!], []),
            new DomainManifest("Mike", "Mike", [typeof(long).FullName!], []),
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty
                .Add(typeof(string), 0)
                .Add(typeof(int), 0)
                .Add(typeof(long), 0)));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            markup.ShouldContain("data-testid=\"fc-home-cards-flat\"");
            int alpha = markup.IndexOf("data-testid=\"fc-home-card-Alpha\"", StringComparison.Ordinal);
            int mike = markup.IndexOf("data-testid=\"fc-home-card-Mike\"", StringComparison.Ordinal);
            int yankee = markup.IndexOf("data-testid=\"fc-home-card-Yankee\"", StringComparison.Ordinal);
            alpha.ShouldBeGreaterThan(-1);
            alpha.ShouldBeLessThan(mike);
            mike.ShouldBeLessThan(yankee);
        });
    }

    [Fact]
    public void RendersOtherAreasAccordionZeros_InNameOrdinalOrder() {
        // AC2 / D17 — the collapsed zero-urgency cards inside fc-home-other-areas are
        // ordered OrderBy(Name, Ordinal) when at least one context is actionable.
        _registry.GetManifests().Returns([
            new DomainManifest("UrgentZ", "UrgentZ", [typeof(string).FullName!], []), // urgent, count 5
            new DomainManifest("Yankee", "Yankee", [typeof(int).FullName!], []),      // zero
            new DomainManifest("Alpha", "Alpha", [typeof(long).FullName!], []),       // zero
            new DomainManifest("Mike", "Mike", [typeof(double).FullName!], []),       // zero
        ]);

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(
            ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal)));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty
                .Add(typeof(string), 5)
                .Add(typeof(int), 0)
                .Add(typeof(long), 0)
                .Add(typeof(double), 0)));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            string markup = cut.Markup;
            int otherAreas = markup.IndexOf("data-testid=\"fc-home-other-areas\"", StringComparison.Ordinal);
            int alpha = markup.IndexOf("data-testid=\"fc-home-card-Alpha\"", StringComparison.Ordinal);
            int mike = markup.IndexOf("data-testid=\"fc-home-card-Mike\"", StringComparison.Ordinal);
            int yankee = markup.IndexOf("data-testid=\"fc-home-card-Yankee\"", StringComparison.Ordinal);
            otherAreas.ShouldBeGreaterThan(-1);
            // zero cards live inside the accordion (after its marker)…
            alpha.ShouldBeGreaterThan(otherAreas);
            // …and are ordered alphabetically by ordinal.
            alpha.ShouldBeLessThan(mike);
            mike.ShouldBeLessThan(yankee);
        });
    }

    [Fact]
    public void NewBadge_BCLevel_DismissalDoesNotDismissProjectionLevel() {
        // D12 / G25 — dismissing the BC-level "New" (bc:Counter in seen-set) must NOT dismiss the
        // projection-level "New" for a projection whose `proj:` id is still absent from the seen-set.
        _registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", [typeof(string).FullName!], []),
        ]);
        ImmutableHashSet<string> seen = ImmutableHashSet<string>.Empty
            .WithComparer(StringComparer.Ordinal)
            .Add("bc:Counter");

        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        dispatcher.Dispatch(new SeenCapabilitiesHydratedAction(seen));
        dispatcher.Dispatch(new BadgeCountsSeededAction(
            ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 4)));

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("data-testid=\"fc-home-card-new-Counter\"");
            cut.Markup.ShouldContain("data-testid=\"fc-home-card-projection-new-Counter-String\"");
        });
    }

}
