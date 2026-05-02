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
    [InlineData("RestoreDefaultsLabel", "Restore defaults", "Rétablir les paramètres par défaut")]
    [InlineData("RestoreDefaultsHelperText",
        "Clears density preference and sets theme to follow system.",
        "Efface la préférence de densité et règle le thème sur le suivi système.")]
    [InlineData("PreviewOnlyBadgeText",
        "Preview only — Comfortable is active.",
        "Aperçu uniquement — Confortable est actif.")]
    [InlineData("DensityAnnouncementTemplate",
        "Density set to {0}.",
        "Densité réglée sur {0}.")]
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

    // --- Story 3-4 Task 10.11 (D14 / D23 / AC2 / AC5 / AC6) — 14 new palette + shortcut keys ---

    [Theory]
    [InlineData("PaletteTriggerAriaLabel", "Open command palette", "Ouvrir la palette de commandes")]
    [InlineData("CommandPaletteTitle", "Command palette", "Palette de commandes")]
    [InlineData("PaletteSearchPlaceholder",
        "Search projections, commands, recent… (type ? for shortcuts)",
        "Rechercher projections, commandes, récents… (tapez ? pour raccourcis)")]
    [InlineData("PaletteCategoryProjections", "Projections", "Projections")]
    [InlineData("PaletteCategoryCommands", "Commands", "Commandes")]
    [InlineData("PaletteCategoryRecent", "Recent", "Récents")]
    [InlineData("PaletteResultCountTemplate", "{0} results", "{0} résultats")]
    [InlineData("PaletteNoResultsText", "No matches found", "Aucun résultat trouvé")]
    [InlineData("PaletteInCurrentContextSuffix", "(in current context)", "(dans le contexte actuel)")]
    [InlineData("ShortcutsCategoryLabel", "Keyboard shortcuts", "Raccourcis clavier")]
    [InlineData("PaletteShortcutDescription", "Open command palette", "Ouvrir la palette de commandes")]
    [InlineData("SettingsShortcutDescription", "Open settings", "Ouvrir les paramètres")]
    [InlineData("HomeShortcutDescription", "Go to home", "Aller à l'accueil")]
    [InlineData("KeyboardShortcutsCommandLabel", "Keyboard Shortcuts", "Raccourcis clavier")]
    [InlineData("KeyboardShortcutsCommandDescription", "View all keyboard shortcuts", "Voir tous les raccourcis clavier")]
    public void PaletteAndShortcutKeysResolveInBothLocales(string key, string enValue, string frValue) {
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

    // --- Story 4-2 T6.3 (AC5 / D4 / D12) — 2 new status-badge resource keys ---

    [Theory]
    [InlineData("StatusBadgeAriaLabelTemplate", "{0}: {1}", "{0} : {1}")]
    [InlineData("StatusBadgeUnknownStateFallback", "Unknown", "Inconnu")]
    public void StatusBadgeKeysResolveInBothLocales(string key, string enValue, string frValue) {
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

    // --- Story 4-3 T8.3 (D19 / AC1-AC6) — 13 new filter + search + summary keys ---

    [Theory]
    [InlineData("ColumnFilterPlaceholderTemplate", "Filter {0}", "Filtrer {0}")]
    [InlineData("ColumnFilterAriaLabelTemplate", "Filter column {0}", "Filtrer la colonne {0}")]
    [InlineData("StatusFilterChipsAriaLabel", "Status filter chips", "Puces de filtrage par statut")]
    [InlineData("StatusFilterChipAriaLabelTemplate", "{0}, currently {1}", "{0}, actuellement {1}")]
    [InlineData("GlobalSearchPlaceholder", "Search…", "Rechercher…")]
    [InlineData("GlobalSearchAriaLabel", "Search across all columns", "Rechercher dans toutes les colonnes")]
    [InlineData("FilterSummaryShowingTemplate", "Showing {0} of {1} {2}", "Affichage de {0} sur {1} {2}")]
    [InlineData("FilterResetButtonLabel", "Reset filters", "Réinitialiser les filtres")]
    [InlineData("FilterResetButtonAriaLabelTemplate",
        "Reset all filters. {0} filters currently active.",
        "Réinitialiser tous les filtres. {0} filtres actuellement actifs.")]
    [InlineData("EmptyFilteredStateTemplate",
        "No {0} match the current filters. Reset filters to see all {1} {0}.",
        "Aucun {0} ne correspond aux filtres. Réinitialiser les filtres pour voir les {1} {0}.")]
    [InlineData("FilterSummaryColumnContainsTemplate",
        "{0} contains \"{1}\"",
        "{0} contient « {1} »")]
    [InlineData("FilterSummaryStatusClauseTemplate", "Status: {0}", "Statut : {0}")]
    [InlineData("FilterSummarySearchClauseTemplate", "Search: \"{0}\"", "Recherche : « {0} »")]
    [InlineData("StatusFilterChipActiveLabel", "active", "actif")]
    [InlineData("StatusFilterChipInactiveLabel", "inactive", "inactif")]
    [InlineData("FilterSummarySortClauseTemplate", "Sorted by {0} {1}", "Trié par {0} {1}")]
    [InlineData("SortDirectionAscending", "ascending", "croissant")]
    [InlineData("SortDirectionDescending", "descending", "décroissant")]
    [InlineData("FilterSummaryOrConjunction", " or ", " ou ")]
    [InlineData("SlashFocusFilterShortcutDescription",
        "Focus the first column filter in the current DataGrid",
        "Placer le focus sur le premier filtre de colonne du DataGrid actif")]
    public void FilterSurfaceKeysResolveInBothLocales(string key, string enValue, string frValue) {
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

    [Fact]
    public void FilterSummaryStatusClauseTemplateFrenchUsesNonBreakingSpaceBeforeColon() {
        // Story 4-3 T8.4 / D19 — byte-level guard: the French clause MUST use U+00A0 before the
        // colon per French typographic convention. An ASCII space would fail the convention silently.
        ResourceManager manager = new(typeof(FcShellResources));
        string frValue = manager.GetString("FilterSummaryStatusClauseTemplate", new CultureInfo("fr"))!;

        frValue.ShouldNotBeNull();
        int colonIndex = frValue.IndexOf(':');
        colonIndex.ShouldBeGreaterThan(0);
        char precedingChar = frValue[colonIndex - 1];
        precedingChar.ShouldBe(' ', $"Expected U+00A0 NBSP before colon in French FilterSummaryStatusClauseTemplate; got U+{(int)precedingChar:X4}");
    }

    [Fact]
    public void FilterSummarySearchClauseTemplateFrenchUsesNonBreakingSpaceBeforeColon() {
        ResourceManager manager = new(typeof(FcShellResources));
        string frValue = manager.GetString("FilterSummarySearchClauseTemplate", new CultureInfo("fr"))!;

        frValue.ShouldNotBeNull();
        int colonIndex = frValue.IndexOf(':');
        colonIndex.ShouldBeGreaterThan(0);
        char precedingChar = frValue[colonIndex - 1];
        precedingChar.ShouldBe(' ', $"Expected U+00A0 NBSP before colon in French FilterSummarySearchClauseTemplate; got U+{(int)precedingChar:X4}");
    }

    [Fact]
    public void StatusBadgeAriaLabelTemplateFrenchUsesNonBreakingSpaceBeforeColon() {
        // Story 4-2 T6.4 / D12 — explicit byte-level guard: the template must contain U+00A0
        // between "{0}" and ":". A well-meaning editor save that collapses NBSP to ASCII
        // space would fail French typographic convention silently otherwise.
        ResourceManager manager = new(typeof(FcShellResources));
        string frValue = manager.GetString("StatusBadgeAriaLabelTemplate", new CultureInfo("fr"))!;

        frValue.ShouldNotBeNull();
        int colonIndex = frValue.IndexOf(':');
        colonIndex.ShouldBeGreaterThan(0);
        char precedingChar = frValue[colonIndex - 1];
        precedingChar.ShouldBe(' ', $"Expected U+00A0 NBSP before colon in French StatusBadgeAriaLabelTemplate; got U+{(int)precedingChar:X4}");
    }

    // Story 7-3 Pass 3 / Pass-2 P35 — placeholder count parity across EN/FR for new authorization
    // resource keys. If EN later adds {1} and FR is missed, runtime IndexOutOfRange at warning render.
    [Theory]
    [InlineData("UnauthorizedCommandWarningTitle")]
    [InlineData("UnauthorizedCommandWarningMessage")]
    [InlineData("AuthorizationActionUnavailableTitle")]
    [InlineData("AuthorizationActionUnavailableMessage")]
    [InlineData("UnauthenticatedCommandWarningTitle")]
    [InlineData("UnauthenticatedCommandWarningMessage")]
    [InlineData("AuthorizationCheckingPermissionTitle")]
    [InlineData("AuthorizationCheckingPermissionMessage")]
    public void AuthorizationResourceKey_PlaceholderCountMatchesAcrossLocales(string key) {
        ResourceManager manager = new(typeof(FcShellResources));
        string? enValue = manager.GetString(key, new CultureInfo("en"));
        string? frValue = manager.GetString(key, new CultureInfo("fr"));

        enValue.ShouldNotBeNull($"EN value missing for key '{key}'.");
        frValue.ShouldNotBeNull($"FR value missing for key '{key}'.");

        HashSet<int> enPlaceholders = ExtractFormatPlaceholderIndices(enValue);
        HashSet<int> frPlaceholders = ExtractFormatPlaceholderIndices(frValue);

        frPlaceholders.SetEquals(enPlaceholders).ShouldBeTrue(
            $"Placeholder set mismatch for key '{key}'. EN={string.Join(",", enPlaceholders.OrderBy(static i => i))} FR={string.Join(",", frPlaceholders.OrderBy(static i => i))}. Update the missing locale to keep placeholder counts aligned.");
    }

    private static HashSet<int> ExtractFormatPlaceholderIndices(string template) {
        // Walk the template extracting every {N} index. Mirrors String.Format index detection without
        // consulting the format provider — sufficient for parity comparison.
        HashSet<int> indices = [];
        for (int i = 0; i < template.Length; i++) {
            char c = template[i];
            if (c != '{') {
                continue;
            }

            // Escaped '{{' literal; skip past it.
            if (i + 1 < template.Length && template[i + 1] == '{') {
                i++;
                continue;
            }

            int closing = template.IndexOf('}', i + 1);
            if (closing < 0) {
                break;
            }

            string body = template.Substring(i + 1, closing - i - 1);
            // Strip optional ',alignment' or ':format' suffix per String.Format grammar.
            int commaOrColon = body.IndexOfAny([',', ':']);
            string indexText = commaOrColon >= 0 ? body.Substring(0, commaOrColon) : body;
            if (int.TryParse(indexText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index)) {
                indices.Add(index);
            }

            i = closing;
        }

        return indices;
    }

    private static ServiceProvider BuildLocalizedProvider() {
        ServiceCollection services = new();
        _ = services.AddLogging();
        _ = services.AddLocalization();
        _ = Shell.Extensions.ServiceCollectionExtensions.AddHexalithShellLocalization(services);
        return services.BuildServiceProvider();
    }
}
