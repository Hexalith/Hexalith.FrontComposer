using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Tests the body-level page tab and panel contract.
/// </summary>
public sealed class FcPageTabsTests : LayoutComponentTestBase
{
    [Fact]
    public void FcPageTabs_WithPanelContent_RendersExactlyOneControlledPanelPerTab()
    {
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "summary")
            .Add(tabs => tabs.AriaLabel, "Order sections")
            .Add(tabs => tabs.TestId, "orders-page-tabs")
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));

        IRenderedComponent<FluentTabs> fluentTabs = cut.FindComponent<FluentTabs>();
        fluentTabs.Instance.ActiveTabId.ShouldBe("summary");
        fluentTabs.Instance.Appearance.ShouldBe(TabsAppearance.Subtle);
        fluentTabs.Instance.Orientation.ShouldBe(Orientation.Horizontal);
        fluentTabs.Instance.Width.ShouldBe("100%");

        IElement tabsRoot = cut.Find("[data-testid='orders-page-tabs']");
        tabsRoot.GetAttribute("aria-label").ShouldBe("Order sections");

        foreach (string id in new[] { "summary", "activity" })
        {
            IElement tab = cut.FindAll($"#{id}").ShouldHaveSingleItem();
            string panelId = tab.GetAttribute("aria-controls").ShouldNotBeNull();
            panelId.ShouldBe($"{id}-panel");
            IElement panel = cut.FindAll($"#{panelId}").ShouldHaveSingleItem();
            panel.GetAttribute("role").ShouldBe("tabpanel");
            panel.QuerySelector($"[data-testid='{id}-content']").ShouldNotBeNull();
            panel.TextContent.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task FcPageTabs_ActiveTabChanged_RaisesCallerCallback()
    {
        string? observed = null;
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "summary")
            .Add(tabs => tabs.ActiveTabIdChanged, EventCallback.Factory.Create<string?>(this, value => observed = value))
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));

        await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync("activity"));

        observed.ShouldBe("activity");
    }

    [Fact]
    public void FcPageTabs_RouteAlias_SelectsDeclaredDefault()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/orders");
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));

        cut.FindComponent<FluentTabs>().Instance.ActiveTabId.ShouldBe("summary");
    }

    [Fact]
    public void FcPageTabs_RouteSelection_SelectsMatchingEnabledTab()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/orders/activity");
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));

        cut.FindComponent<FluentTabs>().Instance.ActiveTabId.ShouldBe("activity");
    }

    [Theory]
    [InlineData("bad/segment")]
    [InlineData("bad?query")]
    [InlineData("bad#fragment")]
    [InlineData("bad%2Fsegment")]
    [InlineData("..")]
    public void FcPageTabs_RouteBackedTabRejectsUnsafePathSegment(string id)
    {
        Should.Throw<InvalidOperationException>(() => Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .AddChildContent(SingleTab(id))));
    }

    [Fact]
    public void FcPageTabs_NonRouteBackedTabPreservesLegacyId()
    {
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "legacy/section")
            .AddChildContent(SingleTab("legacy/section")));

        cut.FindComponent<FluentTab>().Instance.Id.ShouldBe("legacy/section");
    }

    [Theory]
    [InlineData("/orders/ACTIVITY")]
    [InlineData("/orders/activity/")]
    public void FcPageTabs_MixedCaseOrTrailingSlash_SelectsTabWithoutFallback(string route)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(route);
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));

        cut.FindComponent<FluentTabs>().Instance.ActiveTabId.ShouldBe("activity");
        // The AM-31 live region always exists for route-backed tabs, so a later fallback is a text change.
        cut.FindAll("[data-testid='fc-module-tab-fallback']").Count.ShouldBe(1);
        cut.Find("[data-testid='fc-module-tab-fallback']").TextContent.Trim().ShouldBeEmpty();
    }

    [Fact]
    public async Task FcPageTabs_AdopterNavigation_PreservesPanelQuery()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/orders/summary");
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .Add(tabs => tabs.ActiveTabIdChanged, EventCallback.Factory.Create<string?>(this,
                _ => navigation.NavigateTo("/orders/activity?filter=retained")))
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));

        await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync("activity"));

        navigation.Uri.ShouldEndWith("/orders/activity?filter=retained");
    }

    [Fact]
    public async Task FcPageTabs_RouteDepartureDuringFocusInteropDoesNotInvokeOldCallback()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/orders/summary");
        bool callbackInvoked = false;
        var pendingFocus = FocusModule.SetupVoid("prepareTabNavigation", _ => true);
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .Add(tabs => tabs.ActiveTabIdChanged, EventCallback.Factory.Create<string?>(this,
                _ => callbackInvoked = true))
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));

        Task change = cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync("activity"));
        cut.WaitForAssertion(() => FocusModule.Invocations.Any(invocation => invocation.Identifier == "prepareTabNavigation").ShouldBeTrue());
        navigation.NavigateTo("/home");
        pendingFocus.SetVoidResult();
        await change.ConfigureAwait(true);

        callbackInvoked.ShouldBeFalse();
        navigation.Uri.ShouldEndWith("/home");
    }

    [Theory]
    [InlineData("/orders/missing")]
    [InlineData("/orders/activity")]
    public void FcPageTabs_InvalidOrDisabledRoute_SelectsDefaultWithOnePersistentStatus(string route)
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo(route);
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .AddChildContent(PageTabs(deferredLoading: false)));

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/orders/summary"));
        cut.FindComponent<FluentTabs>().Instance.ActiveTabId.ShouldBe("summary");
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='fc-module-tab-fallback']").Count.ShouldBe(1);
            cut.Find("[data-testid='fc-module-tab-fallback']").TextContent.ShouldContain("Summary");
        });
    }

    [Fact]
    public void FcPageTabs_RouteDepartureDuringPanelInteropCancelsInvalidTabFallback()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/orders/missing");
        var pendingLabels = FocusModule.SetupVoid("labelTabPanels", _ => true);
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));
        cut.WaitForAssertion(() => FocusModule.Invocations.Any(invocation => invocation.Identifier == "labelTabPanels").ShouldBeTrue());

        navigation.NavigateTo("/home");
        pendingLabels.SetVoidResult();

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/home"));
    }

    [Fact]
    public void FcPageTabs_RevisitedInvalidRoute_AnnouncesFallbackAgain()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/orders/missing");
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, "summary")
            .AddChildContent(PageTabs(deferredLoading: false, disableActivity: false)));

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/orders/summary"));
        navigation.NavigateTo("/orders/activity");
        cut.WaitForAssertion(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabId.ShouldBe("activity"));
        navigation.NavigateTo("/orders/missing");

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/orders/summary"));
        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='fc-module-tab-fallback']").Count.ShouldBe(1);
            cut.Find("[data-testid='fc-module-tab-fallback']").TextContent.ShouldContain("Summary");
        });
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("activity")]
    public void FcPageTabs_AbsentOrDisabledDefaultIsRejected(string defaultTabId)
    {
        Should.Throw<InvalidOperationException>(() => Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ModuleRoute, "/orders")
            .Add(tabs => tabs.DefaultTabId, defaultTabId)
            .AddChildContent(PageTabs(deferredLoading: false))));
    }

    [Fact]
    public void FcPageTab_Options_MapToFluentTabWithContractOwnedDeferredRendering()
    {
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "summary")
            .AddChildContent(PageTabs(deferredLoading: true)));

        IRenderedComponent<FcPageTab>[] descriptors = cut.FindComponents<FcPageTab>().ToArray();
        descriptors.Length.ShouldBe(2);
        descriptors[0].Instance.DeferredLoading.ShouldBeTrue();
        descriptors[1].Instance.DeferredLoading.ShouldBeTrue();

        IRenderedComponent<FluentTab>[] tabs = cut.FindComponents<FluentTab>().ToArray();
        tabs.Length.ShouldBe(2);
        tabs[0].Instance.Id.ShouldBe("summary");
        tabs[0].Instance.Header.ShouldBe("Summary");
        tabs[0].Instance.DeferredLoading.ShouldBeFalse();
        tabs[0].Instance.Disabled.ShouldBeFalse();
        tabs[0].Instance.ChildContent.ShouldNotBeNull();
        tabs[1].Instance.Id.ShouldBe("activity");
        tabs[1].Instance.Disabled.ShouldBeTrue();
        tabs[1].Instance.DeferredLoading.ShouldBeFalse();
    }

    [Fact]
    public void FcPageTab_DeferredInactivePanel_DoesNotRenderItsContentEagerly()
    {
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "summary")
            .AddChildContent(PageTabs(deferredLoading: true, disableActivity: false)));

        cut.Find("#summary-panel").TextContent.ShouldContain("Summary body");
        cut.Markup.ShouldNotContain("Activity body");
    }

    [Fact]
    public async Task FcPageTab_DeferredPanel_ActivatesThroughCallbackAndRetainsContentOnReturn()
    {
        string? observed = null;
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "summary")
            .Add(tabs => tabs.ActiveTabIdChanged, EventCallback.Factory.Create<string?>(this, value => observed = value))
            .AddChildContent(PageTabs(deferredLoading: true, disableActivity: false)));

        cut.FindAll("[data-testid='summary-content']").ShouldHaveSingleItem();
        cut.FindAll("[data-testid='activity-content']").ShouldBeEmpty();

        await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync("activity"));
        observed.ShouldBe("activity");
        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveTabId, observed));

        cut.FindComponent<FluentTabs>().Instance.ActiveTabId.ShouldBe("activity");
        cut.Find("#activity-panel [data-testid='activity-content']").TextContent.ShouldBe("Activity body");

        await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync("summary"));
        observed.ShouldBe("summary");
        cut.Render(parameters => parameters.Add(tabs => tabs.ActiveTabId, observed));

        cut.FindComponent<FluentTabs>().Instance.ActiveTabId.ShouldBe("summary");
        cut.Find("#summary-panel [data-testid='summary-content']").TextContent.ShouldBe("Summary body");
        cut.Find("#activity-panel [data-testid='activity-content']").TextContent.ShouldBe("Activity body");
    }

    [Theory]
    [InlineData("summary")]
    [InlineData("activity")]
    [InlineData("missing")]
    [InlineData(null)]
    public async Task FcPageTabs_CurrentDisabledOrUnknownSelection_DoesNotNotifyCaller(string? selectedId)
    {
        int callbacks = 0;
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "summary")
            .Add(tabs => tabs.ActiveTabIdChanged, EventCallback.Factory.Create<string?>(this, _ => callbacks++))
            .AddChildContent(PageTabs(deferredLoading: true)));

        await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync(selectedId));

        callbacks.ShouldBe(0);
        cut.FindAll("[data-testid='activity-content']").ShouldBeEmpty();
    }

    private static RenderFragment PageTabs(bool deferredLoading, bool disableActivity = true)
        => builder =>
        {
            builder.OpenComponent<FcPageTab>(0);
            builder.AddAttribute(1, nameof(FcPageTab.Id), "summary");
            builder.AddAttribute(2, nameof(FcPageTab.Header), "Summary");
            builder.AddAttribute(3, nameof(FcPageTab.DeferredLoading), deferredLoading);
            builder.AddAttribute(4, nameof(FcPageTab.ChildContent), Markup("summary-content", "Summary body"));
            builder.CloseComponent();

            builder.OpenComponent<FcPageTab>(5);
            builder.AddAttribute(6, nameof(FcPageTab.Id), "activity");
            builder.AddAttribute(7, nameof(FcPageTab.Header), "Activity");
            builder.AddAttribute(8, nameof(FcPageTab.Disabled), disableActivity);
            builder.AddAttribute(9, nameof(FcPageTab.DeferredLoading), deferredLoading);
            builder.AddAttribute(10, nameof(FcPageTab.ChildContent), Markup("activity-content", "Activity body"));
            builder.CloseComponent();
        };

    private static RenderFragment SingleTab(string id)
        => builder =>
        {
            builder.OpenComponent<FcPageTab>(0);
            builder.AddAttribute(1, nameof(FcPageTab.Id), id);
            builder.AddAttribute(2, nameof(FcPageTab.Header), "Legacy");
            builder.AddAttribute(3, nameof(FcPageTab.ChildContent), Markup("legacy-content", "Legacy body"));
            builder.CloseComponent();
        };

    private static RenderFragment Markup(string testId, string text)
        => builder => builder.AddMarkupContent(0, $"<span data-testid=\"{testId}\">{text}</span>");
}
