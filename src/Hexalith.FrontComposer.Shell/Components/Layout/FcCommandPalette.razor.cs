using System.Globalization;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Routing;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Command palette dialog content (Story 3-4 Task 5 — D11 / D15 / D17 / D20 / D23; AC2, AC5, AC6).
/// </summary>
/// <remarks>
/// <para>
/// <b>Lifecycle:</b> opened via <c>IDialogService.ShowDialogAsync&lt;FcCommandPalette&gt;</c>.
/// Auto-focuses the search input on first render (AC2 F1).
/// </para>
/// <para>
/// <b>Dismiss-path coherence (D11):</b> every close path eventually dispatches
/// <see cref="PaletteClosedAction"/>. The keyboard handler dispatches it explicitly for Escape /
/// Enter / activation; <see cref="DisposeAsync"/> dispatches it as a catch-all for X-button /
/// backdrop / circuit-disconnect dismisses (wrapped in a guard against
/// <see cref="ObjectDisposedException"/> on dirty-disconnect circuits).
/// </para>
/// <para>
/// <b>D15 anti-regression:</b> the live region renders empty on first paint and populates on the
/// next tick via <see cref="OnAfterRenderAsync(bool)"/> so AT engines (NVDA / JAWS) pick up the
/// DOM mutation as an aria-live announce.
/// </para>
/// </remarks>
public partial class FcCommandPalette : Fluxor.Blazor.Web.Components.FluxorComponent, IAsyncDisposable
{
    private const string FocusModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js";
    private const string KeyboardModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js";

    private FluentTextInput? _searchRef;
    private string _localQuery = string.Empty;
    private string _liveRegionText = string.Empty;
    private bool _explicitlyClosed;
    private bool _restoreBodyFocusOnDispose;
    private ElementReference _paletteRoot;
    private IJSObjectReference? _focusModule;
    private IJSObjectReference? _keyboardModule;

    /// <summary>
    /// DOM id shared by the search input's <c>aria-controls</c> and the result list root. Kept as a
    /// single backing field so overriding one without the other is impossible (P12).
    /// </summary>
    private const string ResultListId = "fc-palette-results";

    /// <summary>The dialog instance cascaded by <see cref="IDialogService"/> (null when rendered standalone in tests).</summary>
    [CascadingParameter] public IDialogInstance? Dialog { get; set; }

    /// <summary>Injected Fluxor state subscription — re-renders on results / selection / IsOpen changes.</summary>
    [Inject] private IState<FrontComposerCommandPaletteState> PaletteState { get; set; } = default!;

    /// <summary>Injected Fluxor dispatcher.</summary>
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

    /// <summary>Injected ULID factory — correlates every dispatched action.</summary>
    [Inject] private IUlidFactory UlidFactory { get; set; } = default!;

    /// <summary>Injected localizer — resolves the "X results" template at announce time.</summary>
    [Inject] private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <summary>Injected JS runtime for focus + browser-default suppression interop.</summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Injected navigation manager used to detect same-route activations.</summary>
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    private string? ActiveDescendantId
        => PaletteState.Value.SelectedIndex >= 0 && PaletteState.Value.SelectedIndex < PaletteState.Value.Results.Length
            ? $"fc-palette-result-{PaletteState.Value.SelectedIndex}"
            : null;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await RegisterKeyboardInteropAsync().ConfigureAwait(false);
            await FocusSearchAsync().ConfigureAwait(false);

            // D15 empty-then-populate live-region choreography. DO NOT refactor — see anti-regression
            // comment in FcCommandPalette.razor for the full rationale.
            await Task.Yield();
            _liveRegionText = ComputeLiveRegionText(PaletteState.Value);
            StateHasChanged();
            return;
        }

        // Refresh aria-live text on every later render (results changed, query changed, etc.).
        string nextText = ComputeLiveRegionText(PaletteState.Value);
        if (!string.Equals(_liveRegionText, nextText, StringComparison.Ordinal))
        {
            _liveRegionText = nextText;
            StateHasChanged();
        }
    }

    /// <summary>Disposes the component, dispatching a catch-all <see cref="PaletteClosedAction"/> per D11.</summary>
    /// <returns>A value task that completes when disposal finishes.</returns>
    public new async ValueTask DisposeAsync()
    {
        // D11 dismiss-path catch-all — if the dialog was dismissed without going through Escape /
        // activation (X-button, backdrop click, circuit disconnect), make sure Fluxor still sees a
        // PaletteClosedAction so a subsequent Ctrl+K can re-open. Wrap in ObjectDisposedException
        // guard for dirty-disconnect robustness.
        if (!_explicitlyClosed)
        {
            try
            {
                Dispatcher.Dispatch(new PaletteClosedAction(UlidFactory.NewUlid()));
            }
            catch (ObjectDisposedException)
            {
                // Circuit disposed — Fluxor store is gone, nothing to update. Silent by design.
            }
            catch (InvalidOperationException)
            {
                // Fluxor store disposed ("Store has been disposed") — mirrors the FrontComposerShell
                // HandleLocationChanged guard. Silent by design.
            }
        }

        if (_restoreBodyFocusOnDispose)
        {
            await RestoreBodyFocusFallbackAsync().ConfigureAwait(false);
        }

        if (_focusModule is not null)
        {
            try { await _focusModule.DisposeAsync(); } catch (OperationCanceledException) { } catch (JSDisconnectedException) { } catch (JSException) { }
        }

        if (_keyboardModule is not null)
        {
            // P9 (2026-04-21 pass-3): release the keydown handler attached by
            // registerPaletteKeyFilter before dropping the module so hot-reload / reconnect paths
            // don't accumulate stale handlers on the palette root element.
            try { await _keyboardModule.InvokeVoidAsync("unregisterPaletteKeyFilter", _paletteRoot).ConfigureAwait(false); }
            catch (OperationCanceledException) { } catch (JSDisconnectedException) { } catch (JSException) { }

            try { await _keyboardModule.DisposeAsync(); } catch (OperationCanceledException) { } catch (JSDisconnectedException) { } catch (JSException) { }
        }

        await base.DisposeAsync();
    }

    private async Task OnQueryChangedAsync(string newQuery)
    {
        _localQuery = newQuery ?? string.Empty;
        Dispatcher.Dispatch(new PaletteQueryChangedAction(UlidFactory.NewUlid(), _localQuery));
        await Task.CompletedTask;
    }

    private async Task OnSelectionClickedAsync(int flatIndex)
    {
        // P7: snapshot Results once — the debounced results effect can replace PaletteState.Value.Results
        // between the bounds check and index read, so a second read could return a different row.
        System.Collections.Immutable.ImmutableArray<PaletteResult> results = PaletteState.Value.Results;
        if (flatIndex < 0 || flatIndex >= results.Length)
        {
            return;
        }

        PaletteResult result = results[flatIndex];
        if (IsInformationalShortcut(result))
        {
            return;
        }

        bool isSentinel = string.Equals(
            result.CommandTypeName,
            CommandPaletteEffects.KeyboardShortcutsSentinel,
            StringComparison.Ordinal);

        // Compute the body-focus verdict BEFORE dispatch so the navigation effect can't racily
        // advance NavigationManager.Uri past the pre-dispatch value we want to compare against.
        bool restoreFocus = !isSentinel && ShouldRestoreBodyFocusOnDispose(result);

        Dispatcher.Dispatch(new PaletteResultActivatedAction(flatIndex));

        if (!isSentinel)
        {
            _restoreBodyFocusOnDispose = restoreFocus;
            _explicitlyClosed = true;
            if (Dialog is not null)
            {
                await Dialog.CloseAsync();
            }
        }
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        switch (e.Key)
        {
            case "Escape":
                _explicitlyClosed = true;
                Dispatcher.Dispatch(new PaletteClosedAction(UlidFactory.NewUlid()));
                if (Dialog is not null)
                {
                    await Dialog.CloseAsync();
                }

                break;

            case "ArrowDown":
                Dispatcher.Dispatch(new PaletteSelectionMovedAction(+1));
                break;

            case "ArrowUp":
                Dispatcher.Dispatch(new PaletteSelectionMovedAction(-1));
                break;

            case "Enter":
                // P7: snapshot Results once — see OnSelectionClickedAsync rationale.
                System.Collections.Immutable.ImmutableArray<PaletteResult> enterResults = PaletteState.Value.Results;
                int selected = PaletteState.Value.SelectedIndex;
                if (selected < 0 || selected >= enterResults.Length)
                {
                    return;
                }

                PaletteResult result = enterResults[selected];
                if (IsInformationalShortcut(result))
                {
                    return;
                }

                bool isSentinel = string.Equals(
                    result.CommandTypeName,
                    CommandPaletteEffects.KeyboardShortcutsSentinel,
                    StringComparison.Ordinal);

                // Compute body-focus verdict BEFORE dispatch (see OnSelectionClickedAsync rationale).
                bool restoreFocus = !isSentinel && ShouldRestoreBodyFocusOnDispose(result);

                Dispatcher.Dispatch(new PaletteResultActivatedAction(selected));
                if (!isSentinel)
                {
                    _restoreBodyFocusOnDispose = restoreFocus;
                    _explicitlyClosed = true;
                    if (Dialog is not null)
                    {
                        await Dialog.CloseAsync();
                    }
                }

                break;

            default:
                break;
        }
    }

    private async Task RestoreBodyFocusFallbackAsync()
    {
        IJSObjectReference? focusModule = await EnsureFocusModuleAsync().ConfigureAwait(false);
        if (focusModule is null)
        {
            return;
        }

        try
        {
            await focusModule.InvokeVoidAsync("focusBodyIfNeeded").ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    private async Task RegisterKeyboardInteropAsync()
    {
        IJSObjectReference? keyboardModule = await EnsureKeyboardModuleAsync().ConfigureAwait(false);
        if (keyboardModule is null)
        {
            return;
        }

        try
        {
            await keyboardModule.InvokeVoidAsync("registerPaletteKeyFilter", _paletteRoot).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException)
        {
            // Non-fatal — list navigation still works, but browser-default suppression is skipped.
        }
    }

    private async Task FocusSearchAsync()
    {
        if (_searchRef is not { Element: { } element })
        {
            return;
        }

        IJSObjectReference? keyboardModule = await EnsureKeyboardModuleAsync().ConfigureAwait(false);
        if (keyboardModule is not null)
        {
            try
            {
                await keyboardModule.InvokeVoidAsync("focusElement", element).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) { }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
        }

        try
        {
            await element.FocusAsync();
        }
        catch (InvalidOperationException)
        {
            // ElementReference not attached — happens when called pre-render in some test/JS boot
            // races. User's first keystroke implicitly re-focuses the input.
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected mid-focus.
        }
        catch (JSException)
        {
            // JS interop boot race — element.focus() failed non-fatally.
        }
    }

    private async Task<IJSObjectReference?> EnsureKeyboardModuleAsync()
    {
        if (_keyboardModule is not null)
        {
            return _keyboardModule;
        }

        try
        {
            _keyboardModule = await JS.InvokeAsync<IJSObjectReference>("import", KeyboardModulePath).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }

        return _keyboardModule;
    }

    private async Task<IJSObjectReference?> EnsureFocusModuleAsync()
    {
        if (_focusModule is not null)
        {
            return _focusModule;
        }

        try
        {
            _focusModule = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (JSDisconnectedException)
        {
            return null;
        }
        catch (JSException)
        {
            return null;
        }

        return _focusModule;
    }

    private static bool IsInformationalShortcut(PaletteResult result)
        => result.Category == PaletteResultCategory.Shortcut && string.IsNullOrEmpty(result.RouteUrl);

    private bool ShouldRestoreBodyFocusOnDispose(PaletteResult result)
    {
        string? targetUrl = result.Category switch
        {
            PaletteResultCategory.Projection or PaletteResultCategory.Recent or PaletteResultCategory.Shortcut => result.RouteUrl,
            PaletteResultCategory.Command => string.IsNullOrWhiteSpace(result.CommandTypeName)
                    || string.IsNullOrWhiteSpace(result.BoundedContext)
                ? null
                : CommandRouteBuilder.BuildRoute(result.BoundedContext, result.CommandTypeName),
            _ => null,
        };

        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return false;
        }

        Uri current = new(NavigationManager.Uri, UriKind.Absolute);
        Uri target = NavigationManager.ToAbsoluteUri(targetUrl);
        return Uri.Compare(
            current,
            target,
            UriComponents.PathAndQuery | UriComponents.Fragment,
            UriFormat.SafeUnescaped,
            StringComparison.OrdinalIgnoreCase) == 0;
    }

    private string ComputeLiveRegionText(FrontComposerCommandPaletteState state)
        => state.Results.IsEmpty
            ? Localizer["PaletteNoResultsText"].Value
            : string.Format(
                CultureInfo.CurrentCulture,
                Localizer["PaletteResultCountTemplate"].Value,
                state.Results.Length);
}
