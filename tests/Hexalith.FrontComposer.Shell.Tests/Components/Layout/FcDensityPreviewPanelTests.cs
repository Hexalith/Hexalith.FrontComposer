// ATDD RED PHASE — Story 3-3 Task 10.9 (D14; AC2, AC5, AC6)
// Fails at compile until Task 7.1 (FcDensityPreviewPanel component) lands.

using Bunit;

using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Tests.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-3 Task 10.9 — preview panel is parameter-driven (no Fluxor subscription)
/// and renders its specimen inside a <c>data-fc-density</c> wrapper that overrides
/// the body-level cascade locally (D14, ADR-041).
/// </summary>
public sealed class FcDensityPreviewPanelTests : LayoutComponentTestBase {
    [Theory]
    [InlineData(DensityLevel.Compact, "compact")]
    [InlineData(DensityLevel.Comfortable, "comfortable")]
    [InlineData(DensityLevel.Roomy, "roomy")]
    public void RendersAtRequestedDensity(DensityLevel level, string expectedAttributeValue) {
        IRenderedComponent<FcDensityPreviewPanel> cut = Render<FcDensityPreviewPanel>(p => p
            .Add(c => c.Density, level));

        cut.WaitForAssertion(() =>
            cut.Markup.ShouldContain(
                $"data-fc-density=\"{expectedAttributeValue}\"",
                Case.Sensitive,
                $"Preview panel wrapper must carry data-fc-density=\"{expectedAttributeValue}\" (D14)."));
    }

    [Fact]
    public void PreviewStack_RendersStableClassReachedThroughWrapperDeepSelector() {
        IRenderedComponent<FcDensityPreviewPanel> cut = Render<FcDensityPreviewPanel>(p => p
            .Add(c => c.Density, DensityLevel.Comfortable));

        AngleSharp.Dom.IElement wrapper = cut.Find(".fc-density-preview-wrapper");
        AngleSharp.Dom.IElement preview = cut.Find("[data-testid=\"fc-density-preview\"]");
        wrapper.QuerySelector("[data-testid=\"fc-density-preview\"]").ShouldNotBeNull();
        preview.ClassList.Contains("fc-density-preview").ShouldBeTrue();
        preview.GetAttribute("data-fc-density").ShouldBe("comfortable");

        string css = VisualReachabilityTestSupport.ReadShellComponentCss(
            "Layout",
            "FcDensityPreviewPanel.razor.css");
        css.ShouldContain(".fc-density-preview-wrapper ::deep .fc-density-preview");
    }
}
