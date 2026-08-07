using Bunit;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DocumentLanguage;

/// <summary>
/// AC2 render observation for the App.razor document-language binding.
/// The full UI <c>App</c> host graph is not referenced from Shell.Tests; this harness uses the
/// identical <c>CultureInfo.CurrentUICulture.Name</c> expression, and governance pins App.razor to
/// that same binding with no JavaScript second authority (prerender and interactive agree).
/// </summary>
public sealed class UiAppDocumentLanguageRenderTests : BunitContext
{
    private const string AppDocumentLanguageBinding =
        "<html lang=\"@System.Globalization.CultureInfo.CurrentUICulture.Name\">";

    public UiAppDocumentLanguageRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Theory]
    [InlineData("en")]
    [InlineData("fr")]
    public void AppDocumentLanguageBinding_RendersEffectiveUiCultureLang(string cultureName)
    {
        string app = ReadRepoFile("src", "Hexalith.FrontComposer.UI", "Components", "App.razor");
        string harness = ReadRepoFile(
            "tests",
            "Hexalith.FrontComposer.Shell.Tests",
            "Components",
            "DocumentLanguage",
            "UiAppDocumentLanguageHarness.razor");

        app.ShouldContain(AppDocumentLanguageBinding);
        harness.ShouldContain(AppDocumentLanguageBinding);
        app.ShouldNotContain("<html lang=\"en\">");
        app.ShouldNotContain("document.documentElement.lang");

        using CultureScope _ = new(cultureName);
        IRenderedComponent<UiAppDocumentLanguageHarness> cut = Render<UiAppDocumentLanguageHarness>();

        cut.Find("html").GetAttribute("lang").ShouldBe(cultureName);
    }

    private static string ReadRepoFile(params string[] pathSegments)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && directory.GetFiles("Hexalith.FrontComposer.slnx").Length == 0)
        {
            directory = directory.Parent;
        }

        directory.ShouldNotBeNull("Could not locate the repository root.");
        return File.ReadAllText(Path.Combine([directory.FullName, .. pathSegments]));
    }
}
