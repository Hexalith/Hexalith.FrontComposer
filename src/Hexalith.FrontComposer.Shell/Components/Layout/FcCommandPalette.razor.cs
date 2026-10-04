using Fluxor;

using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Routing;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.CommandPalette;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.DependencyInjection;
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
public partial class FcCommandPalette : Fluxor.Blazor.Web.Components.FluxorComponent, IAsyncDisposable {
    private const string FocusModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-focus.js";
    private const string KeyboardModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js";

    private FluentTextInput? _searchRef;
    private string _localQuery = string.Empty;
    private string _liveRegionText = string.Empty;
    private bool _explicitlyClosed;
    private bool _navigatesToOtherRoute;
    private long _lastDenialVersion;
    private long _lastFailureVersion;
    private string? _openedRoute;
    private string? _announcedZeroQuery;
    private string? _pendingZeroQuery;
    private string? _pendingFailureText;
    private bool _liveRegionHoldsFailure;
    private CancellationTokenSource? _zeroStatusDelay;
    private bool _disposed;
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

    /// <summary>Injected localizer for palette labels and status messages.</summary>
    [Inject] private IStringLocalizer<FcShellResources> Localizer { get; set; } = default!;

    /// <summary>Injected JS runtime for focus + browser-default suppression interop.</summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Injected navigation manager used to detect same-route activations.</summary>
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [Inject] private IServiceProvider Services { get; set; } = default!;

    private string? ActiveDescendantId
        => PaletteState.Value.SelectedIndex >= 0 && PaletteState.Value.SelectedIndex < PaletteState.Value.Results.Length
            ? $"fc-palette-result-{PaletteState.Value.SelectedIndex}"
            : null;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            _openedRoute = NavigationManager.Uri;
            // A failure from an earlier palette session must not speak in this one.
            _lastFailureVersion = PaletteState.Value.ActivationFailureVersion;
            await RegisterKeyboardInteropAsync();
            await FocusSearchAsync();
        }

        FrontComposerCommandPaletteState state = PaletteState.Value;
        if (state.ActivationDenialVersion != _lastDenialVersion) {
            _lastDenialVersion = state.ActivationDenialVersion;
            _navigatesToOtherRoute = false;
            IJSObjectReference? focus = await EnsureFocusModuleAsync();
            if (focus is not null) {
                try { await focus.InvokeVoidAsync("focusOverlayEntry", "fc-palette-denied-heading"); }
                catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException) { }
            }
        }
        if (state.ActivationFailureVersion != _lastFailureVersion) {
            _lastFailureVersion = state.ActivationFailureVersion;
            _navigatesToOtherRoute = false;
            QueueActivationFailureStatus();
        }
        else if (_pendingFailureText is { } failureText) {
            _pendingFailureText = null;
            _liveRegionText = failureText;
            StateHasChanged();
        }
        if (!_explicitlyClosed && !state.IsOpen && Dialog is not null) {
            _navigatesToOtherRoute = _openedRoute is not null
                && !string.Equals(new Uri(_openedRoute).AbsolutePath, new Uri(NavigationManager.Uri).AbsolutePath, StringComparison.OrdinalIgnoreCase);
            _explicitlyClosed = true;
            await Dialog.CloseAsync();
        }

        ScheduleZeroResultStatus();
    }

    /// <summary>Disposes the component, dispatching a catch-all <see cref="PaletteClosedAction"/> per D11.</summary>
    /// <returns>A value task that completes when disposal finishes.</returns>
    public new async ValueTask DisposeAsync() {
        _disposed = true;
        _zeroStatusDelay?.Cancel();
        _zeroStatusDelay?.Dispose();

        // D11 dismiss-path catch-all — if the dialog was dismissed without going through Escape /
        // activation (X-button, backdrop click, circuit disconnect), make sure Fluxor still sees a
        // PaletteClosedAction so a subsequent Ctrl+K can re-open. Wrap in ObjectDisposedException
        // guard for dirty-disconnect robustness.
        if (!_explicitlyClosed) {
            try {
                Dispatcher.Dispatch(new PaletteClosedAction(UlidFactory.NewUlid()));
            }
            catch (ObjectDisposedException) {
                // Circuit disposed — Fluxor store is gone, nothing to update. Silent by design.
            }
            catch (InvalidOperationException) {
                // Fluxor store disposed ("Store has been disposed") — mirrors the FrontComposerShell
                // HandleLocationChanged guard. Silent by design.
            }
        }

        if (_navigatesToOtherRoute && _openedRoute is not null) {
            // Search entry normally imports only the keyboard module. Navigation still needs the
            // focus module to release the palette reservation while preserving the route heading.
            IJSObjectReference? focusModule = await EnsureFocusModuleAsync();
            if (focusModule is not null) {
                try {
                    await focusModule.InvokeVoidAsync("preserveRouteFocusAfterOverlay", _openedRoute);
                }
                catch (Exception ex) when (ex is JSDisconnectedException or JSException or OperationCanceledException) {
                    // The circuit can close while the navigation is completing.
                }
            }
        }

        if (!_navigatesToOtherRoute && _openedRoute is not null
            && string.Equals(new Uri(_openedRoute).AbsolutePath, new Uri(NavigationManager.Uri).AbsolutePath, StringComparison.OrdinalIgnoreCase)) {
            await RestoreOriginFocusAsync().ConfigureAwait(false);
        }

        if (_focusModule is not null) {
            try { await _focusModule.DisposeAsync(); } catch (OperationCanceledException) { } catch (JSDisconnectedException) { } catch (JSException) { }
        }

        if (_keyboardModule is not null) {
            // P9 (2026-04-21 pass-3): release the keydown handler attached by
            // registerPaletteKeyFilter before dropping the module so hot-reload / reconnect paths
            // don't accumulate stale handlers on the palette root element.
            try { await _keyboardModule.InvokeVoidAsync("unregisterPaletteKeyFilter", _paletteRoot).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (JSDisconnectedException) { }
            catch (JSException) { }

            try { await _keyboardModule.DisposeAsync(); } catch (OperationCanceledException) { } catch (JSDisconnectedException) { } catch (JSException) { }
        }

        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private Task OnQueryChangedAsync(string newQuery) {
        _localQuery = newQuery ?? string.Empty;
        _navigatesToOtherRoute = false;
        _liveRegionHoldsFailure = false;
        _pendingFailureText = null;
        Dispatcher.Dispatch(new PaletteQueryChangedAction(UlidFactory.NewUlid(), _localQuery));
        return Task.CompletedTask;
    }

    private Task OnSelectionClickedAsync(int flatIndex) {
        // P7: snapshot Results once — the debounced results effect can replace PaletteState.Value.Results
        // between the bounds check and index read, so a second read could return a different row.
        System.Collections.Immutable.ImmutableArray<PaletteResult> results = PaletteState.Value.Results;
        if (flatIndex < 0 || flatIndex >= results.Length) {
            return Task.CompletedTask;
        }

        PaletteResult result = results[flatIndex];
        if (IsInformationalShortcut(result)) {
            return Task.CompletedTask;
        }

        bool isSentinel = string.Equals(
            result.CommandTypeName,
            CommandPaletteEffects.KeyboardShortcutsSentinel,
            StringComparison.Ordinal);
        _navigatesToOtherRoute = !isSentinel && ActivatesOtherRoute(result);
        Dispatcher.Dispatch(new PaletteResultActivatedAction(flatIndex, result, PaletteState.Value.Query));
        return Task.CompletedTask;
    }

    private async Task HandleKeyDownAsync(KeyboardEventArgs e) {
        ArgumentNullException.ThrowIfNull(e);

        // P12 — rapid Escape→Enter on a keyboard-buffered input can dispatch both PaletteClosedAction
        // and PaletteResultActivatedAction on the same component instance; the second navigation would
        // race the first close. Bail if the palette was already explicitly dismissed.
        if (_explicitlyClosed) {
            return;
        }

        switch (e.Key) {
            case "Escape":
                _explicitlyClosed = true;
                _navigatesToOtherRoute = false;
                Dispatcher.Dispatch(new PaletteClosedAction(UlidFactory.NewUlid()));
                if (Dialog is not null) {
                    await Dialog.CloseAsync();
                }

                break;

            case "ArrowDown":
                _navigatesToOtherRoute = false;
                Dispatcher.Dispatch(new PaletteSelectionMovedAction(+1));
                break;

            case "ArrowUp":
                _navigatesToOtherRoute = false;
                Dispatcher.Dispatch(new PaletteSelectionMovedAction(-1));
                break;

            case "Enter":
                // P7: snapshot Results once — see OnSelectionClickedAsync rationale.
                System.Collections.Immutable.ImmutableArray<PaletteResult> enterResults = PaletteState.Value.Results;
                int selected = PaletteState.Value.SelectedIndex;
                if (selected < 0 || selected >= enterResults.Length) {
                    return;
                }

                PaletteResult result = enterResults[selected];
                if (IsInformationalShortcut(result)) {
                    return;
                }

                bool isSentinel = string.Equals(
                    result.CommandTypeName,
                    CommandPaletteEffects.KeyboardShortcutsSentinel,
                    StringComparison.Ordinal);
                _navigatesToOtherRoute = !isSentinel && ActivatesOtherRoute(result);
                Dispatcher.Dispatch(new PaletteResultActivatedAction(selected, result, PaletteState.Value.Query));

                break;

            default:
                break;
        }
    }

    private async Task RestoreOriginFocusAsync() {
        IJSObjectReference? focusModule = await EnsureFocusModuleAsync().ConfigureAwait(false);
        if (focusModule is null) {
            return;
        }

        try {
            await focusModule.InvokeVoidAsync("restoreOverlayOrigin").ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    private async Task RegisterKeyboardInteropAsync() {
        IJSObjectReference? keyboardModule = await EnsureKeyboardModuleAsync().ConfigureAwait(false);
        if (keyboardModule is null) {
            return;
        }

        try {
            await keyboardModule.InvokeVoidAsync("registerPaletteKeyFilter", _paletteRoot).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException) {
            // Non-fatal — list navigation still works, but browser-default suppression is skipped.
        }
    }

    private async Task FocusSearchAsync() {
        if (_searchRef is not { Element: { } element }) {
            return;
        }

        IJSObjectReference? keyboardModule = await EnsureKeyboardModuleAsync().ConfigureAwait(false);
        if (keyboardModule is not null) {
            try {
                await keyboardModule.InvokeVoidAsync("focusElement", element).ConfigureAwait(false);
                return;
            }
            catch (OperationCanceledException) { }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
        }

        try {
            await element.FocusAsync();
        }
        catch (InvalidOperationException) {
            // ElementReference not attached — happens when called pre-render in some test/JS boot
            // races. User's first keystroke implicitly re-focuses the input.
        }
        catch (JSDisconnectedException) {
            // Circuit disconnected mid-focus.
        }
        catch (JSException) {
            // JS interop boot race — element.focus() failed non-fatally.
        }
    }

    private async Task<IJSObjectReference?> EnsureKeyboardModuleAsync() {
        if (_keyboardModule is not null) {
            return _keyboardModule;
        }

        try {
            _keyboardModule = await JS.InvokeAsync<IJSObjectReference>("import", KeyboardModulePath).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            return null;
        }
        catch (JSDisconnectedException) {
            return null;
        }
        catch (JSException) {
            return null;
        }

        return _keyboardModule;
    }

    private async Task<IJSObjectReference?> EnsureFocusModuleAsync() {
        if (_focusModule is not null) {
            return _focusModule;
        }

        try {
            _focusModule = await JS.InvokeAsync<IJSObjectReference>("import", FocusModulePath).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            return null;
        }
        catch (JSDisconnectedException) {
            return null;
        }
        catch (JSException) {
            return null;
        }

        return _focusModule;
    }

    private static bool IsInformationalShortcut(PaletteResult result)
        => result.Category == PaletteResultCategory.Shortcut && string.IsNullOrEmpty(result.RouteUrl);

    private bool ActivatesOtherRoute(PaletteResult result) {
        string? destination = result.Category == PaletteResultCategory.Command
            && !string.IsNullOrWhiteSpace(result.BoundedContext)
            && !string.IsNullOrWhiteSpace(result.CommandTypeName)
                ? CommandRouteBuilder.BuildRoute(result.BoundedContext, result.CommandTypeName)
                : result.RouteUrl;
        if (string.IsNullOrWhiteSpace(destination) || _openedRoute is null) {
            return false;
        }

        try {
            return !string.Equals(
                NavigationManager.ToAbsoluteUri(destination).AbsolutePath,
                new Uri(_openedRoute).AbsolutePath,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (UriFormatException) {
            return false;
        }
    }

    // The modal palette makes the shell's status region inert, so an activation failure (AM-23)
    // speaks through this palette-owned region while the palette stays open.
    private void QueueActivationFailureStatus() {
        string text = Services.GetService<NavigationFailureNotifier>()?.Message
            ?? Localizer["RouteNavigationFailedText", Localizer["RouteNavigationUnknownDestinationLabel"].Value].Value;
        _liveRegionHoldsFailure = true;
        if (_liveRegionText.Length > 0) {
            // Clear first so the next render inserts fresh text into the existing live region.
            _liveRegionText = string.Empty;
            _pendingFailureText = text;
        }
        else {
            _liveRegionText = text;
        }

        StateHasChanged();
    }

    private void ScheduleZeroResultStatus() {
        FrontComposerCommandPaletteState state = PaletteState.Value;
        string query = state.Query.Trim().ToLowerInvariant();
        bool zero = state.IsOpen && query.Length > 0 && state.LoadState == PaletteLoadState.Ready && state.Results.IsEmpty;
        if (!zero) {
            if (state.Results.Length > 0) {
                _announcedZeroQuery = null;
            }
            _zeroStatusDelay?.Cancel();
            _zeroStatusDelay?.Dispose();
            _zeroStatusDelay = null;
            _pendingZeroQuery = null;
            if (_liveRegionText.Length > 0 && !_liveRegionHoldsFailure) {
                _liveRegionText = string.Empty;
                StateHasChanged();
            }
            return;
        }

        if (string.Equals(_pendingZeroQuery, query, StringComparison.Ordinal)) {
            return;
        }

        _zeroStatusDelay?.Cancel();
        _zeroStatusDelay?.Dispose();
        _zeroStatusDelay = null;
        _pendingZeroQuery = null;
        if (_liveRegionText.Length > 0 && !string.Equals(_announcedZeroQuery, query, StringComparison.Ordinal)) {
            _liveRegionText = string.Empty;
            StateHasChanged();
        }
        if (string.Equals(_announcedZeroQuery, query, StringComparison.Ordinal)) {
            return;
        }

        CancellationTokenSource delay = new();
        _zeroStatusDelay = delay;
        _pendingZeroQuery = query;
        _ = AnnounceZeroAfterDelayAsync(query, delay);
    }

    private async Task AnnounceZeroAfterDelayAsync(string query, CancellationTokenSource delay) {
        try {
            TimeProvider clock = Services.GetService<TimeProvider>() ?? TimeProvider.System;
            await Task.Delay(TimeSpan.FromMilliseconds(250), clock, delay.Token);
            if (_disposed || delay.IsCancellationRequested) {
                return;
            }

            await InvokeAsync(() => {
                FrontComposerCommandPaletteState current = PaletteState.Value;
                if (current.IsOpen && current.LoadState == PaletteLoadState.Ready && current.Results.IsEmpty
                    && string.Equals(current.Query.Trim(), query, StringComparison.OrdinalIgnoreCase)) {
                    _announcedZeroQuery = query;
                    _liveRegionHoldsFailure = false;
                    _pendingFailureText = null;
                    _liveRegionText = Localizer["PaletteNoCommandsOrPagesText"].Value;
                    StateHasChanged();
                }
            });
        }
        catch (OperationCanceledException) {
            // A later query or close superseded this status.
        }
        finally {
            if (ReferenceEquals(_zeroStatusDelay, delay)) {
                _zeroStatusDelay = null;
                _pendingZeroQuery = null;
            }
            delay.Dispose();
        }
    }
}
