using System.Globalization;

using Bunit;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Home;
using Hexalith.FrontComposer.Shell.Components.Pages;
using Hexalith.FrontComposer.Shell.Tests.Components.Layout;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Pages;

/// <summary>
/// Story 2.2 QA gap-pin (AC2) — <see cref="FcHomeRouteView"/> is the pure routing shim that mounts
/// the home directory at the framework "/" and "/home" routes (Story 3-5 D16). Task 2 says to
/// "re-confirm" this mapping, but no test covered it. This file pins both the declared <c>@page</c>
/// routes (so the deep-link contract can't silently regress) and the <see cref="FcHomeDirectory"/>
/// mount (so the shim can't be hollowed out).
/// </summary>
public sealed class FcHomeRouteViewTests : LayoutComponentTestBase {
    public FcHomeRouteViewTests() {
        // Pin culture so the mounted directory resolves EN resource strings deterministically.
        CultureInfo.CurrentUICulture = new CultureInfo("en");
        CultureInfo.CurrentCulture = new CultureInfo("en");

        IFrontComposerRegistry registry = Substitute.For<IFrontComposerRegistry>();
        registry.GetManifests().Returns([]);
        Services.Replace(ServiceDescriptor.Singleton(registry));

        IUlidFactory ulidFactory = Substitute.For<IUlidFactory>();
        ulidFactory.NewUlid().Returns("01J0ROUTE000000000000000000");
        Services.Replace(ServiceDescriptor.Singleton(ulidFactory));

        EnsureStoreInitialized();
    }

    [Fact]
    public void DeclaresRootAndHomeRoutes() {
        // AC2 — @page "/" + @page "/home". Adopters override by registering their own @page "/" in a
        // later-scanned assembly (Blazor's route table takes the last matching exact route).
        IReadOnlyList<string> templates = [.. typeof(FcHomeRouteView)
            .GetCustomAttributes(typeof(RouteAttribute), inherit: false)
            .Cast<RouteAttribute>()
            .Select(static r => r.Template)];

        templates.ShouldContain("/");
        templates.ShouldContain("/home");
    }

    [Fact]
    public void MountsFcHomeDirectory() {
        // AC2 — the shim renders <FcHomeDirectory />. With no manifests registered the directory
        // resolves to its empty state; finding the typed component proves it is actually mounted.
        IRenderedComponent<FcHomeRouteView> cut = Render<FcHomeRouteView>();

        cut.WaitForAssertion(() => {
            _ = cut.FindComponent<FcHomeDirectory>();
            cut.Markup.ShouldContain("data-testid=\"fc-home-directory\"");
        });
    }
}
