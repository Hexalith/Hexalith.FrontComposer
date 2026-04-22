using Bunit;
using Bunit.TestDoubles;

using Fluxor;
using Fluxor.Blazor.Web;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
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
public sealed class FrontComposerShellTests : LayoutComponentTestBase {
    [Fact]
    public void Renders_shell_chrome_and_omits_navigation_when_not_provided() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
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
    public void Renders_navigation_slot_when_provided() {
        RenderFragment navigation = builder => builder.AddMarkupContent(0, "<nav>Navigation rail</nav>");

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.Navigation, navigation)
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("Navigation rail");
            cut.Markup.ShouldContain("220px");

            // F5 — lock the e2e selector contract at the unit level.
            cut.Markup.ShouldContain("data-testid=\"fc-shell-navigation\"");
        });
    }

    [Fact]
    public void Applies_theme_once_on_first_render() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() =>
            ThemeService.Received(1).SetThemeAsync(
                Arg.Is<ThemeSettings>(settings => settings.Mode == ThemeMode.Light && settings.Color == "#0097A7")));
    }

    // --- Story 3-2 Task 10.10 — ADR-035 / D18 auto-populate + override ---

    [Fact]
    public void AutoRendersNavigationWhenSlotIsNullAndRegistryNonEmpty() {
        // ATDD RED PHASE — fails until Task 8.3/8.4 wire the auto-populate render block
        // into FrontComposerShell.razor and inject IFrontComposerRegistry.
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            _ = cut.FindComponent<FrontComposerNavigation>();
            cut.Markup.ShouldContain("220px", Case.Sensitive);
        });
    }

    [Fact]
    public void AutoRenderedNavigationUsesRailWidthAtCompactDesktop() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        Services.GetRequiredService<Fluxor.IDispatcher>()
            .Dispatch(new ViewportTierChangedAction(ViewportTier.CompactDesktop));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("48px", Case.Sensitive);
            cut.Markup.ShouldContain("id=\"fc-nav\"", Case.Sensitive);
            cut.Markup.ShouldContain("tabindex=\"-1\"", Case.Sensitive);
        });
    }

    [Fact]
    public void AdopterSuppliedNavigationFragmentWins() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        RenderFragment custom = builder => builder.AddMarkupContent(0, "<nav data-testid=\"adopter-nav\">Custom</nav>");

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.Navigation, custom)
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("adopter-nav");
            cut.Markup.ShouldContain("Custom");
            // Framework nav must NOT be rendered when adopter supplies their own fragment.
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(() => cut.FindComponent<FrontComposerNavigation>());
        });
    }

    [Fact]
    public void NoNavigationRendersWhenSlotNullAndRegistryEmpty() {
        // Inherited Story 3-1 AC1 — when registry has zero manifests AND adopter left Navigation null,
        // the Navigation layout area is OMITTED (no 220 px empty column).
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("220px", Case.Sensitive);
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(() => cut.FindComponent<FrontComposerNavigation>());
        });
    }

    [Fact]
    public void NoNavigationRendersWhenRegistryContainsOnlyCommands() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest(
                Name: "CommandsOnly",
                BoundedContext: "CommandsOnly",
                Projections: [],
                Commands: ["CommandsOnly.Domain.Commands.RunThing"]),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("data-testid=\"fc-shell-navigation\"", Case.Sensitive);
            cut.Markup.ShouldNotContain("220px", Case.Sensitive);
            cut.Markup.ShouldContain("id=\"fc-main-content\" tabindex=\"-1\"", Case.Sensitive);
        });
    }

    // Story 3-3 Task 10.10 (D12 / D16 / AC7) — HeaderEnd auto-populate + Ctrl+, wiring.

    [Fact]
    public void AutoRendersSettingsButtonWhenHeaderEndIsNull() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            _ = cut.FindComponent<FcSettingsButton>();
        });
    }

    [Fact]
    public void AdopterSuppliedHeaderEndWins() {
        RenderFragment custom = builder => builder.AddMarkupContent(0, "<span data-testid=\"adopter-header-end\">Custom</span>");

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.HeaderEnd, custom)
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("adopter-header-end");
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(
                () => cut.FindComponent<FcSettingsButton>(),
                "Adopter-supplied HeaderEnd fragment must suppress the framework-owned FcSettingsButton (D12).");
        });
    }

    [Fact]
    public async Task CtrlCommaOpensSettingsDialogFromShellRoot() {
        RecordingDialogService dialogService = new();
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        await cut.Find(".fc-shell-root").TriggerEventAsync(
            "onkeydown",
            new KeyboardEventArgs { Key = ",", CtrlKey = true });

        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));
        dialogService.LastDialogType.ShouldBe(typeof(FcSettingsDialog));
        dialogService.LastOptions.ShouldNotBeNull();
        dialogService.LastOptions!.Modal.ShouldBe(true);
        dialogService.LastOptions.Width.ShouldBe("480px");
        string.IsNullOrWhiteSpace(dialogService.LastOptions.Header.Title).ShouldBeFalse();
    }

    // --- Code-review round 2 (2026-04-19) AC5 regression — Navigation pane hidden at Tablet/Phone ---

    [Theory]
    [InlineData(ViewportTier.Tablet)]
    [InlineData(ViewportTier.Phone)]
    public void NavigationPaneHiddenAtSubCompactDesktopViewports(ViewportTier tier) {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "Counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        Services.GetRequiredService<Fluxor.IDispatcher>()
            .Dispatch(new ViewportTierChangedAction(tier));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain(
                "data-testid=\"fc-shell-navigation\"",
                Case.Sensitive,
                $"Navigation pane must be hidden at {tier} — navigation appears only through the hamburger drawer (AC5 / dev-notes §39).");
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(
                () => cut.FindComponent<FrontComposerNavigation>(),
                $"FrontComposerNavigation must not render at {tier}.");
        });
    }

    // --- Story 3-4 Task 10.10 / 10.10a (D5 / D18 / AC2 / AC8) — palette trigger + Ctrl+K wiring ---

    [Fact]
    public void PaletteTriggerAutoPopulatesAheadOfSettings() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            _ = cut.FindComponent<FcPaletteTriggerButton>();
            _ = cut.FindComponent<FcSettingsButton>();

            int paletteIndex = cut.Markup.IndexOf("fc-palette-trigger", StringComparison.Ordinal);
            int settingsIndex = cut.Markup.IndexOf("fc-settings-button", StringComparison.Ordinal);

            paletteIndex.ShouldBeGreaterThanOrEqualTo(0);
            settingsIndex.ShouldBeGreaterThan(paletteIndex);
        });
    }

    // P11 (2026-04-21 pass-3 — Story 3-4 D18 / scope line 35) — adopter-supplied non-null HeaderEnd
    // must suppress BOTH the auto-populated FcPaletteTriggerButton AND FcSettingsButton, not just one.
    [Fact]
    public void AdopterSuppliedHeaderEndSuppressesPaletteTrigger() {
        RenderFragment custom = builder => builder.AddMarkupContent(0, "<span data-testid=\"adopter-header-end\">Custom</span>");

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .Add(c => c.HeaderEnd, custom)
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldContain("adopter-header-end");
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(
                () => cut.FindComponent<FcPaletteTriggerButton>(),
                "Adopter-supplied HeaderEnd fragment must suppress the framework-owned FcPaletteTriggerButton (D18).");
        });
    }

    [Fact]
    public async Task CtrlKOpensPaletteDialogViaShortcutService() {
        RecordingDialogService dialogService = new();
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        await cut.Find(".fc-shell-root").TriggerEventAsync(
            "onkeydown",
            new KeyboardEventArgs { Key = "K", CtrlKey = true });

        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));
        dialogService.LastDialogType.ShouldBe(typeof(FcCommandPalette));
        dialogService.LastOptions!.Modal.ShouldBe(true);
        dialogService.LastOptions.Width.ShouldBe("600px");
    }

    [Fact]
    public async Task BareChordPrefixAlone_DoesNotOpenDialogOrNavigate() {
        // DN3 resolution (2026-04-21): the D5 text-input guard was moved from the C# shell to
        // fc-keyboard.js:registerShellKeyFilter (which bUnit does not exercise). At the C# layer
        // a bare chord-prefix only primes the FSM — no handler fires until the chord completes.
        // This asserts the "no side effects from a lone prefix key" contract.
        RecordingDialogService dialogService = new();
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService));

        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        string initialUri = navigation.Uri;

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        await cut.Find(".fc-shell-root").TriggerEventAsync(
            "onkeydown",
            new KeyboardEventArgs { Key = "g" });

        dialogService.ShowDialogCallCount.ShouldBe(0);
        navigation.Uri.ShouldBe(initialUri);
    }

    [Fact]
    public async Task GHChordNavigatesHomeViaShortcutService() {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/counter/counter-view");

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        await cut.Find(".fc-shell-root").TriggerEventAsync(
            "onkeydown",
            new KeyboardEventArgs { Key = "g" });
        await cut.Find(".fc-shell-root").TriggerEventAsync(
            "onkeydown",
            new KeyboardEventArgs { Key = "h" });

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/"));
    }

    [Fact]
    public void RouteChangesUpdateCurrentBoundedContextInNavigationState() {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        IState<FrontComposerNavigationState> state = Services.GetRequiredService<IState<FrontComposerNavigationState>>();

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p.AddChildContent("<p>Body</p>"));

        navigation.NavigateTo("/counter/counter-view");
        cut.WaitForAssertion(() => state.Value.CurrentBoundedContext.ShouldBe("counter"));

        navigation.NavigateTo("/domain/commerce/submit-order-command");
        cut.WaitForAssertion(() => state.Value.CurrentBoundedContext.ShouldBe("commerce"));

        navigation.NavigateTo("/");
        cut.WaitForAssertion(() => state.Value.CurrentBoundedContext.ShouldBeNull());
    }

    [Fact]
    public void LateHydratedSessionRoute_RestoresFromHomeAfterHydrationCompletes() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch(new LastActiveRouteHydratedAction("domain/counter/counter-view"));
        dispatcher.Dispatch(new NavigationHydratedCompletedAction());

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/domain/counter/counter-view"));
    }

    [Fact]
    public void DeepLinkRoute_IsNotOverriddenByLateSessionRestore() {
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([
            new DomainManifest("Counter", "counter", ["Counter.Domain.Projections.CounterView"], Commands: []),
        ]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/settings");

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch(new LastActiveRouteHydratedAction("domain/counter/counter-view"));
        dispatcher.Dispatch(new NavigationHydratedCompletedAction());

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/settings"));
    }

    [Fact]
    public void ExternalSessionRoute_IsRejectedDuringRestore() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        IDispatcher dispatcher = Services.GetRequiredService<IDispatcher>();

        dispatcher.Dispatch(new LastActiveRouteHydratedAction("https://evil.example/pwn"));
        dispatcher.Dispatch(new NavigationHydratedCompletedAction());

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/"));
    }
}
