using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Hexalith.FrontComposer.Shell.Shortcuts;

/// <summary>
/// Registers the three v1 shell-default shortcuts on first render of <see cref="FrontComposerShell"/>
/// (Story 3-4 D1 / D12 / D24 / AC1 / AC8).
/// </summary>
/// <remarks>
/// <para>
/// <b>Shell defaults:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>ctrl+k</c> → opens the palette via <c>IDialogService.ShowDialogAsync&lt;FcCommandPalette&gt;</c> after dispatching <see cref="PaletteOpenedAction"/>; idempotent (D12 — no-op when palette already open).</description></item>
///   <item><description><c>ctrl+,</c> → opens settings via <see cref="FcSettingsDialogLauncher"/> (MIGRATES Story 3-3 D16 inline binding per AC8).</description></item>
///   <item><description><c>g h</c> → navigates to <c>/</c> via <see cref="NavigationManager"/>.</description></item>
/// </list>
/// <para>
/// <b>D24 idempotency:</b> the <see cref="_registered"/> flag guards against repeated invocation
/// across bUnit / hot-reload / prerender boot paths so HFC2108 conflict logs are not spammed.
/// </para>
/// </remarks>
public sealed class FrontComposerShortcutRegistrar(
    IShortcutService shortcuts,
    IDispatcher dispatcher,
    IState<FrontComposerCommandPaletteState> paletteState,
    IDialogService dialogService,
    NavigationManager navigation,
    IStringLocalizer<FcShellResources> localizer,
    IUlidFactory ulidFactory)
{
    // Uses `int` + `Interlocked.Exchange` so two concurrent first-render paths (hot-reload
    // restart, bUnit teardown race) cannot both observe zero and double-register the defaults
    // (which would then spam HFC2108 conflict logs).
    private int _registered;

    /// <summary>
    /// Idempotently registers the three v1 shell shortcuts. Subsequent calls on the same instance
    /// no-op (D24 guard).
    /// </summary>
    /// <returns>A completed task — registration is synchronous; the async signature exists so the shell can <c>await</c> alongside other bootstrap work.</returns>
    public Task RegisterShellDefaultsAsync()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return Task.CompletedTask;
        }

        _ = shortcuts.Register(
            "ctrl+k",
            "PaletteShortcutDescription",
            OpenPaletteAsync);

        // Mac `Cmd+K` (event.metaKey) is a separate binding from `ctrl+k` per normalisation, so
        // the palette must register both to be reachable on macOS. D3 last-writer-wins keeps the
        // adopter override path intact — an adopter that re-registers `meta+k` after us wins.
        _ = shortcuts.Register(
            "meta+k",
            "PaletteShortcutDescription",
            OpenPaletteAsync);

        _ = shortcuts.Register(
            "ctrl+,",
            "SettingsShortcutDescription",
            OpenSettingsAsync);

        _ = shortcuts.Register(
            "meta+,",
            "SettingsShortcutDescription",
            OpenSettingsAsync);

        _ = shortcuts.Register(
            "g h",
            "HomeShortcutDescription",
            NavigateHomeAsync);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Opens the palette dialog. D12 idempotent guard — uses <see cref="Interlocked.Exchange"/> on
    /// <see cref="_palettePending"/> so two concurrent <c>Ctrl+K</c> presses can never both observe
    /// the "not open" state and stack dialog instances. The flag is cleared by the
    /// <see cref="PaletteClosedAction"/> observer on the component side (and by the catch block
    /// below for failure paths).
    /// </summary>
    /// <returns>A task representing the dialog presentation.</returns>
    public async Task OpenPaletteAsync()
    {
        if (paletteState.Value.IsOpen)
        {
            return;
        }

        // Serialize read-and-open so two near-simultaneous invocations do not both proceed to
        // ShowDialogAsync.
        if (Interlocked.Exchange(ref _palettePending, 1) == 1)
        {
            return;
        }

        dispatcher.Dispatch(new PaletteOpenedAction(ulidFactory.NewUlid()));

        try
        {
            _ = await dialogService.ShowDialogAsync<FcCommandPalette>(options =>
            {
                options.Modal = true;
                options.Width = "600px";
                options.Header.Title = localizer["CommandPaletteTitle"].Value;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Narrow the rollback path: OperationCanceledException from a user-initiated close
            // flows through DisposeAsync which already dispatches PaletteClosedAction. For genuine
            // failures (JSDisconnectedException, missing registration), guard the compensating
            // dispatch in case the dispatcher is already disposed during circuit teardown.
            try
            {
                dispatcher.Dispatch(new PaletteClosedAction(ulidFactory.NewUlid()));
            }
            catch (ObjectDisposedException)
            {
                // Dispatcher tore down alongside the circuit — ignore and let the original
                // exception propagate for Blazor's error boundary.
            }

            throw;
        }
        finally
        {
            _palettePending = 0;
        }
    }

    private int _palettePending;

    /// <summary>
    /// Opens the settings dialog via the shared launcher (Story 3-3 D11 / Story 3-4 AC8).
    /// </summary>
    /// <returns>A task representing the dialog presentation.</returns>
    public async Task OpenSettingsAsync()
    {
        _ = await FcSettingsDialogLauncher
            .ShowAsync(dialogService, localizer["SettingsDialogTitle"].Value)
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Navigates to the application home via <see cref="NavigationManager.NavigateTo(string)"/>.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task NavigateHomeAsync()
    {
        navigation.NavigateTo("/");
        return Task.CompletedTask;
    }
}
