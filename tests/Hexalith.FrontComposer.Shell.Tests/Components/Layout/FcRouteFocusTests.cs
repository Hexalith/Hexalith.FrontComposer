using Bunit;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Routing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class FcRouteFocusTests : LayoutComponentTestBase
{
    [Theory]
    [InlineData("Tab")]
    [InlineData("RouteTab")]
    public void RouteIdentityPreservesTabsAndSeparatesEntityParameters(string selector)
    {
        RouteData first = new(typeof(FcModuleLandingPage), new Dictionary<string, object?> { [selector] = "overview", ["TenantId"] = "first" });
        RouteData nextTab = new(typeof(FcModuleLandingPage), new Dictionary<string, object?> { [selector] = "projection", ["TenantId"] = "first" });
        RouteData nextTenant = new(typeof(FcModuleLandingPage), new Dictionary<string, object?> { [selector] = "projection", ["TenantId"] = "second" });
        RouteComponentIdentity.For(first).ShouldBe(RouteComponentIdentity.For(nextTab));
        RouteComponentIdentity.For(first).ShouldNotBe(RouteComponentIdentity.For(nextTenant));
    }

    [Theory]
    [InlineData(false, "Could not open Sales. You remain on Accounting.")]
    [InlineData(true, "Could not open Sales.")]
    public void FailureCopyUsesRegisteredLabelsAndOnlyClaimsAnUnchangedPage(bool alreadyMoved, string expected)
    {
        IFrontComposerRegistry registry = Services.GetRequiredService<IFrontComposerRegistry>();
        registry.RegisterDomain(new DomainManifest("Accounting", "accounting", [], []));
        registry.RegisterDomain(new DomainManifest("Sales", "sales", [], []));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/accounting");
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        failure.ConfirmRoute(navigation.Uri);
        failure.BeginAttempt("sensitive caller label", navigation.ToAbsoluteUri("/sales").AbsoluteUri);
        if (alreadyMoved) navigation.NavigateTo("/sales");
        failure.ReportFailure("sensitive heading");
        failure.Message.ShouldBe(expected);
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
        FocusModule.Invocations.Any(invocation => invocation.Identifier == "focusOverlayEntry").ShouldBeTrue();
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
        FocusModule.SetupVoid("focusRouteHeading", _ => true)
            .SetException(new JSException("focus unavailable"));

        _ = Render<FcRouteFocus>(parameters => parameters.Add(focus => focus.RouteKey, navigation.Uri));

        (await pending.ConfigureAwait(true)).ShouldBeFalse();
        navigation.Uri.ShouldBe(returnUri);
        failure.Message.ShouldNotBeNull();
    }

    [Fact]
    public async Task MountedOwnerConfirmsOnlyAfterRenderedHeading()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        IRenderedComponent<FcRouteFocus> cut = Render<FcRouteFocus>(parameters => parameters.Add(focus => focus.RouteKey, navigation.Uri));
        failure.BeginAttempt("Home", navigation.Uri);
        Task<bool> pending = failure.PrepareRouteConfirmation(navigation.Uri);
        failure.ObserveLocation(navigation.Uri);
        pending.IsCompleted.ShouldBeFalse();
        cut.Instance.ConfirmRouteHeading(navigation.Uri);
        (await pending.ConfigureAwait(true)).ShouldBeTrue();
    }

    [Fact]
    public void DirectUnavailableEntryHasOnlyFocusedHeading()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/missing/route");
        IRenderedComponent<FcRouteUnavailable> cut = Render<FcRouteUnavailable>();
        cut.Find("#fc-route-unavailable-heading").GetAttribute("tabindex").ShouldBe("-1");
        Services.GetRequiredService<NavigationFailureNotifier>().Message.ShouldBeNull();
        FocusModule.Invocations.Count(call => call.Identifier == "focusOverlayEntry").ShouldBe(1);
    }

    [Fact]
    public void GenericModuleInvalidTabRetainsStatusAcrossNewPageInstance()
    {
        IFrontComposerRegistry registry = Services.GetRequiredService<IFrontComposerRegistry>();
        registry.RegisterDomain(new DomainManifest("Accounting", "Accounting", [], []));
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        navigation.NavigateTo("/accounting/missing");
        IRenderedComponent<FcModuleLandingPage> first = Render<FcModuleLandingPage>(parameters => parameters.Add(page => page.Module, "Accounting"));
        first.WaitForAssertion(() => navigation.Uri.ShouldEndWith("/accounting/overview"));
        first.Dispose();
        IRenderedComponent<FcModuleLandingPage> next = Render<FcModuleLandingPage>(parameters => parameters.Add(page => page.Module, "Accounting"));
        next.Find("[data-testid='fc-module-tab-fallback']").TextContent.ShouldContain("Overview");
    }
}
