using Bunit;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Badges;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Components.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcScopeBlockedTests : BunitContext {
    [Fact]
    public void HeadingFocusTimeoutCanRetryOnALaterRenderWithoutALiveAnnouncement()
    {
        Services.AddLocalization();
        Services.AddFluentUIComponents();
        var focus = JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true)
            .SetException(new TaskCanceledException("interop timeout"));
        IRenderedComponent<FcScopeBlocked> cut = Render<FcScopeBlocked>();
        focus.SetVoidResult();
        cut.Render();
        cut.Render();
        JSInterop.Invocations.Count(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus").ShouldBe(2);
        cut.Markup.ShouldNotContain("aria-live");
    }

    [Fact]
    public void MissingScope_RendersSupportSafeBlockingMeaning() {
        _ = Services.AddLocalization();
        _ = Services.AddFluentUIComponents();

        IRenderedComponent<FcScopeBlocked> cut = Render<FcScopeBlocked>();

        cut.Find("[data-testid='fc-scope-blocked'] h1").TextContent.ShouldBe("Workspace unavailable");
        cut.Markup.ShouldContain("Contact support");
        cut.Markup.ShouldNotContain("tenant-a");
        cut.Markup.ShouldNotContain("user-a");
        cut.Markup.ShouldNotContain("aria-live");
        cut.Markup.ShouldNotContain("autofocus");
    }

    [Fact]
    public void CoMountedHeadingsFocusOnceUntilTheScopeGenerationChanges()
    {
        Services.AddLocalization();
        Services.AddFluentUIComponents();
        Services.AddSingleton(new ScopeBoundaryService(
            Substitute.For<AuthenticationStateProvider>(),
            Substitute.For<IFrontComposerTenantContextAccessor>(),
            Substitute.For<IDispatcher>(),
            Substitute.For<IScopeReadinessGate>(),
            Substitute.For<IPendingCommandStateService>(),
            Substitute.For<INewItemIndicatorStateService>(),
            Substitute.For<IBadgeCountService>(),
            Substitute.For<ICommandExecutionAdmissionGate>(),
            Substitute.For<IServiceProvider>()));
        _ = JSInterop.SetupVoid("Blazor._internal.domWrapper.focus", _ => true);
        IRenderedComponent<FcScopeBlocked> first = Render<FcScopeBlocked>();
        IRenderedComponent<FcScopeBlocked> second = Render<FcScopeBlocked>();
        int FocusCount() => JSInterop.Invocations.Count(invocation => invocation.Identifier == "Blazor._internal.domWrapper.focus");
        FocusCount().ShouldBe(1);

        Services.GetRequiredService<ScopeBoundaryService>().AdvanceGeneration();
        first.WaitForAssertion(() => FocusCount().ShouldBe(2));
        second.WaitForAssertion(() => FocusCount().ShouldBe(2));
    }
}
