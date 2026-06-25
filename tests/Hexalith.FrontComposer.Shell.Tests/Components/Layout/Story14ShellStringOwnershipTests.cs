using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 1.4 (FC-L10N) — the consolidated shell-frame string-ownership ready-gate. Where Story 1.3
/// proved chrome accessible-names are *present* and resource-sourced, this gate proves the distinct
/// FC-L10N invariant: under a non-default UI culture (<c>fr</c>) the rendered shell chrome resolves to
/// its <b>French</b> resx values — and is <b>not</b> the English literal — so no hard-coded English
/// chrome string can silently survive (AC2). It asserts the round-trip at the whole-shell-frame level,
/// complementing the per-key round-trip already pinned in <c>FcShellResourcesTests</c>.
/// <para>
/// The localizer is the real, embedded-resx-backed <c>IStringLocalizer&lt;FcShellResources&gt;</c> the
/// base already registers via <c>AddHexalithFrontComposerQuickstart()</c>
/// (<c>AddLocalization()</c> + <c>AddHexalithShellLocalization()</c>) — no double-registration. bUnit
/// cannot DOM-walk FluentUI v5 shadow DOM, so the gate asserts the markup-level ARIA attributes the
/// shell itself sets (exactly as <c>Story13AccessibilityPrimitivesTests</c> does).
/// </para>
/// </summary>
public sealed class Story14ShellStringOwnershipTests : LayoutComponentTestBase {
    // ── AC2 — Non-default culture: chrome resolves to FR resx values, not the EN literals ────────

    [Fact]
    public void FrontComposerShell_UnderFrenchCulture_ResolvesChromeAccessibleNamesToFrenchResxValues() {
        RegisterRenderableRegistry();
        IStringLocalizer<FcShellResources> localizer =
            Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        // Capture the expected EN and FR values from the *same* real localizer by toggling the ambient
        // UI culture — keeps the assertion resilient to copy edits while still proving "rendered != EN".
        (string enPalette, string frPalette) = ResolveBothCultures(localizer, "PaletteTriggerAriaLabel");
        (string enSettings, string frSettings) = ResolveBothCultures(localizer, "SettingsTriggerAriaLabel");
        (string enTheme, string frTheme) = ResolveBothCultures(localizer, "ThemeToggleAriaLabel");

        // Sanity: the FR resx values genuinely differ from EN, else the round-trip proves nothing.
        frPalette.ShouldNotBe(enPalette);
        frSettings.ShouldNotBe(enSettings);
        frTheme.ShouldNotBe(enTheme);

        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = new CultureInfo("fr");

            IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
                .AddChildContent("<p>Body</p>"));

            cut.WaitForAssertion(() => {
                // Palette + settings trigger carry the FR aria-label — and NOT the EN value.
                string renderedPalette = cut.Find("[data-testid=\"fc-palette-trigger\"]").GetAttribute("aria-label")!;
                renderedPalette.ShouldBe(frPalette);
                renderedPalette.ShouldNotBe(enPalette);

                string renderedSettings = cut.Find("[data-testid=\"fc-settings-button\"]").GetAttribute("aria-label")!;
                renderedSettings.ShouldBe(frSettings);
                renderedSettings.ShouldNotBe(enSettings);

                // The theme toggle names itself via Title on its Fluent menu button — assert the FR name
                // is in the rendered markup and the EN literal is absent (the no-hard-coded-English guard).
                cut.Markup.ShouldContain(frTheme);
                cut.Markup.ShouldNotContain(enTheme);
            });
        }
        finally {
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    // ── AC1 — Culture-sensitivity converse: default culture renders the EN resx values ───────────

    [Fact]
    public void FrontComposerShell_UnderEnglishCulture_ResolvesChromeAccessibleNamesToEnglishResxValues() {
        RegisterRenderableRegistry();
        IStringLocalizer<FcShellResources> localizer =
            Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        (string enPalette, string frPalette) = ResolveBothCultures(localizer, "PaletteTriggerAriaLabel");
        (string enSettings, string frSettings) = ResolveBothCultures(localizer, "SettingsTriggerAriaLabel");

        // Resource-sourced (not a hard-coded literal) and non-empty — the ownership-boundary guard.
        enPalette.ShouldNotBeNullOrEmpty();
        enSettings.ShouldNotBeNullOrEmpty();

        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = new CultureInfo("en");

            IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
                .AddChildContent("<p>Body</p>"));

            cut.WaitForAssertion(() => {
                // The same chrome that renders FR under `fr` renders EN under `en` — proving the rendered
                // names are culture-sensitive resource lookups, not literals frozen to one locale.
                string renderedPalette = cut.Find("[data-testid=\"fc-palette-trigger\"]").GetAttribute("aria-label")!;
                renderedPalette.ShouldBe(enPalette);
                renderedPalette.ShouldNotBe(frPalette);

                string renderedSettings = cut.Find("[data-testid=\"fc-settings-button\"]").GetAttribute("aria-label")!;
                renderedSettings.ShouldBe(enSettings);
                renderedSettings.ShouldNotBe(frSettings);
            });
        }
        finally {
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    // ── AC2 — Non-default culture across MORE chrome categories: skip-links + nav landmark ───────

    [Fact]
    public void FrontComposerShell_UnderFrenchCulture_ResolvesSkipLinkAndNavLandmarkChromeToFrenchResxValues() {
        // Broadens the AC2 "no hard-coded English chrome string remains" guard beyond the header
        // controls (palette/settings/theme) onto two more chrome categories the contract enumerates —
        // skip-links and the navigation landmark — so a literal slipped into either still fails the gate.
        RegisterRenderableRegistry();
        IStringLocalizer<FcShellResources> localizer =
            Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        (string enSkipContent, string frSkipContent) = ResolveBothCultures(localizer, "SkipToContentLabel");
        (string enSkipNav, string frSkipNav) = ResolveBothCultures(localizer, "SkipToNavigationLabel");
        (string enNav, string frNav) = ResolveBothCultures(localizer, "NavMenuAriaLabel");

        // Sanity: the FR resx values genuinely differ from EN, else the round-trip proves nothing.
        frSkipContent.ShouldNotBe(enSkipContent);
        frSkipNav.ShouldNotBe(enSkipNav);
        frNav.ShouldNotBe(enNav);

        CultureInfo previousUi = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = new CultureInfo("fr");

            IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
                .AddChildContent("<p>Body</p>"));

            cut.WaitForAssertion(() => {
                // Skip-to-content link text carries the FR value — and NOT the EN literal.
                string renderedSkipContent = cut.Find("a.fc-skip-link[href=\"#fc-main-content\"]").TextContent.Trim();
                renderedSkipContent.ShouldBe(frSkipContent);
                renderedSkipContent.ShouldNotBe(enSkipContent);

                // Skip-to-navigation link renders in the has-navigation shape — assert it too.
                string renderedSkipNav = cut.Find("a.fc-skip-link[href=\"#fc-nav\"]").TextContent.Trim();
                renderedSkipNav.ShouldBe(frSkipNav);
                renderedSkipNav.ShouldNotBe(enSkipNav);

                // The navigation landmark accessible-name resolves to the FR value, not the EN literal.
                string renderedNav = cut.Find("[data-testid=\"fc-navigation-rail\"]").GetAttribute("aria-label")!;
                renderedNav.ShouldBe(frNav);
                renderedNav.ShouldNotBe(enNav);
            });
        }
        finally {
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    // ── AC1 — The swap seam: a replaced IStringLocalizer<FcShellResources> sources the chrome ─────

    [Fact]
    public void FrontComposerShell_WhenLocalizerReplaced_ResolvesChromeFromTheSwappedLocalizer() {
        // AC1 names `services.Replace(IStringLocalizer<FcShellResources>)` as the supported
        // whitelabel/DB-backed extensibility seam. This proves the seam is real, not just documented:
        // swap in a sentinel localizer and the rendered chrome must source its accessible-names from it,
        // confirming the shell resolves chrome through whatever IStringLocalizer<FcShellResources> is
        // registered (the resx-backed default is not hard-wired).
        RegisterRenderableRegistry();
        Services.Replace(ServiceDescriptor.Scoped<IStringLocalizer<FcShellResources>>(_ => new SentinelLocalizer()));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            cut.Find("[data-testid=\"fc-palette-trigger\"]").GetAttribute("aria-label")
                .ShouldBe("SWAP::PaletteTriggerAriaLabel");
            cut.Find("[data-testid=\"fc-settings-button\"]").GetAttribute("aria-label")
                .ShouldBe("SWAP::SettingsTriggerAriaLabel");
            cut.Find("[data-testid=\"fc-navigation-rail\"]").GetAttribute("aria-label")
                .ShouldBe("SWAP::NavMenuAriaLabel");
        });
    }

    private static (string En, string Fr) ResolveBothCultures(
        IStringLocalizer<FcShellResources> localizer,
        string key) {
        CultureInfo previous = CultureInfo.CurrentUICulture;
        try {
            CultureInfo.CurrentUICulture = new CultureInfo("en");
            string en = localizer[key].Value;
            CultureInfo.CurrentUICulture = new CultureInfo("fr");
            string fr = localizer[key].Value;
            return (en, fr);
        }
        finally {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private void RegisterRenderableRegistry() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));
    }

    /// <summary>
    /// Stand-in for a whitelabel / DB-backed <c>IStringLocalizer&lt;FcShellResources&gt;</c> swapped in
    /// via the documented <c>services.Replace</c> seam. Returns a <c>SWAP::{key}</c> sentinel per key so a
    /// test can prove the rendered chrome sourced its accessible-name from the replaced localizer.
    /// </summary>
    private sealed class SentinelLocalizer : IStringLocalizer<FcShellResources> {
        public LocalizedString this[string name] => new(name, $"SWAP::{name}", resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(name, $"SWAP::{name}", resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
