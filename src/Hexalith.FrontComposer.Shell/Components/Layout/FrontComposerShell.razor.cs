using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Framework-owned application shell composing Fluent UI v5's <c>FluentLayout</c> into the
/// spec-pinned Header / Navigation / Content / Footer regions (Story 3-1 D3 / D4 / D6 / D20).
/// Adopters' <c>MainLayout.razor</c> collapses to <c>&lt;FrontComposerShell&gt;@Body&lt;/FrontComposerShell&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Parameter surface (D4 — append-only through v1):</b> <see cref="HeaderStart"/>, <see cref="HeaderEnd"/>,
/// <see cref="Navigation"/>, <see cref="Footer"/>, <see cref="ChildContent"/>, <see cref="AppTitle"/>.
/// The snapshot test <c>FrontComposerShellParameterSurfaceTests</c> locks this list — any addition
/// must be append-only, no parameter may be removed/renamed/retyped without a major bump.
/// </para>
/// <para>
/// <b>Theme bootstrap (D6 / AC3):</b> on first render, reads the current <see cref="FrontComposerThemeState"/>
/// and calls <c>IThemeService.SetThemeAsync(ThemeSettings)</c> exactly once so the initial paint
/// reflects the hydrated preference. Subsequent theme changes flow through <see cref="ThemeEffects"/>
/// and the Fluxor pipeline.
/// </para>
/// <para>
/// <b>beforeunload hook (D17):</b> registers a <c>DotNetObjectReference</c> into the
/// <c>fc-beforeunload.js</c> module so <see cref="IStorageService.FlushAsync"/> runs before the
/// browser tears the page down. The module is imported lazily via <see cref="IJSRuntime"/> on the
/// first interactive render.
/// </para>
/// </remarks>
public partial class FrontComposerShell : ComponentBase, IAsyncDisposable {
    private const string BeforeUnloadModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-beforeunload.js";

    private IJSObjectReference? _beforeUnloadModule;
    private IJSObjectReference? _beforeUnloadSubscription;
    private DotNetObjectReference<FrontComposerShell>? _selfRef;
    private bool _themeBootstrapped;

    /// <summary>
    /// Header content rendered BEFORE the application title (left-aligned). Defaults to empty.
    /// Story 3-2 hamburger + breadcrumbs wire in here.
    /// </summary>
    [Parameter] public RenderFragment? HeaderStart { get; set; }

    /// <summary>
    /// Header content rendered AFTER the theme toggle (right-aligned). Defaults to empty.
    /// </summary>
    [Parameter] public RenderFragment? HeaderEnd { get; set; }

    /// <summary>
    /// Navigation rail content (~220 px). When <see langword="null"/> the Navigation layout area
    /// is OMITTED so Content spans edge-to-edge — avoids the empty 220 px column during the
    /// 3-1 / 3-2 sprint gap (AC1 Navigation-hide-when-null addendum).
    /// </summary>
    [Parameter] public RenderFragment? Navigation { get; set; }

    /// <summary>
    /// Footer content. When <see langword="null"/> the shell renders the default copyright
    /// "Hexalith FrontComposer © {Year}" via <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/>.
    /// </summary>
    [Parameter] public RenderFragment? Footer { get; set; }

    /// <summary>The page body (adopter <c>@Body</c>).</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>
    /// Application title shown in the header. When <see langword="null"/> (default) the shell
    /// resolves <c>FcShellResources.AppTitle</c> — the framework-owned product name string.
    /// </summary>
    [Parameter] public string? AppTitle { get; set; }

    /// <summary>Injected Fluent UI theme service. Called once on first render per D6.</summary>
    [Inject] private IThemeService ThemeService { get; set; } = default!;

    /// <summary>Injected shell options (accent color, localization, storage cap).</summary>
    [Inject] private IOptions<FcShellOptions> Options { get; set; } = default!;

    /// <summary>Injected Fluxor theme state (for first-render mode resolution).</summary>
    [Inject] private IState<FrontComposerThemeState> ThemeState { get; set; } = default!;

    /// <summary>Injected storage service whose drain is flushed on beforeunload via <see cref="FlushAsync"/>.</summary>
    [Inject] private IStorageService Storage { get; set; } = default!;

    /// <summary>Injected JS runtime for loading the beforeunload module.</summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>Accent color projected into the inline <c>:root</c> style block (AC2).</summary>
    protected string AccentColor => Options.Value.AccentColor;

    /// <summary>
    /// [JSInvokable] called by the <c>fc-beforeunload.js</c> module before the page unloads.
    /// Drains pending <see cref="IStorageService"/> writes so the user's last action persists.
    /// </summary>
    /// <returns>A task representing the flush.</returns>
    [JSInvokable]
    public Task FlushAsync() => Storage.FlushAsync();

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (!firstRender) {
            return;
        }

        await ApplyThemeAsync().ConfigureAwait(false);
        await RegisterBeforeUnloadAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync() {
        if (_beforeUnloadSubscription is not null && _beforeUnloadModule is not null) {
            try {
                await _beforeUnloadModule.InvokeVoidAsync("unregister", _beforeUnloadSubscription).ConfigureAwait(false);
            }
            catch (JSDisconnectedException) {
                // Circuit already torn down.
            }
            catch (JSException) {
                // Module unload errors are non-fatal on circuit teardown.
            }
        }

        if (_beforeUnloadSubscription is not null) {
            try { await _beforeUnloadSubscription.DisposeAsync().ConfigureAwait(false); } catch (JSDisconnectedException) { }
        }

        if (_beforeUnloadModule is not null) {
            try { await _beforeUnloadModule.DisposeAsync().ConfigureAwait(false); } catch (JSDisconnectedException) { }
        }

        _selfRef?.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task ApplyThemeAsync() {
        if (_themeBootstrapped) {
            return;
        }

        _themeBootstrapped = true;
        ThemeMode mode = ThemeState.Value.CurrentTheme switch {
            ThemeValue.Light => ThemeMode.Light,
            ThemeValue.Dark => ThemeMode.Dark,
            _ => ThemeMode.System,
        };
        await ThemeService.SetThemeAsync(new ThemeSettings(AccentColor, 0, 0, mode, true)).ConfigureAwait(false);
    }

    private async Task RegisterBeforeUnloadAsync() {
        try {
            _beforeUnloadModule = await JS.InvokeAsync<IJSObjectReference>("import", BeforeUnloadModulePath).ConfigureAwait(false);
            _selfRef = DotNetObjectReference.Create(this);
            _beforeUnloadSubscription = await _beforeUnloadModule.InvokeAsync<IJSObjectReference>("register", _selfRef).ConfigureAwait(false);
        }
        catch (JSException) {
            // Non-fatal — persistence still works without the beforeunload guard; the drain
            // worker flushes in-flight writes eventually.
        }
    }
}
