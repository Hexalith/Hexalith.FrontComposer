using System.Collections.Immutable;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Story 3-6 Task 2.3 — pure reducer tests for <c>LastActiveRouteChanged</c> /
/// <c>LastActiveRouteHydrated</c> (D1 / D2 / ADR-048).
/// </summary>
public sealed class NavigationReducersLastActiveRouteTests {
    private static FrontComposerNavigationState BaseState() => new(
        SidebarCollapsed: false,
        CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
        CurrentViewport: ViewportTier.Desktop);

    [Fact]
    public void ReduceLastActiveRouteChanged_UpdatesStateField() {
        FrontComposerNavigationState state = BaseState();
        FrontComposerNavigationState next = NavigationReducers.ReduceLastActiveRouteChanged(
            state,
            new LastActiveRouteChangedAction("c1", "/domain/counter/counter-list"));

        next.LastActiveRoute.ShouldBe("/domain/counter/counter-list");
    }

    [Fact]
    public void ReduceLastActiveRouteChanged_EmptyOrWhitespace_CoercesToNull() {
        FrontComposerNavigationState seeded = BaseState() with { LastActiveRoute = "/old" };
        FrontComposerNavigationState next = NavigationReducers.ReduceLastActiveRouteChanged(
            seeded,
            new LastActiveRouteChangedAction("c1", "   "));

        next.LastActiveRoute.ShouldBeNull();
    }

    [Fact]
    public void ReduceLastActiveRouteHydrated_NullRoute_SetsNull() {
        FrontComposerNavigationState seeded = BaseState() with { LastActiveRoute = "/old" };
        FrontComposerNavigationState next = NavigationReducers.ReduceLastActiveRouteHydrated(
            seeded,
            new LastActiveRouteHydratedAction(null));

        next.LastActiveRoute.ShouldBeNull();
    }

    [Fact]
    public void ReduceLastActiveRouteHydrated_NonNullRoute_ReplacesValue() {
        FrontComposerNavigationState state = BaseState();
        FrontComposerNavigationState next = NavigationReducers.ReduceLastActiveRouteHydrated(
            state,
            new LastActiveRouteHydratedAction("/domain/orders/order-list"));

        next.LastActiveRoute.ShouldBe("/domain/orders/order-list");
    }
}
