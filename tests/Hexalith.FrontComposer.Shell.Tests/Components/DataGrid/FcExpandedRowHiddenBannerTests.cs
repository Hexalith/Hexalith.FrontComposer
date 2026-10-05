using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services.Announcements;

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
        _localizer["Am22DetailHidden"]
            .Returns(new LocalizedString("Am22DetailHidden", "Expanded item hidden by current filter."));

        Services.AddSingleton(_dispatcher);
        Services.AddSingleton(_localizer);
        Services.AddLogging();
        Services.AddFluentUIComponents();
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(new SurfaceAnnouncementCoordinator(TimeProvider.System));
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
        banner.GetAttribute("role").ShouldBe("group");
        banner.GetAttribute("aria-live").ShouldBe("off");
        banner.TextContent.ShouldContain("hidden by current filter");
        string? reasonId = cut.Find("[data-testid='fc-expanded-row-hidden-banner-clear']").GetAttribute("aria-describedby");
        reasonId.ShouldNotBeNullOrWhiteSpace();
        cut.Find("#" + reasonId).TextContent.ShouldContain("hidden by current filter");
    }

    [Fact]
    public void ClearFilterLink_DispatchesFiltersResetAction() {
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(parameters => parameters
            .Add(p => p.ViewKey, "orders:Orders")
            .Add(p => p.IsHiddenByFilter, true));

        cut.Find("[data-testid='fc-expanded-row-hidden-banner-clear']").Click();

        _dispatcher.Received(1).Dispatch(ArgEx.Is<FiltersResetAction>(a => a.ViewKey == "orders:Orders"));
    }

    [Fact]
    public void SecondHiddenResultSpeaksOnce() {
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:orders:Orders", spoken.Add);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "row-a/filter-1"));

        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "row-b/filter-2"));
        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "row-b/filter-2"));

        spoken.Where(message => message.Length > 0).ShouldBe(["Expanded item hidden by current filter."]);

        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false)
            .Add(c => c.ResultIdentity, "row-b/filter-2"));
        announcements.Current("projection:orders:Orders").ShouldBe(string.Empty);
    }

    [Fact]
    public void FirstHiddenResultFocusesClearFilterButLaterResultsDoNotRefocus() {
        _ = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true).SetResult(true);
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, false));

        announcements.Announce("projection:orders:Orders", "query:failed", "failed", "Data could not be loaded.", terminal: true);

        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1"));
        cut.WaitForAssertion(() => _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1));
        announcements.Current("projection:orders:Orders").ShouldBe("Data could not be loaded.");

        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-2"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);
    }

    [Fact]
    public void FalseFocusOutcomeRetriesOnLaterRender() {
        var focus = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js")
            .Setup<bool>("focusFirstButtonWithin", _ => true);
        _ = focus.SetResult(false);
        IRenderedComponent<FcExpandedRowHiddenBanner> cut = Render<FcExpandedRowHiddenBanner>(p => p
            .Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 1);

        _ = focus.SetResult(true);
        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 2);
        cut.Render(p => p.Add(c => c.ViewKey, "orders:Orders").Add(c => c.IsHiddenByFilter, true)
            .Add(c => c.ResultIdentity, "filter-1"));
        _ = JSInterop.VerifyInvoke("focusFirstButtonWithin", 2);
    }
}
