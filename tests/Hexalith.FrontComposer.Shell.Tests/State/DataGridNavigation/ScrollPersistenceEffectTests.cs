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
/// Story 4-4 T4.5b / D4 — <see cref="ScrollPersistenceEffect"/> coalesces rapid
/// <see cref="ScrollCapturedAction"/>s via a 150 ms <c>TimeProvider</c> debounce, isolates
/// per-viewKey, and cancels pending delays on dispose.
/// </summary>
public sealed class ScrollPersistenceEffectTests {
    private const string ViewKey = "acme:OrdersProjection";
    private const string ViewKeyB = "acme:UsersProjection";

    private static (ScrollPersistenceEffect Sut, FakeTimeProvider Time, RecordingDispatcher Dispatcher) BuildSut(
        params (string ViewKey, double ScrollTop)[] seeds) {
        IState<DataGridNavigationState> state = Substitute.For<IState<DataGridNavigationState>>();
        ImmutableDictionary<string, GridViewSnapshot> views = ImmutableDictionary<string, GridViewSnapshot>.Empty;
        foreach ((string vk, double top) in seeds) {
            views = views.Add(vk, new GridViewSnapshot(
                scrollTop: top,
                filters: ImmutableDictionary<string, string>.Empty,
                sortColumn: null, sortDescending: false,
                expandedRowId: null, selectedRowId: null,
                capturedAt: DateTimeOffset.UtcNow));
        }

        state.Value.Returns(new DataGridNavigationState(views, Cap: 50));
        FakeTimeProvider time = new();
        return (new ScrollPersistenceEffect(state, time), time, new RecordingDispatcher());
    }

    [Fact]
    public async Task CoalescesRapidDispatches_ToSingleCaptureGridState() {
        (ScrollPersistenceEffect sut, FakeTimeProvider time, RecordingDispatcher dispatcher) =
            BuildSut((ViewKey, 500d));

        Task first = sut.HandleScrollCapturedAsync(new ScrollCapturedAction(ViewKey, 100), dispatcher);
        Task second = sut.HandleScrollCapturedAsync(new ScrollCapturedAction(ViewKey, 200), dispatcher);
        Task third = sut.HandleScrollCapturedAsync(new ScrollCapturedAction(ViewKey, 500), dispatcher);

        time.Advance(TimeSpan.FromMilliseconds(150));
        await Task.WhenAll(first, second, third);

        dispatcher.All<CaptureGridStateAction>().Count.ShouldBe(1);
    }

    [Fact]
    public async Task PerViewKey_AreIsolated() {
        (ScrollPersistenceEffect sut, FakeTimeProvider time, RecordingDispatcher dispatcher) =
            BuildSut((ViewKey, 500d), (ViewKeyB, 750d));

        Task a = sut.HandleScrollCapturedAsync(new ScrollCapturedAction(ViewKey, 500), dispatcher);
        Task b = sut.HandleScrollCapturedAsync(new ScrollCapturedAction(ViewKeyB, 750), dispatcher);

        time.Advance(TimeSpan.FromMilliseconds(150));
        await Task.WhenAll(a, b);

        List<CaptureGridStateAction> dispatches = [.. dispatcher.All<CaptureGridStateAction>()];
        dispatches.Count.ShouldBe(2);
        dispatches.ShouldContain(a => a.ViewKey == ViewKey);
        dispatches.ShouldContain(a => a.ViewKey == ViewKeyB);
    }

    [Fact]
    public async Task Dispose_CancelsPendingDelays_NoDispatch() {
        (ScrollPersistenceEffect sut, FakeTimeProvider _, RecordingDispatcher dispatcher) =
            BuildSut((ViewKey, 500d));

        Task pending = sut.HandleScrollCapturedAsync(new ScrollCapturedAction(ViewKey, 500), dispatcher);

        sut.Dispose();
        await pending;

        dispatcher.All<CaptureGridStateAction>().Count.ShouldBe(0);
    }
}
