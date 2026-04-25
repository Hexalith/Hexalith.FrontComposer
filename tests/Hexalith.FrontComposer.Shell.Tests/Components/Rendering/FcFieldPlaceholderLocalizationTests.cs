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

        cut.Markup.ShouldContain("Metadata requires a custom renderer.");
        cut.Markup.ShouldContain("Unsupported type: System.Collections.Generic.Dictionary&lt;string, string&gt;");
        cut.Markup.ShouldContain("Learn how to customize this field");
        cut.Markup.ShouldContain("aria-label=\"Metadata with unsupported type System.Collections.Generic.Dictionary&lt;string, string&gt; requires a custom renderer\"");
    }

    [Fact]
    public void FrenchLocale_UsesFrenchLocalizedBody() {
        CultureInfo french = new("fr-FR");
        CultureInfo.CurrentCulture = french;
        CultureInfo.CurrentUICulture = french;

        IRenderedComponent<FcFieldPlaceholder> cut = Render<FcFieldPlaceholder>(parameters => parameters
            .Add(p => p.FieldName, "Metadata")
            .Add(p => p.TypeName, "System.Object"));

        cut.Markup.ShouldContain("Metadata nécessite un rendu personnalisé.");
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

        cut.Markup.ShouldContain("role=\"region\"");
        cut.Markup.ShouldContain("tabindex=\"0\"");
    }
}
