#pragma warning disable CA2007
using System.Globalization;
using System.Linq;

using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 4-4 T4.1 / D6 / D19 — bUnit coverage for <see cref="FcColumnPrioritizer"/>:
/// gear-icon renders with hidden-count aria-label, dispatches actions on toggle and reset,
/// renders popover with checkbox list, and swaps aria-label at N=0.
/// </summary>
public sealed class FcColumnPrioritizerTests : BunitContext {
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();

    public FcColumnPrioritizerTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");

        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IDispatcher>(_dispatcher);
        Services.AddLogging();
        Services.AddLocalization();
        Services.AddFluentUIComponents();
    }

    private static ColumnDescriptor[] MakeColumns(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new ColumnDescriptor("Col" + i, "Column " + i, i))
            .ToArray();

    private IRenderedComponent<FcColumnPrioritizer> RenderPrioritizer(
        ColumnDescriptor[] columns,
        string[]? hidden = null) {
        RenderFragment<ColumnVisibilityContext> child = ctx => builder => builder.AddContent(0, "child");
        return Render<FcColumnPrioritizer>(parameters => parameters
            .Add(p => p.ViewKey, "acme:OrdersProjection")
            .Add(p => p.AllColumns, columns)
            .Add(p => p.HiddenColumns, hidden ?? Array.Empty<string>())
            .Add(p => p.MaxVisibleColumns, 10)
            .Add(p => p.ChildContent, child));
    }

    [Fact]
    public void Gear_RendersTopRightWithHiddenCountAriaLabel() {
        IRenderedComponent<FcColumnPrioritizer> cut = RenderPrioritizer(
            MakeColumns(16),
            hidden: new[] { "Col11", "Col12", "Col13" });

        AngleSharp.Dom.IElement gear = cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]");
        (gear.GetAttribute("aria-label") ?? string.Empty).ShouldContain("Hidden columns: 3");
        gear.GetAttribute("aria-haspopup").ShouldBe("dialog");
        gear.GetAttribute("aria-expanded").ShouldBe("false");
    }

    [Fact]
    public void Gear_SwapsAriaLabel_WhenNoColumnsHidden() {
        IRenderedComponent<FcColumnPrioritizer> cut = RenderPrioritizer(
            MakeColumns(16),
            hidden: Array.Empty<string>());

        AngleSharp.Dom.IElement gear = cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]");
        gear.GetAttribute("aria-label").ShouldBe("All columns visible. Open column visibility settings.");
    }

    [Fact]
    public void ClickingGear_OpensPopover_WithCheckboxForEachColumn() {
        IRenderedComponent<FcColumnPrioritizer> cut = RenderPrioritizer(
            MakeColumns(16),
            hidden: new[] { "Col11" });

        cut.FindAll("[data-testid=\"fc-column-prioritizer-popover\"]").Count.ShouldBe(0);

        cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]").Click();

        cut.FindAll("[data-testid=\"fc-column-prioritizer-popover\"]").Count.ShouldBe(1);
        cut.FindAll("[data-testid^=\"fc-column-prioritizer-checkbox-\"]").Count.ShouldBe(16);
    }

    [Fact]
    public void ClickingCheckbox_DispatchesColumnVisibilityChangedAction() {
        IRenderedComponent<FcColumnPrioritizer> cut = RenderPrioritizer(
            MakeColumns(16),
            hidden: Array.Empty<string>());

        cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]").Click();

        // Click the checkbox for Col3 → becomes hidden (was visible).
        AngleSharp.Dom.IElement checkbox = cut.Find("[data-testid=\"fc-column-prioritizer-checkbox-Col3\"]");
        checkbox.Change(false);

        _dispatcher.Received(1).Dispatch(Arg.Is<ColumnVisibilityChangedAction>(a =>
            a.ViewKey == "acme:OrdersProjection"
            && a.ColumnKey == "Col3"
            && a.IsVisible == false));
    }

    [Fact]
    public void ClickingResetButton_DispatchesResetColumnVisibilityAction() {
        IRenderedComponent<FcColumnPrioritizer> cut = RenderPrioritizer(
            MakeColumns(16),
            hidden: new[] { "Col11", "Col12" });

        cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]").Click();
        cut.Find("[data-testid=\"fc-column-prioritizer-reset\"]").Click();

        _dispatcher.Received(1).Dispatch(Arg.Is<ResetColumnVisibilityAction>(a =>
            a.ViewKey == "acme:OrdersProjection"));
    }

    [Fact]
    public void RootDiv_CarriesPrioritizerClass_AndViewKeyAttribute() {
        IRenderedComponent<FcColumnPrioritizer> cut = RenderPrioritizer(
            MakeColumns(16),
            hidden: Array.Empty<string>());

        AngleSharp.Dom.IElement root = cut.Find("div.fc-column-prioritizer");
        root.GetAttribute("data-fc-column-prioritizer").ShouldBe("acme:OrdersProjection");
    }

    [Fact]
    public void Popover_CarriesDialogRole_AndLabelledByHeader() {
        IRenderedComponent<FcColumnPrioritizer> cut = RenderPrioritizer(
            MakeColumns(16),
            hidden: Array.Empty<string>());

        cut.Find("[data-testid=\"fc-column-prioritizer-gear\"]").Click();

        AngleSharp.Dom.IElement popover = cut.Find("[data-testid=\"fc-column-prioritizer-popover\"]");
        popover.GetAttribute("role").ShouldBe("dialog");
        string? labelledBy = popover.GetAttribute("aria-labelledby");
        labelledBy.ShouldNotBeNullOrEmpty();
        cut.Find("#" + labelledBy!).TextContent.ShouldContain("Column visibility");
    }
}
