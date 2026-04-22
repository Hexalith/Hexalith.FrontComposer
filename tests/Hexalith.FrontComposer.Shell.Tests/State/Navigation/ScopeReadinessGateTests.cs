using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Story 3-6 Task 5.8 (D20 / A3) — exactly-once <see cref="StorageReadyAction"/> dispatch under
/// concurrent handler invocations.
/// </summary>
public sealed class ScopeReadinessGateTests {
    private static FrontComposerNavigationState BaseState(bool storageReady = false) => new(
        SidebarCollapsed: false,
        CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
        CurrentViewport: ViewportTier.Desktop,
        StorageReady: storageReady);

    private static IState<FrontComposerNavigationState> FakeState(FrontComposerNavigationState value) {
        IState<FrontComposerNavigationState> state = Substitute.For<IState<FrontComposerNavigationState>>();
        state.Value.Returns(value);
        return state;
    }

    private static IUserContextAccessor MakeAccessor(string? tenant, string? user) {
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(tenant);
        accessor.UserId.Returns(user);
        return accessor;
    }

    [Fact]
    public async Task EvaluateAsync_DispatchesStorageReadyOnceAfterObservedEmptyToReadyTransition() {
        IState<FrontComposerNavigationState> state = FakeState(BaseState());
        string? tenant = null;
        string? user = "alice";
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(_ => tenant);
        accessor.UserId.Returns(_ => user);
        ILogger<ScopeReadinessGate> logger = Substitute.For<ILogger<ScopeReadinessGate>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var gate = new ScopeReadinessGate(state, accessor, null, logger);

        await gate.EvaluateAsync(dispatcher, Xunit.TestContext.Current.CancellationToken);
        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);

        tenant = "acme";
        await gate.EvaluateAsync(dispatcher, Xunit.TestContext.Current.CancellationToken);

        dispatcher.Received(1).Dispatch(Arg.Any<StorageReadyAction>());
    }

    [Fact]
    public async Task EvaluateAsync_FirstObservedReadyScope_DoesNotDispatchStorageReady() {
        // Story 3-6 D1 (Review) — the gate only dispatches when an empty-to-ready transition has
        // been observed. A circuit that starts already-authenticated (no prerender-empty window)
        // already completed hydrate successfully via HandleAppInitialized, so re-dispatch is not
        // required. The ScopeFlipObserverEffect observes AppInitializedAction to seed the empty
        // observation when prerender scope is empty (D1 review-finding patch).
        IState<FrontComposerNavigationState> state = FakeState(BaseState());
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        ILogger<ScopeReadinessGate> logger = Substitute.For<ILogger<ScopeReadinessGate>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var gate = new ScopeReadinessGate(state, accessor, null, logger);

        await gate.EvaluateAsync(dispatcher, Xunit.TestContext.Current.CancellationToken);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Fact]
    public async Task EvaluateAsync_NoOpWhenScopeStillEmpty() {
        IState<FrontComposerNavigationState> state = FakeState(BaseState());
        IUserContextAccessor accessor = MakeAccessor(null, "alice");
        ILogger<ScopeReadinessGate> logger = Substitute.For<ILogger<ScopeReadinessGate>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var gate = new ScopeReadinessGate(state, accessor, null, logger);

        await gate.EvaluateAsync(dispatcher, Xunit.TestContext.Current.CancellationToken);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Fact]
    public async Task EvaluateAsync_NoOpWhenAlreadyReady() {
        IState<FrontComposerNavigationState> state = FakeState(BaseState(storageReady: true));
        IUserContextAccessor accessor = MakeAccessor("acme", "alice");
        ILogger<ScopeReadinessGate> logger = Substitute.For<ILogger<ScopeReadinessGate>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var gate = new ScopeReadinessGate(state, accessor, null, logger);

        await gate.EvaluateAsync(dispatcher, Xunit.TestContext.Current.CancellationToken);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }

    [Fact]
    public async Task EvaluateAsync_ConcurrentHandlersDispatchStorageReadyExactlyOnce() {
        IState<FrontComposerNavigationState> state = FakeState(BaseState());
        string? tenant = null;
        string? user = "alice";
        IUserContextAccessor accessor = Substitute.For<IUserContextAccessor>();
        accessor.TenantId.Returns(_ => tenant);
        accessor.UserId.Returns(_ => user);
        ILogger<ScopeReadinessGate> logger = Substitute.For<ILogger<ScopeReadinessGate>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var gate = new ScopeReadinessGate(state, accessor, null, logger);

        await gate.EvaluateAsync(dispatcher, Xunit.TestContext.Current.CancellationToken);
        tenant = "acme";

        CancellationToken ct = Xunit.TestContext.Current.CancellationToken;
        Task[] concurrent = Enumerable.Range(0, 8)
            .Select(_ => gate.EvaluateAsync(dispatcher, ct))
            .ToArray();
        await Task.WhenAll(concurrent);

        dispatcher.Received(1).Dispatch(Arg.Any<StorageReadyAction>());
    }

    [Fact]
    public async Task EvaluateAsync_EmptyTenantWhitespace_NoOp() {
        IState<FrontComposerNavigationState> state = FakeState(BaseState());
        IUserContextAccessor accessor = MakeAccessor("   ", "alice");
        ILogger<ScopeReadinessGate> logger = Substitute.For<ILogger<ScopeReadinessGate>>();
        IDispatcher dispatcher = Substitute.For<IDispatcher>();
        var gate = new ScopeReadinessGate(state, accessor, null, logger);

        await gate.EvaluateAsync(dispatcher, Xunit.TestContext.Current.CancellationToken);

        dispatcher.DidNotReceiveWithAnyArgs().Dispatch(default!);
    }
}
