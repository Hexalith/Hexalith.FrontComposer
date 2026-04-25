#pragma warning disable CA2007
using System.Collections.Immutable;
using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 4-4 T4.6 — reducer+effect round-trip assertions: ScrollCapturedAction → debounced
/// CaptureGridStateAction; ColumnVisibilityChangedAction → immediate CaptureGridStateAction with
/// CSV packed into <c>GridViewSnapshot.Filters[__hidden]</c>; hydration from a fresh snapshot
/// restores both surfaces (scroll is clamped per D5, hidden columns are preserved).
/// </summary>
public sealed class ScrollAndColumnVisibilityRoundTripTests {
    private const string ViewKey = "acme:OrdersProjection";

    [Fact]
    public async Task Scroll_Captured_DebouncesAndChainsCaptureWithCurrentScrollTop() {
        DataGridNavigationState snapshot = new(
            ImmutableDictionary<string, GridViewSnapshot>.Empty.Add(ViewKey, new GridViewSnapshot(
                scrollTop: 0,
                filters: ImmutableDictionary<string, string>.Empty,
                sortColumn: null, sortDescending: false,
                expandedRowId: null, selectedRowId: null,
                capturedAt: DateTimeOffset.UtcNow)),
            Cap: 50);

        // Step 1: ScrollCapturedReducer mutates the snapshot IN state.
        snapshot = snapshot with {
            ViewStates = snapshot.ViewStates.SetItem(
                ViewKey,
                VirtualizationViewStateReducers.ReduceScrollCaptured(
                    snapshot, new ScrollCapturedAction(ViewKey, 500)).ViewStates[ViewKey]),
        };
        snapshot.ViewStates[ViewKey].ScrollTop.ShouldBe(500);

        // Step 2: persistence effect debounces then chains CaptureGridStateAction.
        IState<DataGridNavigationState> state = Substitute.For<IState<DataGridNavigationState>>();
        state.Value.Returns(snapshot);
        FakeTimeProvider time = new();
        ScrollPersistenceEffect effect = new(state, time);
        RecordingDispatcher dispatcher = new();

        Task pending = effect.HandleScrollCapturedAsync(new ScrollCapturedAction(ViewKey, 500), dispatcher);
        time.Advance(TimeSpan.FromMilliseconds(150));
        await pending;

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.ViewKey.ShouldBe(ViewKey);
        captured.Snapshot.ScrollTop.ShouldBe(500);
    }

    [Fact]
    public async Task ColumnVisibility_Changed_ProducesCaptureWithHiddenCsvPacked() {
        DataGridNavigationState snapshot = new(ImmutableDictionary<string, GridViewSnapshot>.Empty, Cap: 50);

        foreach (string column in new[] { "Created", "Priority", "Owner" }) {
            snapshot = VirtualizationViewStateReducers.ReduceColumnVisibilityChanged(
                snapshot, new ColumnVisibilityChangedAction(ViewKey, column, isVisible: false));
        }

        string csv = snapshot.ViewStates[ViewKey].Filters[VirtualizationReservedKeys.HiddenColumnsKey];
        csv.ShouldBe("Created,Owner,Priority");

        IState<DataGridNavigationState> state = Substitute.For<IState<DataGridNavigationState>>();
        state.Value.Returns(snapshot);
        ColumnVisibilityPersistenceEffect effect = new(state);
        RecordingDispatcher dispatcher = new();

        await effect.HandleColumnVisibilityChangedAsync(
            new ColumnVisibilityChangedAction(ViewKey, "NewlyHidden", isVisible: false), dispatcher);

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.Snapshot.Filters[VirtualizationReservedKeys.HiddenColumnsKey].ShouldBe("Created,Owner,Priority");
    }

    [Fact]
    public void CrossSessionHydration_ClampsScrollTop_ButPreservesHiddenColumns() {
        DataGridNavigationState state = new(ImmutableDictionary<string, GridViewSnapshot>.Empty, Cap: 50);

        GridViewSnapshot stored = new(
            scrollTop: 1024,
            filters: ImmutableDictionary<string, string>.Empty
                .Add(VirtualizationReservedKeys.HiddenColumnsKey, "Created,Priority"),
            sortColumn: null,
            sortDescending: false,
            expandedRowId: null,
            selectedRowId: null,
            capturedAt: DateTimeOffset.UtcNow);

        DataGridNavigationState hydrated = DataGridNavigationReducers.ReduceGridViewHydrated(
            state, new GridViewHydratedAction(ViewKey, stored));

        hydrated.ViewStates[ViewKey].ScrollTop.ShouldBe(0); // D5 clamp
        hydrated.ViewStates[ViewKey].Filters[VirtualizationReservedKeys.HiddenColumnsKey].ShouldBe("Created,Priority");
    }
}
