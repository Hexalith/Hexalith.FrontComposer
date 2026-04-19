using Bunit;

using Fluxor.Blazor.Web;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 3-1 shell layout render tests covering the framework-owned shell composition.
/// Story 3-2 Task 10.10 adds the Navigation auto-populate path (D18 / ADR-035) and the
/// adopter-supplied override path.
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

            // F5 — lock the e2e selector contract at the unit level.
            cut.Markup.ShouldContain("data-testid=\"fc-shell-navigation\"");
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

    // --- Story 3-2 Task 10.10 — ADR-035 / D18 auto-populate + override ---

    [Fact]
    public void AutoRendersNavigationWhenSlotIsNullAndRegistryNonEmpty()
    {
        // ATDD RED PHASE — fails until Task 8.3/8.4 wire the auto-populate render block
        // into FrontComposerShell.razor and inject IFrontComposerRegistry.
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>
        {
            _ = cut.FindComponent<FrontComposerNavigation>();
            cut.Markup.ShouldContain("220px", Case.Sensitive);
        });
    }

    [Fact]
    public void AdopterSuppliedNavigationFragmentWins()
    {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        RenderFragment custom = builder => builder.AddMarkupContent(0, "<nav data-testid=\"adopter-nav\">Custom</nav>");

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.Navigation, custom)
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("adopter-nav");
            cut.Markup.ShouldContain("Custom");
            // Framework nav must NOT be rendered when adopter supplies their own fragment.
            Should.Throw<Bunit.ComponentNotFoundException>(() => cut.FindComponent<FrontComposerNavigation>());
        });
    }

    [Fact]
    public void NoNavigationRendersWhenSlotNullAndRegistryEmpty()
    {
        // Inherited Story 3-1 AC1 — when registry has zero manifests AND adopter left Navigation null,
        // the Navigation layout area is OMITTED (no 220 px empty column).
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldNotContain("220px", Case.Sensitive);
            Should.Throw<Bunit.ComponentNotFoundException>(() => cut.FindComponent<FrontComposerNavigation>());
        });
    }
}
