#pragma warning disable CA2007
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 4-4 T4.10 / D10 re-revised — exercises <see cref="FcShellOptions.MaxCachedPages"/> policy:
/// bound-at-N (happy-path no eviction), eviction-on-overflow (FIFO via PageInsertionOrder, with
/// Information-level log), and the evicted-while-visible regression gate (eviction of the
/// currently-rendered page does not disturb the Fluxor state shape downstream callers depend on).
/// </summary>
public sealed class LoadedPageStateCacheBoundTests {
    private const string ViewKey = "acme:OrdersProjection";

    private static LoadedPageReducers MakeReducers(int maxCachedPages, CapturingLogger logger) {
        IOptionsMonitor<FcShellOptions> monitor = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        monitor.CurrentValue.Returns(new FcShellOptions { MaxCachedPages = maxCachedPages });
        return new LoadedPageReducers(monitor, logger);
    }

    private static LoadedPageState Register(LoadedPageState state, int skip, TaskCompletionSource<object> tcs) =>
        LoadedPageReducers.ReduceLoadPage(state, new LoadPageAction(
            ViewKey, skip, take: 20, ImmutableDictionary<string, string>.Empty, null, false, null, tcs, CancellationToken.None));

    [Fact]
    public void BoundAtN_HappyPath_20DistinctWrites_NoEviction() {
        CapturingLogger logger = new();
        LoadedPageReducers reducers = MakeReducers(maxCachedPages: 100, logger);
        LoadedPageState state = new LoadedPageState();

        List<TaskCompletionSource<object>> tcss = [];
        for (int i = 0; i < 20; i++) {
            TaskCompletionSource<object> tcs = new();
            tcss.Add(tcs);
            state = Register(state, skip: i * 20, tcs);
            state = reducers.ReduceLoadPageSucceeded(state,
                new LoadPageSucceededAction(ViewKey, i * 20, new object[] { i }, totalCount: 200, elapsedMs: 5));
        }

        state.PagesByKey.Count.ShouldBe(20);
        state.PageInsertionOrder.Count().ShouldBe(20);
        logger.Messages.ShouldNotContain(m => m.Contains("eviction"));

        // Sweep: TCS entries should already be removed (all succeeded).
        state.PendingCompletionsByKey.Count.ShouldBe(0);
    }

    [Fact]
    public void EvictionOnOverflow_FifoViaPageInsertionOrder_WithInformationLog() {
        CapturingLogger logger = new();
        LoadedPageReducers reducers = MakeReducers(maxCachedPages: 5, logger);
        LoadedPageState state = new LoadedPageState();

        int[] skips = [0, 20, 40, 60, 80, 100, 120, 140];
        foreach (int skip in skips) {
            TaskCompletionSource<object> tcs = new();
            state = Register(state, skip, tcs);
            state = reducers.ReduceLoadPageSucceeded(state,
                new LoadPageSucceededAction(ViewKey, skip, new object[] { skip }, totalCount: skips.Length * 20, elapsedMs: 5));
        }

        state.PagesByKey.Count.ShouldBe(5);
        // Oldest three (0, 20, 40) evicted; newest five (60, 80, 100, 120, 140) retained.
        state.PagesByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        state.PagesByKey.ContainsKey((ViewKey, 20)).ShouldBeFalse();
        state.PagesByKey.ContainsKey((ViewKey, 40)).ShouldBeFalse();
        state.PagesByKey.ContainsKey((ViewKey, 60)).ShouldBeTrue();
        state.PagesByKey.ContainsKey((ViewKey, 80)).ShouldBeTrue();
        state.PagesByKey.ContainsKey((ViewKey, 100)).ShouldBeTrue();
        state.PagesByKey.ContainsKey((ViewKey, 120)).ShouldBeTrue();
        state.PagesByKey.ContainsKey((ViewKey, 140)).ShouldBeTrue();

        logger.Messages.Count(m => m.Contains("eviction") && m.StartsWith("Information:")).ShouldBe(3);
    }

    [Fact]
    public void EvictedWhileVisible_DoesNotDisturbStateShape() {
        CapturingLogger logger = new();
        LoadedPageReducers reducers = MakeReducers(maxCachedPages: 3, logger);
        LoadedPageState state = new LoadedPageState();

        foreach (int skip in new[] { 0, 20, 40 }) {
            TaskCompletionSource<object> tcs = new();
            state = Register(state, skip, tcs);
            state = reducers.ReduceLoadPageSucceeded(state,
                new LoadPageSucceededAction(ViewKey, skip, new object[] { skip }, totalCount: 80, elapsedMs: 1));
        }

        state.PagesByKey.Count.ShouldBe(3);

        // Add a fourth page while skip=0 is the "currently visible" page — eviction must not throw
        // or touch TotalCountByKey/LastElapsedMsByKey values for the viewKey.
        TaskCompletionSource<object> newTcs = new();
        state = Register(state, 60, newTcs);
        state = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 60, new object[] { 60 }, totalCount: 80, elapsedMs: 1));

        state.PagesByKey.Count.ShouldBe(3);
        state.PagesByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        state.PagesByKey.ContainsKey((ViewKey, 60)).ShouldBeTrue();
        state.TotalCountByKey[ViewKey].ShouldBe(80);
        state.LastElapsedMsByKey[ViewKey].ShouldBe(1L);
    }

    private sealed class CapturingLogger : ILogger<LoadedPageReducers> {
        public List<string> Messages { get; } = [];
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add($"{logLevel}: {formatter(state, exception)}");
    }
}
