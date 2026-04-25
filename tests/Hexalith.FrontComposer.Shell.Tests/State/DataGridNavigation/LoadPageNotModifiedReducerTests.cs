using System.Collections.Immutable;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.State.DataGridNavigation;

/// <summary>
/// Story 5-2 D4 / AC4 / AC7 — explicit no-change path. The reducer MUST resolve the pending
/// TCS from the cached items WITHOUT touching <see cref="LoadedPageState.PagesByKey"/>,
/// <see cref="LoadedPageState.TotalCountByKey"/>, or
/// <see cref="LoadedPageState.LastElapsedMsByKey"/>. Any state mutation here would emit a
/// loading flash, badge animation, or success toast on a 304 — exactly what AC4 forbids.
/// </summary>
public class LoadPageNotModifiedReducerTests {
    [Fact]
    public void NotModified_ResolvesTcs_FromCachedItems_WithoutMutatingPagesByKey() {
        TaskCompletionSource<object> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LoadedPageState state = new() {
            PendingCompletionsByKey = ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>>.Empty
                .Add(("orders:Counter.Domain.OrderProjection", 0), completion),
        };
        LoadPageNotModifiedAction action = new(
            viewKey: "orders:Counter.Domain.OrderProjection",
            skip: 0,
            cachedItems: new object[] { new { Id = "order-1" } },
            completion: completion);

        LoadedPageState next = LoadedPageReducers.ReduceLoadPageNotModified(state, action);

        next.PagesByKey.Count.ShouldBe(0);
        next.TotalCountByKey.Count.ShouldBe(0);
        next.LastElapsedMsByKey.Count.ShouldBe(0);
        next.PendingCompletionsByKey.Count.ShouldBe(0);
        completion.Task.IsCompletedSuccessfully.ShouldBeTrue();
    }

    [Fact]
    public void NotModified_StaleAction_DoesNotResolveCurrentTcs() {
        TaskCompletionSource<object> currentTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<object> staleTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LoadedPageState state = new() {
            PendingCompletionsByKey = ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>>.Empty
                .Add(("orders:Counter.Domain.OrderProjection", 0), currentTcs),
        };
        LoadPageNotModifiedAction action = new(
            viewKey: "orders:Counter.Domain.OrderProjection",
            skip: 0,
            cachedItems: System.Array.Empty<object>(),
            completion: staleTcs);

        LoadedPageState next = LoadedPageReducers.ReduceLoadPageNotModified(state, action);

        next.ShouldBeSameAs(state);
        currentTcs.Task.IsCompleted.ShouldBeFalse();
    }

    [Fact]
    public void NotModified_NoMatchingPending_ReturnsStateUnchanged() {
        LoadedPageState state = new();
        LoadPageNotModifiedAction action = new(
            viewKey: "orders:Counter.Domain.OrderProjection",
            skip: 0,
            cachedItems: System.Array.Empty<object>());

        LoadedPageState next = LoadedPageReducers.ReduceLoadPageNotModified(state, action);

        next.ShouldBeSameAs(state);
    }

    [Fact]
    public void LateSuccessAfterCancellation_DoesNotMutateLoadedPageState() {
        TaskCompletionSource<object> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LoadedPageState state = new() {
            PendingCompletionsByKey = ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>>.Empty
                .Add(("orders:Counter.Domain.OrderProjection", 0), completion),
        };

        LoadedPageState cancelled = LoadedPageReducers.ReduceLoadPageCancelled(
            state,
            new LoadPageCancelledAction("orders:Counter.Domain.OrderProjection", 0, completion));
        LoadedPageState afterLateSuccess = new LoadedPageReducers(
            new TestOptionsMonitor(new Hexalith.FrontComposer.Contracts.FcShellOptions()),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<LoadedPageReducers>.Instance)
            .ReduceLoadPageSucceeded(
                cancelled,
                new LoadPageSucceededAction(
                    "orders:Counter.Domain.OrderProjection",
                    0,
                    new object[] { new { Id = "stale" } },
                    totalCount: 1,
                    elapsedMs: 10,
                    completion));

        afterLateSuccess.ShouldBeSameAs(cancelled);
        afterLateSuccess.PagesByKey.ShouldBeEmpty();
        afterLateSuccess.TotalCountByKey.ShouldBeEmpty();
        afterLateSuccess.LastElapsedMsByKey.ShouldBeEmpty();
    }

    private sealed class TestOptionsMonitor(Hexalith.FrontComposer.Contracts.FcShellOptions value)
        : Microsoft.Extensions.Options.IOptionsMonitor<Hexalith.FrontComposer.Contracts.FcShellOptions> {
        public Hexalith.FrontComposer.Contracts.FcShellOptions CurrentValue => value;
        public Hexalith.FrontComposer.Contracts.FcShellOptions Get(string? name) => value;
        public IDisposable? OnChange(Action<Hexalith.FrontComposer.Contracts.FcShellOptions, string?> listener) => null;
    }
}
