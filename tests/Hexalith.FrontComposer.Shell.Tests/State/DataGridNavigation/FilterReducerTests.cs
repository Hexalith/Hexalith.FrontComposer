using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.Time.Testing;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 4-3 T4.7 / D3 / D8 / D17 / D21 — reducer-layer tests for <see cref="FilterEffects"/>.
/// Verifies that each filter action produces the expected <see cref="CaptureGridStateAction"/>
/// (or <see cref="ClearGridStateAction"/> for reset) with the reserved-key packing applied.
/// </summary>
public sealed class FilterReducerTests {
    private const string ViewKey = "acme:MyApp.Projections.OrdersProjection";

    private static readonly DateTimeOffset FixedNow = new(2026, 4, 24, 12, 0, 0, TimeSpan.Zero);

    private static GridViewSnapshot EmptySnapshot(DateTimeOffset at) => new(
        scrollTop: 0,
        filters: ImmutableDictionary<string, string>.Empty.WithComparers(StringComparer.Ordinal),
        sortColumn: null,
        sortDescending: false,
        expandedRowId: null,
        selectedRowId: null,
        capturedAt: at);

    private static (FilterEffects Sut, RecordingDispatcher Dispatcher) BuildSut(GridViewSnapshot? seed = null) {
        ImmutableDictionary<string, GridViewSnapshot> views = seed is null
            ? ImmutableDictionary<string, GridViewSnapshot>.Empty
            : ImmutableDictionary<string, GridViewSnapshot>.Empty.Add(ViewKey, seed);
        IState<DataGridNavigationState> state = Substitute.For<IState<DataGridNavigationState>>();
        state.Value.Returns(new DataGridNavigationState(views, Cap: 50));
        FakeTimeProvider time = new();
        time.SetUtcNow(FixedNow);
        return (new FilterEffects(state, time), new RecordingDispatcher());
    }

    [Fact]
    public async Task ColumnFilterChanged_WritesKeyWhenNonEmpty() {
        (FilterEffects sut, RecordingDispatcher dispatcher) = BuildSut();

        await sut.HandleColumnFilterChanged(new ColumnFilterChangedAction(ViewKey, "Name", "acme"), dispatcher);

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.ViewKey.ShouldBe(ViewKey);
        captured.Snapshot.Filters["Name"].ShouldBe("acme");
        captured.Snapshot.CapturedAt.ShouldBe(FixedNow);
    }

    [Fact]
    public async Task ColumnFilterChanged_NullRemovesKey() {
        GridViewSnapshot seed = EmptySnapshot(FixedNow) with {
            Filters = ImmutableDictionary<string, string>.Empty
                .WithComparers(StringComparer.Ordinal)
                .Add("Name", "acme"),
        };
        (FilterEffects sut, RecordingDispatcher dispatcher) = BuildSut(seed);

        await sut.HandleColumnFilterChanged(new ColumnFilterChangedAction(ViewKey, "Name", null), dispatcher);

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.Snapshot.Filters.ContainsKey("Name").ShouldBeFalse();
    }

    [Fact]
    public void ColumnFilterChanged_RejectsReservedKey() {
        // Review pass 2 — reserved-key guard now fires at the action-record level (ArgumentException
        // in ColumnKey's init) so a misconfigured caller cannot poison the Filters dictionary even
        // by bypassing the effect. The effect's defensive check remains as a belt-and-suspenders
        // invariant for any future dispatch path we haven't thought of.
        Should.Throw<ArgumentException>(() =>
            new ColumnFilterChangedAction(ViewKey, "__status", "Pending"));
    }

    [Fact]
    public async Task StatusFilterToggled_AddsThenRemovesSlot() {
        (FilterEffects sut, RecordingDispatcher dispatcher) = BuildSut();

        await sut.HandleStatusFilterToggled(new StatusFilterToggledAction(ViewKey, "Success"), dispatcher);
        CaptureGridStateAction firstToggle = dispatcher.Single<CaptureGridStateAction>();
        firstToggle.Snapshot.Filters[ReservedFilterKeys.StatusKey].ShouldBe("Success");

        (FilterEffects sut2, RecordingDispatcher dispatcher2) = BuildSut(firstToggle.Snapshot);
        await sut2.HandleStatusFilterToggled(new StatusFilterToggledAction(ViewKey, "Success"), dispatcher2);
        CaptureGridStateAction secondToggle = dispatcher2.Single<CaptureGridStateAction>();
        secondToggle.Snapshot.Filters.ContainsKey(ReservedFilterKeys.StatusKey).ShouldBeFalse();
    }

    [Fact]
    public async Task StatusFilterToggled_SerializesSlotsInOrdinalOrder() {
        GridViewSnapshot seed = EmptySnapshot(FixedNow) with {
            Filters = ImmutableDictionary<string, string>.Empty
                .WithComparers(StringComparer.Ordinal)
                .Add(ReservedFilterKeys.StatusKey, "Warning"),
        };
        (FilterEffects sut, RecordingDispatcher dispatcher) = BuildSut(seed);

        await sut.HandleStatusFilterToggled(new StatusFilterToggledAction(ViewKey, "Error"), dispatcher);

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.Snapshot.Filters[ReservedFilterKeys.StatusKey].ShouldBe("Error,Warning");
    }

    [Fact]
    public async Task GlobalSearchChanged_WritesReservedSearchKey() {
        (FilterEffects sut, RecordingDispatcher dispatcher) = BuildSut();

        await sut.HandleGlobalSearchChanged(new GlobalSearchChangedAction(ViewKey, "foo"), dispatcher);

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.Snapshot.Filters[ReservedFilterKeys.SearchKey].ShouldBe("foo");
    }

    [Fact]
    public async Task SortChanged_UpdatesSortColumnAndDirection() {
        (FilterEffects sut, RecordingDispatcher dispatcher) = BuildSut();

        await sut.HandleSortChanged(new SortChangedAction(ViewKey, "Name", sortDescending: true), dispatcher);

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.Snapshot.SortColumn.ShouldBe("Name");
        captured.Snapshot.SortDescending.ShouldBeTrue();
    }

    [Fact]
    public async Task FiltersReset_DispatchesClearGridStateAction() {
        (FilterEffects sut, RecordingDispatcher dispatcher) = BuildSut();

        await sut.HandleFiltersReset(new FiltersResetAction(ViewKey), dispatcher);

        ClearGridStateAction cleared = dispatcher.Single<ClearGridStateAction>();
        cleared.ViewKey.ShouldBe(ViewKey);
    }

    private sealed class RecordingDispatcher : IDispatcher {
        private readonly List<object> _dispatched = [];

        public event EventHandler<Fluxor.ActionDispatchedEventArgs>? ActionDispatched;

        public void Dispatch(object action) {
            _dispatched.Add(action);
            ActionDispatched?.Invoke(this, new Fluxor.ActionDispatchedEventArgs(action));
        }

        public T Single<T>() where T : class {
            List<T> matches = [.. _dispatched.OfType<T>()];
            matches.Count.ShouldBe(1, $"Expected exactly one {typeof(T).Name} dispatched; got {matches.Count}.");
            return matches[0];
        }
    }
}
