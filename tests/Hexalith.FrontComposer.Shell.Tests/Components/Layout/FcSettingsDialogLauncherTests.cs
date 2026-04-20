using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.FluentUI.AspNetCore.Components;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

public sealed class FcSettingsDialogLauncherTests
{
    [Fact]
    public void CreateOptions_UsesModal480pxSettingsContract()
    {
        DialogOptions options = FcSettingsDialogLauncher.CreateOptions("Settings");

        options.Modal.ShouldBe(true);
        options.Width.ShouldBe("480px");
        options.Header.Title.ShouldBe("Settings");
    }
}