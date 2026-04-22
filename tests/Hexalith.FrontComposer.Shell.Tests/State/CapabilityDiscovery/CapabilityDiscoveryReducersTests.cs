using System.Collections.Immutable;

using Hexalith.FrontComposer.Shell.State.CapabilityDiscovery;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.CapabilityDiscovery;

/// <summary>
/// Story 3-5 Task 7.3 — pure reducer tests for the capability-discovery feature (D8 / ADR-046).
/// </summary>
public sealed class CapabilityDiscoveryReducersTests {
    [Fact]
    public void ReduceBadgeCountsSeeded_AssignsCounts_AndFlipsHydrationStateToSeeded() {
        FrontComposerCapabilityDiscoveryState initial = FrontComposerCapabilityDiscoveryState.Empty;
        ImmutableDictionary<Type, int> seed = ImmutableDictionary<Type, int>.Empty
            .Add(typeof(string), 3)
            .Add(typeof(int), 0);

        FrontComposerCapabilityDiscoveryState next = CapabilityDiscoveryReducers.ReduceBadgeCountsSeeded(
            initial, new BadgeCountsSeededAction(seed));

        next.Counts.ShouldBe(seed);
        next.HydrationState.ShouldBe(CapabilityDiscoveryHydrationState.Seeded);
        next.SeenCapabilities.ShouldBeSameAs(initial.SeenCapabilities);
    }

    [Fact]
    public void ReduceBadgeCountChanged_AppliesSingleKeyUpdate_LeavesHydrationStateUntouched() {
        FrontComposerCapabilityDiscoveryState seeded = FrontComposerCapabilityDiscoveryState.Empty with {
            Counts = ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 1),
            HydrationState = CapabilityDiscoveryHydrationState.Seeded,
        };

        FrontComposerCapabilityDiscoveryState next = CapabilityDiscoveryReducers.ReduceBadgeCountChanged(
            seeded, new BadgeCountChangedAction(typeof(string), 5));

        next.Counts[typeof(string)].ShouldBe(5);
        next.HydrationState.ShouldBe(CapabilityDiscoveryHydrationState.Seeded);
    }

    [Fact]
    public void ReduceBadgeCountChanged_BeforeSeed_PromotesHydrationStateToSeeding() {
        FrontComposerCapabilityDiscoveryState initial = FrontComposerCapabilityDiscoveryState.Empty;

        FrontComposerCapabilityDiscoveryState next = CapabilityDiscoveryReducers.ReduceBadgeCountChanged(
            initial, new BadgeCountChangedAction(typeof(string), 2));

        next.Counts[typeof(string)].ShouldBe(2);
        next.HydrationState.ShouldBe(CapabilityDiscoveryHydrationState.Seeding);
    }

    [Fact]
    public void ReduceCapabilityVisited_AddsCapabilityIdToSeenSet() {
        FrontComposerCapabilityDiscoveryState initial = FrontComposerCapabilityDiscoveryState.Empty;

        FrontComposerCapabilityDiscoveryState next = CapabilityDiscoveryReducers.ReduceCapabilityVisited(
            initial, new CapabilityVisitedAction("bc:Counter"));

        next.SeenCapabilities.ShouldContain("bc:Counter");
    }

    [Fact]
    public void ReduceCapabilityVisited_IsIdempotent_ReturnsSameSetReference() {
        FrontComposerCapabilityDiscoveryState seeded = FrontComposerCapabilityDiscoveryState.Empty with {
            SeenCapabilities = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal).Add("bc:Counter"),
        };

        FrontComposerCapabilityDiscoveryState next = CapabilityDiscoveryReducers.ReduceCapabilityVisited(
            seeded, new CapabilityVisitedAction("bc:Counter"));

        next.SeenCapabilities.ShouldBeSameAs(seeded.SeenCapabilities);
    }

    [Fact]
    public void ReduceSeenCapabilitiesHydrated_ReplacesSeenSetWholesale() {
        FrontComposerCapabilityDiscoveryState initial = FrontComposerCapabilityDiscoveryState.Empty with {
            SeenCapabilities = ImmutableHashSet<string>.Empty.WithComparer(StringComparer.Ordinal).Add("bc:Stale"),
        };
        ImmutableHashSet<string> hydrated = ImmutableHashSet<string>.Empty
            .WithComparer(StringComparer.Ordinal)
            .Add("bc:Counter")
            .Add("proj:Counter:Counter.Domain.CounterProjection");

        FrontComposerCapabilityDiscoveryState next = CapabilityDiscoveryReducers.ReduceSeenCapabilitiesHydrated(
            initial, new SeenCapabilitiesHydratedAction(hydrated));

        next.SeenCapabilities.ShouldBe(hydrated);
        next.SeenCapabilities.ShouldNotContain("bc:Stale");
    }

    [Fact]
    public void BadgeCountMirror_SeededFollowedByOnNext_NoLostUpdate() {
        // Murat — explicit guard against the seed-vs-notifier race: a Changed action that lands
        // after the Seeded action MUST win for that type, not get clobbered by Seeded.
        FrontComposerCapabilityDiscoveryState initial = FrontComposerCapabilityDiscoveryState.Empty;

        FrontComposerCapabilityDiscoveryState seeded = CapabilityDiscoveryReducers.ReduceBadgeCountsSeeded(
            initial,
            new BadgeCountsSeededAction(
                ImmutableDictionary<Type, int>.Empty.Add(typeof(string), 7)));
        FrontComposerCapabilityDiscoveryState changed = CapabilityDiscoveryReducers.ReduceBadgeCountChanged(
            seeded, new BadgeCountChangedAction(typeof(string), 12));

        changed.Counts[typeof(string)].ShouldBe(12);
        changed.HydrationState.ShouldBe(CapabilityDiscoveryHydrationState.Seeded);
    }

    [Fact]
    public void BadgeCountMirror_OnNextFollowedBySeeded_NoLostUpdate() {
        // Guard the inverse race too: a live update that lands while the seed is still in flight
        // must survive the later snapshot publish for that same type.
        FrontComposerCapabilityDiscoveryState initial = FrontComposerCapabilityDiscoveryState.Empty;

        FrontComposerCapabilityDiscoveryState changed = CapabilityDiscoveryReducers.ReduceBadgeCountChanged(
            initial, new BadgeCountChangedAction(typeof(string), 12));
        FrontComposerCapabilityDiscoveryState seeded = CapabilityDiscoveryReducers.ReduceBadgeCountsSeeded(
            changed,
            new BadgeCountsSeededAction(
                ImmutableDictionary<Type, int>.Empty
                    .Add(typeof(string), 7)
                    .Add(typeof(int), 3)));

        seeded.Counts[typeof(string)].ShouldBe(12);
        seeded.Counts[typeof(int)].ShouldBe(3);
        seeded.HydrationState.ShouldBe(CapabilityDiscoveryHydrationState.Seeded);
    }
}
