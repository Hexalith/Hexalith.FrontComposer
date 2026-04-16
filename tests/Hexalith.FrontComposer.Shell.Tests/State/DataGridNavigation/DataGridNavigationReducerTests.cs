using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 2-2 Task 6.4 — 11 tests for the reducer-only DataGridNavigation feature
/// (AC7, Decisions D30, D33). Effects are deferred to Story 4.3.
/// </summary>
public class DataGridNavigationReducerTests {
    // Shared fixtures ---------------------------------------------------------
    private static GridViewSnapshot Snap(double scroll, DateTimeOffset at)
        => new(scroll, ImmutableDictionary<string, string>.Empty, null, false, null, null, at);

    private static DataGridNavigationState Empty()
        => new(ImmutableDictionary<string, GridViewSnapshot>.Empty);

    // 1. Capture adds snapshot
    [Fact]
    public void CaptureGridStateAction_AddsSnapshot() {
        DataGridNavigationState state = Empty();
        GridViewSnapshot snap = Snap(100, DateTimeOffset.UtcNow);
        DataGridNavigationState result = DataGridNavigationReducers.ReduceCapture(state, new CaptureGridStateAction("v1", snap));
        result.ViewStates.Count.ShouldBe(1);
        result.ViewStates["v1"].ShouldBe(snap);
    }

    // 2. Capture overwrites existing snapshot for same viewKey
    [Fact]
    public void CaptureGridStateAction_OverwritesExistingSnapshot() {
        GridViewSnapshot first = Snap(100, DateTimeOffset.UtcNow);
        GridViewSnapshot second = Snap(200, DateTimeOffset.UtcNow.AddSeconds(5));
        DataGridNavigationState state = new(ImmutableDictionary<string, GridViewSnapshot>.Empty.Add("v1", first));
        DataGridNavigationState result = DataGridNavigationReducers.ReduceCapture(state, new CaptureGridStateAction("v1", second));
        result.ViewStates["v1"].ShouldBe(second);
        result.ViewStates.Count.ShouldBe(1);
    }

    // 3. Restore is pure no-op on state (Decision D30)
    [Fact]
    public void RestoreGridStateAction_IsPureNoOp() {
        GridViewSnapshot snap = Snap(100, DateTimeOffset.UtcNow);
        DataGridNavigationState withSnap = new(ImmutableDictionary<string, GridViewSnapshot>.Empty.Add("v1", snap));

        DataGridNavigationReducers.ReduceRestore(Empty(), new RestoreGridStateAction("missing")).ShouldBe(Empty());
        DataGridNavigationReducers.ReduceRestore(withSnap, new RestoreGridStateAction("v1")).ShouldBe(withSnap);
    }

    // 4. Clear removes snapshot
    [Fact]
    public void ClearGridStateAction_RemovesSnapshot() {
        DataGridNavigationState state = new(ImmutableDictionary<string, GridViewSnapshot>.Empty.Add("v1", Snap(1, DateTimeOffset.UtcNow)));
        DataGridNavigationState result = DataGridNavigationReducers.ReduceClear(state, new ClearGridStateAction("v1"));
        result.ViewStates.Count.ShouldBe(0);
    }

    // 5. PruneExpired removes snapshots strictly before threshold
    [Fact]
    public void PruneExpiredAction_RemovesSnapshotsStrictlyBeforeThreshold() {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        DataGridNavigationState state = new(
            ImmutableDictionary<string, GridViewSnapshot>.Empty
                .Add("old", Snap(1, now.AddHours(-25)))
                .Add("fresh", Snap(2, now)));
        DataGridNavigationState result = DataGridNavigationReducers.ReducePruneExpired(state, new PruneExpiredAction(now.AddHours(-24)));
        result.ViewStates.Count.ShouldBe(1);
        result.ViewStates.ShouldContainKey("fresh");
    }

    // 6. PruneExpired keeps snapshots at/after threshold
    [Fact]
    public void PruneExpiredAction_KeepsSnapshotsAtOrAfterThreshold() {
        DateTimeOffset threshold = DateTimeOffset.UtcNow;
        DataGridNavigationState state = new(
            ImmutableDictionary<string, GridViewSnapshot>.Empty.Add("boundary", Snap(1, threshold)));
        DataGridNavigationState result = DataGridNavigationReducers.ReducePruneExpired(state, new PruneExpiredAction(threshold));
        result.ViewStates.Count.ShouldBe(1);
    }

    // 7. Per-view isolation
    [Fact]
    public void Capture_PerViewIsolation_TwoViewKeysCoexist() {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        GridViewSnapshot s1 = Snap(100, now);
        GridViewSnapshot s2 = Snap(200, now.AddSeconds(1));
        DataGridNavigationState state = DataGridNavigationReducers.ReduceCapture(Empty(), new CaptureGridStateAction("v1", s1));
        state = DataGridNavigationReducers.ReduceCapture(state, new CaptureGridStateAction("v2", s2));
        state.ViewStates.Count.ShouldBe(2);
        state.ViewStates["v1"].ShouldBe(s1);
        state.ViewStates["v2"].ShouldBe(s2);
    }

    // 8. IEquatable — GridViewSnapshot structural equality
    [Fact]
    public void GridViewSnapshot_IEquatable_StructuralEquality() {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        GridViewSnapshot a = Snap(100, now);
        GridViewSnapshot b = Snap(100, now);
        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    // 9. IEquatable — DataGridNavigationState dictionary content equality
    [Fact]
    public void DataGridNavigationState_IEquatable_DictionaryContentEquality() {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        ImmutableDictionary<string, GridViewSnapshot> dictA = ImmutableDictionary<string, GridViewSnapshot>.Empty.Add("v1", Snap(100, now));
        ImmutableDictionary<string, GridViewSnapshot> dictB = ImmutableDictionary<string, GridViewSnapshot>.Empty.Add("v1", Snap(100, now));
        DataGridNavigationState a = new(dictA);
        DataGridNavigationState b = new(dictB);

        // NOTE: ImmutableDictionary equality is reference-based in .NET; record equality comparer
        // uses SequenceEqual-like semantics via EqualityComparer<T>.Default on properties, which for
        // ImmutableDictionary returns reference equality. So two separately-built dicts are not equal.
        // Verify the documented behavior so Story 4.3 effects know to compare via ViewStates.SequenceEqual.
        a.ShouldNotBe(b);

        DataGridNavigationState sameRef = new(dictA);
        a.ShouldBe(sameRef);
    }

    // 10. LRU eviction when ViewStates exceeds cap
    [Fact]
    public void CaptureGridStateAction_ExceedsCap_EvictsOldestCapturedAt() {
        DateTimeOffset baseT = DateTimeOffset.UtcNow;
        DataGridNavigationState state = new(ImmutableDictionary<string, GridViewSnapshot>.Empty, Cap: 2);
        state = DataGridNavigationReducers.ReduceCapture(state, new CaptureGridStateAction("old", Snap(1, baseT)));
        state = DataGridNavigationReducers.ReduceCapture(state, new CaptureGridStateAction("mid", Snap(2, baseT.AddSeconds(1))));
        state = DataGridNavigationReducers.ReduceCapture(state, new CaptureGridStateAction("new", Snap(3, baseT.AddSeconds(2))));
        state.ViewStates.Count.ShouldBe(2);
        state.ViewStates.ShouldNotContainKey("old");
        state.ViewStates.ShouldContainKey("mid");
        state.ViewStates.ShouldContainKey("new");
    }

    // 11. Cap configurability — the reducer honors the state-embedded cap
    [Fact]
    public void CaptureGridStateAction_CapConfigurable_RespectsStateCap() {
        DateTimeOffset baseT = DateTimeOffset.UtcNow;
        DataGridNavigationState state = new(ImmutableDictionary<string, GridViewSnapshot>.Empty, Cap: 1);
        state = DataGridNavigationReducers.ReduceCapture(state, new CaptureGridStateAction("a", Snap(1, baseT)));
        state = DataGridNavigationReducers.ReduceCapture(state, new CaptureGridStateAction("b", Snap(2, baseT.AddSeconds(1))));
        state.ViewStates.Count.ShouldBe(1);
        state.ViewStates.ShouldContainKey("b");
    }
}
