using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

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
}
