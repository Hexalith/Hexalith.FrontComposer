using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Shared launcher for the framework-owned settings dialog entry points.
/// The current Fluent UI package expresses the light-dismiss contract through <see cref="DialogOptions.Modal"/>,
/// so both the header button and the Ctrl+, shortcut delegate to the same options builder.
/// </summary>
internal static class FcSettingsDialogLauncher
{
    internal const string DialogWidth = "480px";

    internal static void ApplyOptions(DialogOptions options, string title)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        options.Modal = true;
        options.Width = DialogWidth;
        options.Header.Title = title;
    }

    internal static DialogOptions CreateOptions(string title)
    {
        DialogOptions options = new();
        ApplyOptions(options, title);
        return options;
    }

    internal static Task<DialogResult> ShowAsync(IDialogService dialogService, string title)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        return dialogService.ShowDialogAsync<FcSettingsDialog>(options => ApplyOptions(options, title));
    }
}