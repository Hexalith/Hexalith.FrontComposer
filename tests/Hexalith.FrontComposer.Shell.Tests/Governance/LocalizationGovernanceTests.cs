using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Governance;

public sealed class LocalizationGovernanceTests
{
    [Fact]
    [Trait("Category", "Governance")]
    public void HomeCardAccessibleName_Source_UsesWholeStringResourceWithoutEnglishSuffix()
    {
        string source = ReadRepoFile("src", "Hexalith.FrontComposer.Shell", "Components", "Home", "FcHomeCard.razor");

        source.ShouldContain("Localizer[\"HomeCardPendingAriaLabelTemplate\"]");
        source.ShouldNotContain("items pending");
    }

    [Fact]
    [Trait("Category", "Governance")]
    public void UiHostDocumentLanguage_Source_UsesUiCultureWithoutSecondAuthority()
    {
        string source = ReadRepoFile("src", "Hexalith.FrontComposer.UI", "Components", "App.razor");
        string harness = ReadRepoFile(
            "tests",
            "Hexalith.FrontComposer.Shell.Tests",
            "Components",
            "DocumentLanguage",
            "UiAppDocumentLanguageHarness.razor");

        const string binding = "<html lang=\"@System.Globalization.CultureInfo.CurrentUICulture.Name\">";
        source.ShouldContain(binding);
        harness.ShouldContain(binding);
        source.ShouldNotContain("<html lang=\"en\">");
        source.ShouldNotContain("document.documentElement.lang");
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
