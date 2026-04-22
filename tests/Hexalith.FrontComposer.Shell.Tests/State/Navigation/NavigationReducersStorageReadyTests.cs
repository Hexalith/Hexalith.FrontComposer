using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Story 3-6 Task 2.4 — pure reducer tests for <c>StorageReady</c> (D13 / ADR-049) and the D19
/// hydration-state transitions.
/// </summary>
public sealed class NavigationReducersStorageReadyTests {
    private static FrontComposerNavigationState BaseState() => new(
        SidebarCollapsed: false,
        CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
        CurrentViewport: ViewportTier.Desktop);

    [Fact]
    public void ReduceStorageReady_FlipsStorageReadyToTrue() {
        FrontComposerNavigationState state = BaseState();
        state.StorageReady.ShouldBeFalse();

        FrontComposerNavigationState next = NavigationReducers.ReduceStorageReady(
            state,
            new StorageReadyAction("c1"));

        next.StorageReady.ShouldBeTrue();
    }

    [Fact]
    public void ReduceStorageReady_IdempotentWhenAlreadyTrue() {
        FrontComposerNavigationState state = BaseState() with { StorageReady = true };

        FrontComposerNavigationState next = NavigationReducers.ReduceStorageReady(
            state,
            new StorageReadyAction("c1"));

        next.ShouldBeSameAs(state);
    }

    [Fact]
    public void ReduceNavigationHydrating_FromIdle_FlipsToHydrating() {
        FrontComposerNavigationState state = BaseState();
        state.HydrationState.ShouldBe(NavigationHydrationState.Idle);

        FrontComposerNavigationState next = NavigationReducers.ReduceNavigationHydrating(
            state,
            new NavigationHydratingAction());

        next.HydrationState.ShouldBe(NavigationHydrationState.Hydrating);
    }

    [Fact]
    public void ReduceNavigationHydratedCompleted_FromHydrating_FlipsToHydrated() {
        FrontComposerNavigationState state = BaseState() with { HydrationState = NavigationHydrationState.Hydrating };

        FrontComposerNavigationState next = NavigationReducers.ReduceNavigationHydratedCompleted(
            state,
            new NavigationHydratedCompletedAction());

        next.HydrationState.ShouldBe(NavigationHydrationState.Hydrated);
    }

    [Fact]
    public void ReduceNavigationHydrating_FromHydrated_IsNoOp() {
        FrontComposerNavigationState state = BaseState() with { HydrationState = NavigationHydrationState.Hydrated };

        FrontComposerNavigationState next = NavigationReducers.ReduceNavigationHydrating(
            state,
            new NavigationHydratingAction());

        next.ShouldBeSameAs(state);
    }
}
