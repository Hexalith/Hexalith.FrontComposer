using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Light / Dark / System theme picker (Story 3-1 D5 / D7 / D22 / AC3). Dispatches
/// <see cref="ThemeChangedAction"/> — the single writer into <see cref="FrontComposerThemeState"/>
/// per D7; the Fluxor effect layer persists and applies the change.
/// </summary>
public partial class FcThemeToggle : Fluxor.Blazor.Web.Components.FluxorComponent
{
    /// <summary>Injected Fluxor state subscription (re-renders the button label/icon on change).</summary>
    [Inject] private IState<FrontComposerThemeState> ThemeState { get; set; } = default!;

    /// <summary>Injected Fluxor dispatcher.</summary>
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    /// <summary>Injected ULID factory for correlation IDs (reuses the Story 2-3 registration).</summary>
    [Inject] private IUlidFactory UlidFactory { get; set; } = default!;

    /// <summary>
    /// Dispatches <see cref="ThemeChangedAction"/>. The effect layer owns applying the theme and
    /// persisting the user choice so there is a single write path into both Fluent UI and storage.
    /// </summary>
    /// <param name="selected">The theme value selected by the user.</param>
    private Task SelectThemeAsync(ThemeValue selected)
    {
        Dispatcher.Dispatch(new ThemeChangedAction(UlidFactory.NewUlid(), selected));
        return Task.CompletedTask;
    }

    private string GetAccessibleLabel()
        => $"{Localizer["ThemeToggleAriaLabel"].Value}: {Localizer[GetCurrentLabelKey()].Value}";

    private string GetCurrentLabelKey() => ThemeState.Value.CurrentTheme switch
    {
        ThemeValue.Light => "ThemeLightLabel",
        ThemeValue.Dark => "ThemeDarkLabel",
        _ => "ThemeSystemLabel",
    };
}
