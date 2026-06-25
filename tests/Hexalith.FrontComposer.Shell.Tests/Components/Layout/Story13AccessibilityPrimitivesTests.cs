using System.Text.RegularExpressions;

using AngleSharp.Dom;

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
/// Story 1.3 (FC-A11Y) — the consolidated Layer-1 shell-frame accessibility ready-gate. Pins the
/// three shell-frame invariants the FC-A11Y contract names so they cannot silently regress:
/// (1) every skip link resolves to a real <c>tabindex="-1"</c> focus target, (2) the always-present
/// header-chrome controls expose a non-empty accessible name, and (3) the scoped shell CSS suppresses
/// no focus indicator (the documented zero-override invariant). The override-time layer
/// (HFC1050–HFC1055) and the e2e axe layer live elsewhere by design; bUnit cannot DOM-walk FluentUI
/// v5 shadow DOM (see <c>AxeCoreA11yTests</c>), so this gate asserts the markup-level ARIA contract
/// only.
/// </summary>
public sealed class Story13AccessibilityPrimitivesTests : LayoutComponentTestBase {
    // ── AC1 — Skip-link → real focus target ─────────────────────────────────────────────────────

    [Fact]
    public void FrontComposerShell_WhenRendered_SkipLinkResolvesToMainContentFocusTarget() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            // The skip link exists and points at the content region…
            IElement skip = cut.Find("a.fc-skip-link[href=\"#fc-main-content\"]");
            skip.ShouldNotBeNull();

            // …and that href resolves to a real, focusable target (id + tabindex="-1").
            IElement target = cut.Find("#fc-main-content");
            target.Id.ShouldBe("fc-main-content");
            target.GetAttribute("tabindex").ShouldBe("-1");
        });
    }

    [Fact]
    public void FrontComposerShell_WithNavigation_SkipToNavLinkResolvesToNavFocusTarget() {
        RegisterRenderableRegistry();

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            // When the registry has ≥1 renderable manifest the shell is in its has-navigation shape,
            // so the second skip link renders and must resolve to the nav focus target.
            IElement skip = cut.Find("a.fc-skip-link[href=\"#fc-nav\"]");
            skip.ShouldNotBeNull();

            IElement target = cut.Find("#fc-nav");
            target.Id.ShouldBe("fc-nav");
            target.GetAttribute("tabindex").ShouldBe("-1");
        });
    }

    // ── AC1 — Accessible-name coverage on always-present header chrome ───────────────────────────

    [Fact]
    public void FrontComposerShell_WithNavigation_HeaderChromeControlsExposeAccessibleNames() {
        RegisterRenderableRegistry();
        IStringLocalizer<FcShellResources> localizer =
            Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        string paletteLabel = localizer["PaletteTriggerAriaLabel"].Value;
        string settingsLabel = localizer["SettingsTriggerAriaLabel"].Value;
        string themeLabel = localizer["ThemeToggleAriaLabel"].Value;

        // The accessible names are localized (FC-L10N, Story 1.4): assert against the resolved
        // resource values, never hard-coded English — but only require them to be present + non-empty.
        paletteLabel.ShouldNotBeNullOrEmpty();
        settingsLabel.ShouldNotBeNullOrEmpty();
        themeLabel.ShouldNotBeNullOrEmpty();

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            // Palette trigger + settings button carry the localized aria-label on the rendered control.
            cut.Find("[data-testid=\"fc-palette-trigger\"]").GetAttribute("aria-label")
                .ShouldBe(paletteLabel);
            cut.Find("[data-testid=\"fc-settings-button\"]").GetAttribute("aria-label")
                .ShouldBe(settingsLabel);

            // The theme toggle names itself via Title on its menu button; assert the localized name
            // is present in the rendered markup (resilient to the exact Fluent menu-button element).
            cut.Markup.ShouldContain(themeLabel);
        });
    }

    [Fact]
    public void FrontComposerShell_WithNavigation_NavigationLandmarkExposesAccessibleName() {
        RegisterRenderableRegistry();
        IStringLocalizer<FcShellResources> localizer =
            Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        string navLabel = localizer["NavMenuAriaLabel"].Value;
        navLabel.ShouldNotBeNullOrEmpty();

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>             // AC1 names *every* interactive shell-chrome element — the auto-populated navigation
                                               // landmark (the #fc-nav focus target's content) must itself carry the localized accessible
                                               // name so the skip-to-nav link lands on a named region, not an anonymous one. Asserted
                                               // against the localized IStringLocalizer<FcShellResources> value (FC-L10N-safe), not English.
            cut.Find("[data-testid=\"fc-navigation-rail\"]").GetAttribute("aria-label")
                .ShouldBe(navLabel));
    }

    // ── AC2 / primitive #3 — aria-live politeness ladder wired into the shell frame ──────────────

    [Fact]
    public void FrontComposerShell_WhenRendered_DensityAnnouncerExposesPoliteLiveRegion() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            // Primitive #3 (the aria-live politeness ladder) is a named FC-A11Y primitive, but it was
            // pinned only in the isolated FcDensityAnnouncerTests. Confirm the *shell frame itself*
            // mounts the polite, visually-hidden live region (role=status / aria-live=polite /
            // aria-atomic=true / .fc-sr-only) so density changes are announced without stealing focus —
            // the consolidated ready-gate, not just the component, now guarantees it.
            IRenderedComponent<FcDensityAnnouncer> announcer = cut.FindComponent<FcDensityAnnouncer>();
            IElement liveRegion = announcer.Find("div");
            liveRegion.GetAttribute("role").ShouldBe("status");
            liveRegion.GetAttribute("aria-live").ShouldBe("polite");
            liveRegion.GetAttribute("aria-atomic").ShouldBe("true");
            liveRegion.ClassList.ShouldContain("fc-sr-only");
        });
    }

    // ── AC1 — Zero-override focus invariant: no suppressed focus in scoped shell CSS ─────────────

    [Fact]
    public void FrontComposerShell_ScopedCss_SuppressesNoFocusIndicator() {
        string cssPath = LocateShellScopedCssFile();
        string content = File.ReadAllText(cssPath);

        // Normalize whitespace so "outline : none" / "outline:none" are matched identically.
        string normalized = Regex.Replace(content, @"\s+", string.Empty);

        bool suppressesOutline = normalized.Contains("outline:none", StringComparison.OrdinalIgnoreCase);
        bool suppressesShadow = normalized.Contains("box-shadow:none", StringComparison.OrdinalIgnoreCase);

        // The documented zero-override invariant: shell CSS suppresses no focus indicator. If a future
        // suppression is ever introduced it MUST pair with a :focus-visible restore (the WCAG 2.4.7 /
        // HFC1052 contract); a bare suppression is a ready-gate regression.
        if (suppressesOutline || suppressesShadow) {
            normalized.ShouldContain(
                ":focus-visible",
                Case.Insensitive,
                "Scoped shell CSS suppresses a focus indicator (outline/box-shadow: none) without an "
                + "adjacent :focus-visible restore — this breaks the FC-A11Y zero-override invariant "
                + "(WCAG 2.4.7 Focus Visible / HFC1052).");
        }

        suppressesOutline.ShouldBeFalse(
            "FrontComposerShell.razor.css must not contain `outline: none` (zero-override focus invariant).");
        suppressesShadow.ShouldBeFalse(
            "FrontComposerShell.razor.css must not contain `box-shadow: none` (zero-override focus invariant).");
    }

    private void RegisterRenderableRegistry() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));
    }

    private static string LocateShellScopedCssFile() {
        string here = AppContext.BaseDirectory;
        DirectoryInfo? cursor = new(here);
        while (cursor is not null) {
            string candidate = Path.Combine(
                cursor.FullName,
                "src",
                "Hexalith.FrontComposer.Shell",
                "Components",
                "Layout",
                "FrontComposerShell.razor.css");
            if (File.Exists(candidate)) {
                return candidate;
            }

            cursor = cursor.Parent;
        }

        throw new FileNotFoundException("FrontComposerShell.razor.css not found from the test base directory.");
    }
}
