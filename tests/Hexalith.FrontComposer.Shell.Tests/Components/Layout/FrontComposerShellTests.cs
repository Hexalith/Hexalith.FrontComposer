using Bunit;

using Fluxor.Blazor.Web;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-1 shell layout render tests covering the framework-owned shell composition.
/// </summary>
public sealed class FrontComposerShellTests : LayoutComponentTestBase
{
    [Fact]
    public void Renders_shell_chrome_and_omits_navigation_when_not_provided()
    {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("fc-shell-root");
            cut.Markup.ShouldContain("Hexalith FrontComposer");
            cut.Markup.ShouldContain(DateTime.Now.Year.ToString(), Case.Sensitive);
            cut.Markup.ShouldContain("48px");
            cut.Markup.ShouldNotContain("220px");
            _ = cut.FindComponent<FcSystemThemeWatcher>();
            _ = cut.FindComponent<FluentProviders>();
            _ = cut.FindComponent<StoreInitializer>();
        });
    }

    [Fact]
    public void Renders_navigation_slot_when_provided()
    {
        RenderFragment navigation = builder => builder.AddMarkupContent(0, "<nav>Navigation rail</nav>");

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.Navigation, navigation)
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Navigation rail");
            cut.Markup.ShouldContain("220px");
        });
    }

    [Fact]
    public void Applies_theme_once_on_first_render()
    {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>
            ThemeService.Received(1).SetThemeAsync(
                Arg.Is<ThemeSettings>(settings => settings.Mode == ThemeMode.Light && settings.Color == "#0097A7")));
    }
}
