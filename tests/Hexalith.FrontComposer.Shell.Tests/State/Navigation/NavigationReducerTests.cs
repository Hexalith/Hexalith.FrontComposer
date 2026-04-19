// ATDD RED PHASE — Story 3-2 Task 10.5
// This file WILL NOT COMPILE until Task 1 (ViewportTier + FrontComposerNavigationState) and
// Task 2 (NavigationActions + NavigationReducers) land. That is the intended red-phase signal.
// See _bmad-output/test-artifacts/atdd-checklist-3-2.md for the coverage matrix.

using System.Collections.Immutable;

using Hexalith.FrontComposer.Shell.State.Navigation;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.State.Navigation;

/// <summary>
/// Story 3-2 Task 10.5 — pure reducer transitions for <see cref="FrontComposerNavigationState"/>.
/// Covers Decisions D3 (state record shape), D4 (ViewportTier enum values),
/// D11 (sparse-by-default CollapsedGroups), D14 (ViewportTier NOT persisted — reducer only),
/// D15 (hydrate replaces wholesale). AC2, AC3, AC4.
/// </summary>
public sealed class NavigationReducerTests
{
    private static FrontComposerNavigationState InitialState() => new(
        SidebarCollapsed: false,
        CollapsedGroups: ImmutableDictionary<string, bool>.Empty.WithComparers(StringComparer.Ordinal),
        CurrentViewport: ViewportTier.Desktop);

    [Fact]
    public void SidebarToggledFlipsFlag()
    {
        FrontComposerNavigationState state = InitialState();

        FrontComposerNavigationState next = NavigationReducers.ReduceSidebarToggled(state, new SidebarToggledAction("c1"));

        next.SidebarCollapsed.ShouldBeTrue();
        next.CollapsedGroups.ShouldBeSameAs(state.CollapsedGroups);
        next.CurrentViewport.ShouldBe(ViewportTier.Desktop);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SidebarToggledIsInvolution(bool startCollapsed)
    {
        FrontComposerNavigationState state = InitialState() with { SidebarCollapsed = startCollapsed };

        FrontComposerNavigationState once = NavigationReducers.ReduceSidebarToggled(state, new SidebarToggledAction("c1"));
        FrontComposerNavigationState twice = NavigationReducers.ReduceSidebarToggled(once, new SidebarToggledAction("c2"));

        twice.SidebarCollapsed.ShouldBe(startCollapsed);
    }

    [Fact]
    public void NavGroupToggled_Collapse_AddsEntry()
    {
        FrontComposerNavigationState state = InitialState();

        FrontComposerNavigationState next = NavigationReducers.ReduceNavGroupToggled(
            state,
            new NavGroupToggledAction("c1", "Counter", Collapsed: true));

        next.CollapsedGroups.ShouldContainKeyAndValue("Counter", true);
    }

    [Fact]
    public void NavGroupToggled_Expand_RemovesEntryForSparseBlob()
    {
        // D11: expanded groups are NOT written as "Counter":false — they are removed from the map
        // so the persisted blob stays minimal. New BCs discovered post-persistence default to expanded.
        FrontComposerNavigationState state = InitialState() with
        {
            CollapsedGroups = ImmutableDictionary<string, bool>.Empty
                .WithComparers(StringComparer.Ordinal)
                .Add("Counter", true)
                .Add("Orders", true),
        };

        FrontComposerNavigationState next = NavigationReducers.ReduceNavGroupToggled(
            state,
            new NavGroupToggledAction("c1", "Counter", Collapsed: false));

        next.CollapsedGroups.ContainsKey("Counter").ShouldBeFalse();
        next.CollapsedGroups.ShouldContainKeyAndValue("Orders", true);
    }

    [Fact]
    public void ViewportTierChangedOnlyUpdatesCurrentViewport()
    {
        FrontComposerNavigationState state = InitialState() with
        {
            SidebarCollapsed = true,
            CollapsedGroups = ImmutableDictionary<string, bool>.Empty
                .WithComparers(StringComparer.Ordinal)
                .Add("Counter", true),
        };

        FrontComposerNavigationState next = NavigationReducers.ReduceViewportTierChanged(
            state,
            new ViewportTierChangedAction(ViewportTier.Tablet));

        next.CurrentViewport.ShouldBe(ViewportTier.Tablet);
        next.SidebarCollapsed.ShouldBeTrue("Viewport change MUST NOT mutate SidebarCollapsed (D14)");
        next.CollapsedGroups.ShouldContainKeyAndValue("Counter", true);
    }

    [Fact]
    public void SidebarExpandedIsIdempotent()
    {
        FrontComposerNavigationState state = InitialState();

        FrontComposerNavigationState first = NavigationReducers.ReduceSidebarExpanded(state, new SidebarExpandedAction("c1"));
        FrontComposerNavigationState second = NavigationReducers.ReduceSidebarExpanded(first, new SidebarExpandedAction("c2"));

        first.SidebarCollapsed.ShouldBeFalse();
        second.SidebarCollapsed.ShouldBeFalse();
    }

    [Fact]
    public void NavigationHydratedReplacesWholesale()
    {
        // D15 — hydrate replaces SidebarCollapsed + CollapsedGroups wholesale, never merges.
        FrontComposerNavigationState state = InitialState() with
        {
            SidebarCollapsed = false,
            CollapsedGroups = ImmutableDictionary<string, bool>.Empty
                .WithComparers(StringComparer.Ordinal)
                .Add("Legacy", true),
        };

        ImmutableDictionary<string, bool> fromBlob = ImmutableDictionary<string, bool>.Empty
            .WithComparers(StringComparer.Ordinal)
            .Add("Counter", true);

        FrontComposerNavigationState next = NavigationReducers.ReduceNavigationHydrated(
            state,
            new NavigationHydratedAction(SidebarCollapsed: true, CollapsedGroups: fromBlob));

        next.SidebarCollapsed.ShouldBeTrue();
        next.CollapsedGroups.Keys.ShouldBe(["Counter"]);
        next.CurrentViewport.ShouldBe(ViewportTier.Desktop, "Hydrate MUST NOT touch CurrentViewport (ADR-037)");
    }

    [Fact]
    public void ViewportTierEnumValuesArePinned()
    {
        // D4 — JS wire format depends on these ordinal values staying stable.
        ((byte)ViewportTier.Phone).ShouldBe((byte)0);
        ((byte)ViewportTier.Tablet).ShouldBe((byte)1);
        ((byte)ViewportTier.CompactDesktop).ShouldBe((byte)2);
        ((byte)ViewportTier.Desktop).ShouldBe((byte)3);
    }
}
