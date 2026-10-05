#pragma warning disable CA2007
using System.Collections.Immutable;

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

    private static LoadPageAction MakeLoadPage(string viewKey, int skip, TaskCompletionSource<object> tcs, string requestIdentity = "") =>
        new LoadPageAction(viewKey, skip, take: 20, ImmutableDictionary<string, string>.Empty, null, false, null, tcs, CancellationToken.None) {
            RequestIdentity = requestIdentity,
        };

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

        var state = new LoadedPageState();
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

        var state = new LoadedPageState();
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

    // Removed by Story 11.7 code review (DN-2 option 2): a former
    // SchemaMismatchFailure_PropagatesViaTrySetException_AndRemovesPendingCompletion test
    // fabricated the English message "Projection schema mismatch. Keeping the current page
    // visible." as input and asserted the same substring back. Production code in
    // LoadPageEffects dispatches the localized SectionUpdatingText resource, not the
    // hardcoded string. AC16 end-to-end coverage now lives in
    // LoadPageEffectIntegrationTests.SchemaMismatch_DispatchesSectionUpdatingCopy_AndResolvesTcsViaReducer
    // which drives from the real ProjectionSchemaMismatchException catch through to the TCS.
    // Generic LoadPageFailedAction propagation remains covered by
    // EffectException_PropagatesViaTrySetException_IntoProvider above.

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

        _ = reducers.ReduceLoadPageSucceeded(
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
        ex.Message.ShouldBe("Data could not be loaded.");
        logger.Messages.ShouldContain(m => m.Contains("null Items payload") && m.Contains("Warning"));
    }

    [Fact]
    public void CompletedPageResultsKeepDistinctRequestAndFailureIdentities() {
        LoadedPageReducers reducers = MakeReducers();
        TaskCompletionSource<object> first = new();
        LoadedPageState state = LoadedPageReducers.ReduceLoadPage(
            new LoadedPageState(), MakeLoadPage(ViewKey, 0, first, "filter-a"));
        state = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 0, Array.Empty<object>(), 0, 1, first) {
                RequestIdentity = "filter-a", OperatorInitiated = true,
            });
        LoadedPageResult firstResult = state.LastResultByKey[ViewKey];
        firstResult.RequestIdentity.ShouldBe("filter-a");
        firstResult.TotalCount.ShouldBe(0);
        firstResult.OperatorInitiated.ShouldBeTrue();

        TaskCompletionSource<object> second = new();
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, second, "filter-b"));
        state = LoadedPageReducers.ReduceLoadPageFailed(state,
            new LoadPageFailedAction(ViewKey, 0, "Safe failure", second) { RequestIdentity = "filter-b" });
        LoadedPageResult secondResult = state.LastResultByKey[ViewKey];
        secondResult.Identity.ShouldBe(firstResult.Identity + 1);
        secondResult.RequestIdentity.ShouldBe("filter-b");
        secondResult.Failed.ShouldBeTrue();
        state.FailureByKey[ViewKey].ShouldBe("Safe failure");
    }

    [Fact]
    public async Task OlderFilterCompletionCannotReplaceCurrentRequest() {
        LoadedPageReducers reducers = MakeReducers();
        TaskCompletionSource<object> oldCompletion = new();
        TaskCompletionSource<object> currentCompletion = new();
        LoadedPageState state = LoadedPageReducers.ReduceLoadPage(
            new LoadedPageState(), MakeLoadPage(ViewKey, 0, oldCompletion, "filter-a"));
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, currentCompletion, "filter-b"));

        _ = await Should.ThrowAsync<TaskCanceledException>(async () => await oldCompletion.Task);
        LoadedPageState afterOld = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 0, ["old"], 1, 1, oldCompletion) { RequestIdentity = "filter-a" });
        afterOld.ShouldBe(state);
        currentCompletion.Task.IsCompleted.ShouldBeFalse();

        LoadedPageState afterCurrent = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 0, ["current"], 1, 1, currentCompletion) { RequestIdentity = "filter-b" });
        afterCurrent.LastResultByKey[ViewKey].RequestIdentity.ShouldBe("filter-b");
        afterCurrent.PagesByKey[(ViewKey, 0)].ShouldBe(["current"]);
    }

    [Fact]
    public void OtherOffsetSuccessDoesNotClearFailedPageAndOnlyIts304ClearsIt() {
        LoadedPageReducers reducers = MakeReducers();
        TaskCompletionSource<object> failedCompletion = new();
        TaskCompletionSource<object> otherCompletion = new();
        LoadedPageState state = LoadedPageReducers.ReduceLoadPage(
            new LoadedPageState(), MakeLoadPage(ViewKey, 500, failedCompletion, "filter-a"));
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, otherCompletion, "filter-a"));
        state = LoadedPageReducers.ReduceLoadPageFailed(state,
            new LoadPageFailedAction(ViewKey, 500, "This section is being updated", failedCompletion) {
                RequestIdentity = "filter-a", Take = 20,
            });
        state = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 0, ["row"], 1, 1, otherCompletion) { RequestIdentity = "filter-a" });

        state.ResultsByPage[(ViewKey, 500)].ErrorMessage.ShouldBe("This section is being updated");
        state.FailureByKey[ViewKey].ShouldBe("This section is being updated");
        state.LastSuccessfulPrimaryByView[ViewKey].Skip.ShouldBe(0);

        TaskCompletionSource<object> retry = new();
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 500, retry, "filter-a"));
        state = LoadedPageReducers.ReduceLoadPageNotModified(state,
            new LoadPageNotModifiedAction(ViewKey, 500, ["cached"], retry) { RequestIdentity = "filter-a" });
        state.ResultsByPage.ShouldNotContainKey((ViewKey, 500));
        state.FailureByKey.ShouldNotContainKey(ViewKey);
        state.ResultsByPage[(ViewKey, 0)].Failed.ShouldBeFalse();
    }

    [Fact]
    public void SameTextFallbackCompletionFromEarlierRequestGenerationIsDiscarded() {
        LoadedPageReducers reducers = MakeReducers();
        LoadedPageState state = new();
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, new(), "A") with { RequestGeneration = 1 });
        state = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 0, ["first-a"], 1, 1) { RequestIdentity = "A", RequestGeneration = 1 });
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, new(), "B") with { RequestGeneration = 2 });
        state.LastResultByKey.ShouldNotContainKey(ViewKey);
        state.LastSuccessfulPrimaryByView.ShouldNotContainKey(ViewKey);
        state = LoadedPageReducers.ReduceLoadPage(state, MakeLoadPage(ViewKey, 0, new(), "A") with { RequestGeneration = 3 });

        LoadedPageState stale = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 0, ["old-a"], 1, 1) { RequestIdentity = "A", RequestGeneration = 1 });
        stale.ShouldBeSameAs(state);
        LoadedPageState current = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 0, ["new-a"], 1, 1) { RequestIdentity = "A", RequestGeneration = 3 });
        current.PagesByKey[(ViewKey, 0)].ShouldBe(["new-a"]);
    }

    [Fact]
    public void ResultMetadataStaysWithinPageCapAndKeepsCurrentFailedOffset() {
        IOptionsMonitor<FcShellOptions> monitor = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        monitor.CurrentValue.Returns(new FcShellOptions { MaxCachedPages = 10 });
        LoadedPageReducers reducers = new(monitor, NullLogger<LoadedPageReducers>.Instance);
        LoadedPageState state = new();
        for (int skip = 0; skip < 12; skip++) {
            TaskCompletionSource<object> completion = new();
            state = LoadedPageReducers.ReduceLoadPage(state,
                MakeLoadPage(ViewKey, skip, completion, "A") with { RequestGeneration = 1 });
            state = reducers.ReduceLoadPageSucceeded(state,
                new LoadPageSucceededAction(ViewKey, skip, ["row"], 12, 1, completion) {
                    RequestIdentity = "A", RequestGeneration = 1,
                });
        }
        state.PagesByKey.Count.ShouldBe(10);
        state.ResultsByPage.Count.ShouldBeLessThanOrEqualTo(10);

        TaskCompletionSource<object> failedCompletion = new();
        state = LoadedPageReducers.ReduceLoadPage(state,
            MakeLoadPage(ViewKey, 500, failedCompletion, "A") with { RequestGeneration = 1 });
        state = LoadedPageReducers.ReduceLoadPageFailed(state,
            new LoadPageFailedAction(ViewKey, 500, "Safe failure", failedCompletion) {
                RequestIdentity = "A", RequestGeneration = 1, Take = 20,
            });
        TaskCompletionSource<object> otherCompletion = new();
        state = LoadedPageReducers.ReduceLoadPage(state,
            MakeLoadPage(ViewKey, 12, otherCompletion, "A") with { RequestGeneration = 1 });
        state = reducers.ReduceLoadPageSucceeded(state,
            new LoadPageSucceededAction(ViewKey, 12, ["next"], 13, 1, otherCompletion) {
                RequestIdentity = "A", RequestGeneration = 1,
            });

        state.ResultsByPage.Count.ShouldBeLessThanOrEqualTo(10);
        state.ResultsByPage[(ViewKey, 500)].Failed.ShouldBeTrue();
        state.ResultsByPage[(ViewKey, 500)].Take.ShouldBe(20);
    }

    private sealed class CapturingLogger<T> : ILogger<T> {
        public List<string> Messages { get; } = [];
        IDisposable? ILogger.BeginScope<TState>(TState state) => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add($"{logLevel}: {formatter(state, exception)}");
    }
}
