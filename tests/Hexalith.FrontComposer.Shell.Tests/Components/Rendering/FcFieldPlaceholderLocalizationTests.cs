using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcFieldPlaceholderLocalizationTests : LayoutComponentTestBase {
    public FcFieldPlaceholderLocalizationTests() {
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");
        EnsureStoreInitialized();
    }

    [Fact]
    public void EnglishLocale_UsesLocalizedBodyAndAriaLabel() {
        IRenderedComponent<FcFieldPlaceholder> cut = Render<FcFieldPlaceholder>(parameters => parameters
            .Add(p => p.FieldName, "Metadata")
            .Add(p => p.TypeName, "System.Collections.Generic.Dictionary<string, string>"));

        cut.Markup.ShouldContain("This field requires a custom renderer.");
        cut.Markup.ShouldContain("(System.Collections.Generic.Dictionary&lt;string, string&gt;)");
        cut.Markup.ShouldContain("Learn how to customize this field");
        cut.Find("div").GetAttribute("aria-label").ShouldBe("Metadata with unsupported type System.Collections.Generic.Dictionary<string, string> requires a custom renderer");
    }

    [Fact]
    public void FrenchLocale_UsesFrenchLocalizedBody() {
        // P-15 (Pass-3 review fix): wrap culture mutation in try/finally so the fr-FR setting
        // doesn't leak to sibling test classes via xUnit's arbitrary test-class ordering.
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try {
            CultureInfo french = new("fr-FR");
            CultureInfo.CurrentCulture = french;
            CultureInfo.CurrentUICulture = french;

            IRenderedComponent<FcFieldPlaceholder> cut = Render<FcFieldPlaceholder>(parameters => parameters
                .Add(p => p.FieldName, "Metadata")
                .Add(p => p.TypeName, "System.Object"));

            cut.Markup.ShouldContain("Ce champ nécessite un rendu personnalisé.");
            cut.Markup.ShouldContain("Apprendre à personnaliser ce champ");
        }
        finally {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public void DevMode_AppliesDevCssClass() {
        IRenderedComponent<FcFieldPlaceholder> cut = Render<FcFieldPlaceholder>(parameters => parameters
            .Add(p => p.FieldName, "Metadata")
            .Add(p => p.TypeName, "System.Object")
            .Add(p => p.IsDevMode, true));

        cut.Markup.ShouldContain("fc-field-placeholder-dev");
    }

    [Fact]
    public void AccessibilityAttributesRemainStable() {
        IRenderedComponent<FcFieldPlaceholder> cut = Render<FcFieldPlaceholder>(parameters => parameters
            .Add(p => p.FieldName, "Metadata")
            .Add(p => p.TypeName, "System.Object"));

        // P-28 / DN9-c: role="status" removed — placeholder appears once per cell × per row,
        // which would create N×M live regions on a single grid. Column-header carries the
        // UnsupportedColumnHeaderSuffix and the dashed-card affordance is visually obvious.
        // aria-label remains for screen readers that focus into the cell.
        cut.Markup.ShouldNotContain("role=\"status\"");
        cut.Find("div").GetAttribute("aria-label").ShouldNotBeNullOrWhiteSpace();
        // tabindex stays absent per resolved D7 — cell-renderer focus stops would multiply N×M.
        cut.Markup.ShouldNotContain("tabindex=\"0\"");
    }

    [Fact]
    public void DocsLinkTargetsHfc1002DiagnosticsPage() {
        IRenderedComponent<FcFieldPlaceholder> cut = Render<FcFieldPlaceholder>(parameters => parameters
            .Add(p => p.FieldName, "Metadata")
            .Add(p => p.TypeName, "System.Object"));

        cut.Find("a").GetAttribute("href").ShouldBe(FcFieldPlaceholder.DocsLink);
    }
}
