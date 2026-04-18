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
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!firstRender) {
            return;
        }

        try {
            _module = await JS.InvokeAsync<IJSObjectReference>("import", ModulePath).ConfigureAwait(false);
            _selfRef = DotNetObjectReference.Create(this);
            _subscription = await _module.InvokeAsync<IJSObjectReference>("subscribe", _selfRef).ConfigureAwait(false);
        }
        catch (JSException) {
            // Non-fatal — System mode falls back to whatever the initial paint decided.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_module is not null && _subscription is not null) {
            try { await _module.InvokeVoidAsync("unsubscribe", _subscription).ConfigureAwait(false); }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
        }

        if (_subscription is not null) {
            try { await _subscription.DisposeAsync().ConfigureAwait(false); } catch (JSDisconnectedException) { }
        }

        if (_module is not null) {
            try { await _module.DisposeAsync().ConfigureAwait(false); } catch (JSDisconnectedException) { }
        }

        _selfRef?.Dispose();
        GC.SuppressFinalize(this);
    }
}
