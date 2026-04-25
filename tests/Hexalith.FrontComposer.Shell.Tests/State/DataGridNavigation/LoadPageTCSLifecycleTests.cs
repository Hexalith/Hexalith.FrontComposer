#pragma warning disable CA2007
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 4-4 T4.5a / D3 — TCS lifecycle correctness: no orphans, no double-resolution, dispose
/// sweeps every in-flight entry, two viewports cannot cross-resolve, and the null-items guard
/// converts a null payload into a TCS exception without polluting <c>PagesByKey</c>.
/// </summary>
public sealed class LoadPageTCSLifecycleTests {
    private const string ViewKey = "acme:OrdersProjection";

    private static LoadedPageReducers MakeReducers(ILogger<LoadedPageReducers>? logger = null) {
        IOptionsMonitor<FcShellOptions> monitor = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        monitor.CurrentValue.Returns(new FcShellOptions { MaxCachedPages = 200 });
        return new LoadedPageReducers(monitor, logger ?? NullLogger<LoadedPageReducers>.Instance);
    }

    private static LoadPageAction MakeLoadPage(string viewKey, int skip, TaskCompletionSource<object> tcs) =>
        new(viewKey, skip, take: 20, ImmutableDictionary<string, string>.Empty, null, false, null, tcs, CancellationToken.None);

    [Fact]
    public async Task CancellationTokenFires_WhilePending_TransitionsTcsToCanceled_NoOrphan() {
        TaskCompletionSource<object> tcs = new();
        LoadedPageState state = LoadedPageReducers.ReduceLoadPage(
            new LoadedPageState(), MakeLoadPage(ViewKey, 0, tcs));

        // Simulate the cancellation-token callback path that dispatches LoadPageCancelledAction.
        LoadedPageState after = LoadedPageReducers.ReduceLoadPageCancelled(
            state, new LoadPageCancelledAction(ViewKey, 0));

        after.PendingCompletionsByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await tcs.Task);
    }

    [Fact]
    public async Task RapidScroll_SameKey_DoubleRegisterCancelsFirstTcs_NoThrow() {
        TaskCompletionSource<object> first = new();
        TaskCompletionSource<object> second = new();
        LoadedPageState state = LoadedPageReducers.ReduceLoadPage(
            new LoadedPageState(), MakeLoadPage(ViewKey, 0, first));

        // Second LoadPageAction for the same (viewKey, skip) BEFORE first resolves.
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, second));

        // First TCS is canceled; second is registered in its place.
        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await first.Task);
        state.PendingCompletionsByKey[(ViewKey, 0)].ShouldBe(second);
        second.Task.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public async Task DisposeMidFlight_ClearPendingPagesSweepsAllForViewKey() {
        TaskCompletionSource<object> a = new();
        TaskCompletionSource<object> b = new();
        TaskCompletionSource<object> c = new();

        LoadedPageState state = new LoadedPageState();
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, a));
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 20, b));
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 40, c));

        LoadedPageState after = LoadedPageReducers.ReduceClearPendingPages(
            state, new ClearPendingPagesAction(ViewKey));

        after.PendingCompletionsByKey.Count.ShouldBe(0);
        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await a.Task);
        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await b.Task);
        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await c.Task);
    }

    [Fact]
    public async Task TwoViewports_OverlappingSkip_AreCorrelatedIndependently() {
        TaskCompletionSource<object> ordersTcs = new();
        TaskCompletionSource<object> usersTcs = new();

        LoadedPageState state = new LoadedPageState();
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage("acme:Orders", 0, ordersTcs));
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage("acme:Users", 0, usersTcs));

        LoadedPageReducers reducers = MakeReducers();
        IReadOnlyList<object> ordersItems = new object[] { "order-1" };
        state = reducers.ReduceLoadPageSucceeded(
            state, new LoadPageSucceededAction("acme:Orders", 0, ordersItems, totalCount: 1, elapsedMs: 1));

        // Orders TCS resolves; Users TCS still pending.
        object resolved = await ordersTcs.Task;
        resolved.ShouldBe(ordersItems);
        usersTcs.Task.IsCompleted.ShouldBeFalse();
        state.PendingCompletionsByKey.ContainsKey(("acme:Users", 0)).ShouldBeTrue();
    }

    [Fact]
    public async Task EffectException_PropagatesViaTrySetException_IntoProvider() {
        LoadedPageReducers reducers = MakeReducers();
        TaskCompletionSource<object> tcs = new();
        LoadedPageState state = LoadedPageReducers.ReduceLoadPage(new LoadedPageState(), MakeLoadPage(ViewKey, 0, tcs));

        state = LoadedPageReducers.ReduceLoadPageFailed(state, new LoadPageFailedAction(ViewKey, 0, "service exploded"));

        state.PendingCompletionsByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(async () => await tcs.Task);
        ex.Message.ShouldBe("service exploded");
        _ = reducers;
    }

    [Fact]
    public void DoubleRegistrationIdempotency_StaleSuccessForReplacedEntry_DoesNotResolveNewTcs() {
        TaskCompletionSource<object> first = new();
        TaskCompletionSource<object> second = new();
        LoadedPageState state = LoadedPageReducers.ReduceLoadPage(new LoadedPageState(), MakeLoadPage(ViewKey, 0, first));
        LoadPageAction secondAction = MakeLoadPage(ViewKey, 0, second);
        state = LoadedPageReducers.ReduceLoadPage(state, secondAction);

        // A stale success from the replaced first request must not resolve the newer pending TCS.
        LoadedPageReducers reducers = MakeReducers();
        IReadOnlyList<object> items = new object[] { "x" };
        state = reducers.ReduceLoadPageSucceeded(
            state,
            new LoadPageSucceededAction(ViewKey, 0, items, totalCount: 1, elapsedMs: 1, completion: first));

        state.PendingCompletionsByKey[(ViewKey, 0)].ShouldBe(second);
        second.Task.IsCompleted.ShouldBeFalse();
        state.PagesByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();

        state = reducers.ReduceLoadPageSucceeded(
            state,
            new LoadPageSucceededAction(ViewKey, 0, items, totalCount: 1, elapsedMs: 1, completion: secondAction.Completion));

        second.Task.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public async Task NullItemsGuard_ConvertsNullPayloadIntoTcsException_PagesByKeyUnchanged() {
        CapturingLogger<LoadedPageReducers> logger = new();
        LoadedPageReducers reducers = MakeReducers(logger);
        TaskCompletionSource<object> tcs = new();
        LoadedPageState state = LoadedPageReducers.ReduceLoadPage(new LoadedPageState(), MakeLoadPage(ViewKey, 0, tcs));

        LoadedPageState after = reducers.ReduceLoadPageSucceeded(
            state, new LoadPageSucceededAction(ViewKey, 0, items: null, totalCount: 0, elapsedMs: 10));

        after.PagesByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        after.PendingCompletionsByKey.ContainsKey((ViewKey, 0)).ShouldBeFalse();
        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(async () => await tcs.Task);
        ex.Message.ShouldContain("null Items payload");
        logger.Messages.ShouldContain(m => m.Contains("null Items payload") && m.Contains("Warning"));
    }

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<string> Messages { get; } = [];
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add($"{logLevel}: {formatter(state, exception)}");
    }
}
