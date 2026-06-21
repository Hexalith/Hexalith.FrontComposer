using System.Reflection;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// FC-LST aggregate list page contract tests: the wrapper composes FcPageLayout + FcPageHeader (with the
/// header Actions slot as the toolbar and the Metadata slot for return context) and renders the domain
/// filters/commands/states/body/pager slots, while owning no domain copy and staying generic over TItem.
/// </summary>
public sealed class FcAggregateListPageTests : LayoutComponentTestBase {
    [Fact]
    public void FcAggregateListPage_ComposesLayoutHeaderAndRootStack() {
        IRenderedComponent<FcAggregateListPage<string>> cut = RenderList(parameters => parameters
            .Add(page => page.LayoutMode, FcPageLayoutMode.FullWidth)
            .Add(page => page.PageTitle, "Browser tenants title")
            .Add(page => page.Heading, "Tenants")
            .Add(page => page.Eyebrow, "Workspace")
            .Add(page => page.Description, "Operational tenant browse.")
            .Add(page => page.HeadingId, "list-heading")
            .Add(page => page.HeadingTabIndex, -1)
            .Add(page => page.HeaderTestId, "list-header")
            .Add(page => page.RootTestId, "list-root"));

        // FC-LYT measure is forwarded to the composed FcPageLayout.
        cut.FindComponent<FcPageLayout>().Instance.Mode.ShouldBe(FcPageLayoutMode.FullWidth);

        cut.Find("[data-testid='list-root']").ShouldNotBeNull();

        IElement header = cut.Find("[data-testid='list-header']");
        header.TagName.ShouldBe("HEADER");

        IReadOnlyList<IElement> headings = cut.FindAll("h1");
        headings.Count.ShouldBe(1);
        headings[0].Id.ShouldBe("list-heading");
        headings[0].TextContent.ShouldBe("Tenants");
        headings[0].GetAttribute("tabindex").ShouldBe("-1");

        cut.Find("[data-fc-page-header-eyebrow]").TextContent.ShouldBe("Workspace");
        cut.Find("[data-fc-page-header-description]").TextContent.ShouldBe("Operational tenant browse.");
        cut.FindComponents<PageTitle>().Count.ShouldBe(1);
    }

    [Fact]
    public void FcAggregateListPage_RendersToolbarInHeaderActionsAndMetadataSlot() {
        IRenderedComponent<FcAggregateListPage<string>> cut = RenderList(parameters => parameters
            .Add(page => page.Heading, "Tenants")
            .Add(page => page.Toolbar, Markup("toolbar", "Refresh"))
            .Add(page => page.HeaderMetadata, Markup("metadata", "Returned context")));

        IElement actions = cut.Find("[data-fc-page-header-actions]");
        actions.QuerySelector("[data-testid='toolbar']").ShouldNotBeNull();
        actions.TextContent.ShouldContain("Refresh");

        IElement metadata = cut.Find("[data-fc-page-header-metadata]");
        metadata.QuerySelector("[data-testid='metadata']").ShouldNotBeNull();
        metadata.TextContent.ShouldContain("Returned context");
    }

    [Fact]
    public void FcAggregateListPage_RendersEveryDomainSlot() {
        IRenderedComponent<FcAggregateListPage<string>> cut = RenderList(parameters => parameters
            .Add(page => page.Heading, "Tenants")
            .Add(page => page.Filters, Markup("filters", "filters"))
            .Add(page => page.Commands, Markup("commands", "commands"))
            .Add(page => page.States, Markup("states", "states"))
            .Add(page => page.Body, Markup("body", "body"))
            .Add(page => page.Pager, Markup("pager", "pager"))
            .Add(page => page.ChildContent, Markup("extra", "extra")));

        foreach (string slot in new[] { "filters", "commands", "states", "body", "pager", "extra" }) {
            cut.Find($"[data-testid='{slot}']").ShouldNotBeNull();
        }
    }

    [Fact]
    public void FcAggregateListPage_NoToolbarOrMetadata_RendersNoEmptyHeaderSlots() {
        IRenderedComponent<FcAggregateListPage<string>> cut = RenderList(parameters => parameters
            .Add(page => page.Heading, "Tenants"));

        cut.FindAll("[data-fc-page-header-actions]").ShouldBeEmpty();
        cut.FindAll("[data-fc-page-header-metadata]").ShouldBeEmpty();
    }

    [Fact]
    public async Task FcAggregateListPage_NotifyItemSelected_RaisesTypedCallback() {
        string? selected = null;
        IRenderedComponent<FcAggregateListPage<string>> cut = RenderList(parameters => parameters
            .Add(page => page.Heading, "Tenants")
            .Add(page => page.OnItemSelected, EventCallback.Factory.Create<string>(this, value => selected = value)));

        await cut.InvokeAsync(() => cut.Instance.NotifyItemSelectedAsync("tenant.alpha"));

        selected.ShouldBe("tenant.alpha");
    }

    [Fact]
    public void FcAggregateListPage_FocusHeadingAsync_DoesNotThrow() {
        IRenderedComponent<FcAggregateListPage<string>> cut = RenderList(parameters => parameters
            .Add(page => page.Heading, "Tenants")
            .Add(page => page.HeadingId, "list-heading")
            .Add(page => page.HeadingTabIndex, -1));

        Should.NotThrow(() => cut.InvokeAsync(() => cut.Instance.FocusHeadingAsync().AsTask()));
    }

    [Fact]
    public void FcAggregateListPage_TypeContract_DoesNotInjectDomainResources() {
        PropertyInfo[] injectedProperties = typeof(FcAggregateListPage<string>)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static property => property.GetCustomAttribute<InjectAttribute>() is not null)
            .ToArray();

        injectedProperties.ShouldBeEmpty("FcAggregateListPage must stay domain-agnostic; consumers pass strings and fragments.");
    }

    private IRenderedComponent<FcAggregateListPage<string>> RenderList(
        Action<ComponentParameterCollectionBuilder<FcAggregateListPage<string>>> parameters)
        => Render(parameters);

    private static RenderFragment Markup(string testId, string text)
        => builder => builder.AddMarkupContent(0, $"<span data-testid=\"{testId}\">{text}</span>");
}
