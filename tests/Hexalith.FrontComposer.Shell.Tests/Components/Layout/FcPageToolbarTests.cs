using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Reusable page-toolbar tests for the Aspire-grade page chrome pattern.
/// </summary>
public sealed class FcPageToolbarTests : LayoutComponentTestBase {
    [Fact]
    public void FcPageToolbar_DefaultRendering_RendersSearchToolbarAndNoEmptyOptionalRegions() {
        IRenderedComponent<FcPageToolbar> cut = Render<FcPageToolbar>(parameters => parameters
            .Add(toolbar => toolbar.SearchValue, "orders")
            .Add(toolbar => toolbar.SearchPlaceholder, "Search orders")
            .Add(toolbar => toolbar.SearchAriaLabel, "Search orders"));

        IElement toolbar = cut.Find("[data-testid='fc-page-toolbar']");
        toolbar.GetAttribute("role").ShouldBe("toolbar");
        toolbar.GetAttribute("aria-label").ShouldBe("Page tools");

        IRenderedComponent<FluentTextInput> search = cut.FindComponent<FluentTextInput>();
        search.Instance.TextInputType.ShouldBe(TextInputType.Search);
        search.Instance.Id.ShouldBe("fc-page-toolbar-search-input");
        search.Instance.Value.ShouldBe("orders");
        search.Instance.Placeholder.ShouldBe("Search orders");
        search.Instance.AdditionalAttributes.ShouldNotBeNull();
        search.Instance.AdditionalAttributes!["data-testid"].ShouldBe("fc-page-toolbar-search");
        IElement searchLabel = cut.Find("label[for='fc-page-toolbar-search-input']");
        searchLabel.ClassList.ShouldContain("fc-sr-only");
        searchLabel.TextContent.ShouldBe("Search orders");

        cut.FindAll("[data-testid='fc-page-toolbar-filter-trigger']").ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-page-toolbar-view-trigger']").ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-page-toolbar-actions']").ShouldBeEmpty();
        cut.FindAll("[data-testid='fc-page-toolbar-tabs']").ShouldBeEmpty();
    }

    [Fact]
    public async Task FcPageToolbar_SearchValueChanged_RaisesCallerCallback() {
        string? observed = null;
        IRenderedComponent<FcPageToolbar> cut = Render<FcPageToolbar>(parameters => parameters
            .Add(toolbar => toolbar.SearchValue, "initial")
            .Add(toolbar => toolbar.SearchValueChanged, EventCallback.Factory.Create<string?>(this, value => observed = value)));

        IRenderedComponent<FluentTextInput> search = cut.FindComponent<FluentTextInput>();

        await cut.InvokeAsync(() => search.Instance.ValueChanged.InvokeAsync("needle"));

        observed.ShouldBe("needle");
    }

    [Fact]
    public void FcPageToolbar_WithFilterContent_TogglesAccessiblePopover() {
        IRenderedComponent<FcPageToolbar> cut = Render<FcPageToolbar>(parameters => parameters
            .Add(toolbar => toolbar.FilterLabel, "Filters")
            .Add(toolbar => toolbar.FilterContent, Markup("filter-panel", "Status filter")));

        IElement trigger = cut.Find("[data-testid='fc-page-toolbar-filter-trigger']");
        trigger.GetAttribute("aria-haspopup").ShouldBe("dialog");
        trigger.GetAttribute("aria-expanded").ShouldBe("false");
        cut.FindAll("[data-testid='fc-page-toolbar-filter-popover']").ShouldBeEmpty();

        trigger.Click();

        trigger = cut.Find("[data-testid='fc-page-toolbar-filter-trigger']");
        trigger.GetAttribute("aria-expanded").ShouldBe("true");

        IRenderedComponent<FluentPopover> popover = cut.FindComponent<FluentPopover>();
        popover.Instance.AnchorId.ShouldBe("fc-page-toolbar-filter-trigger");
        popover.Instance.Opened.ShouldBeTrue();
        popover.Instance.AdditionalAttributes.ShouldNotBeNull();
        popover.Instance.AdditionalAttributes!["role"].ShouldBe("dialog");
        popover.Instance.AdditionalAttributes!["aria-labelledby"].ShouldBe("fc-page-toolbar-filter-title");
        popover.Instance.AdditionalAttributes!["data-testid"].ShouldBe("fc-page-toolbar-filter-popover");
        cut.Find("[data-testid='filter-panel']").TextContent.ShouldBe("Status filter");
    }

    [Fact]
    public void FcPageToolbar_WithViewMenuAndActions_RendersCallerOwnedSlots() {
        IRenderedComponent<FcPageToolbar> cut = Render<FcPageToolbar>(parameters => parameters
            .Add(toolbar => toolbar.ViewMenuLabel, "View")
            .Add(toolbar => toolbar.ViewMenuContent, MenuItem("compact", "Compact view"))
            .Add(toolbar => toolbar.Actions, Markup("toolbar-action", "Refresh")));

        IRenderedComponent<FluentMenuButton> menuButton = cut.FindComponent<FluentMenuButton>();
        menuButton.Instance.AdditionalAttributes.ShouldNotBeNull();
        menuButton.Instance.AdditionalAttributes!["data-testid"].ShouldBe("fc-page-toolbar-view-trigger");
        menuButton.Markup.ShouldContain("View");

        cut.FindComponent<FluentMenu>().ShouldNotBeNull();
        IRenderedComponent<FluentMenuItem> menuItem = cut.FindComponent<FluentMenuItem>();
        menuItem.Instance.AdditionalAttributes.ShouldNotBeNull();
        menuItem.Instance.AdditionalAttributes!["data-testid"].ShouldBe("compact");
        menuItem.Instance.Label.ShouldBe("Compact view");

        IElement actions = cut.Find("[data-testid='fc-page-toolbar-actions']");
        actions.TextContent.ShouldContain("Refresh");
        actions.QuerySelector("[data-testid='toolbar-action']").ShouldNotBeNull();
    }

    [Fact]
    public async Task FcPageToolbar_WithTabs_RendersSubtleTabsAndRaisesActiveTabCallback() {
        string? activeTab = null;
        FcPageToolbarTab[] tabs = [
            new("summary", "Summary"),
            new("activity", "Activity", Disabled: true),
        ];

        IRenderedComponent<FcPageToolbar> cut = Render<FcPageToolbar>(parameters => parameters
            .Add(toolbar => toolbar.Tabs, tabs)
            .Add(toolbar => toolbar.ActiveTabId, "summary")
            .Add(toolbar => toolbar.ActiveTabIdChanged, EventCallback.Factory.Create<string?>(this, value => activeTab = value)));

        IRenderedComponent<FluentTabs> fluentTabs = cut.FindComponent<FluentTabs>();
        fluentTabs.Instance.Appearance.ShouldBe(TabsAppearance.Subtle);
        fluentTabs.Instance.ActiveTabId.ShouldBe("summary");
        fluentTabs.Instance.AdditionalAttributes.ShouldNotBeNull();
        fluentTabs.Instance.AdditionalAttributes!["data-testid"].ShouldBe("fc-page-toolbar-tabs");

        IRenderedComponent<FluentTab>[] renderedTabs = cut.FindComponents<FluentTab>().ToArray();
        renderedTabs.Length.ShouldBe(2);
        renderedTabs[0].Instance.Id.ShouldBe("summary");
        renderedTabs[0].Instance.Header.ShouldBe("Summary");
        renderedTabs[1].Instance.Id.ShouldBe("activity");
        renderedTabs[1].Instance.Disabled.ShouldBeTrue();

        await cut.InvokeAsync(() => fluentTabs.Instance.ActiveTabIdChanged.InvokeAsync("activity"));

        activeTab.ShouldBe("activity");
    }

    [Fact]
    public void FcPageToolbar_ComposesThroughAggregateListToolbarSlot() {
        IRenderedComponent<FcAggregateListPage<string>> cut = Render<FcAggregateListPage<string>>(parameters => parameters
            .Add(page => page.Heading, "Tenants")
            .Add(page => page.Toolbar, (RenderFragment)(builder => {
                builder.OpenComponent<FcPageToolbar>(0);
                builder.AddAttribute(1, nameof(FcPageToolbar.SearchAriaLabel), "Search tenants");
                builder.AddAttribute(2, nameof(FcPageToolbar.Actions), Markup("tenant-action", "Add tenant"));
                builder.CloseComponent();
            })));

        IElement headerActions = cut.Find("[data-fc-page-header-actions]");
        headerActions.QuerySelector("[data-testid='fc-page-toolbar']").ShouldNotBeNull();
        headerActions.QuerySelector("[data-testid='fc-page-toolbar-search']").ShouldNotBeNull();
        headerActions.QuerySelector("[data-testid='tenant-action']").ShouldNotBeNull();
    }

    [Fact]
    public void FcPageToolbar_AppliesLayoutViaRenderedInlineStyle_NotDeadScopedCss() {
        // Story 8.6 review: a .razor.css scoped sheet cannot reach Fluent child-component
        // output because no raw HTML element in the .razor carries the component scope, so
        // every scoped rule was dead (the Story 8.4 trap). Layout therefore rides on inline
        // Style that actually renders to the DOM. This proves AC1's right-aligned actions slot
        // and the search sizing are applied, not silently dropped like the prior scoped CSS.
        IRenderedComponent<FcPageToolbar> cut = Render<FcPageToolbar>(parameters => parameters
            .Add(toolbar => toolbar.SearchAriaLabel, "Search orders")
            .Add(toolbar => toolbar.Actions, Markup("toolbar-action", "Refresh")));

        IRenderedComponent<FluentTextInput> search = cut.FindComponent<FluentTextInput>();
        search.Instance.Style.ShouldNotBeNull();
        search.Instance.Style!.ShouldContain("flex");
        search.Instance.Style!.ShouldContain("max-width");

        IElement actions = cut.Find("[data-testid='fc-page-toolbar-actions']");
        (actions.GetAttribute("style") ?? string.Empty).ShouldContain("margin-inline-start: auto");
    }

    private static RenderFragment Markup(string testId, string text)
        => builder => builder.AddMarkupContent(0, $"<span data-testid=\"{testId}\">{text}</span>");

    private static RenderFragment MenuItem(string testId, string text)
        => builder => {
            builder.OpenComponent<FluentMenuItem>(0);
            builder.AddAttribute(1, "data-testid", testId);
            builder.AddAttribute(2, nameof(FluentMenuItem.Label), text);
            builder.CloseComponent();
        };
}
