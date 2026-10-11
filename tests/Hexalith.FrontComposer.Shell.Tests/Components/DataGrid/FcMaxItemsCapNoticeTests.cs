using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.Components.DataGrid;
using Hexalith.FrontComposer.Shell.Services.Announcements;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DataGrid;

/// <summary>
/// Story 4-4 T4.3 / D11 / AC4 — <see cref="FcMaxItemsCapNotice"/> visibility is driven by
/// (<see cref="FcMaxItemsCapNotice.ItemsCount"/>, <see cref="FcMaxItemsCapNotice.AnyRealFilterActive"/>)
/// against <see cref="FcShellOptions.MaxUnfilteredItems"/>. The banner is non-dismissing per D11.
/// </summary>
public sealed class FcMaxItemsCapNoticeTests : BunitContext {
    [Fact]
    public void DirectNoticeKeepsOnePersistentPoliteOwnerAcrossTransitionsAndRemount()
    {
        IRenderedComponent<FcMaxItemsCapNotice> cut = RenderNotice(9999, false);
        cut.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
        cut.Find("[data-testid='fc-surface-status']").TextContent.ShouldBeEmpty();
        cut.Render(p => p.Add(c => c.ItemsCount, 10000).Add(c => c.ResultIdentity, "result"));
        cut.Find("[data-testid='fc-surface-status']").TextContent
            .ShouldBe("Showing the first 10000 items. Refine filters to narrow the result.");
        cut.Find("[data-testid='fc-max-items-cap-notice']").GetAttribute("aria-live").ShouldBe("off");
        cut.FindComponent<Hexalith.FrontComposer.Shell.Components.Rendering.FcSurfaceStatus>().Instance.Dispose();
        cut.Instance.Dispose();
        cut.Dispose();
        IRenderedComponent<FcMaxItemsCapNotice> remounted = Render<FcMaxItemsCapNotice>(p => p
            .Add(c => c.ViewKey, ViewKey).Add(c => c.ItemsCount, 10000).Add(c => c.AnyRealFilterActive, false)
            .Add(c => c.ResultIdentity, "result"));
        remounted.FindAll("[role='status'][aria-live='polite']").Count.ShouldBe(1);
        remounted.Find("[data-testid='fc-surface-status']").TextContent.ShouldBeEmpty();
    }

    private const string ViewKey = "acme:OrdersProjection";

    public FcMaxItemsCapNoticeTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        JSInterop.Mode = JSRuntimeMode.Loose;

        Services.AddSingleton<IOptionsMonitor<FcShellOptions>>(
            MakeOptionsMonitor(new FcShellOptions { MaxUnfilteredItems = 10_000 }));
        Services.AddLogging();
        Services.AddLocalization();
        Services.AddFluentUIComponents();
        Services.AddSingleton<ISurfaceAnnouncementCoordinator>(new SurfaceAnnouncementCoordinator(TimeProvider.System));
    }

    private static IOptionsMonitor<FcShellOptions> MakeOptionsMonitor(FcShellOptions options) {
        IOptionsMonitor<FcShellOptions> monitor = Substitute.For<IOptionsMonitor<FcShellOptions>>();
        monitor.CurrentValue.Returns(options);
        return monitor;
    }

    private IRenderedComponent<FcMaxItemsCapNotice> RenderNotice(int itemsCount, bool anyRealFilterActive) =>
        Render<FcMaxItemsCapNotice>(p => p
            .Add(c => c.ViewKey, ViewKey)
            .Add(c => c.ItemsCount, itemsCount)
            .Add(c => c.AnyRealFilterActive, anyRealFilterActive));

    [Fact]
    public void Renders_WhenCountMeetsCap_AndNoFiltersActive() {
        IRenderedComponent<FcMaxItemsCapNotice> cut = RenderNotice(itemsCount: 10_000, anyRealFilterActive: false);

        AngleSharp.Dom.IElement banner = cut.Find("[data-testid=\"fc-max-items-cap-notice\"]");
        banner.TextContent.ShouldContain("10000");
        banner.TextContent.ShouldContain("Use filters");
    }

    [Fact]
    public void Renders_WhenCountExceedsCap() {
        IRenderedComponent<FcMaxItemsCapNotice> cut = RenderNotice(itemsCount: 10_001, anyRealFilterActive: false);

        cut.FindAll("[data-testid=\"fc-max-items-cap-notice\"]").Count.ShouldBe(1);
    }

    [Fact]
    public void DoesNotRender_WhenBelowCap() {
        IRenderedComponent<FcMaxItemsCapNotice> cut = RenderNotice(itemsCount: 9_999, anyRealFilterActive: false);

        cut.FindAll("[data-testid=\"fc-max-items-cap-notice\"]").Count.ShouldBe(0);
    }

    [Fact]
    public void DoesNotRender_WhenFiltersActive_EvenAtCap() {
        IRenderedComponent<FcMaxItemsCapNotice> cut = RenderNotice(itemsCount: 15_000, anyRealFilterActive: true);

        cut.FindAll("[data-testid=\"fc-max-items-cap-notice\"]").Count.ShouldBe(0);
    }

    [Fact]
    public void SecondCompletedCapResultSpeaksOnce() {
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:" + ViewKey, spoken.Add);
        IRenderedComponent<FcMaxItemsCapNotice> cut = Render<FcMaxItemsCapNotice>(p => p
            .Add(c => c.ViewKey, ViewKey).Add(c => c.ItemsCount, 10_000)
            .Add(c => c.AnyRealFilterActive, false).Add(c => c.ResultIdentity, "result-1"));

        cut.Render(p => p.Add(c => c.ViewKey, ViewKey).Add(c => c.ItemsCount, 10_000)
            .Add(c => c.AnyRealFilterActive, false).Add(c => c.ResultIdentity, "result-2"));
        cut.Render(p => p.Add(c => c.ViewKey, ViewKey).Add(c => c.ItemsCount, 10_000)
            .Add(c => c.AnyRealFilterActive, false).Add(c => c.ResultIdentity, "result-2"));

        spoken.Count(message => message.Length > 0).ShouldBe(2);

        cut.Render(p => p.Add(c => c.ViewKey, ViewKey).Add(c => c.ItemsCount, 9_999)
            .Add(c => c.AnyRealFilterActive, false).Add(c => c.ResultIdentity, "result-2"));
        announcements.Current("projection:" + ViewKey).ShouldBe(string.Empty);
    }
    [Fact]
    public void ChangedCapOnTheSameResultSpeaksItsNewLimitOnce()
    {
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        List<string> spoken = [];
        using IDisposable subscription = announcements.Subscribe("projection:" + ViewKey, spoken.Add);
        IRenderedComponent<FcMaxItemsCapNotice> cut = Render<FcMaxItemsCapNotice>(p => p
            .Add(c => c.ViewKey, ViewKey).Add(c => c.ItemsCount, 20000)
            .Add(c => c.AnyRealFilterActive, false).Add(c => c.ResultIdentity, "same-result"));
        Services.GetRequiredService<IOptionsMonitor<FcShellOptions>>().CurrentValue.MaxUnfilteredItems = 20000;
        cut.Render();
        cut.Render();
        spoken.Where(message => message.Length > 0).ToArray().ShouldBe([
            "Showing the first 10000 items. Refine filters to narrow the result.",
            "Showing the first 20000 items. Refine filters to narrow the result."]);
        cut.Find("[data-testid='fc-max-items-cap-notice']").TextContent.ShouldContain("20000");
    }
    [Fact]
    public void EqualInputSurfaceRebindWithdrawsOriginalAndPublishesReplacement()
    {
        ISurfaceAnnouncementCoordinator announcements = Services.GetRequiredService<ISurfaceAnnouncementCoordinator>();
        IRenderedComponent<FcMaxItemsCapNotice> cut = Render<FcMaxItemsCapNotice>(p => p.Add(c => c.ViewKey, ViewKey)
            .Add(c => c.ItemsCount, 10000).Add(c => c.AnyRealFilterActive, false).Add(c => c.ResultIdentity, "same"));
        announcements.Current("projection:" + ViewKey).ShouldNotBeEmpty();
        cut.Render(p => p.Add(c => c.ViewKey, "replacement").Add(c => c.ItemsCount, 10000)
            .Add(c => c.AnyRealFilterActive, false).Add(c => c.ResultIdentity, "same"));
        announcements.Current("projection:" + ViewKey).ShouldBeEmpty();
        announcements.Current("projection:replacement").ShouldContain("10000");
        announcements.Announce("projection:replacement", "other", "result", "Keep other speech", immediate: true);
        cut.Instance.Dispose();
        cut.Dispose();
        announcements.Current("projection:replacement").ShouldBe("Keep other speech");
    }


}
