#pragma warning disable CA2007
using System.Collections.Immutable;
using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 4-4 T4.5c / D7 — <see cref="ColumnVisibilityPersistenceEffect"/> chains
/// <see cref="CaptureGridStateAction"/> on every visibility action (no debounce — checkbox
/// clicks are low-frequency).
/// </summary>
public sealed class ColumnVisibilityPersistenceEffectTests {
    private const string ViewKey = "acme:OrdersProjection";

    private static (ColumnVisibilityPersistenceEffect Sut, RecordingDispatcher Dispatcher) BuildSut() {
        IState<DataGridNavigationState> state = Substitute.For<IState<DataGridNavigationState>>();
        GridViewSnapshot snapshot = new(
            scrollTop: 0,
            filters: ImmutableDictionary<string, string>.Empty
                .Add(VirtualizationReservedKeys.HiddenColumnsKey, "Created"),
            sortColumn: null, sortDescending: false,
            expandedRowId: null, selectedRowId: null,
            capturedAt: DateTimeOffset.UtcNow);
        state.Value.Returns(new DataGridNavigationState(
            ImmutableDictionary<string, GridViewSnapshot>.Empty.Add(ViewKey, snapshot), Cap: 50));
        return (new ColumnVisibilityPersistenceEffect(state), new RecordingDispatcher());
    }

    [Fact]
    public async Task ColumnVisibilityChanged_ChainsCaptureGridState() {
        (ColumnVisibilityPersistenceEffect sut, RecordingDispatcher dispatcher) = BuildSut();

        await sut.HandleColumnVisibilityChangedAsync(
            new ColumnVisibilityChangedAction(ViewKey, "Priority", isVisible: false), dispatcher);

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.ViewKey.ShouldBe(ViewKey);
    }

    [Fact]
    public async Task ResetColumnVisibility_ChainsCaptureGridState() {
        (ColumnVisibilityPersistenceEffect sut, RecordingDispatcher dispatcher) = BuildSut();

        await sut.HandleResetColumnVisibilityAsync(new ResetColumnVisibilityAction(ViewKey), dispatcher);

        CaptureGridStateAction captured = dispatcher.Single<CaptureGridStateAction>();
        captured.ViewKey.ShouldBe(ViewKey);
    }
}
