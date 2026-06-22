using AngleSharp.Dom;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Handoff <c>frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening</c> (Requested
/// outcome 1/2) — the shell's single content <c>main</c> landmark (<c>#fc-main-content</c>) exposes an
/// accessible-name contract so a route heading can name it WITHOUT an orphaned page-level
/// <c>aria-labelledby</c> on a non-landmark wrapper. Two naming paths are pinned: the shell
/// <c>ContentLabel</c>/<c>ContentLabelledBy</c> parameters (app-wide static) and the page-driven
/// <see cref="FcContentLabel"/> marker (per-route, via cascaded coordinator). The unlabelled default
/// (the pre-handoff behavior) is also pinned for backward compatibility.
/// </summary>
public sealed class FrontComposerShellContentLandmarkTests : LayoutComponentTestBase {
    [Fact]
    public void MainLandmark_ByDefault_CarriesNoAccessibleNameAttributes() {
        // Backward compatibility: with neither parameter nor <FcContentLabel> set, the landmark
        // emits no aria-label / aria-labelledby (the implicit "main" name), exactly as before.
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            IElement main = cut.Find("#fc-main-content");
            main.GetAttribute("role").ShouldBe("main");
            main.HasAttribute("aria-label").ShouldBeFalse();
            main.HasAttribute("aria-labelledby").ShouldBeFalse();
        });
    }

    [Fact]
    public void MainLandmark_WithContentLabelParameter_EmitsAriaLabel() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.ContentLabel, "Tenant operations")
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            IElement main = cut.Find("#fc-main-content");
            main.GetAttribute("aria-label").ShouldBe("Tenant operations");
            main.HasAttribute("aria-labelledby").ShouldBeFalse();
        });
    }

    [Fact]
    public void MainLandmark_WithContentLabelledByParameter_EmitsAriaLabelledByAndSuppressesAriaLabel() {
        // aria-labelledby wins over aria-label so the two never compete on the landmark.
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.ContentLabel, "ignored when labelledby is present")
            .Add(c => c.ContentLabelledBy, "tenants-list-heading")
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            IElement main = cut.Find("#fc-main-content");
            main.GetAttribute("aria-labelledby").ShouldBe("tenants-list-heading");
            main.HasAttribute("aria-label").ShouldBeFalse();
        });
    }

    [Fact]
    public void MainLandmark_NamedByPageContentLabelMarker_UsesRouteHeadingId() {
        // The page-driven path (Requested outcome 2): a page drops <FcContentLabel LabelledBy=...>
        // into the shell body so the shell-owned main landmark is named by the route heading id,
        // with NO orphaned aria-labelledby on a page-level non-landmark wrapper.
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent<FcContentLabel>(fc => fc
                .Add(c => c.LabelledBy, "tenants-detail-heading")
                .AddChildContent("<h1 id=\"tenants-detail-heading\">Tenant detail</h1>")));

        cut.WaitForAssertion(() => {
            IElement main = cut.Find("#fc-main-content");
            main.GetAttribute("aria-labelledby").ShouldBe("tenants-detail-heading");
            main.HasAttribute("aria-label").ShouldBeFalse();
        });
    }

    [Fact]
    public void MainLandmark_PageContentLabelMarker_WinsOverShellParameter() {
        // Last-writer-wins precedence: a page-declared <FcContentLabel> overrides the app-wide shell
        // ContentLabelledBy parameter so a route can name the landmark by its own heading.
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.ContentLabelledBy, "app-wide-fallback")
            .AddChildContent<FcContentLabel>(fc => fc
                .Add(c => c.LabelledBy, "page-route-heading")
                .AddChildContent("<p>Body</p>")));

        cut.WaitForAssertion(() =>
            cut.Find("#fc-main-content").GetAttribute("aria-labelledby").ShouldBe("page-route-heading"));
    }

    [Fact]
    public void MainLandmark_IdAndTabIndexStayAdjacent_AfterNamingAttributesAdded() {
        // Locks the FC-LYT skip-link/focus-target substring (`id="fc-main-content" tabindex="-1"`)
        // so adding the naming attributes never breaks the contiguous focus-target contract.
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.ContentLabel, "Tenant operations")
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain("id=\"fc-main-content\" tabindex=\"-1\"", Case.Sensitive));
    }
}
