using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Header-end settings trigger (Story 3-3 D11 / AC7). Click opens <see cref="FcSettingsDialog"/>
/// via <see cref="IDialogService.ShowDialogAsync{TContent}(DialogOptions)"/> — NO intermediate
/// Fluxor action. Fluent UI's <see cref="IDialogService"/> already owns dialog lifetime; a Fluxor
/// action for "is dialog open?" would double the source of truth (D11 rationale).
/// </summary>
public partial class FcSettingsButton : ComponentBase
{
    /// <summary>Injected Fluent UI dialog service.</summary>
    [Inject] private IDialogService DialogService { get; set; } = default!;

    /// <summary>
    /// Opens <see cref="FcSettingsDialog"/> as a 480 px modal. Mirrors the same call the
    /// <c>Ctrl+,</c> inline shortcut in <see cref="FrontComposerShell"/> makes so both entry points
    /// stay in lockstep.
    /// </summary>
    /// <returns>A task representing the async dialog presentation.</returns>
    private async Task OpenDialogAsync()
    {
        _ = await FcSettingsDialogLauncher
            .ShowAsync(DialogService, Localizer["SettingsDialogTitle"].Value);
    }
}
