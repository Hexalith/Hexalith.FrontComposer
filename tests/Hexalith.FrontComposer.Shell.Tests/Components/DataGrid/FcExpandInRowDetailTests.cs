#pragma warning disable CA2007
using Bunit;

using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Services;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 4-5 T1.5 / D7 / D8 / D19 — bUnit coverage for the expand-in-row detail wrapper.
/// </summary>
public sealed class FcExpandInRowDetailTests : BunitContext {
    private readonly IExpandInRowJSModule _expandInRowModule = Substitute.For<IExpandInRowJSModule>();

    public FcExpandInRowDetailTests() {
        _ = _expandInRowModule.InitializeAsync(Arg.Any<ElementReference>()).Returns(Task.CompletedTask);

        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton(_expandInRowModule);
        Services.AddLogging();
        Services.AddFluentUIComponents();
    }

    [Fact]
    public void Expanded_RendersChildContentInsideRegion() {
        IRenderedComponent<FcExpandInRowDetail> cut = RenderDetail(hasExpanded: true);

        AngleSharp.Dom.IElement region = cut.Find("div.fc-expand-in-row-detail");
        region.GetAttribute("role").ShouldBe("region");
        region.TextContent.ShouldContain("Order details");
    }

    [Fact]
    public void Collapsed_KeepsRegionButSuppressesChildContent() {
        IRenderedComponent<FcExpandInRowDetail> cut = RenderDetail(hasExpanded: false);

        cut.Find("div.fc-expand-in-row-detail").GetAttribute("role").ShouldBe("region");
        cut.Markup.ShouldNotContain("Order details");
    }

    [Fact]
    public void Region_UsesProvidedAriaLabel() {
        IRenderedComponent<FcExpandInRowDetail> cut = RenderDetail(
            hasExpanded: true,
            detailPanelAriaLabel: "Expanded order 123");

        cut.Find("div.fc-expand-in-row-detail")
            .GetAttribute("aria-label")
            .ShouldBe("Expanded order 123");
    }

    [Fact]
    public void Expanding_InvokesScrollStabilizerWithNonDefaultElementReference() {
        IRenderedComponent<FcExpandInRowDetail> cut = RenderDetail(hasExpanded: false);

        cut.Render(parameters => parameters
            .Add(p => p.ViewKey, "orders:view:instance")
            .Add(p => p.HasExpanded, true)
            .Add(p => p.DetailPanelAriaLabel, "Expanded order")
            .Add(p => p.ChildContent, ChildContent()));

        _expandInRowModule.Received(1).InitializeAsync(Arg.Is<ElementReference>(e =>
            !string.IsNullOrWhiteSpace(e.Id)));
    }

    [Fact]
    public void RerenderWhileExpanded_DoesNotInvokeScrollStabilizerAgain() {
        IRenderedComponent<FcExpandInRowDetail> cut = RenderDetail(hasExpanded: true);

        cut.Render(parameters => parameters
            .Add(p => p.ViewKey, "orders:view:instance")
            .Add(p => p.HasExpanded, true)
            .Add(p => p.DetailPanelAriaLabel, "Expanded order updated")
            .Add(p => p.ChildContent, ChildContent("Order details updated")));

        _expandInRowModule.Received(1).InitializeAsync(Arg.Any<ElementReference>());
    }

    [Fact]
    public void SuppressedAnnouncement_RendersPoliteLiveRegion() {
        IRenderedComponent<FcExpandInRowDetail> cut = Render<FcExpandInRowDetail>(parameters => parameters
            .Add(p => p.ViewKey, "orders:view:instance")
            .Add(p => p.HasExpanded, false)
            .Add(p => p.DetailPanelAriaLabel, "Expanded order")
            .Add(p => p.SuppressedAnnouncement, "Expanded item hidden by current filter.")
            .Add(p => p.ChildContent, ChildContent()));

        AngleSharp.Dom.IElement liveRegion = cut.Find("div[role=\"status\"]");
        liveRegion.GetAttribute("aria-live").ShouldBe("polite");
        liveRegion.GetAttribute("aria-atomic").ShouldBe("true");
        liveRegion.TextContent.ShouldContain("Expanded item hidden by current filter.");
    }

    private IRenderedComponent<FcExpandInRowDetail> RenderDetail(
        bool hasExpanded,
        string detailPanelAriaLabel = "Expanded order") =>
        Render<FcExpandInRowDetail>(parameters => parameters
            .Add(p => p.ViewKey, "orders:view:instance")
            .Add(p => p.HasExpanded, hasExpanded)
            .Add(p => p.DetailPanelAriaLabel, detailPanelAriaLabel)
            .Add(p => p.ChildContent, ChildContent()));

    private static RenderFragment ChildContent(string text = "Order details") =>
        builder => builder.AddContent(0, text);
}
