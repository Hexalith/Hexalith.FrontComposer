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

    // ── Handoff frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening ────────────

    [Fact]
    public void FcPageHeader_HeaderRoot_IsNotABannerLandmark() {
        // Requested outcome 2: the page header must not create a competing global `banner` landmark
        // on every route page (the shell header owns the single page banner). The header carries
        // role="presentation" so it never resolves to banner even when rendered outside a sectioning
        // ancestor (as in this isolated bUnit render).
        IRenderedComponent<FcPageHeader> cut = Render<FcPageHeader>(parameters => parameters
            .Add(component => component.Heading, "Tenant operations")
            .Add(component => component.TestId, "tenant-page-header"));

        IElement header = cut.Find("[data-testid='tenant-page-header']");
        header.TagName.ShouldBe("HEADER");
        header.GetAttribute("role").ShouldBe("presentation");

        // No element in the rendered header advertises the banner role.
        cut.FindAll("[role='banner']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void FcPageHeader_WithBlankHeading_RendersNoDanglingHeadingElement(string blankHeading) {
        // Requested outcome 3: a blank/whitespace Heading must not render a dangling/empty <h1>
        // (an empty heading is itself a WCAG failure). The component fails safely by suppressing the
        // heading element entirely rather than throwing or emitting an empty <h1>.
        IRenderedComponent<FcPageHeader> cut = Render<FcPageHeader>(parameters => parameters
            .Add(component => component.PageTitle, "Browser title")
            .Add(component => component.Heading, blankHeading)
            .Add(component => component.Description, "Body still renders."));

        cut.FindAll("h1").ShouldBeEmpty();
        // The rest of the header still renders (it is fail-safe, not a hard failure).
        cut.Find("[data-fc-page-header-description]").TextContent.ShouldBe("Body still renders.");
        // The document title still resolves from the explicit PageTitle.
        cut.FindComponents<PageTitle>().Count.ShouldBe(1);
    }

    [Fact]
    public void FcPageHeader_WithHeading_RendersExactlyOneHeading() {
        // The non-blank path still renders exactly one <h1> (no regression from the blank fail-safe).
        IRenderedComponent<FcPageHeader> cut = Render<FcPageHeader>(parameters => parameters
            .Add(component => component.Heading, "Visible title"));

        cut.FindAll("h1").Count.ShouldBe(1);
    }

    [Fact]
    public async Task FcPageHeader_FocusHeadingAsync_WithoutTabIndex_FailsDiagnostically() {
        // Requested outcome 5: a heading is not focusable without a tabindex, so a browser focus call
        // would silently no-op. When HeadingTabIndex is omitted, FocusHeadingAsync must fail
        // diagnostically (throw) rather than silently no-op.
        IRenderedComponent<FcPageHeader> cut = Render<FcPageHeader>(parameters => parameters
            .Add(component => component.Heading, "Tenant operations")
            .Add(component => component.HeadingId, "tenants-heading"));
        // HeadingTabIndex intentionally NOT set.

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            () => cut.InvokeAsync(() => cut.Instance.FocusHeadingAsync().AsTask()));

        ex.Message.ShouldContain(nameof(FcPageHeader.HeadingTabIndex));
    }

    [Fact]
    public void FcPageHeader_FocusHeadingAsync_WithTabIndex_DoesNotThrow() {
        // The supported focus-target path (HeadingTabIndex set) keeps working: focus moves to the
        // heading via JS interop (loose mode), no exception.
        IRenderedComponent<FcPageHeader> cut = Render<FcPageHeader>(parameters => parameters
            .Add(component => component.Heading, "Tenant operations")
            .Add(component => component.HeadingId, "tenants-heading")
            .Add(component => component.HeadingTabIndex, -1));

        Should.NotThrow(() => cut.InvokeAsync(() => cut.Instance.FocusHeadingAsync().AsTask()));
    }
}
