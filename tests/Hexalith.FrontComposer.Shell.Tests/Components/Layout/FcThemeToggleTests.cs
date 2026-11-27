using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-1 theme toggle render and accessibility tests.
/// </summary>
public sealed class FcThemeToggleTests : LayoutComponentTestBase {
    [Fact]
    public void Toggle_button_exposes_theme_aria_label() {
        IRenderedComponent<FcThemeToggle> cut = Render<FcThemeToggle>();
        IStringLocalizer<FcShellResources> localizer = Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        cut.Markup.ShouldContain("fc-sr-only");
        cut.Markup.ShouldContain(localizer["ThemeToggleAriaLabel"].Value);
        cut.Find("fluent-menu-button").GetAttribute("aria-label")
            .ShouldBe($"{localizer["ThemeToggleAriaLabel"].Value}: {localizer["ThemeLightLabel"].Value}");
    }

    [Fact]
    public void Current_label_tracks_fluxor_theme_state() {
        DispatchTheme(ThemeValue.Dark);

        IRenderedComponent<FcThemeToggle> cut = Render<FcThemeToggle>();
        IStringLocalizer<FcShellResources> localizer = Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        cut.Markup.ShouldContain(localizer["ThemeDarkLabel"].Value);
        cut.Find("fluent-menu-button").GetAttribute("aria-label")
            .ShouldBe($"{localizer["ThemeToggleAriaLabel"].Value}: {localizer["ThemeDarkLabel"].Value}");
    }

    [Theory]
    [InlineData("Light", "WeatherSunny")]
    [InlineData("Dark", "WeatherMoon")]
    [InlineData("System", "DesktopMac")]
    public void ThemeMenuItemsHaveIconsAndKeepTheirNames(string label, string iconName) {
        using CultureScope culture = new("en");
        IRenderedComponent<FcThemeToggle> cut = Render<FcThemeToggle>();

        FluentMenuItem item = cut.FindComponents<FluentMenuItem>()
            .Single(menuItem => menuItem.Find("fluent-menu-item").GetAttribute("aria-label") == label)
            .Instance;

        item.IconStart.ShouldNotBeNull().GetType().Name.ShouldBe(iconName);
        cut.Find($"fluent-menu-item[aria-label='{label}']").TextContent.Trim().ShouldBe(label);
    }

    [Fact]
    public void AccessibleNameTracksThemeChangesAfterRender() {
        IRenderedComponent<FcThemeToggle> cut = Render<FcThemeToggle>();
        IStringLocalizer<FcShellResources> localizer = Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        DispatchTheme(ThemeValue.System);
        cut.WaitForAssertion(() => cut.Find("fluent-menu-button").GetAttribute("aria-label")
            .ShouldBe($"{localizer["ThemeToggleAriaLabel"].Value}: {localizer["ThemeSystemLabel"].Value}"));

        DispatchTheme(ThemeValue.Dark);
        cut.WaitForAssertion(() => cut.Find("fluent-menu-button").GetAttribute("aria-label")
            .ShouldBe($"{localizer["ThemeToggleAriaLabel"].Value}: {localizer["ThemeDarkLabel"].Value}"));
    }
}
