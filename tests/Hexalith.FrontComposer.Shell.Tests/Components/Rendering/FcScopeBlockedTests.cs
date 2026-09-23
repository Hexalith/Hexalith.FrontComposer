using Bunit;

using Hexalith.FrontComposer.Shell.Components.Rendering;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Rendering;

public sealed class FcScopeBlockedTests : BunitContext {
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
