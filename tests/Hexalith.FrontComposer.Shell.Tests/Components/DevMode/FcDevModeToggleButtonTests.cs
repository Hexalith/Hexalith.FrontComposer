using Bunit;

using Hexalith.FrontComposer.Shell.Components.DevMode;
using Hexalith.FrontComposer.Shell.Services.DevMode;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.DevMode;

public sealed class FcDevModeToggleButtonTests : BunitContext {
    public FcDevModeToggleButtonTests() {
        _ = Services.AddLocalization();
        _ = Services.AddFluentUIComponents();
        _ = Services.AddScoped<IDevModeOverlayController, DevModeOverlayController>();
    }

    [Fact]
    public void ToggleButtonRendersDevModeIconInsteadOfLiteralPlaceholder() {
        IRenderedComponent<FcDevModeToggleButton> cut = Render<FcDevModeToggleButton>();
        IRenderedComponent<FluentIcon<Icon>> icon = cut.FindComponent<FluentIcon<Icon>>();

        cut.Markup.ShouldContain("fc-devmode-toggle");
        icon.Instance.Value.ShouldNotBeNull();
        icon.Instance.Value!.Name.ShouldBe("DeveloperBoard");
        cut.Markup.ShouldNotContain(">i</button>");
    }
}
