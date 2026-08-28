using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Tests the body-level page tab and panel contract.
/// </summary>
public sealed class FcPageTabsTests : LayoutComponentTestBase
{
    [Fact]
    public void FcPageTabs_WithPanelContent_RendersDeterministicReciprocalAssociation()
    {
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "summary")
            .Add(tabs => tabs.AriaLabel, "Order sections")
            .Add(tabs => tabs.TestId, "orders-page-tabs")
            .AddChildContent(PageTabs(deferredLoading: false)));

        IRenderedComponent<FluentTabs> fluentTabs = cut.FindComponent<FluentTabs>();
        fluentTabs.Instance.ActiveTabId.ShouldBe("summary");
        fluentTabs.Instance.Appearance.ShouldBe(TabsAppearance.Subtle);
        fluentTabs.Instance.Orientation.ShouldBe(Orientation.Horizontal);
        fluentTabs.Instance.Width.ShouldBe("100%");

        IElement tabsRoot = cut.Find("[data-testid='orders-page-tabs']");
        tabsRoot.GetAttribute("aria-label").ShouldBe("Order sections");

        IElement summaryTab = cut.Find("#summary");
        IElement summaryPanel = cut.Find("#summary-panel");
        summaryTab.GetAttribute("aria-controls").ShouldBe("summary-panel");
        summaryPanel.GetAttribute("role").ShouldBe("tabpanel");
        summaryPanel.TextContent.ShouldContain("Summary body");

        cut.Find("#activity-panel").TextContent.ShouldContain("Activity body");
    }

    [Fact]
    public async Task FcPageTabs_ActiveTabChanged_RaisesCallerCallback()
    {
        string? observed = null;
        IRenderedComponent<FcPageTabs> cut = Render<FcPageTabs>(parameters => parameters
            .Add(tabs => tabs.ActiveTabId, "summary")
            .Add(tabs => tabs.ActiveTabIdChanged, EventCallback.Factory.Create<string?>(this, value => observed = value))
            .AddChildContent(PageTabs(deferredLoading: false)));

        await cut.InvokeAsync(() => cut.FindComponent<FluentTabs>().Instance.ActiveTabIdChanged.InvokeAsync("activity"));

        observed.ShouldBe("activity");
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

    private static RenderFragment Markup(string testId, string text)
        => builder => builder.AddMarkupContent(0, $"<span data-testid=\"{testId}\">{text}</span>");
}
