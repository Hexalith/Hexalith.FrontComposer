using Bunit;

using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Components.Layout;

/// <summary>Verifies palette-trigger activation when focus interop is unavailable.</summary>
public sealed class FcPaletteTriggerButtonTests : LayoutComponentTestBase
{
    /// <summary>Focus capture failures leave the palette dialog available.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Click_WhenFocusCaptureInteropFails_StillOpensPaletteDialog(bool canceled)
    {
        RecordingDialogService dialogService = new();
        Services.Replace(ServiceDescriptor.Scoped<IDialogService>(_ => dialogService));
        Exception failure = canceled ? new OperationCanceledException("capture canceled") : new JSException("capture failed");
        _ = FocusModule.Setup<bool>("captureOverlayOrigin", _ => true).SetException(failure);
        EnsureStoreInitialized();
        IRenderedComponent<FcPaletteTriggerButton> cut = Render<FcPaletteTriggerButton>();

        await cut.Find("[data-testid='fc-palette-trigger']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() => dialogService.ShowDialogCallCount.ShouldBe(1));
        dialogService.LastDialogType.ShouldBe(typeof(FcCommandPalette));
        _ = FocusModule.VerifyInvoke("captureOverlayOrigin", 1);
    }
}
