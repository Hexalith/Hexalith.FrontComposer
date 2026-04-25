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
        CultureInfo french = new("fr-FR");
        CultureInfo.CurrentCulture = french;
        CultureInfo.CurrentUICulture = french;

        IRenderedComponent<FcFieldPlaceholder> cut = Render<FcFieldPlaceholder>(parameters => parameters
            .Add(p => p.FieldName, "Metadata")
            .Add(p => p.TypeName, "System.Object"));

        cut.Markup.ShouldContain("Ce champ nécessite un rendu personnalisé.");
        cut.Markup.ShouldContain("Apprendre à personnaliser ce champ");
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

        // AC7 / Story 4-6 review fix: outer wrapper carries role="status" (live region for AT)
        // and is NOT focusable per cell-renderer instance — focus stops would multiply N×M with
        // unsupported columns × rows.
        cut.Markup.ShouldContain("role=\"status\"");
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
