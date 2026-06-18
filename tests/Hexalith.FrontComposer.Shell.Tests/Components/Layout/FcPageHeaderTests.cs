using System.Reflection;

using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Page-header contract tests: document title and visible route heading are FrontComposer-owned, while
/// consuming domains provide localized strings and page-specific fragments.
/// </summary>
public sealed class FcPageHeaderTests : LayoutComponentTestBase {
    [Fact]
    public void FcPageHeader_WhenRendered_RendersPageTitleAndSingleRouteHeading() {
        IRenderedComponent<FcPageHeader> cut = Render<FcPageHeader>(parameters => parameters
            .Add(component => component.PageTitle, "Browser tenant title")
            .Add(component => component.Heading, "Tenant operations")
            .Add(component => component.HeadingId, "tenants-heading")
            .Add(component => component.HeadingTabIndex, -1)
            .Add(component => component.TestId, "tenant-page-header"));

        cut.FindComponents<PageTitle>().Count.ShouldBe(1);

        IElement header = cut.Find("[data-testid='tenant-page-header']");
        header.TagName.ShouldBe("HEADER");
        header.ClassList.ShouldContain("fc-page-header");

        IReadOnlyList<IElement> headings = cut.FindAll("h1");
        headings.Count.ShouldBe(1);
        headings[0].Id.ShouldBe("tenants-heading");
        headings[0].TextContent.ShouldBe("Tenant operations");
        headings[0].GetAttribute("tabindex").ShouldBe("-1");
    }

    [Fact]
    public void FcPageHeader_WithOptionalText_RendersEyebrowAndDescription() {
        IRenderedComponent<FcPageHeader> cut = Render<FcPageHeader>(parameters => parameters
            .Add(component => component.PageTitle, "Browser title")
            .Add(component => component.Heading, "Visible title")
            .Add(component => component.Eyebrow, "Administration")
            .Add(component => component.Description, "Manage tenant access and lifecycle."));

        IElement eyebrow = cut.Find("[data-fc-page-header-eyebrow]");
        eyebrow.TextContent.ShouldBe("Administration");

        IElement description = cut.Find("[data-fc-page-header-description]");
        description.TextContent.ShouldBe("Manage tenant access and lifecycle.");
    }

    [Fact]
    public void FcPageHeader_WithOptionalFragments_RendersMetadataAndActions() {
        IRenderedComponent<FcPageHeader> cut = Render<FcPageHeader>(parameters => parameters
            .Add(component => component.PageTitle, "Browser title")
            .Add(component => component.Heading, "Visible title")
            .Add(component => component.Metadata, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<span data-testid=\"header-metadata\">Restored context</span>")))
            .Add(component => component.Actions, (RenderFragment)(builder =>
                builder.AddMarkupContent(0, "<span data-testid=\"header-actions\">Refresh</span>"))));

        IElement metadata = cut.Find("[data-fc-page-header-metadata]");
        metadata.TextContent.ShouldContain("Restored context");
        cut.Find("[data-testid='header-metadata']").TextContent.ShouldBe("Restored context");

        IElement actions = cut.Find("[data-fc-page-header-actions]");
        actions.TextContent.ShouldContain("Refresh");
        cut.Find("[data-testid='header-actions']").TextContent.ShouldBe("Refresh");
    }

    [Fact]
    public void FcPageHeader_TypeContract_DoesNotInjectDomainResources() {
        PropertyInfo[] injectedProperties = typeof(FcPageHeader)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(static property => property.GetCustomAttribute<InjectAttribute>() is not null)
            .ToArray();

        injectedProperties.ShouldBeEmpty("FcPageHeader must stay generic; consuming domains pass localized strings and fragments.");
    }
}
