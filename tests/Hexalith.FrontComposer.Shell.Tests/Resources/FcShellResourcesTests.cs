using System.Globalization;
using System.Resources;

using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Resources;

/// <summary>
/// Story 3-1 Task 10.7 (D12 / AC7) — resource parity + culture round-trip tests.
/// </summary>
public sealed class FcShellResourcesTests {
    [Fact]
    public void CanonicalKeysHaveFrenchCounterparts() {
        ResourceManager manager = new(typeof(FcShellResources));
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

    [Fact]
    public void ThemeToggleAriaLabelResolvesInFrench() {
        ServiceProvider provider = BuildLocalizedProvider();
        using IServiceScope scope = provider.CreateScope();
        IStringLocalizer<FcShellResources> localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<FcShellResources>>();

        CultureInfo previous = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = new CultureInfo("fr");
            localizer["ThemeToggleAriaLabel"].Value.ShouldBe("Changer de thème");
        }
        finally {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void AddHexalithShellLocalizationRegistersLocalizer() {
        ServiceProvider provider = BuildLocalizedProvider();
        using IServiceScope scope = provider.CreateScope();

        IStringLocalizer<FcShellResources> localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<FcShellResources>>();

        localizer.ShouldNotBeNull();
        localizer["AppTitle"].Value.ShouldBe("Hexalith FrontComposer");
    }

    // --- Story 3-2 Task 10.8 (D19 amended 2026-04-19) — 3 navigation ARIA / skip-link resource keys
    // (was 5). NavGroupExpandAriaLabel / NavGroupCollapseAriaLabel dropped per code-review round 2 ---

    [Theory]
    [InlineData("NavMenuAriaLabel", "Primary navigation", "Navigation principale")]
    [InlineData("HamburgerToggleAriaLabel", "Toggle navigation", "Basculer la navigation")]
    [InlineData("SkipToNavigationLabel", "Skip to navigation", "Passer à la navigation")]
    public void NavigationStaticKeysResolveInBothLocales(string key, string enValue, string frValue) {
        // ATDD RED PHASE — fails at assertion time until Task 8.5 / 8.6 add the keys to
        // FcShellResources.resx + .fr.resx.
        ServiceProvider provider = BuildLocalizedProvider();
        using IServiceScope scope = provider.CreateScope();
        IStringLocalizer<FcShellResources> localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<FcShellResources>>();

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

    // --- Story 3-3 Task 10.11 (D17 / AC2 / AC4 / AC7) — 11 new settings/density resource keys ---

    [Theory]
    [InlineData("SettingsDialogTitle", "Settings", "Paramètres")]
    [InlineData("SettingsDialogCloseAriaLabel", "Close settings", "Fermer les paramètres")]
    [InlineData("DensitySectionLabel", "Display density", "Densité d'affichage")]
    [InlineData("DensityCompactLabel", "Compact", "Compact")]
    [InlineData("DensityComfortableLabel", "Comfortable", "Confortable")]
    [InlineData("DensityRoomyLabel", "Roomy", "Spacieux")]
    [InlineData("ThemeSectionLabel", "Theme", "Thème")]
    [InlineData("DensityPreviewHeading", "Preview", "Aperçu")]
    [InlineData("CtrlCommaShortcutHint", "Ctrl+, to open settings", "Ctrl+, pour ouvrir les paramètres")]
    [InlineData("DensityForcedByViewportNote",
        "Your device size is forcing Comfortable density. Your preference will re-apply at larger screen sizes.",
        "La taille de votre appareil impose la densité Confortable. Votre préférence s'appliquera de nouveau sur des écrans plus larges.")]
    [InlineData("ResetToDefaultsLabel", "Reset to defaults", "Réinitialiser")]
    public void SettingsDialogStaticKeysResolveInBothLocales(string key, string enValue, string frValue) {
        // ATDD RED PHASE — fails at assertion time until Task 9.1 / 9.2 add the keys to
        // FcShellResources.resx + .fr.resx.
        ServiceProvider provider = BuildLocalizedProvider();
        using IServiceScope scope = provider.CreateScope();
        IStringLocalizer<FcShellResources> localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<FcShellResources>>();

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

    // D19 amended 2026-04-19 (code-review round 2): `NavigationParameterisedKeysRoundTripArgument`
    // covering NavGroupExpandAriaLabel / NavGroupCollapseAriaLabel removed. FluentNavCategory v5
    // does not expose an AriaExpandedLabel / AriaCollapsedLabel seam; the keys were never wired
    // and were deleted from the resx files. Story 10-2 may re-introduce both the keys and this
    // theory when a verified seam exists.

    private static ServiceProvider BuildLocalizedProvider() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddLocalization();
        _ = Shell.Extensions.ServiceCollectionExtensions.AddHexalithShellLocalization(services);
        return services.BuildServiceProvider();
    }
}
