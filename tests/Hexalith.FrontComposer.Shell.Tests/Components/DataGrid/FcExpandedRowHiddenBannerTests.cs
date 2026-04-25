#pragma warning disable CA2007
using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 4-5 T4.7 / AC8 — bUnit coverage for the AC8 breadcrumb banner. Three tests:
/// renders nothing on no-suppression, renders banner copy when IsHiddenByFilter is true,
/// "Clear filter" link dispatches FiltersResetAction.
/// </summary>
public sealed class FcExpandedRowHiddenBannerTests : BunitContext {
    private readonly IDispatcher _dispatcher = Substitute.For<IDispatcher>();
    private readonly IStringLocalizer<FcShellResources> _localizer = Substitute.For<IStringLocalizer<FcShellResources>>();

    public FcExpandedRowHiddenBannerTests() {
        JSInterop.Mode = JSRuntimeMode.Loose;
        _localizer["ExpandedRowHiddenByFilterBanner", Arg.Any<object[]>()]
            .Returns(callInfo => new LocalizedString(
                "ExpandedRowHiddenByFilterBanner",
                "1 expanded item hidden by current filter"));
        _localizer["ExpandedRowHiddenByFilterBannerClearLink"]
            .Returns(new LocalizedString("ExpandedRowHiddenByFilterBannerClearLink", "Clear filter"));

        Services.AddSingleton(_dispatcher);
        Services.AddSingleton(_localizer);
        Services.AddLogging();
        Services.AddFluentUIComponents();
    }

    [Fact]
    public void RendersNothing_WhenNotHiddenByFilter() {
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(parameters => parameters
            .Add(p => p.ViewKey, "orders:Orders")
            .Add(p => p.IsHiddenByFilter, false));

        cut.FindAll("[data-testid='fc-expanded-row-hidden-banner']").Count.ShouldBe(0);
    }

    [Fact]
    public void RendersBannerCopy_WhenHiddenByFilter() {
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(parameters => parameters
            .Add(p => p.ViewKey, "orders:Orders")
            .Add(p => p.IsHiddenByFilter, true));

        AngleSharp.Dom.IElement banner = cut.Find("[data-testid='fc-expanded-row-hidden-banner']");
        banner.GetAttribute("role").ShouldBe("status");
        banner.GetAttribute("aria-live").ShouldBe("polite");
        banner.TextContent.ShouldContain("hidden by current filter");
    }

    [Fact]
    public void ClearFilterLink_DispatchesFiltersResetAction() {
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(parameters => parameters
            .Add(p => p.ViewKey, "orders:Orders")
            .Add(p => p.IsHiddenByFilter, true));

        cut.Find("[data-testid='fc-expanded-row-hidden-banner-clear']").Click();

        _dispatcher.Received(1).Dispatch(Arg.Is<FiltersResetAction>(a => a.ViewKey == "orders:Orders"));
    }
}
