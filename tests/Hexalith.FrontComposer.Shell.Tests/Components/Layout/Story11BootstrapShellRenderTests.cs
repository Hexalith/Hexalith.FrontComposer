using Bunit;

using Fluxor.Blazor.Web;

using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Shell.Components.Home;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>
/// Story 1.1 AC1 / AC3 — pins the empty-shell render behaviour the three-call bootstrap produces:
/// the framework frame (skip link, <see cref="FluentProviders"/>, <see cref="StoreInitializer"/>),
/// the global shortcuts registered on first render, and the home empty-state (no domain types) with
/// the Navigation area omitted so content spans edge-to-edge.
/// </summary>
public sealed class Story11BootstrapShellRenderTests : LayoutComponentTestBase {
    [Fact]
    public void Shell_RendersFrameworkFrame_SkipLinkProvidersAndStoreInitializer() {
        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            // AC1 — skip-to-content link targets the main content region.
            _ = cut.Find("a.fc-skip-link[href=\"#fc-main-content\"]");
            // AC1 — Fluent providers + the shell-owned Fluxor StoreInitializer.
            _ = cut.FindComponent<FluentProviders>();
            _ = cut.FindComponent<StoreInitializer>();
            cut.Markup.ShouldContain("id=\"fc-main-content\"", Case.Sensitive);
        });
    }

    [Fact]
    public void Shell_RegistersGlobalShortcuts_CtrlKAndCtrlComma_AfterFirstRender() {
        IShortcutService shortcuts = Services.GetRequiredService<IShortcutService>();

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        // Verify via the IShortcutService surface (deterministic) rather than simulating keypresses.
        cut.WaitForAssertion(() => {
            string[] bindings = [.. shortcuts.GetRegistrations().Select(r => r.Binding)];
            bindings.ShouldContain("ctrl+k", "Ctrl+K (palette) must be registered on first render (AC1).");
            bindings.ShouldContain("ctrl+,", "Ctrl+, (settings) must be registered on first render (AC1).");
        });
    }

    [Fact]
    public void Shell_EmptyRegistry_OmitsNavigationArea() {
        // AC3 — with no domain types registered, the Navigation FluentLayoutItem is omitted so
        // content spans edge-to-edge (HasNavigation is false on the empty shell).
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IRenderedComponent<FrontComposerShell> cut = Render<FrontComposerShell>(p => p
            .AddChildContent("<p>Body</p>"));

        cut.WaitForAssertion(() => {
            cut.Markup.ShouldNotContain("data-testid=\"fc-shell-navigation\"", Case.Sensitive);
            Should.Throw<Bunit.Rendering.ComponentNotFoundException>(
                () => cut.FindComponent<FrontComposerNavigation>(),
                "The empty shell must not render the Navigation rail.");
        });
    }

    [Fact]
    public void HomeDirectory_EmptyRegistry_RendersEmptyStateWithoutThrowing() {
        // AC3 — the home content area shows the empty state when no microservices are registered.
        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([]);
        Services.Replace(ServiceDescriptor.Singleton(registry));
        EnsureStoreInitialized();

        IRenderedComponent<FcHomeDirectory> cut = Render<FcHomeDirectory>();

        cut.WaitForAssertion(() =>
            _ = cut.Find("[data-testid=\"fc-home-empty-no-microservices\"]"));
    }
}
