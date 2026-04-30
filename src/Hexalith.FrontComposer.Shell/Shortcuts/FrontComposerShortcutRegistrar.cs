using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

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
///   <item><description><c>g h</c> → navigates to <c>/</c> via <see cref="NavigationManager"/>.</description></item>
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
    DataGridFocusScope dataGridFocusScope)
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

        try
        {
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
                routeUrl: "/");

            // Story 4-3 D10 / AC1 — `/` focuses the first column filter inside the active DataGrid.
            // Scope-gated via DataGridFocusScope — outside the grid the handler is a no-op (returns
            // without focusing, so the native `/` key behaviour in other contexts is unaffected).
            _ = shortcuts.Register(
                "/",
                "SlashFocusFilterShortcutDescription",
                FocusFirstColumnFilterAsync);
        }
        catch
        {
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

        try
        {
            // P5 (2026-04-21 pass-4): moved dispatch inside the try so a synchronous throw from
            // ulidFactory.NewUlid() or dispatcher.Dispatch() is caught by the rollback path below.
            // Previously only the await could throw into the catch; an earlier sync throw would
            // leak state (PaletteOpenedAction dispatched but no compensating close).
            dispatcher.Dispatch(new PaletteOpenedAction(ulidFactory.NewUlid()));

            _ = await dialogService.ShowDialogAsync<FcCommandPalette>(options =>
            {
                options.Modal = true;
                options.Width = "600px";
                options.Header.Title = localizer["CommandPaletteTitle"].Value;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // P4 (2026-04-21 pass-4): compensate for ALL exceptions including OperationCanceledException.
            // Earlier code relied on FluentDialog's DisposeAsync to dispatch PaletteClosedAction on
            // OCE, but OCE can be thrown from ShowDialogAsync before DisposeAsync wires up — leaving
            // IsOpen=true with no close. PaletteClosedAction reducers are idempotent (second dispatch
            // is a no-op) so double-dispatch on the dialog-close path is safe per D20.
            _ = ex;
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

    /// <summary>
    /// Story 4-3 D10 / AC1 — scope-gated focus of the first column-filter input inside the active
    /// DataGrid. Returns without focusing when focus is outside any <c>[data-fc-datagrid]</c>
    /// container so the shortcut stays transparent in non-DataGrid contexts.
    /// </summary>
    /// <returns>A task that resolves when the focus attempt completes.</returns>
    public async Task FocusFirstColumnFilterAsync()
    {
        bool inGrid = await dataGridFocusScope.IsFocusWithinDataGridAsync().ConfigureAwait(false);
        if (!inGrid)
        {
            return;
        }

        string? viewKey = await dataGridFocusScope.GetActiveViewKeyAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(viewKey))
        {
            return;
        }

        _ = await dataGridFocusScope.FocusFirstColumnFilterAsync(viewKey!).ConfigureAwait(false);
    }
}
