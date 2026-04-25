#pragma warning disable CA2007
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 4-4 T4.4 / D3 / D4 / D6 / D7 — PURE reducer tests for <see cref="LoadedPageReducers"/>
/// and <see cref="VirtualizationViewStateReducers"/>. None of these tests assert on chained
/// dispatches: reducers never dispatch (persistence effects do).
/// </summary>
public sealed class LoadPageReducerTests {
    private const string ViewKey = "acme:OrdersProjection";

    private static LoadedPageReducers MakeReducers(int maxCachedPages = 200) {
        IOptionsMonitor<FcShellOptions> monitor = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        monitor.CurrentValue.Returns(new FcShellOptions { MaxCachedPages = maxCachedPages });
        return new LoadedPageReducers(monitor, NullLogger<LoadedPageReducers>.Instance);
    }

    private static LoadPageAction MakeLoadPage(string viewKey, int skip, TaskCompletionSource<object> tcs) =>
        new(viewKey, skip, take: 20, ImmutableDictionary<string, string>.Empty, null, false, null, tcs, CancellationToken.None);

    [Fact]
    public async Task ReduceLoadPageSucceeded_HappyPath_WritesPageTotalElapsedAndResolvesTcs() {
        LoadedPageReducers reducers = MakeReducers();
        TaskCompletionSource<object> tcs = new();

        LoadedPageState mid = LoadedPageReducers.ReduceLoadPage(new LoadedPageState(), MakeLoadPage(ViewKey, 0, tcs));

        IReadOnlyList<object> items = new object[] { 1, 2, 3 };
        LoadedPageState after = reducers.ReduceLoadPageSucceeded(
            mid,
            new LoadPageSucceededAction(ViewKey, 0, items, totalCount: 42, elapsedMs: 17));

        after.PagesByKey[(ViewKey, 0)].ShouldBe(items);
        after.TotalCountByKey[ViewKey].ShouldBe(42);
        after.LastElapsedMsByKey[ViewKey].ShouldBe(17L);
        after.PendingCompletionsByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        object resolved = await tcs.Task;
        resolved.ShouldBe(items);
    }

    [Fact]
    public async Task ReduceLoadPageFailed_ResolvesTcsWithException_AndRemovesEntry() {
        LoadedPageReducers reducers = MakeReducers();
        TaskCompletionSource<object> tcs = new();
        LoadedPageState mid = LoadedPageReducers.ReduceLoadPage(new LoadedPageState(), MakeLoadPage(ViewKey, 0, tcs));

        LoadedPageState after = LoadedPageReducers.ReduceLoadPageFailed(
            mid, new LoadPageFailedAction(ViewKey, 0, "boom"));

        after.PendingCompletionsByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(async () => await tcs.Task);
        ex.Message.ShouldBe("boom");
        _ = reducers;
    }

    [Fact]
    public async Task ReduceLoadPageCancelled_ResolvesTcsAsCanceled_AndRemovesEntry() {
        TaskCompletionSource<object> tcs = new();
        LoadedPageState mid = LoadedPageReducers.ReduceLoadPage(new LoadedPageState(), MakeLoadPage(ViewKey, 0, tcs));

        LoadedPageState after = LoadedPageReducers.ReduceLoadPageCancelled(
            mid, new LoadPageCancelledAction(ViewKey, 0));

        after.PendingCompletionsByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await tcs.Task);
    }

    [Fact]
    public async Task ReduceClearPendingPages_SweepsAllEntriesForViewKey() {
        TaskCompletionSource<object> tcs0 = new();
        TaskCompletionSource<object> tcs20 = new();
        TaskCompletionSource<object> tcsOther = new();

        LoadedPageState mid = new LoadedPageState();
        mid = LoadedPageReducers.ReduceLoadPage(mid, MakeLoadPage(ViewKey, 0, tcs0));
        mid = LoadedPageReducers.ReduceLoadPage(mid, MakeLoadPage(ViewKey, 20, tcs20));
        mid = LoadedPageReducers.ReduceLoadPage(mid, MakeLoadPage("other:Proj", 0, tcsOther));

        LoadedPageState after = LoadedPageReducers.ReduceClearPendingPages(
            mid, new ClearPendingPagesAction(ViewKey));

        after.PendingCompletionsByKey.Count.ShouldBe(1);
        after.PendingCompletionsByKey.ContainsKey(("other:Proj", 0)).ShouldBeTrue();
        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await tcs0.Task);
        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await tcs20.Task);
        tcsOther.Task.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void ColumnVisibilityChangedReducer_UpdatesHiddenCsv_NoChainedDispatch() {
        DataGridNavigationState state = new(ImmutableDictionary<string, GridViewSnapshot>.Empty, Cap: 50);

        DataGridNavigationState afterHide = VirtualizationViewStateReducers.ReduceColumnVisibilityChanged(
            state, new ColumnVisibilityChangedAction(ViewKey, "Created", isVisible: false));

        afterHide.ViewStates[ViewKey].Filters[VirtualizationReservedKeys.HiddenColumnsKey].ShouldBe("Created");

        DataGridNavigationState afterAddSecond = VirtualizationViewStateReducers.ReduceColumnVisibilityChanged(
            afterHide, new ColumnVisibilityChangedAction(ViewKey, "Priority", isVisible: false));
        afterAddSecond.ViewStates[ViewKey].Filters[VirtualizationReservedKeys.HiddenColumnsKey].ShouldBe("Created,Priority");

        DataGridNavigationState afterReveal = VirtualizationViewStateReducers.ReduceColumnVisibilityChanged(
            afterAddSecond, new ColumnVisibilityChangedAction(ViewKey, "Created", isVisible: true));
        afterReveal.ViewStates[ViewKey].Filters[VirtualizationReservedKeys.HiddenColumnsKey].ShouldBe("Priority");

        DataGridNavigationState afterRemoveLast = VirtualizationViewStateReducers.ReduceColumnVisibilityChanged(
            afterReveal, new ColumnVisibilityChangedAction(ViewKey, "Priority", isVisible: true));
        afterRemoveLast.ViewStates[ViewKey].Filters[VirtualizationReservedKeys.HiddenColumnsKey].ShouldBe(string.Empty);
    }

    [Fact]
    public void LaneLatch_DoesNotOscillate_OnSecondaryLoadPageDispatches() {
        TaskCompletionSource<object> first = new();
        TaskCompletionSource<object> second = new();
        LoadedPageState state = new LoadedPageState();

        // First dispatch latches server-side lane.
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, first));
        state.LaneByKey[ViewKey].ShouldBe(VirtualizationLane.ServerSide);

        // Subsequent dispatches preserve the initial latch.
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 20, second));
        state.LaneByKey[ViewKey].ShouldBe(VirtualizationLane.ServerSide);
        state.LaneByKey.Count.ShouldBe(1);
    }
}
