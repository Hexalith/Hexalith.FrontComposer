#pragma warning disable CA2007
using System.Collections.Immutable;
using System.Globalization;

using AngleSharp.Dom;

using Bunit;

using Counter.Domain;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.State.ExpandedRow;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2.4 AC1/AC2 — runtime pins for the generated grid expand-in-row accessibility path.
/// Component, reducer, and emitter tests already pin the isolated pieces; these tests render the
/// generated Counter grid and exercise the click/filter flow end-to-end in bUnit.
/// </summary>
public sealed class ExpandInRowGeneratedGridTests : GeneratedComponentTestBase {
    private static readonly DateTimeOffset s_lastUpdated = new(2026, 4, 14, 0, 0, 0, TimeSpan.Zero);

    public ExpandInRowGeneratedGridTests()
        : base(typeof(CounterProjection).Assembly) {
    }

    [Fact]
    public async Task CounterProjectionView_ExpandTrigger_PopulatesAlwaysPresentRegion() {
        SetupExpandInRowModule();
        using CultureScope cultureScope = new("en");
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        List<object> dispatchedActions = [];
        dispatcher.ActionDispatched += (_, e) => dispatchedActions.Add(e.Action);
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-ac1",
            [
                Counter("counter-1", 1),
                Counter("counter-2", 2),
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() => {
            IElement region = cut.Find(".fc-expand-in-row-detail");
            region.GetAttribute("role").ShouldBe("region");
            region.GetAttribute("aria-label").ShouldBe("Details for ");
            region.TextContent.ShouldNotContain("counter-1");

            IElement button = ExpandButton(cut, "counter-1");
            button.GetAttribute("aria-expanded").ShouldBe("false");
            string? controls = button.GetAttribute("aria-controls");
            controls.ShouldNotBeNull();
            string.IsNullOrWhiteSpace(controls).ShouldBeFalse();
            region.GetAttribute("id").ShouldBe(controls);
        });

        await cut.InvokeAsync(() => ExpandButton(cut, "counter-1").Click());
        ExpandRowAction expandAction = await WaitForExpandActionAsync(cut, dispatchedActions, "counter-1");
        await cut.InvokeAsync(() => RestoreExpandedRowState(expandAction));

        await cut.WaitForAssertionAsync(() => {
            IElement region = cut.Find(".fc-expand-in-row-detail");
            region.GetAttribute("role").ShouldBe("region");
            region.GetAttribute("aria-label").ShouldBe("Details for counter-1");
            region.TextContent.ShouldContain("counter-1");
            region.GetAttribute("id").ShouldBe(CollapseButton(cut, "counter-1").GetAttribute("aria-controls"));
            CollapseButton(cut, "counter-1").GetAttribute("aria-expanded").ShouldBe("true");
            ExpandButton(cut, "counter-2").GetAttribute("aria-expanded").ShouldBe("false");
        });
    }

    [Fact]
    public async Task CounterProjectionView_FilterHidesExpandedRow_RendersBannerAndSuppressedAnnouncement() {
        SetupExpandInRowModule();
        using CultureScope cultureScope = new("en");
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        List<object> dispatchedActions = [];
        dispatcher.ActionDispatched += (_, e) => dispatchedActions.Add(e.Action);
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-ac2",
            [
                Counter("counter-1", 1),
                Counter("counter-2", 2),
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() => _ = ExpandButton(cut, "counter-1"));
        await cut.InvokeAsync(() => ExpandButton(cut, "counter-1").Click());
        ExpandRowAction expandAction = await WaitForExpandActionAsync(cut, dispatchedActions, "counter-1");
        await cut.InvokeAsync(() => RestoreExpandedRowState(expandAction));
        await cut.WaitForAssertionAsync(() => cut.Find(".fc-expand-in-row-detail").TextContent.ShouldContain("counter-1"));

        await cut.InvokeAsync(() => dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-filtered",
            [
                Counter("counter-2", 2),
            ])));

        await cut.WaitForAssertionAsync(() => {
            IElement banner = cut.Find("[data-testid='fc-expanded-row-hidden-banner']");
            banner.GetAttribute("role").ShouldBe("status");
            banner.GetAttribute("aria-live").ShouldBe("polite");
            banner.TextContent.ShouldContain("1 expanded item hidden by current filter");
            banner.TextContent.ShouldContain("Clear filter");

            IElement region = cut.Find(".fc-expand-in-row-detail");
            region.GetAttribute("role").ShouldBe("region");
            region.GetAttribute("aria-label").ShouldBe("Details for counter-1");
            region.TextContent.ShouldNotContain("counter-1");

            IElement liveRegion = cut.Find(".fc-expand-in-row-suppression-live-region");
            liveRegion.GetAttribute("role").ShouldBe("status");
            liveRegion.GetAttribute("aria-live").ShouldBe("polite");
            liveRegion.GetAttribute("aria-atomic").ShouldBe("true");
            liveRegion.TextContent.ShouldContain("Your expanded item is hidden by the current filter");
        });
    }

    [Fact]
    public async Task CounterProjectionView_CollapseTrigger_EmptiesRegionAndResetsAria() {
        SetupExpandInRowModule();
        using CultureScope cultureScope = new("en");
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        List<object> dispatchedActions = [];
        dispatcher.ActionDispatched += (_, e) => dispatchedActions.Add(e.Action);
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-collapse",
            [
                Counter("counter-1", 1),
                Counter("counter-2", 2),
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        // Expand first, then restore reducer state (the base harness does not scan Shell reducers).
        await cut.WaitForAssertionAsync(() => _ = ExpandButton(cut, "counter-1"));
        await cut.InvokeAsync(() => ExpandButton(cut, "counter-1").Click());
        ExpandRowAction expandAction = await WaitForExpandActionAsync(cut, dispatchedActions, "counter-1");
        await cut.InvokeAsync(() => RestoreExpandedRowState(expandAction));
        await cut.WaitForAssertionAsync(() => cut.Find(".fc-expand-in-row-detail").TextContent.ShouldContain("counter-1"));

        // Toggling the same row's trigger dispatches CollapseRowAction; restore the emptied slice.
        await cut.InvokeAsync(() => CollapseButton(cut, "counter-1").Click());
        await WaitForCollapseActionAsync(cut, dispatchedActions, expandAction.ViewKey);
        await cut.InvokeAsync(RestoreCollapsedRowState);

        await cut.WaitForAssertionAsync(() => {
            IElement region = cut.Find(".fc-expand-in-row-detail");
            region.GetAttribute("role").ShouldBe("region");
            region.GetAttribute("aria-label").ShouldBe("Details for ");
            region.TextContent.ShouldNotContain("counter-1");

            IElement button = ExpandButton(cut, "counter-1");
            button.GetAttribute("aria-expanded").ShouldBe("false");
            region.GetAttribute("id").ShouldBe(button.GetAttribute("aria-controls"));
        });
    }

    [Fact]
    public async Task CounterProjectionView_ExpandingSecondRow_ReplacesFirstExpansion() {
        SetupExpandInRowModule();
        using CultureScope cultureScope = new("en");
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        List<object> dispatchedActions = [];
        dispatcher.ActionDispatched += (_, e) => dispatchedActions.Add(e.Action);
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-replace",
            [
                Counter("counter-1", 1),
                Counter("counter-2", 2),
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();

        await cut.WaitForAssertionAsync(() => _ = ExpandButton(cut, "counter-1"));
        await cut.InvokeAsync(() => ExpandButton(cut, "counter-1").Click());
        ExpandRowAction firstExpand = await WaitForExpandActionAsync(cut, dispatchedActions, "counter-1");
        await cut.InvokeAsync(() => RestoreExpandedRowState(firstExpand));
        await cut.WaitForAssertionAsync(() => cut.Find(".fc-expand-in-row-detail").TextContent.ShouldContain("counter-1"));

        // Single-expand invariant (UX-DR17): both triggers in the same grid emit the SAME ephemeral
        // view key at runtime — the precondition the reducer's REPLACE keys off (the REPLACE itself is
        // pinned in ExpandedRowReducerTests.ExpandRowAction_ReplacesEntry_OnSameViewKey). With the
        // restored single-key slice, one populated region remains and the first row reverts to collapsed.
        await cut.InvokeAsync(() => ExpandButton(cut, "counter-2").Click());
        ExpandRowAction secondExpand = await WaitForExpandActionAsync(cut, dispatchedActions, "counter-2");
        secondExpand.ViewKey.ShouldBe(firstExpand.ViewKey);
        await cut.InvokeAsync(() => RestoreExpandedRowState(secondExpand));

        await cut.WaitForAssertionAsync(() => {
            IElement region = cut.Find(".fc-expand-in-row-detail");
            region.GetAttribute("aria-label").ShouldBe("Details for counter-2");
            region.TextContent.ShouldContain("counter-2");
            region.TextContent.ShouldNotContain("counter-1");
            CollapseButton(cut, "counter-2").GetAttribute("aria-expanded").ShouldBe("true");
            ExpandButton(cut, "counter-1").GetAttribute("aria-expanded").ShouldBe("false");
        });
    }

    [Fact]
    public async Task CounterProjectionView_ExpandedRow_HasNoBlockingAxeContractViolations() {
        SetupExpandInRowModule();
        using CultureScope cultureScope = new("en");
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();
        List<object> dispatchedActions = [];
        dispatcher.ActionDispatched += (_, e) => dispatchedActions.Add(e.Action);
        dispatcher.Dispatch(new CounterProjectionLoadedAction(
            "story-2-4-axe",
            [
                Counter("counter-1", 1),
                Counter("counter-2", 2),
            ]));

        IRenderedComponent<CounterProjectionView> cut = Render<CounterProjectionView>();
        await cut.WaitForAssertionAsync(() => _ = ExpandButton(cut, "counter-1"));
        await cut.InvokeAsync(() => ExpandButton(cut, "counter-1").Click());
        ExpandRowAction expandAction = await WaitForExpandActionAsync(cut, dispatchedActions, "counter-1");
        await cut.InvokeAsync(() => RestoreExpandedRowState(expandAction));
        await cut.WaitForAssertionAsync(() => cut.Find(".fc-expand-in-row-detail").TextContent.ShouldContain("counter-1"));

        // In-process axe proxy over the EXPANDED generated grid (AC3). Real axe.run() DOM walking is
        // the CI-only Playwright lane; bUnit cannot drive the FluentUI v5 shadow DOM, so we assert the
        // ARIA contract the blocking axe rules enforce on the expand surface.
        await cut.WaitForAssertionAsync(() => {
            IElement region = cut.Find(".fc-expand-in-row-detail");
            // axe `region` — the detail landmark carries role=region + a discernible accessible name.
            region.GetAttribute("role").ShouldBe("region");
            string.IsNullOrWhiteSpace(region.GetAttribute("aria-label")).ShouldBeFalse();

            IElement trigger = CollapseButton(cut, "counter-1");
            // axe `button-name` — the toggle exposes a discernible accessible name.
            string.IsNullOrWhiteSpace(trigger.GetAttribute("aria-label")).ShouldBeFalse();
            // axe `aria-valid-attr-value` — aria-controls must resolve to exactly one present element.
            string? controls = trigger.GetAttribute("aria-controls");
            controls.ShouldBe(region.GetAttribute("id"));
            cut.Nodes.QuerySelectorAll($"[id='{controls}']").Length.ShouldBe(1);
            // axe state contract — aria-expanded reflects the expanded row.
            trigger.GetAttribute("aria-expanded").ShouldBe("true");
            // the suppressed-announcement live region stays silent while the row is visible.
            cut.Find(".fc-expand-in-row-suppression-live-region").TextContent.Trim().ShouldBeEmpty();
        });
    }

    private static async Task WaitForCollapseActionAsync(
        IRenderedComponent<CounterProjectionView> cut,
        List<object> dispatchedActions,
        string viewKey)
        => await cut.WaitForAssertionAsync(() =>
            dispatchedActions
                .OfType<CollapseRowAction>()
                .Any(action => string.Equals(action.ViewKey, viewKey, StringComparison.Ordinal))
                .ShouldBeTrue("generated collapse trigger must dispatch CollapseRowAction before the test empties reducer state"));

    private void RestoreCollapsedRowState() {
        IFeature feature = Services.GetRequiredService<ExpandedRowFeature>();
        feature.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty,
        });
    }

    private static async Task<ExpandRowAction> WaitForExpandActionAsync(
        IRenderedComponent<CounterProjectionView> cut,
        List<object> dispatchedActions,
        string id) {
        await cut.WaitForAssertionAsync(() =>
            dispatchedActions
                .OfType<ExpandRowAction>()
                .Any(action => string.Equals(action.ItemKey?.ToString(), id, StringComparison.Ordinal))
                .ShouldBeTrue("generated expand trigger must dispatch ExpandRowAction before the test restores reducer state"));

        return dispatchedActions
            .OfType<ExpandRowAction>()
            .Single(action => string.Equals(action.ItemKey?.ToString(), id, StringComparison.Ordinal));
    }

    private void RestoreExpandedRowState(ExpandRowAction action) {
        IFeature feature = Services.GetRequiredService<ExpandedRowFeature>();
        feature.RestoreState(new ExpandedRowState {
            ExpandedByViewKey = ImmutableDictionary<string, ExpandedRowEntry>.Empty.SetItem(
                action.ViewKey,
                new ExpandedRowEntry(action.ItemKey, s_lastUpdated)),
        });
    }

    private void SetupExpandInRowModule() {
        BunitJSModuleInterop module = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-expandinrow.js");
        module.SetupVoid("initializeExpandInRow", _ => true);
    }

    private static CounterProjection Counter(string id, int count)
        => new() {
            Id = id,
            Count = count,
            LastUpdated = s_lastUpdated,
        };

    private static IElement ExpandButton(IRenderedComponent<CounterProjectionView> cut, string id)
        => Button(cut, $"Expand details for {id}");

    private static IElement CollapseButton(IRenderedComponent<CounterProjectionView> cut, string id)
        => Button(cut, $"Collapse details for {id}");

    private static IElement Button(IRenderedComponent<CounterProjectionView> cut, string ariaLabel)
        => cut.Nodes.QuerySelectorAll("fluent-button.fc-expand-button")
            .Single(button => string.Equals(button.GetAttribute("aria-label"), ariaLabel, StringComparison.Ordinal));

    private sealed class CultureScope : IDisposable {
        private readonly CultureInfo _originalCulture;
        private readonly CultureInfo _originalUICulture;

        public CultureScope(string cultureName) {
            CultureInfo culture = new(cultureName);
            _originalCulture = CultureInfo.CurrentCulture;
            _originalUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }

        public void Dispose() {
            CultureInfo.CurrentCulture = _originalCulture;
            CultureInfo.CurrentUICulture = _originalUICulture;
        }
    }
}
