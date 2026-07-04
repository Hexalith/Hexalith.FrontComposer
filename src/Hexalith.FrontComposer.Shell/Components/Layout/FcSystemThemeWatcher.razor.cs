using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Headless component that subscribes to the browser <c>prefers-color-scheme</c> media query and
/// invokes <see cref="IThemeService.SetThemeAsync(ThemeMode)"/> when the OS preference changes —
/// BUT only while <see cref="FrontComposerThemeState.CurrentTheme"/> is <see cref="ThemeValue.System"/>
/// so an explicit Light/Dark selection is never overridden by the OS (Story 3-1 D10 / D23 / AC3).
/// </summary>
public partial class FcSystemThemeWatcher : ComponentBase, IAsyncDisposable {
    private const string ModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-prefers-color-scheme.js";

    private IJSObjectReference? _module;
    private IJSObjectReference? _subscription;
    private DotNetObjectReference<FcSystemThemeWatcher>? _selfRef;
    private bool _disposed;

    /// <summary>Injected JS runtime for module import + subscription.</summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Injected Fluxor state — consulted on every callback to honour the "only when System" guard.</summary>
    [Inject] private IState<FrontComposerThemeState> ThemeState { get; set; } = default!;

    /// <summary>Injected Fluent UI theme service.</summary>
    [Inject] private IThemeService ThemeService { get; set; } = default!;

    /// <summary>Injected shell options (accent color for the theme-settings overload).</summary>
    [Inject] private IOptions<FcShellOptions> Options { get; set; } = default!;

    /// <summary>
    /// [JSInvokable] Callback invoked by fc-prefers-color-scheme.js when the OS preference
    /// changes or on initial subscription.
    /// </summary>
    /// <param name="isDark">Whether the current OS preference is Dark.</param>
    /// <returns>A task representing the theme application.</returns>
    [JSInvokable]
    public async Task OnSystemThemeChangedAsync(bool isDark) {
        if (ThemeState.Value.CurrentTheme != ThemeValue.System) {
            return;
        }

        ThemeMode mode = isDark ? ThemeMode.Dark : ThemeMode.Light;
        await ThemeService.SetThemeAsync(new ThemeSettings(Options.Value.AccentColor, 0, 0, mode, true)).ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Mirrors the <see cref="FcLayoutBreakpointWatcher"/> disposal-race hardening: assign
    /// <c>_selfRef</c> before the awaits so a mid-subscribe throw stays disposable, re-check
    /// <c>_disposed</c> after each await so a racing <see cref="DisposeAsync"/> never leaves a live
    /// matchMedia listener holding a disposed <see cref="DotNetObjectReference{TValue}"/>, and
    /// tolerate circuit-teardown exceptions (<see cref="OperationCanceledException"/> /
    /// <see cref="JSDisconnectedException"/>) that a plain <see cref="JSException"/> catch misses.
    /// </remarks>
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!firstRender) {
            return;
        }

        _selfRef = DotNetObjectReference.Create(this);
        IJSObjectReference? module = null;
        IJSObjectReference? subscription = null;
        try {
            module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(false);
            if (_disposed) {
                await SafeDisposeAsync(module).ConfigureAwait(false);
                return;
            }

            subscription = await module.InvokeAsync<IJSObjectReference>("subscribe", _selfRef).ConfigureAwait(false);
            if (_disposed) {
                await SafeUnsubscribeAsync(module, subscription).ConfigureAwait(false);
                await SafeDisposeAsync(subscription).ConfigureAwait(false);
                await SafeDisposeAsync(module).ConfigureAwait(false);
                return;
            }

            _module = module;
            _subscription = subscription;
        }
        catch (OperationCanceledException) {
            // Circuit disposing mid-import — silent.
            await SafeDisposeAsync(subscription).ConfigureAwait(false);
            await SafeDisposeAsync(module).ConfigureAwait(false);
        }
        catch (JSDisconnectedException) {
            // Circuit already gone — silent; System mode falls back to the initial paint.
            await SafeDisposeAsync(subscription).ConfigureAwait(false);
            await SafeDisposeAsync(module).ConfigureAwait(false);
        }
        catch (JSException) {
            // Non-fatal — System mode falls back to whatever the initial paint decided.
            await SafeDisposeAsync(subscription).ConfigureAwait(false);
            await SafeDisposeAsync(module).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        _disposed = true;

        await SafeUnsubscribeAsync(_module, _subscription).ConfigureAwait(false);
        await SafeDisposeAsync(_subscription).ConfigureAwait(false);
        await SafeDisposeAsync(_module).ConfigureAwait(false);

        _selfRef?.Dispose();
        GC.SuppressFinalize(this);
    }

    private static async ValueTask SafeUnsubscribeAsync(IJSObjectReference? module, IJSObjectReference? subscription) {
        if (module is null || subscription is null) {
            return;
        }

        try {
            await module.InvokeVoidAsync("unsubscribe", subscription).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    private static async ValueTask SafeDisposeAsync(IJSObjectReference? reference) {
        if (reference is null) {
            return;
        }

        try { await reference.DisposeAsync().ConfigureAwait(false); }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
    }
}
