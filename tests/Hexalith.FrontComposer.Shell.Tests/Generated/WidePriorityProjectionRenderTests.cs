#pragma warning disable CA2007
using System;
using System.Linq;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Shell.Tests;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Generated;

/// <summary>
/// Story 2.5 AC1 + AC2 — end-to-end render pin for a generated WIDE (&gt;15-column) projection grid.
/// The isolated layers are already pinned: the emitter wrap (<c>RazorEmitterColumnPrioritizerTests</c>,
/// presence-only), the component (<c>FcColumnPrioritizerTests</c>, hand-built columns), and the
/// transform sort (<c>RazorModelColumnPriorityOrderTests</c>, ColumnModel order). This test closes the
/// remaining gap — that an actual GENERATED wide grid (a) is wrapped by <c>FcColumnPrioritizer</c>
/// (AC1) and (b) exposes its columns in priority-then-declaration ORDER (AC2) at render time — by
/// rendering the generated <c>WidePriorityProjectionView</c> through bUnit, mirroring the Story 2.3
/// (<c>BadgeProjectionRenderTests</c>) and Story 2.4 (<c>ExpandInRowGeneratedGridTests</c>) precedent.
/// The AC2 order is asserted against the prioritizer popover's checkbox sequence — which renders the
/// generator-emitted <c>_allColumnsDescriptor</c> verbatim — rather than the FluentUI-v5 data-grid
/// header DOM, keeping the order assertion robust (per the story's brittleness caveat).
/// </summary>
public sealed class WidePriorityProjectionRenderTests : GeneratedComponentTestBase
{
    private const string CheckboxTestIdPrefix = "fc-column-prioritizer-checkbox-";

    public WidePriorityProjectionRenderTests()
        : base(typeof(WidePriorityProjection).Assembly)
    {
    }

    [Fact]
    public async Task WideGrid_WrapsGeneratedGridInColumnPrioritizer_WithDefaultHiddenCount()
    {
        using CultureScope cultureScope = new("en");
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch(new WidePriorityProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                Row("row-1"),
                Row("row-2"),
            ]));

        IRenderedComponent<WidePriorityProjectionView> cut = Render<WidePriorityProjectionView>();

        await cut.WaitForAssertionAsync(() =>
        {
            // AC1 — the generated grid is wrapped by FcColumnPrioritizer (the >15-column emitter branch).
            AngleSharp.Dom.IElement root = cut.Find("div.fc-column-prioritizer");
            (root.GetAttribute("data-fc-column-prioritizer") ?? string.Empty).ShouldNotBeNullOrEmpty();

            // AC1 — the gear affordance renders, and its aria-label reflects the eight columns hidden by
            // default (18 columns − MaxVisibleColumns 10 = 8).
            AngleSharp.Dom.IElement gear = cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]");
            (gear.GetAttribute("aria-label") ?? string.Empty).ShouldContain("Hidden columns: 8");
            gear.GetAttribute("aria-haspopup").ShouldBe("dialog");
            gear.GetAttribute("aria-expanded").ShouldBe("false");
        });
    }

    [Fact]
    public async Task WideGrid_OrdersColumnsByPriorityThenDeclaration_AtRenderTime()
    {
        using CultureScope cultureScope = new("en");
        await InitializeStoreAsync();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch(new WidePriorityProjectionLoadedAction(
            Guid.NewGuid().ToString(),
            [
                Row("row-1"),
            ]));

        IRenderedComponent<WidePriorityProjectionView> cut = Render<WidePriorityProjectionView>();

        // Open the prioritizer popover; its checkbox list renders AllColumns — the generator-emitted
        // _allColumnsDescriptor — in document order, one checkbox per column keyed by property name.
        await cut.WaitForAssertionAsync(() => _ = cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]"));
        await cut.InvokeAsync(() => cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]").Click());

        await cut.WaitForAssertionAsync(() =>
        {
            string[] renderedOrder = cut
                .FindAll($"[data-testid^=\"{CheckboxTestIdPrefix}\"]")
                .Select(node => node.GetAttribute("data-testid")![CheckboxTestIdPrefix.Length..])
                .ToArray();

            // AC2 — annotated columns ascend by priority, then unannotated columns trail in declaration
            // order via the int.MaxValue sentinel + index-stable tiebreaker. This order is DIFFERENT from
            // the specimen's declaration order, so a regression that dropped the sort (or its anyPriority
            // gate / index tiebreaker) would fail here.
            renderedOrder.ShouldBe(
            [
                "Gamma", "Delta", "Alpha", "Theta", "Zeta",
                "Id", "Beta", "Epsilon", "Eta", "Iota", "Kappa", "Lambda",
                "Mu", "Nu", "Xi", "Omicron", "Pi", "Rho",
            ]);
        });
    }

    private static WidePriorityProjection Row(string id)
        => new() { Id = id };
}
