// Story 3-4 Task 10.4 (D6 / D8 / D11 / D20 — AC2 / AC3 / AC5).
using System.Collections.Immutable;

using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.CommandPalette;

public class CommandPaletteReducerTests {
    private static FrontComposerCommandPaletteState InitialState(bool isOpen = false)
        => new(IsOpen: isOpen,
            Query: string.Empty,
            Results: ImmutableArray<PaletteResult>.Empty,
            RecentRouteUrls: ImmutableArray<string>.Empty,
            SelectedIndex: 0,
            LoadState: PaletteLoadState.Idle);

    [Fact]
    public void ReducePaletteOpened_FlipsIsOpenTrue() {
        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteOpened(
            InitialState(),
            new PaletteOpenedAction("c1"));
        next.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public void ReducePaletteClosed_ResetsTransientFields() {
        FrontComposerCommandPaletteState s = InitialState(isOpen: true) with {
            Query = "cou",
            Results = [new PaletteResult(PaletteResultCategory.Projection, "X", "BC", "/x", null, 100, false)],
            SelectedIndex = 0,
        };

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteClosed(s, new PaletteClosedAction("c1"));

        next.IsOpen.ShouldBeFalse();
        next.Query.ShouldBeEmpty();
        next.Results.IsEmpty.ShouldBeTrue();
    }

    [Fact]
    public void ReducePaletteQueryChanged_UpdatesQueryAndFlipsToSearching() {
        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteQueryChanged(
            InitialState(isOpen: true),
            new PaletteQueryChangedAction("c1", "cou"));

        next.Query.ShouldBe("cou");
        next.LoadState.ShouldBe(PaletteLoadState.Searching);
    }

    [Fact]
    public void ReducePaletteResultsComputed_AssignsResults_WhenPaletteOpen() {
        ImmutableArray<PaletteResult> results = [new PaletteResult(PaletteResultCategory.Projection, "X", "BC", "/x", null, 100, false)];

        // Post-DN4 guard requires state.Query == action.Query (stale-computation race defence).
        FrontComposerCommandPaletteState state = InitialState(isOpen: true) with { Query = "cou" };
        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteResultsComputed(
            state,
            new PaletteResultsComputedAction("cou", results));

        next.Results.Length.ShouldBe(1);
        next.LoadState.ShouldBe(PaletteLoadState.Ready);
    }

    [Fact]
    public void OnPaletteResultsComputed_WhenPaletteClosed_NoOps() {
        ImmutableArray<PaletteResult> results = [new PaletteResult(PaletteResultCategory.Projection, "X", "BC", "/x", null, 100, false)];
        FrontComposerCommandPaletteState closed = InitialState(isOpen: false);

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteResultsComputed(
            closed,
            new PaletteResultsComputedAction("cou", results));

        next.ShouldBeSameAs(closed);
    }

    [Fact]
    public void ReducePaletteSelectionMoved_ClampsToBounds() {
        ImmutableArray<PaletteResult> results = [
            new PaletteResult(PaletteResultCategory.Projection, "A", "BC", "/a", null, 100, false),
            new PaletteResult(PaletteResultCategory.Projection, "B", "BC", "/b", null, 50, false),
        ];
        FrontComposerCommandPaletteState s = InitialState(isOpen: true) with { Results = results, SelectedIndex = 1 };

        FrontComposerCommandPaletteState upBeyond = CommandPaletteReducers.ReducePaletteSelectionMoved(s, new PaletteSelectionMovedAction(+5));
        upBeyond.SelectedIndex.ShouldBe(1); // clamped at last index

        FrontComposerCommandPaletteState downBeyond = CommandPaletteReducers.ReducePaletteSelectionMoved(s with { SelectedIndex = 0 }, new PaletteSelectionMovedAction(-5));
        downBeyond.SelectedIndex.ShouldBe(0); // clamped at zero
    }

    [Fact]
    public void ReducePaletteSelectionMoved_Empty_NoOp() {
        FrontComposerCommandPaletteState s = InitialState(isOpen: true);
        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteSelectionMoved(s, new PaletteSelectionMovedAction(+1));
        next.ShouldBeSameAs(s);
    }

    [Fact]
    public void ReducePaletteHydrated_AssignsRingBuffer() {
        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteHydrated(
            InitialState(),
            new PaletteHydratedAction(["/counter", "/orders"]));

        next.RecentRouteUrls.Length.ShouldBe(2);
    }

    [Fact]
    public void ReduceRecentRouteVisited_PrependsAndCapsAtFive() {
        FrontComposerCommandPaletteState s = InitialState() with {
            RecentRouteUrls = ["/a", "/b", "/c", "/d", "/e"],
        };

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReduceRecentRouteVisited(s, new RecentRouteVisitedAction("/f"));

        next.RecentRouteUrls.Length.ShouldBe(5);
        next.RecentRouteUrls[0].ShouldBe("/f");
        next.RecentRouteUrls.ShouldNotContain("/e");
    }

    [Fact]
    public void ReduceRecentRouteVisited_DeDuplicatesOnExactMatch() {
        FrontComposerCommandPaletteState s = InitialState() with {
            RecentRouteUrls = ["/a", "/b", "/c"],
        };

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReduceRecentRouteVisited(s, new RecentRouteVisitedAction("/b"));

        next.RecentRouteUrls.Length.ShouldBe(3);
        next.RecentRouteUrls[0].ShouldBe("/b");
    }

    [Fact]
    public void ReducePaletteResultsComputed_PreservesSelectedIndex_WhenClampableIntoNewResults() {
        // DN4 — user arrow-selected row 2; new result set still has 4 rows, selection preserved.
        ImmutableArray<PaletteResult> old = [
            new PaletteResult(PaletteResultCategory.Projection, "A", "BC", "/a", null, 100, false),
            new PaletteResult(PaletteResultCategory.Projection, "B", "BC", "/b", null, 90, false),
            new PaletteResult(PaletteResultCategory.Projection, "C", "BC", "/c", null, 80, false),
            new PaletteResult(PaletteResultCategory.Projection, "D", "BC", "/d", null, 70, false),
        ];
        ImmutableArray<PaletteResult> fresh = [
            new PaletteResult(PaletteResultCategory.Projection, "A'", "BC", "/a", null, 100, false),
            new PaletteResult(PaletteResultCategory.Projection, "B'", "BC", "/b", null, 90, false),
            new PaletteResult(PaletteResultCategory.Projection, "C'", "BC", "/c", null, 80, false),
            new PaletteResult(PaletteResultCategory.Projection, "D'", "BC", "/d", null, 70, false),
        ];
        FrontComposerCommandPaletteState state = InitialState(isOpen: true) with {
            Query = "same",
            Results = old,
            SelectedIndex = 2,
        };

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteResultsComputed(
            state,
            new PaletteResultsComputedAction("same", fresh));

        next.SelectedIndex.ShouldBe(2);
    }

    [Fact]
    public void ReducePaletteResultsComputed_ClampsSelectedIndex_WhenNewResultsShrink() {
        ImmutableArray<PaletteResult> old = [
            new PaletteResult(PaletteResultCategory.Projection, "A", "BC", "/a", null, 100, false),
            new PaletteResult(PaletteResultCategory.Projection, "B", "BC", "/b", null, 90, false),
            new PaletteResult(PaletteResultCategory.Projection, "C", "BC", "/c", null, 80, false),
        ];
        ImmutableArray<PaletteResult> fresh = [
            new PaletteResult(PaletteResultCategory.Projection, "A'", "BC", "/a", null, 100, false),
        ];
        FrontComposerCommandPaletteState state = InitialState(isOpen: true) with {
            Query = "same",
            Results = old,
            SelectedIndex = 2,
        };

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteResultsComputed(
            state,
            new PaletteResultsComputedAction("same", fresh));

        next.SelectedIndex.ShouldBe(0);
    }

    [Fact]
    public void ReducePaletteResultsComputed_RejectsStaleQueryResults() {
        // Debounce CTS race — late-arriving results for query "a" must not land on state now bound
        // to query "ab".
        ImmutableArray<PaletteResult> staleResults = [
            new PaletteResult(PaletteResultCategory.Projection, "A", "BC", "/a", null, 100, false),
        ];
        FrontComposerCommandPaletteState state = InitialState(isOpen: true) with { Query = "ab" };

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteResultsComputed(
            state,
            new PaletteResultsComputedAction("a", staleResults));

        next.ShouldBeSameAs(state);
    }

    [Fact]
    public void ReducePaletteHydrated_GuardsAgainstOverwritingRecentRoutes() {
        // Hydrate-vs-first-visit race — if the user visited a route before storage completed,
        // do not let the (now-stale) hydrate payload overwrite the just-visited URL.
        FrontComposerCommandPaletteState state = InitialState() with {
            RecentRouteUrls = ["/just-visited"],
        };

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteHydrated(
            state,
            new PaletteHydratedAction(["/stale-1", "/stale-2"]));

        next.RecentRouteUrls[0].ShouldBe("/just-visited");
        next.RecentRouteUrls.Length.ShouldBe(1);
    }

    [Fact]
    public void ReducePaletteHydrated_CapsAtRingBufferCap_ForTamperedBlobs() {
        FrontComposerCommandPaletteState state = InitialState();

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteHydrated(
            state,
            new PaletteHydratedAction(["/a", "/b", "/c", "/d", "/e", "/f", "/g"]));

        next.RecentRouteUrls.Length.ShouldBe(FrontComposerCommandPaletteState.RingBufferCap);
    }

    [Fact]
    public void ReducePaletteScopeChanged_ClearsRecentRouteUrls() {
        // DN2 — per-user persistence scope changed; in-memory buffer must be cleared before the
        // hydrate effect repopulates from the new scope's storage partition.
        FrontComposerCommandPaletteState state = InitialState() with {
            RecentRouteUrls = ["/user-a-route-1", "/user-a-route-2"],
        };

        FrontComposerCommandPaletteState next = CommandPaletteReducers.ReducePaletteScopeChanged(
            state,
            new PaletteScopeChangedAction());

        next.RecentRouteUrls.IsEmpty.ShouldBeTrue();
    }
}
