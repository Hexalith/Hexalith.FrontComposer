using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.Navigation;
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
/// <b>Parameter ordering (Story 3-2 D10):</b> the 7 parameters follow the L→R visual header layout,
/// NOT alphabetical order: <see cref="HeaderStart"/>, <see cref="HeaderCenter"/>, <see cref="HeaderEnd"/>,
/// <see cref="Navigation"/>, <see cref="Footer"/>, <see cref="ChildContent"/>, <see cref="AppTitle"/>.
/// The snapshot test <c>FrontComposerShellParameterSurfaceTests</c> locks this list — any addition
/// must be append-only, no parameter may be removed/renamed/retyped without a major bump.
/// </para>
/// <para>
/// <b>Navigation auto-populate (Story 3-2 D18 / ADR-035):</b> when <see cref="Navigation"/> is
/// <see langword="null"/> AND <c>IFrontComposerRegistry.GetManifests()</c> returns ≥ 1 manifest,
/// shell renders <see cref="FrontComposerNavigation"/> inside the Navigation layout item.
/// Adopters supplying a non-null fragment win; the empty-registry bootstrap omits the Navigation
/// layout area entirely (Story 3-1 AC1 Nav-hide-when-null addendum).
/// </para>
/// <para>
/// <b>Opt-out escape hatch for adopters with registered domains who want NO sidebar
/// (ADR-035 addendum):</b> supply an empty render fragment, e.g.
/// <example>
/// <code>
/// &lt;FrontComposerShell Navigation="@((RenderFragment)(_ =&gt; { }))"&gt;@Body&lt;/FrontComposerShell&gt;
/// </code>
/// </example>
/// The parameter is non-null so the auto-populate branch is bypassed; the empty fragment renders
/// nothing inside the Navigation layout item.
/// </para>
/// <para>
/// <b>Skip-to-navigation anchor (Story 3-2 Task 8.3a / AC6 / D16):</b> the Navigation
/// <c>FluentLayoutItem</c> carries <c>id="fc-nav"</c>; the <c>SkipToNavigationLabel</c> resource
/// renders an <c>&lt;a class="fc-skip-link" href="#fc-nav"&gt;</c> link immediately after Story 3-1's
/// SkipToContent link. Both are visually-hidden-until-focused per the existing class pattern.
/// </para>
/// <para>
/// <b>Theme bootstrap (D6 / AC3):</b> on first render, reads the current
/// <see cref="FrontComposerThemeState"/> and calls <c>IThemeService.SetThemeAsync(ThemeSettings)</c>
/// exactly once so the initial paint reflects the hydrated preference.
/// </para>
/// <para>
/// <b>beforeunload hook (D17):</b> registers a <c>DotNetObjectReference</c> into the
/// <c>fc-beforeunload.js</c> module so <see cref="IStorageService.FlushAsync"/> runs before the
/// browser tears the page down.
/// </para>
/// </remarks>
public partial class FrontComposerShell : FluxorComponent, IAsyncDisposable {
    private const string BeforeUnloadModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-beforeunload.js";

    private IJSObjectReference? _beforeUnloadModule;
    private IJSObjectReference? _beforeUnloadSubscription;
    private DotNetObjectReference<FrontComposerShell>? _selfRef;
    private bool _themeBootstrapped;
    private readonly LayoutHamburgerCoordinator _hamburgerCoordinator = new();

    /// <summary>
    /// Header content rendered BEFORE the application title (left-aligned). When <see langword="null"/>
    /// the shell auto-populates <see cref="FcHamburgerToggle"/> (Story 3-2 D8 / D18).
    /// </summary>
    [Parameter] public RenderFragment? HeaderStart { get; set; }

    /// <summary>
    /// Header content rendered between the application title and the right-side stack (breadcrumb slot,
    /// Story 3-2 D10). When <see langword="null"/> the slot is omitted. Story 3-5 populates the content.
    /// </summary>
    [Parameter] public RenderFragment? HeaderCenter { get; set; }

    /// <summary>
    /// Header content rendered AFTER the theme toggle (right-aligned). Defaults to empty.
    /// </summary>
    [Parameter] public RenderFragment? HeaderEnd { get; set; }

    /// <summary>
    /// Navigation rail content (~220 px). When <see langword="null"/> AND the registry returns ≥ 1
    /// manifest, the shell auto-renders <see cref="FrontComposerNavigation"/> (Story 3-2 D18 / ADR-035).
    /// When <see langword="null"/> AND the registry is empty, the Navigation layout area is OMITTED
    /// so Content spans edge-to-edge. Adopters supplying a non-null fragment always win.
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

    /// <summary>Injected Fluxor navigation state (for responsive shell width calculation).</summary>
    [Inject] private IState<FrontComposerNavigationState> NavigationState { get; set; } = default!;

    /// <summary>Injected storage service whose drain is flushed on beforeunload.</summary>
    [Inject] private IStorageService Storage { get; set; } = default!;

    /// <summary>Injected JS runtime for loading the beforeunload module.</summary>
    [Inject] private IJSRuntime JS { get; set; } = default!;

    /// <summary>
    /// Injected FrontComposer registry — required for the Navigation auto-populate check
    /// (Story 3-2 D18 / ADR-035) and the skip-to-navigation anchor gate (AC6 / Task 8.3a).
    /// Queried at render time, not reducer time, so a scoped Fluxor <c>IState&lt;&gt;</c> would not suffice.
    /// </summary>
    [Inject] private IFrontComposerRegistry Registry { get; set; } = default!;

    /// <summary>Accent color projected into the inline <c>:root</c> style block (AC2).</summary>
    protected string AccentColor => Options.Value.AccentColor;

    /// <summary>
    /// Whether the shell should render the Navigation area. Adopter-supplied content always wins;
    /// framework auto-navigation appears only when at least one manifest has projections.
    /// </summary>
    protected bool HasNavigation => Navigation is not null || HasRenderableManifest();

    /// <summary>
    /// Whether the current viewport is Tablet or Phone. The Navigation <c>FluentLayoutItem</c> is
    /// suppressed at these tiers per AC5 / dev-notes §39 — navigation appears only through the
    /// hamburger drawer below CompactDesktop.
    /// </summary>
    protected bool IsSubCompactDesktopViewport {
        get {
            ViewportTier tier = NavigationState.Value.CurrentViewport;
            return tier == ViewportTier.Tablet || tier == ViewportTier.Phone;
        }
    }

    /// <summary>
    /// Width of the Navigation area for framework-provided navigation. The compact rail occupies
    /// 48 px; the expanded desktop sidebar remains 220 px.
    /// </summary>
    protected string NavigationWidth => Navigation is null && ShouldUseCollapsedRailWidth()
        ? "48px"
        : "220px";

    /// <summary>
    /// [JSInvokable] called by the <c>fc-beforeunload.js</c> module before the page unloads.
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
    public new async ValueTask DisposeAsync() {
        if (_beforeUnloadSubscription is not null && _beforeUnloadModule is not null) {
            try {
                await _beforeUnloadModule.InvokeVoidAsync("unregister", _beforeUnloadSubscription).ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }
            catch (JSDisconnectedException) { }
            catch (JSException) { }
        }

        if (_beforeUnloadSubscription is not null) {
            try { await _beforeUnloadSubscription.DisposeAsync().ConfigureAwait(false); } catch (OperationCanceledException) { } catch (JSDisconnectedException) { }
        }

        if (_beforeUnloadModule is not null) {
            try { await _beforeUnloadModule.DisposeAsync().ConfigureAwait(false); } catch (OperationCanceledException) { } catch (JSDisconnectedException) { }
        }

        _selfRef?.Dispose();

        // Delegating via base.DisposeAsync() invokes FluxorComponent.DisposeAsync which unhooks
        // IState<T>.StateChanged handlers. Without this call the state subscriptions root the
        // shell across page navigations — a silent leak. FluxorComponent.DisposeAsync is not
        // marked virtual, so we re-implement IAsyncDisposable via `new` and chain explicitly.
        await base.DisposeAsync().ConfigureAwait(false);
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
        catch (OperationCanceledException) {
            // Circuit disposing mid-registration — non-fatal.
        }
        catch (JSException) {
            // Non-fatal — persistence still works without the beforeunload guard.
        }
    }

    private bool HasRenderableManifest() {
        foreach (DomainManifest manifest in Registry.GetManifests()) {
            if (manifest.Projections.Count > 0) {
                return true;
            }
        }

        return false;
    }

    private bool ShouldUseCollapsedRailWidth() {
        FrontComposerNavigationState snapshot = NavigationState.Value;
        return snapshot.CurrentViewport == ViewportTier.CompactDesktop
            || (snapshot.CurrentViewport == ViewportTier.Desktop && snapshot.SidebarCollapsed);
    }
}
