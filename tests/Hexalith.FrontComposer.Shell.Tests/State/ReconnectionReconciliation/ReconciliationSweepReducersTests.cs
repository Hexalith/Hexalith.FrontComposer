using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.ReconnectionReconciliation;

public sealed class ReconciliationSweepReducersTests {
    [Fact]
    public void MarkReconciliationSweepAction_AddsOneMarkerPerDistinctLaneForEpoch() {
        DateTimeOffset expires = new(2026, 4, 26, 12, 0, 1, TimeSpan.Zero);

        ReconciliationSweepState next = ReconciliationSweepReducers.ReduceMark(
            new ReconciliationSweepState(),
            new MarkReconciliationSweepAction(12, ["orders", "orders", "customers"], expires));

        next.MarkersByViewKey.Keys.OrderBy(static x => x, StringComparer.Ordinal).ShouldBe(["customers", "orders"]);
        next.MarkersByViewKey["orders"].Epoch.ShouldBe(12);
        next.MarkersByViewKey["orders"].ExpiresAt.ShouldBe(expires);
    }

    [Fact]
    public void ClearExpiredReconciliationSweeps_RemovesExpiredMarkersOnly() {
        DateTimeOffset now = new(2026, 4, 26, 12, 0, 1, TimeSpan.Zero);
        ReconciliationSweepState state = new() {
            MarkersByViewKey = System.Collections.Immutable.ImmutableDictionary<string, ReconciliationSweepMarker>.Empty
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
