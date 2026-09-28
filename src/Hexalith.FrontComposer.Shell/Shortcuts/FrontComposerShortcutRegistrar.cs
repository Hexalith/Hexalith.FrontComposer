using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services;
#if DEBUG
using Hexalith.FrontComposer.Shell.Services.DevMode;
#endif
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Shortcuts;

/// <summary>
/// Registers the five v1 shell-default shortcuts (including Mac-parity bindings per D25 addendum)
/// on first render of <see cref="FrontComposerShell"/> (Story 3-4 D1 / D12 / D24 / D25 / D29 / AC1 / AC8).
/// </summary>
/// <remarks>
/// <para>
/// <b>Shell defaults:</b>
/// </para>
/// <list type="bullet">
///   <item><description><c>ctrl+k</c> and <c>meta+k</c> → opens the palette via <c>IDialogService.ShowDialogAsync&lt;FcCommandPalette&gt;</c> after dispatching <see cref="PaletteOpenedAction"/>; idempotent (D12 — no-op when palette already open). <c>meta+*</c> covers macOS (D25).</description></item>
///   <item><description><c>ctrl+,</c> and <c>meta+,</c> → opens settings via <see cref="FcSettingsDialogLauncher"/> (MIGRATES Story 3-3 D16 inline binding per AC8). <c>meta+*</c> covers macOS (D25).</description></item>
///   <item><description><c>g h</c> → navigates to <c>/home</c> via <see cref="NavigationManager"/>.</description></item>
/// </list>
/// <para>
/// <b>D24 idempotency:</b> the <see cref="_registered"/> flag guards against repeated invocation
/// across bUnit / hot-reload / prerender boot paths so HFC2108 conflict logs are not spammed.
/// </para>
/// <para>
/// <b>D29 token-discard pattern:</b> the five <see cref="IDisposable"/> handles returned by
/// <c>IShortcutService.Register(...)</c> are intentionally discarded (<c>_ = shortcuts.Register(...)</c>).
/// Cleanup invariant: (a) the D24 idempotency guard prevents the same registrar instance from
/// re-registering; (b) <c>ShortcutService.Dispose</c> clears <c>_entries</c> on circuit teardown,
/// cleaning up all registrations regardless of whether the registrar stored tokens. Hot-reload and
/// shared-service multi-registrar scenarios are tracked as <c>G26</c> for v1.x.
/// </para>
/// </remarks>
public sealed class FrontComposerShortcutRegistrar(
    IShortcutService shortcuts,
    IDispatcher dispatcher,
    IState<FrontComposerCommandPaletteState> paletteState,
    IDialogService dialogService,
    NavigationManager navigation,
    IStringLocalizer<FcShellResources> localizer,
    IUlidFactory ulidFactory,
    DataGridFocusScope dataGridFocusScope,
    IServiceProvider? services = null) {
    // Uses `int` + `Interlocked.Exchange` so two concurrent first-render paths (hot-reload
    // restart, bUnit teardown race) cannot both observe zero and double-register the defaults
    // (which would then spam HFC2108 conflict logs).
    private int _registered;

    /// <summary>
    /// Idempotently registers the three v1 shell shortcuts. Subsequent calls on the same instance
    /// no-op (D24 guard).
    /// </summary>
    /// <returns>A completed task — registration is synchronous; the async signature exists so the shell can <c>await</c> alongside other bootstrap work.</returns>
    public Task RegisterShellDefaultsAsync() {
        _ = services;
        if (Interlocked.Exchange(ref _registered, 1) == 1) {
            return Task.CompletedTask;
        }

        try {
            _ = shortcuts.Register(
                "ctrl+k",
                "PaletteShortcutDescription",
                OpenPaletteAsync);

            // Mac `Cmd+K` (event.metaKey) is a separate binding from `ctrl+k` per normalisation, so
            // the palette must register both to be reachable on macOS (D25). D3 last-writer-wins
            // keeps the adopter override path intact — an adopter that re-registers `meta+k` after
            // us wins.
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
                NavigateHomeAsync,
                routeUrl: "/home");

            // The route's sole enabled toolbar search owns `/`.
            _ = shortcuts.Register(
                "/",
                "SlashFocusPageSearchShortcutDescription",
                FocusSolePageSearchAsync);

#if DEBUG
            // Defense-in-depth (AC2): #if DEBUG compile-time gate AND IDevModeOverlayController
            // runtime check. Release builds compile this branch out entirely so the dev-mode
            // shortcut symbol is absent from production binaries.
            if (services?.GetService<IDevModeOverlayController>() is not null) {
                _ = shortcuts.Register(
                    "ctrl+shift+d",
                    "DevModeOverlayShortcutDescription",
                    ToggleDevModeOverlayAsync);
            }
#endif
        }
        catch {
            // If any Register call throws (localizer lookup failure, concurrent-dispose race, etc.)
            // the idempotency flag must roll back so a subsequent OnAfterRenderAsync pass can retry
            // registration. Without this rollback `_registered` stays at 1 permanently and the shell
            // is left with a partial binding set and no retry path.
            Interlocked.Exchange(ref _registered, 0);
            throw;
        }

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
    public Task OpenPaletteAsync() => OpenPaletteCoreAsync(captureKeyboardOrigin: true);

    internal Task OpenPaletteFromPointerAsync() => OpenPaletteCoreAsync(captureKeyboardOrigin: false);

    private async Task OpenPaletteCoreAsync(bool captureKeyboardOrigin) {
        if (paletteState.Value.IsOpen) {
            return;
        }

        // Serialize read-and-open so two near-simultaneous invocations do not both proceed to
        // ShowDialogAsync.
        if (Interlocked.Exchange(ref _palettePending, 1) == 1) {
            return;
        }

        try {
            if (captureKeyboardOrigin) {
                await CaptureKeyboardOriginAsync().ConfigureAwait(false);
            }
            // P5 (2026-04-21 pass-4): moved dispatch inside the try so a synchronous throw from
            // ulidFactory.NewUlid() or dispatcher.Dispatch() is caught by the rollback path below.
            // Previously only the await could throw into the catch; an earlier sync throw would
            // leak state (PaletteOpenedAction dispatched but no compensating close).
            dispatcher.Dispatch(new PaletteOpenedAction(ulidFactory.NewUlid()));

            _ = await dialogService.ShowDialogAsync<FcCommandPalette>(options => {
                options.Modal = true;
                options.Width = "600px";
                options.Header.Title = localizer["CommandPaletteTitle"].Value;
            }).ConfigureAwait(false);
        }
        catch (Exception ex) {
            // P4 (2026-04-21 pass-4): compensate for ALL exceptions including OperationCanceledException.
            // Earlier code relied on FluentDialog's DisposeAsync to dispatch PaletteClosedAction on
            // OCE, but OCE can be thrown from ShowDialogAsync before DisposeAsync wires up — leaving
            // IsOpen=true with no close. PaletteClosedAction reducers are idempotent (second dispatch
            // is a no-op) so double-dispatch on the dialog-close path is safe per D20.
            _ = ex;
            try {
                dispatcher.Dispatch(new PaletteClosedAction(ulidFactory.NewUlid()));
            }
            catch (ObjectDisposedException) {
                // Dispatcher tore down alongside the circuit — ignore and let the original
                // exception propagate for Blazor's error boundary.
            }

            throw;
        }
        finally {
            _palettePending = 0;
        }
    }

    private int _palettePending;

    /// <summary>
    /// Opens the settings dialog via the shared launcher (Story 3-3 D11 / Story 3-4 AC8).
    /// </summary>
    /// <returns>A task representing the dialog presentation.</returns>
    public async Task OpenSettingsAsync() {
        await CaptureKeyboardOriginAsync().ConfigureAwait(false);
        _ = await FcSettingsDialogLauncher
            .ShowAsync(dialogService, localizer["SettingsDialogTitle"].Value)
            .ConfigureAwait(false);
    }

    private async Task CaptureKeyboardOriginAsync() {
        IJSRuntime? js = services?.GetService<IJSRuntime>();
        if (js is null) {
            return;
        }
        try {
            IJSObjectReference module = await js.InvokeAsync<IJSObjectReference>(
                "import", "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js").ConfigureAwait(false);
            try {
                await module.InvokeVoidAsync("captureOverlayOrigin", null, true).ConfigureAwait(false);
            }
            finally {
                await module.DisposeAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException or InvalidOperationException) {
            // The dialog remains available if origin capture is unavailable.
        }
    }

    /// <summary>
    /// Navigates to the application home via <see cref="NavigationManager.NavigateTo(string)"/>.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task NavigateHomeAsync() {
        NavigationFailureNotifier? navigationFailure = services?.GetService<NavigationFailureNotifier>();
        try {
            navigationFailure?.BeginAttempt(navigation.ToAbsoluteUri("/home").AbsoluteUri);
            navigation.NavigateTo("/home");
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or UriFormatException) {
            navigationFailure?.ReportFailure();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Story 4-3 D10 / AC1 — scope-gated focus of the first column-filter input inside the active
    /// DataGrid. Returns without focusing when focus is outside any <c>[data-fc-datagrid]</c>
    /// container so the shortcut stays transparent in non-DataGrid contexts.
    /// </summary>
    /// <returns>A task that resolves when the focus attempt completes.</returns>
    [Obsolete("Use FocusSolePageSearchAsync for the page-search shortcut.")]
    public async Task FocusFirstColumnFilterAsync() {
        bool inGrid = await dataGridFocusScope.IsFocusWithinDataGridAsync().ConfigureAwait(false);
        if (!inGrid) {
            return;
        }

        string? viewKey = await dataGridFocusScope.GetActiveViewKeyAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(viewKey)) {
            return;
        }

        _ = await dataGridFocusScope.FocusFirstColumnFilterAsync(viewKey).ConfigureAwait(false);
    }

    /// <summary>Focuses the sole enabled search in the current route.</summary>
    public async Task FocusSolePageSearchAsync()
        => _ = await dataGridFocusScope.FocusSolePageSearchAsync().ConfigureAwait(false);

#if DEBUG
    private Task ToggleDevModeOverlayAsync() {
        services?.GetService<IDevModeOverlayController>()?.Toggle();
        return Task.CompletedTask;
    }
#endif
}
