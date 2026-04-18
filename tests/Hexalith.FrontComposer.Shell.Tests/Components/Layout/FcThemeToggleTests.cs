using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-1 theme toggle render and accessibility tests.
/// </summary>
public sealed class FcThemeToggleTests : LayoutComponentTestBase
{
    [Fact]
    public void Toggle_button_exposes_theme_aria_label()
    {
        IRenderedComponent<FcThemeToggle> cut = Render<FcThemeToggle>();
        IStringLocalizer<FcShellResources> localizer = Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        cut.Markup.ShouldContain("fc-sr-only");
        cut.Markup.ShouldContain(localizer["ThemeToggleAriaLabel"].Value);
    }

    [Fact]
    public void Current_label_tracks_fluxor_theme_state()
    {
        DispatchTheme(ThemeValue.Dark);

        IRenderedComponent<FcThemeToggle> cut = Render<FcThemeToggle>();
        IStringLocalizer<FcShellResources> localizer = Services.GetRequiredService<IStringLocalizer<FcShellResources>>();

        cut.Markup.ShouldContain(localizer["ThemeDarkLabel"].Value);
    }
}
