using Hexalith.FrontComposer.Shell.Shortcuts;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

#pragma warning disable CA2007

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Header palette-trigger icon (Story 3-4 D18 / AC2). Click delegates to
/// <see cref="FrontComposerShortcutRegistrar.OpenPaletteAsync"/> so the shortcut + click paths
/// share one entry point (single arbitration source).
/// </summary>
public partial class FcPaletteTriggerButton : ComponentBase {
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Injected shell registrar — exposes the OpenPaletteAsync entry point.</summary>
    [Inject] private FrontComposerShortcutRegistrar Registrar { get; set; } = default!;

    private async Task OpenAsync() {
        try {
            await using IJSObjectReference module = await JS.InvokeAsync<IJSObjectReference>("import", "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js");
            await module.InvokeVoidAsync("captureOverlayOrigin", "fc-palette-trigger", true);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or InvalidOperationException) {
            // Fluent still opens the dialog when focus interop is unavailable.
        }

        await Registrar.OpenPaletteFromPointerAsync();
    }
}
