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
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImportCompletedAfterDisposalReleasesModuleWithoutFocus(bool disconnected)
    {
        HeldRouteFocusRuntime runtime = new();
        Services.AddSingleton<IJSRuntime>(runtime);
        RecordingRouteFocusModule module = new() { DisposalFailure = disconnected ? new JSDisconnectedException("circuit ended") : null };
        IRenderedComponent<FcRouteFocus> cut = Render<FcRouteFocus>();

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        await cut.InvokeAsync(() => runtime.Import.SetResult(module));

        await module.Disposed.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        module.DisposalCount.ShouldBe(1);
        module.FocusCalls.ShouldBe(0);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateInteropFailureAndCallbacksCannotRecoverDisposedOwner(bool failImport)
    {
        HeldRouteFocusRuntime runtime = new();
        Services.AddSingleton<IJSRuntime>(runtime);
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        navigation.NavigateTo("/parties");
        failure.ConfirmRoute(navigation.Uri);
        string confirmed = navigation.Uri;
        navigation.NavigateTo("/accounting");
        failure.BeginAttempt(navigation.Uri);
        RecordingRouteFocusModule module = new() { FocusCompletion = new() };
        IRenderedComponent<FcRouteFocus> cut = Render<FcRouteFocus>(parameters => parameters.Add(focus => focus.RouteKey, navigation.Uri));
        if (!failImport)
        {
            await cut.InvokeAsync(() => runtime.Import.SetResult(module));
            await module.FocusStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        }

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        // Complete on the renderer so the awaited interop continuation has settled before assertions.
        if (failImport) await cut.InvokeAsync(() => runtime.Import.SetException(new JSException("late import failure")));
        else await cut.InvokeAsync(() => module.FocusCompletion.SetException(new JSException("late focus failure")));
        cut.Instance.ConfirmRouteHeading(navigation.Uri);
        cut.Instance.ReportRouteHeadingMissing(navigation.Uri);
        cut.Instance.ReportRouteHeadingDenied(navigation.Uri);

        failure.LastConfirmedUri.ShouldBe(confirmed);
        failure.Message.ShouldBeNull();
        navigation.Uri.ShouldEndWith("/accounting");
        cut.Markup.ShouldNotContain("fc-route-unavailable-heading");
    }

    [Fact]
    public async Task MountedImportFocusesNormallyAndRepeatedDisposalReleasesOnce()
    {
        HeldRouteFocusRuntime runtime = new();
        Services.AddSingleton<IJSRuntime>(runtime);
        RecordingRouteFocusModule module = new();
        IRenderedComponent<FcRouteFocus> cut = Render<FcRouteFocus>();
        await cut.InvokeAsync(() => runtime.Import.SetResult(module));
        await module.FocusStarted.Task.WaitAsync(TimeSpan.FromSeconds(5), Xunit.TestContext.Current.CancellationToken);
        module.FocusCalls.ShouldBe(1);

        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());

        module.DisposalCount.ShouldBe(1);
    }

    [Fact]
    public async Task RepeatedDisposalPreservesAnotherMountedOwnersRouteConfirmation()
    {
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        navigation.NavigateTo("/accounting");
        IRenderedComponent<FcRouteFocus> first = Render<FcRouteFocus>(parameters => parameters.Add(focus => focus.RouteKey, navigation.Uri));
        IRenderedComponent<FcRouteFocus> surviving = Render<FcRouteFocus>(parameters => parameters.Add(focus => focus.RouteKey, navigation.Uri));

        await first.InvokeAsync(() => first.Instance.DisposeAsync().AsTask());
        await first.InvokeAsync(() => first.Instance.DisposeAsync().AsTask());
        failure.BeginAttempt(navigation.Uri);
        Task<bool> pending = failure.PrepareRouteConfirmation(navigation.Uri);
        failure.ObserveLocation(navigation.Uri);

        pending.IsCompleted.ShouldBeFalse();
        surviving.Instance.ConfirmRouteHeading(navigation.Uri);
        (await pending.ConfigureAwait(true)).ShouldBeTrue();
    }

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
        failure.BeginAttempt(navigation.ToAbsoluteUri("/sales").AbsoluteUri);
        if (alreadyMoved) navigation.NavigateTo("/sales");
        failure.ReportFailure();
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

        failure.BeginAttempt(navigation.Uri);
        cut.Instance.ReportRouteHeadingMissing(navigation.Uri);
        cut.Instance.ReportRouteHeadingMissing(navigation.Uri);
        failures.ShouldBe(1);

        failure.BeginAttempt(navigation.Uri);
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
        failure.BeginAttempt(navigation.Uri);
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
        failure.BeginAttempt(navigation.Uri);
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
        failure.BeginAttempt(navigation.Uri);
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
        failure.BeginAttempt(navigation.Uri);
        Task<bool> pending = failure.PrepareRouteConfirmation(navigation.Uri);
        failure.ObserveLocation(navigation.Uri);
        pending.IsCompleted.ShouldBeFalse();
        cut.Instance.ConfirmRouteHeading(navigation.Uri);
        (await pending.ConfigureAwait(true)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("direct", false, false, false)]
    [InlineData("owned", false, true, false)]
    [InlineData("failed-return", true, false, false)]
    [InlineData("alias", false, false, true)]
    public void RouteFocusPassesRecoveryOwnershipAndAliasFlagsToTheScript(string scenario, bool failedReturn, bool owned, bool alias)
    {
        IFrontComposerRegistry registry = Services.GetRequiredService<IFrontComposerRegistry>();
        registry.RegisterDomain(new DomainManifest("Sales", "sales", [], []) {
            CanonicalRouteAliases = new Dictionary<string, string> { ["/sales"] = "/sales/summary" },
        });
        NavigationManager navigation = Services.GetRequiredService<NavigationManager>();
        NavigationFailureNotifier failure = Services.GetRequiredService<NavigationFailureNotifier>();
        navigation.NavigateTo(scenario == "alias" ? "/sales" : "/sales/summary");
        if (scenario == "owned") failure.BeginAttempt(navigation.Uri);
        if (scenario == "failed-return") failure.ReportFailureAndPreserveReturn(navigation.Uri);

        _ = Render<FcRouteFocus>(parameters => parameters.Add(focus => focus.RouteKey, navigation.Uri));

        JSRuntimeInvocation call = FocusModule.Invocations.Single(invocation => invocation.Identifier == "focusRouteHeading");
        call.Arguments[0].ShouldBe(navigation.Uri);
        call.Arguments[2].ShouldBe(failedReturn);
        call.Arguments[3].ShouldBe(owned);
        call.Arguments[4].ShouldBe(alias);
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
        // The new page instance renders its live region empty first, then the retained AM-31 text.
        next.WaitForAssertion(() => next.Find("[data-testid='fc-module-tab-fallback']").TextContent.ShouldContain("Overview"));
    }
}
