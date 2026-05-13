using System.Globalization;
using System.Resources;

using Hexalith.FrontComposer.Shell.Resources.DevMode;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Resources;

public sealed class DevModeStringsTests {
    [Fact]
    public void CanonicalKeysHaveFrenchCounterparts() {
        ResourceManager manager = new(typeof(DevModeStrings));
        ResourceSet enSet = manager.GetResourceSet(new CultureInfo("en"), createIfNotExists: true, tryParents: true)!;
        ResourceSet frSet = manager.GetResourceSet(new CultureInfo("fr"), createIfNotExists: true, tryParents: false)!;

        HashSet<string> enKeys = [];
        foreach (System.Collections.DictionaryEntry entry in enSet) {
            enKeys.Add((string)entry.Key);
        }

        HashSet<string> frKeys = [];
        foreach (System.Collections.DictionaryEntry entry in frSet) {
            frKeys.Add((string)entry.Key);
        }

        string[] missing = [.. enKeys.Where(k => !frKeys.Contains(k))];
        missing.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("ToggleAriaLabel", "Toggle developer overlay (Ctrl+Shift+D)", "Basculer la superposition développeur (Ctrl+Maj+D)")]
    [InlineData("DrawerAriaLabel", "FrontComposer developer details", "Détails développeur FrontComposer")]
    [InlineData("CopyUnavailable", "Copy unavailable", "Copie indisponible")]
    [InlineData("CopySucceeded", "Copied", "Copié")]
    [InlineData("CopyFailedManual", "Copy failed - select and copy manually", "La copie a échoué - sélectionnez et copiez manuellement")]
    [InlineData("EmptyTreeMessage", "Select an annotated element.", "Sélectionnez un élément annoté.")]
    public void DevModeStaticKeysResolveInBothLocales(string key, string enValue, string frValue) {
        ServiceProvider provider = BuildLocalizedProvider();
        using IServiceScope scope = provider.CreateScope();
        IStringLocalizer<DevModeStrings> localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<DevModeStrings>>();

        CultureInfo previous = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            localizer[key].Value.ShouldBe(enValue);
            localizer[key].ResourceNotFound.ShouldBeFalse();

            CultureInfo.CurrentUICulture = new CultureInfo("fr");
            localizer[key].Value.ShouldBe(frValue);
            localizer[key].ResourceNotFound.ShouldBeFalse();
        }
        finally {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void FrenchStatusTextUsesNonBreakingSpaceBeforeColon() {
        ResourceManager manager = new(typeof(DevModeStrings));
        string frValue = manager.GetString("CurrentLevelLabel", new CultureInfo("fr"))!;

        frValue.ShouldNotBeNull();
        int colonIndex = frValue.IndexOf(':');
        colonIndex.ShouldBeGreaterThan(0);
        frValue[colonIndex - 1].ShouldBe(' ');
    }

    private static ServiceProvider BuildLocalizedProvider() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddLocalization();
        return services.BuildServiceProvider();
    }
}
