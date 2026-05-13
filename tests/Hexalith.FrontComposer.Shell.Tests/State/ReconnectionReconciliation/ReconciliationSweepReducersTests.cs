using System.Collections.Immutable;

using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.ReconnectionReconciliation;

public sealed class ReconciliationSweepReducersTests {
    private static readonly DateTimeOffset DispatchedAt = new(2026, 4, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void MarkReconciliationSweepAction_AddsOneMarkerPerDistinctLaneForEpoch() {
        DateTimeOffset expires = DispatchedAt.AddSeconds(1);

        ReconciliationSweepState next = ReconciliationSweepReducers.ReduceMark(
            new ReconciliationSweepState(),
            new MarkReconciliationSweepAction(12, ["orders", "orders", "customers"], expires, DispatchedAt));

        next.MarkersByViewKey.Keys.OrderBy(static x => x, StringComparer.Ordinal).ShouldBe(["customers", "orders"]);
        next.MarkersByViewKey["orders"].Epoch.ShouldBe(12);
        next.MarkersByViewKey["orders"].ExpiresAt.ShouldBe(expires);
    }

    [Fact]
    public void MarkReconciliationSweepAction_CapsMarkersWhenClearSchedulingFallsBehind() {
        DateTimeOffset expires = DispatchedAt.AddSeconds(1);
        string[] lanes = Enumerable.Range(0, 600).Select(static i => $"orders-{i:000}").ToArray();

        ReconciliationSweepState next = ReconciliationSweepReducers.ReduceMark(
            new ReconciliationSweepState(),
            new MarkReconciliationSweepAction(12, lanes, expires, DispatchedAt));

        next.MarkersByViewKey.Count.ShouldBe(512);
    }

    [Fact]
    public void MarkReconciliationSweepAction_SkipsAlreadyExpiredMarkers() {
        // P20 (Story 11.7 code review DN-1) — markers with ExpiresAt <= Now cannot produce
        // any user-visible state; they must not consume cap slots.
        DateTimeOffset expiresAtNow = DispatchedAt;
        DateTimeOffset expiresBeforeNow = DispatchedAt.AddSeconds(-5);

        ReconciliationSweepState state = ReconciliationSweepReducers.ReduceMark(
            new ReconciliationSweepState(),
            new MarkReconciliationSweepAction(12, ["orders"], expiresAtNow, DispatchedAt));
        state.MarkersByViewKey.ShouldBeEmpty();

        state = ReconciliationSweepReducers.ReduceMark(
            new ReconciliationSweepState(),
            new MarkReconciliationSweepAction(12, ["orders"], expiresBeforeNow, DispatchedAt));
        state.MarkersByViewKey.ShouldBeEmpty();
    }

    [Fact]
    public void MarkReconciliationSweepAction_AtCap_EvictsEarliestExpiringMarker_WhenIncomingExpiresLater() {
        // P21 (Story 11.7 code review DN-1) — when the cap is saturated, a fresh marker
        // whose ExpiresAt is later than the earliest existing marker should evict that
        // earliest marker rather than be silently dropped.
        ImmutableDictionary<string, ReconciliationSweepMarker> initial = ImmutableDictionary<string, ReconciliationSweepMarker>.Empty;
        for (int i = 0; i < 512; i++) {
            initial = initial.Add($"old-{i:000}", new ReconciliationSweepMarker(1, DispatchedAt.AddSeconds(10 + i)));
        }

        ReconciliationSweepState seeded = new() { MarkersByViewKey = initial };

        DateTimeOffset freshExpires = DispatchedAt.AddSeconds(60);
        ReconciliationSweepState next = ReconciliationSweepReducers.ReduceMark(
            seeded,
            new MarkReconciliationSweepAction(2, ["fresh"], freshExpires, DispatchedAt));

        next.MarkersByViewKey.Count.ShouldBe(512);
        next.MarkersByViewKey.ContainsKey("fresh").ShouldBeTrue();
        next.MarkersByViewKey["fresh"].ExpiresAt.ShouldBe(freshExpires);
        // The earliest-expiring marker was evicted.
        next.MarkersByViewKey.ContainsKey("old-000").ShouldBeFalse();
    }

    [Fact]
    public void MarkReconciliationSweepAction_AtCap_DropsIncomingMarker_WhenIncomingExpiresBeforeEverythingHeld() {
        ImmutableDictionary<string, ReconciliationSweepMarker> initial = ImmutableDictionary<string, ReconciliationSweepMarker>.Empty;
        for (int i = 0; i < 512; i++) {
            initial = initial.Add($"old-{i:000}", new ReconciliationSweepMarker(1, DispatchedAt.AddSeconds(60 + i)));
        }

        ReconciliationSweepState seeded = new() { MarkersByViewKey = initial };

        DateTimeOffset shortLived = DispatchedAt.AddSeconds(5);
        ReconciliationSweepState next = ReconciliationSweepReducers.ReduceMark(
            seeded,
            new MarkReconciliationSweepAction(2, ["short"], shortLived, DispatchedAt));

        next.MarkersByViewKey.Count.ShouldBe(512);
        next.MarkersByViewKey.ContainsKey("short").ShouldBeFalse();
        next.MarkersByViewKey.ContainsKey("old-000").ShouldBeTrue();
    }

    [Fact]
    public void ClearExpiredReconciliationSweeps_RemovesExpiredMarkersOnly() {
        DateTimeOffset now = new(2026, 4, 26, 12, 0, 1, TimeSpan.Zero);
        ReconciliationSweepState state = new() {
            MarkersByViewKey = ImmutableDictionary<string, ReconciliationSweepMarker>.Empty
                .Add("expired", new ReconciliationSweepMarker(1, now))
                .Add("active", new ReconciliationSweepMarker(1, now.AddMilliseconds(1))),
        };

        ReconciliationSweepState next = ReconciliationSweepReducers.ReduceClearExpired(
            state,
            new ClearExpiredReconciliationSweepsAction(now));

        next.MarkersByViewKey.ContainsKey("expired").ShouldBeFalse();
        next.MarkersByViewKey.ContainsKey("active").ShouldBeTrue();
    }
}
