using System.Reflection;

using Bunit;
using Bunit.TestDoubles;

using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.UI.Components;
using Hexalith.FrontComposer.UI.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;
using Xunit;

#pragma warning disable CS0618 // Production Routes uses Router.NotFound until host routing is upgraded.

namespace Hexalith.FrontComposer.UI.Tests;

/// <summary>Exercises the actual host route assemblies through a mounted Blazor router.</summary>
public sealed class ProductionRouterTests : BunitContext
{
    private readonly BunitJSModuleInterop _focusModule;
    private readonly BunitAuthorizationContext _authorization;

    public ProductionRouterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddLocalization();
        Services.AddFluentUIComponents();
        Services.AddScoped<NavigationFailureNotifier>();
        Services.AddSingleton<IFrontComposerRegistry>(new CommandsOnlyRegistry());
        _authorization = AddAuthorization();
        ComponentFactories.AddStub<FrontComposerShell>(parameters => builder =>
            builder.AddContent(0, parameters.Get(shell => shell.ChildContent)));
        ComponentFactories.AddStub(
            type => type.FullName == "Hexalith.Parties.UI.Components.Pages.PartiesOverview",
            "<h1>Parties</h1>");
        _focusModule = JSInterop.SetupModule("./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js");
        _focusModule.SetupVoid("focusRouteHeading", _ => true).SetVoidResult();
        _focusModule.SetupVoid("focusOverlayEntry", _ => true).SetVoidResult();
        _focusModule.SetupVoid("labelTabPanels", _ => true).SetVoidResult();
    }

    [Theory]
    [InlineData("/parties")]
    [InlineData("/parties/overview")]
    public void PartiesOverview_ResolvesInProductionRouteTable(string path)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);

        IRenderedComponent<Router> cut = RenderProductionRouter();

        cut.Find("[data-testid='matched-route']").TextContent
            .ShouldBe("Hexalith.FrontComposer.UI.Components.Pages.PartiesOverviewRoute");
    }

    [Fact]
    public void RootRoute_RemainsTenantsWorkspace()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/");

        IRenderedComponent<Router> cut = RenderProductionRouter();

        cut.Find("[data-testid='matched-route']").TextContent
            .ShouldBe("Hexalith.Tenants.UI.Components.Pages.TenantsWorkspace");
    }

    [Fact]
    public void HomeShortcutDestinationResolvesToDirectory()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/home");

        IRenderedComponent<Router> cut = RenderProductionRouter();

        cut.Find("[data-testid='matched-route']").TextContent
            .ShouldBe("Hexalith.FrontComposer.UI.Components.Pages.Home");
    }

    [Theory]
    [InlineData("/admin", "Hexalith.FrontComposer.UI.Components.Pages.AdminLanding")]
    [InlineData("/no-party-binding", "Hexalith.FrontComposer.UI.Components.Pages.NoPartyBinding")]
    public void ExistingHostCompatibilityRoutes_RemainMapped(string path, string pageType)
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo(path);

        IRenderedComponent<Router> cut = RenderProductionRouter();

        cut.Find("[data-testid='matched-route']").TextContent.ShouldBe(pageType);
    }

    [Fact]
    public void UnmatchedRoute_ReachesFallback()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/no-such-workspace/page/extra");

        IRenderedComponent<Router> cut = RenderProductionRouter();

        cut.Find("[data-testid='unmatched-route']").TextContent.ShouldBe("unavailable");
    }

    [Fact]
    public void ProductionRoutes_MountsRouteFocusForPartiesOverview()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/parties");

        IRenderedComponent<Routes> cut = Render<Routes>();

        cut.Find("h1").TextContent.ShouldBe("Parties");
        cut.FindComponent<FcRouteFocus>().ShouldNotBeNull();
        cut.WaitForAssertion(() => _focusModule.Invocations.Any(invocation => invocation.Identifier == "focusRouteHeading").ShouldBeTrue());
    }

    [Fact]
    public void ProtectedRouteRendersDeniedMarkerForRouteFocusOwner()
    {
        _authorization.SetAuthorized("operator");
        Services.GetRequiredService<NavigationManager>().NavigateTo("/admin/parties");

        IRenderedComponent<Routes> cut = Render<Routes>();

        cut.WaitForAssertion(() => cut.Find("[data-fc-route-denied='true'] h1")
            .TextContent.ShouldBe("Forbidden"));
    }

    [Fact]
    public void CommandsOnlyManifestTileResolvesToRegisteredOverview()
    {
        Services.AddSingleton<IFrontComposerRegistry>(new CommandsOnlyRegistry());
        Services.GetRequiredService<NavigationManager>().NavigateTo("/accounting");

        IRenderedComponent<Routes> cut = Render<Routes>();

        cut.Find("h1").TextContent.ShouldBe("Accounting");
        cut.Find("[data-fc-module-route='/accounting']").ShouldNotBeNull();
        cut.FindComponent<FluentTabs>().Instance.ActiveTabId.ShouldBe("overview");
        cut.FindComponent<FcRouteFocus>().ShouldNotBeNull();
    }

    [Fact]
    public void QueryOnlyNavigationPreservesMountedModulePage()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/accounting?filter=first");
        IRenderedComponent<Routes> cut = Render<Routes>();
        ModuleLanding mounted = cut.FindComponent<ModuleLanding>().Instance;

        navigation.NavigateTo("/accounting?filter=second");

        cut.WaitForAssertion(() => cut.FindComponent<ModuleLanding>().Instance.ShouldBeSameAs(mounted));
        cut.Find("[data-fc-route-content]").GetAttribute("data-fc-route-content")
            .ShouldBe(navigation.Uri);
    }

    [Fact]
    public void ProductionRoutes_MountsSafeUnavailableHeadingForUnmatchedRoute()
    {
        Services.GetRequiredService<NavigationManager>().NavigateTo("/no-such-workspace/page/extra");

        IRenderedComponent<Routes> cut = Render<Routes>();

        cut.Find("#fc-route-unavailable-heading").TextContent.ShouldBe("Page unavailable");
        cut.FindComponent<FcRouteUnavailable>().ShouldNotBeNull();
        cut.WaitForAssertion(() => _focusModule.Invocations.Any(invocation => invocation.Identifier == "focusOverlayEntry").ShouldBeTrue());
    }

    [Fact]
    public void ProductionRoutes_UnmatchedActivation_ReturnsToConfirmedHeadingWithOneFailure()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/parties");
        IRenderedComponent<Routes> cut = Render<Routes>();
        Stub<FrontComposerShell> shell = cut.FindComponent<Stub<FrontComposerShell>>().Instance;
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        cut.FindComponent<FcRouteFocus>().Instance.ConfirmRouteHeading(navigation.Uri);
        string confirmedUri = navigation.Uri;
        int initialFocusCalls = _focusModule.Invocations.Count(invocation => invocation.Identifier == "focusRouteHeading");
        int failureMessages = 0;
        failure.Changed += () =>
        {
            if (failure.Message is not null)
            {
                failureMessages++;
            }
        };

        failure.BeginAttempt("Missing", navigation.ToAbsoluteUri("/no-such-workspace/page/extra").ToString());
        navigation.NavigateTo("/no-such-workspace/page/extra");

        cut.WaitForAssertion(() => navigation.Uri.ShouldBe(confirmedUri));
        cut.Find("h1").TextContent.ShouldBe("Parties");
        cut.FindComponent<Stub<FrontComposerShell>>().Instance.ShouldBeSameAs(shell);
        cut.FindComponents<FcRouteUnavailable>().ShouldBeEmpty();
        cut.WaitForAssertion(() => _focusModule.Invocations.Count(invocation => invocation.Identifier == "focusRouteHeading")
            .ShouldBeGreaterThan(initialFocusCalls));
        _focusModule.Invocations.Last(invocation => invocation.Identifier == "focusRouteHeading")
            .Arguments[2].ShouldBe(true);
        failure.Message.ShouldBe("Could not open that page. Choose another page or return Home.");
        failureMessages.ShouldBe(1);
        _focusModule.Invocations.Any(invocation => invocation.Identifier == "focusOverlayEntry").ShouldBeFalse();

        cut.FindComponent<FcRouteFocus>().Instance.ConfirmRouteHeading(navigation.Uri);
        failure.Message.ShouldBe("Could not open that page. Choose another page or return Home.");
        failureMessages.ShouldBe(1);
    }

    [Fact]
    public void ProductionRoutes_UnrelatedUnmatchedNavigationKeepsUnavailablePage()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/parties");
        IRenderedComponent<Routes> cut = Render<Routes>();
        Services.GetRequiredService<NavigationFailureNotifier>().ConfirmRoute(navigation.Uri);

        navigation.NavigateTo("/unrelated/broken/page");

        cut.WaitForAssertion(() => cut.Find("#fc-route-unavailable-heading").TextContent
            .ShouldBe("Page unavailable"));
        navigation.Uri.ShouldEndWith("/unrelated/broken/page");
    }

    [Fact]
    public void ConsecutiveUnmatchedRoutesEachReceiveOneFailureStatus()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        int failures = 0;
        failure.Changed += () =>
        {
            if (failure.Message is not null)
            {
                failures++;
            }
        };
        navigation.NavigateTo("/unmapped/first/page");
        IRenderedComponent<Routes> cut = Render<Routes>();
        cut.WaitForAssertion(() => failures.ShouldBe(1));

        navigation.NavigateTo("/unmapped/second/page");

        cut.WaitForAssertion(() => failures.ShouldBe(2));
        cut.Find("#fc-route-unavailable-heading").TextContent.ShouldBe("Page unavailable");
    }

    [Fact]
    public void RetriedBrokenRouteReportsFailureForEachAttempt()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/broken");
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        int failures = 0;
        failure.Changed += () =>
        {
            if (failure.Message is not null)
            {
                failures++;
            }
        };
        IRenderedComponent<FcRouteFocus> cut = Render<FcRouteFocus>(parameters =>
            parameters.Add(focus => focus.RouteKey, navigation.Uri));

        failure.BeginAttempt("Unavailable", navigation.Uri);
        cut.Instance.ReportRouteHeadingMissing(navigation.Uri);
        cut.Instance.ReportRouteHeadingMissing(navigation.Uri);
        failures.ShouldBe(1);

        failure.BeginAttempt("Unavailable", navigation.Uri);
        cut.Instance.ReportRouteHeadingMissing(navigation.Uri);
        failures.ShouldBe(2);
    }

    [Fact]
    public async Task MatchedRouteWithoutHeadingRecoversOwnedActivationAndSettlesPaletteWait()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/parties");
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        failure.ConfirmRoute(navigation.Uri);
        string returnUri = navigation.Uri;
        navigation.NavigateTo("/accounting");
        failure.BeginAttempt("Accounting", navigation.Uri);
        Task<bool> pending = failure.PrepareRouteConfirmation(navigation.Uri);
        IRenderedComponent<FcRouteFocus> cut = Render<FcRouteFocus>(parameters =>
            parameters.Add(focus => focus.RouteKey, navigation.Uri));

        cut.Instance.ReportRouteHeadingMissing(navigation.Uri);

        (await pending.ConfigureAwait(true)).ShouldBeFalse();
        navigation.Uri.ShouldBe(returnUri);
        failure.Message.ShouldNotBeNull();
    }

    [Fact]
    public void DirectMatchedRouteWithoutHeadingShowsFocusableSafeHeading()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/accounting");
        IRenderedComponent<FcRouteFocus> cut = Render<FcRouteFocus>(parameters =>
            parameters.Add(focus => focus.RouteKey, navigation.Uri));

        cut.Instance.ReportRouteHeadingMissing(navigation.Uri);

        cut.WaitForAssertion(() => cut.Find("#fc-route-unavailable-heading").TextContent.ShouldBe("Page unavailable"));
        _focusModule.Invocations.Any(invocation => invocation.Identifier == "focusOverlayEntry").ShouldBeTrue();
    }

    [Fact]
    public async Task DeniedHeadingFailsOwnedActivationWithoutRecordingSuccess()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        navigation.NavigateTo("/parties");
        failure.ConfirmRoute(navigation.Uri);
        string returnUri = navigation.Uri;
        navigation.NavigateTo("/admin/parties");
        failure.BeginAttempt("Parties administration", navigation.Uri);
        Task<bool> pending = failure.PrepareRouteConfirmation(navigation.Uri);
        IRenderedComponent<FcRouteFocus> cut = Render<FcRouteFocus>(parameters =>
            parameters.Add(focus => focus.RouteKey, navigation.Uri));

        cut.Instance.ReportRouteHeadingDenied(navigation.Uri);

        (await pending.ConfigureAwait(true)).ShouldBeFalse();
        navigation.Uri.ShouldBe(returnUri);
        failure.LastConfirmedUri.ShouldBe(returnUri);
    }

    [Fact]
    public async Task FocusInteropFailureSettlesOwnedActivation()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        navigation.NavigateTo("/parties");
        failure.ConfirmRoute(navigation.Uri);
        string returnUri = navigation.Uri;
        navigation.NavigateTo("/accounting");
        failure.BeginAttempt("Accounting", navigation.Uri);
        Task<bool> pending = failure.PrepareRouteConfirmation(navigation.Uri);
        _focusModule.SetupVoid("focusRouteHeading", _ => true)
            .SetException(new JSException("focus unavailable"));

        _ = Render<FcRouteFocus>(parameters => parameters.Add(focus => focus.RouteKey, navigation.Uri));

        (await pending.ConfigureAwait(true)).ShouldBeFalse();
        navigation.Uri.ShouldBe(returnUri);
        failure.Message.ShouldNotBeNull();
    }

    [Fact]
    public void GenericModuleInvalidChildReturnsToOverviewWithTabStatus()
    {
        Services.AddSingleton<IFrontComposerRegistry>(new CommandsOnlyRegistry());
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/accounting/missing");

        IRenderedComponent<Routes> cut = Render<Routes>();

        cut.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/accounting/overview"));
        cut.Find("[data-fc-module-route='/accounting']").ShouldNotBeNull();
    }

    private IRenderedComponent<Router> RenderProductionRouter()
    {
        FieldInfo assembliesField = typeof(Routes).GetField("RouteAssemblies", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new InvalidOperationException("Production route assemblies were not found.");
        Assembly[] routeAssemblies = (Assembly[])assembliesField.GetValue(null)!;

        return Render<Router>(parameters => parameters
            .Add(router => router.AppAssembly, typeof(App).Assembly)
            .Add(router => router.AdditionalAssemblies, routeAssemblies)
            .Add(router => router.Found, (RouteData routeData) => builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-testid", "matched-route");
                builder.AddContent(2, routeData.PageType.FullName);
                builder.CloseElement();
            })
            .Add(router => router.NotFound, builder =>
            {
                builder.OpenElement(0, "span");
                builder.AddAttribute(1, "data-testid", "unmatched-route");
                builder.AddContent(2, "unavailable");
                builder.CloseElement();
            }));
    }
}
