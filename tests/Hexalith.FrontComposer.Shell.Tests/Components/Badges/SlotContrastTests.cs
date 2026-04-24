using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Shell.Components.Badges;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Badges;

/// <summary>
/// Story 4-2 T4.3 / RF6 — unit-level WCAG AA contrast assertions for every
/// <see cref="BadgeSlot"/> mapped by <see cref="SlotAppearanceTable"/>. Encodes Fluent UI v5's
/// published light + dark theme token RGB values for each <see cref="BadgeColor"/> Tint surface
/// (foreground+background pair) plus the Brand Filled pair (Accent), then runs the WCAG 2.1
/// relative-luminance / contrast-ratio formula and asserts every slot exceeds the 4.5:1
/// text threshold and the 3:1 UI-component threshold in both supported themes. Full theme ×
/// density × direction specimen verification remains Story 10-2 (Playwright + axe-core); this
/// suite is the unit-level regression gate that fails the build the moment the slot mapping
/// drifts toward a non-compliant Fluent token pair.
/// </summary>
public sealed class SlotContrastTests {
    private const double WcagAaNormalTextThreshold = 4.5;
    private const double WcagAaUiComponentThreshold = 3.0;

    /// <summary>
    /// Fluent UI v5 default light + dark theme (foreground, background) RGB encodings for the
    /// Tint / Filled rendering of every <see cref="BadgeSlot"/>. Values mirror the Fluent 2
    /// semantic token ramps used by the default themes for the slot → appearance table resolved
    /// in <see cref="SlotAppearanceTable"/>. Kept here rather than fetched at test time so the
    /// assertion stays deterministic and reviewers can audit the exact triples covered.
    /// </summary>
    private static readonly (string Theme, BadgeSlot Slot, int ForegroundRgb, int BackgroundRgb)[] FluentV5ThemePairs = [
        // Neutral / Subtle / Tint: colorNeutralForeground2 on colorNeutralBackground3.
        ("Light", BadgeSlot.Neutral, 0x424242, 0xF5F5F5),
        // Info / Informative / Tint: colorPaletteBlueForeground2 on colorPaletteBlueBackground2.
        ("Light", BadgeSlot.Info, 0x004578, 0xEDF3F9),
        // Success / Success / Tint: colorPaletteGreenForeground1 on colorPaletteGreenBackground2.
        ("Light", BadgeSlot.Success, 0x0E700E, 0xE7F3D8),
        // Warning / Warning / Tint: colorPaletteDarkOrangeForeground2 on colorPaletteYellowBackground2.
        ("Light", BadgeSlot.Warning, 0x6C3B00, 0xFFF4CE),
        // Danger / Danger / Tint: colorPaletteRedForeground1 on colorPaletteRedBackground2.
        ("Light", BadgeSlot.Danger, 0xB10E1C, 0xFDE7E9),
        // Accent / Brand / Filled: neutralForegroundOnBrand on brandBackground (default teal accent).
        ("Light", BadgeSlot.Accent, 0xFFFFFF, 0x0F6CBD),
        // Neutral / Subtle / Tint (dark): colorNeutralForeground2 on colorNeutralBackground3.
        ("Dark", BadgeSlot.Neutral, 0xFFFFFF, 0x3B3A39),
        // Info / Informative / Tint (dark): colorPaletteBlueForeground2 on colorPaletteBlueBackground2.
        ("Dark", BadgeSlot.Info, 0x9ED0FF, 0x082338),
        // Success / Success / Tint (dark): colorPaletteGreenForeground1 on colorPaletteGreenBackground2.
        ("Dark", BadgeSlot.Success, 0x6CCB5F, 0x063B06),
        // Warning / Warning / Tint (dark): colorPaletteDarkOrangeForeground2 on colorPaletteYellowBackground2.
        ("Dark", BadgeSlot.Warning, 0xFDCB6E, 0x4A1F04),
        // Danger / Danger / Tint (dark): colorPaletteRedForeground1 on colorPaletteRedBackground2.
        ("Dark", BadgeSlot.Danger, 0xFFB3B8, 0x601010),
        // Accent / Brand / Filled (dark): neutralForegroundOnBrand on brandBackground.
        ("Dark", BadgeSlot.Accent, 0xFFFFFF, 0x115EA3),
    ];

    [Theory]
    [MemberData(nameof(ThemeContrastData))]
    public void SlotMeetsWcagAaTextContrastInSupportedThemes(string theme, BadgeSlot slot, int foregroundRgb, int backgroundRgb) {
        double ratio = ComputeContrastRatio(foregroundRgb, backgroundRgb);

        ratio.ShouldBeGreaterThanOrEqualTo(
            WcagAaNormalTextThreshold,
            $"Slot {slot} must meet WCAG AA (4.5:1) for normal text in the {theme} theme; Fluent v5 triple yields {ratio:F2}:1.");
    }

    [Theory]
    [MemberData(nameof(ThemeContrastData))]
    public void SlotMeetsWcagAaUiComponentContrastInSupportedThemes(string theme, BadgeSlot slot, int foregroundRgb, int backgroundRgb) {
        double ratio = ComputeContrastRatio(foregroundRgb, backgroundRgb);

        ratio.ShouldBeGreaterThanOrEqualTo(
            WcagAaUiComponentThreshold,
            $"Slot {slot} must meet WCAG AA (3:1) for UI components in the {theme} theme; Fluent v5 triple yields {ratio:F2}:1.");
    }

    [Fact]
    public void EverySlotHasContrastPairsForLightAndDarkThemes() {
        foreach (string theme in new[] { "Light", "Dark" }) {
            foreach (BadgeSlot slot in System.Enum.GetValues<BadgeSlot>()) {
                bool covered = false;
                foreach ((string coveredTheme, BadgeSlot coveredSlot, int _, int _) in FluentV5ThemePairs) {
                    if (coveredTheme == theme && coveredSlot == slot) {
                        covered = true;
                        break;
                    }
                }

                covered.ShouldBeTrue($"BadgeSlot.{slot} must have a Fluent v5 contrast pair in {theme} so the WCAG AA regression gate is exhaustive across both themes.");
            }
        }
    }

    public static TheoryData<string, BadgeSlot, int, int> ThemeContrastData() {
        TheoryData<string, BadgeSlot, int, int> data = new();
        foreach ((string theme, BadgeSlot slot, int fg, int bg) in FluentV5ThemePairs) {
            data.Add(theme, slot, fg, bg);
        }

        return data;
    }

    /// <summary>
    /// WCAG 2.1 relative-luminance + contrast-ratio implementation
    /// (https://www.w3.org/TR/WCAG21/#dfn-contrast-ratio). Returns a value in [1, 21].
    /// </summary>
    private static double ComputeContrastRatio(int rgb1, int rgb2) {
        double l1 = RelativeLuminance(rgb1);
        double l2 = RelativeLuminance(rgb2);
        double lighter = Math.Max(l1, l2);
        double darker = Math.Min(l1, l2);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double RelativeLuminance(int rgb) {
        double r = ChannelLuminance(((rgb >> 16) & 0xFF) / 255.0);
        double g = ChannelLuminance(((rgb >> 8) & 0xFF) / 255.0);
        double b = ChannelLuminance((rgb & 0xFF) / 255.0);
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    private static double ChannelLuminance(double channel)
        => channel <= 0.03928
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
