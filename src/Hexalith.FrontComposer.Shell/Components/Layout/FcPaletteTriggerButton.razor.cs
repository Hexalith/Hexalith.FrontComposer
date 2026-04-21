using Hexalith.FrontComposer.Shell.Shortcuts;

using Microsoft.AspNetCore.Components;

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Header palette-trigger icon (Story 3-4 D18 / AC2). Click delegates to
/// <see cref="FrontComposerShortcutRegistrar.OpenPaletteAsync"/> so the shortcut + click paths
/// share one entry point (single arbitration source).
/// </summary>
public partial class FcPaletteTriggerButton : ComponentBase {
    /// <summary>Injected shell registrar — exposes the OpenPaletteAsync entry point.</summary>
    [Inject] private FrontComposerShortcutRegistrar Registrar { get; set; } = default!;

    private Task OpenAsync() => Registrar.OpenPaletteAsync();
}
