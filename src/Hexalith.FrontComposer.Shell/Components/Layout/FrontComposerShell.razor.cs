using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Shortcuts;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task

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
    private const string KeyboardModulePath = "./_content/Hexalith.FrontComposer.Shell/js/fc-keyboard.js";

    private IJSObjectReference? _beforeUnloadModule;
    private IJSObjectReference? _beforeUnloadSubscription;
    private IJSObjectReference? _keyboardModule;
    private DotNetObjectReference<FrontComposerShell>? _selfRef;
    private bool _themeBootstrapped;
    private bool _locationTrackingRegistered;
    private readonly object _locationTrackingSync = new();
    private bool _sessionRestoreAttempted;
    private string? _initialRenderUri;
    private ElementReference _shellRoot;
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

    /// <summary>Injected Fluxor dispatcher used to mirror route changes into navigation state.</summary>
    [Inject] private IDispatcher Dispatcher { get; set; } = default!;

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

    /// <summary>Injected navigation manager used to resolve static web asset URLs against the app base path.</summary>
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    /// <summary>
    /// Injected shortcut service (Story 3-4 D1). The shell's <c>@onkeydown</c> binding routes every
    /// global key through this surface; the Story 3-3 inline <c>Ctrl+,</c> branch is RETIRED per
    /// Story 3-3 D16 / Story 3-4 AC8.
    /// </summary>
    [Inject] private IShortcutService Shortcuts { get; set; } = default!;

    /// <summary>
    /// Injected shell registrar (Story 3-4 Task 1.4 / AC1). Owns the three v1 shell-default
    /// shortcut registrations (Ctrl+K palette, Ctrl+, settings, g h home).
    /// </summary>
    [Inject] private FrontComposerShortcutRegistrar Registrar { get; set; } = default!;

    /// <summary>Accent color projected into the inline <c>:root</c> style block (AC2).</summary>
    protected string AccentColor => Options.Value.AccentColor;

    /// <summary>Global stylesheet that owns the body density cascade + shared screen-reader-only helper.</summary>
    protected string DensityStylesheetPath => NavigationManager
        .ToAbsoluteUri("_content/Hexalith.FrontComposer.Shell/css/fc-density.css")
        .ToString();

    /// <summary>
    /// Story 4-5 T1.3 — global stylesheet for DataGrid projection layout hooks (expand-in-row
    /// detail, row-action column phone-tier hide, chevron rotation, expanded-row highlight).
    /// Loaded as a global static web asset because the classes are emitted by the source
    /// generator into adopter-namespace .razor.g.cs output where component-scoped selectors
    /// would not apply.
    /// </summary>
    protected string ProjectionStylesheetPath => NavigationManager
        .ToAbsoluteUri("_content/Hexalith.FrontComposer.Shell/css/fc-projection.css")
        .ToString();

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

    /// <summary>
    /// Story 3-4 Task 8.1 (D5 / AC8) — global keyboard router. Skips bare-letter chord prefixes
    /// when focus is inside a text input (D5 simpler-rule sketch — modifier-bearing combos still
    /// fire so <c>Ctrl+K</c> stays global from inside any input). All routing is delegated to
    /// <see cref="IShortcutService.TryInvokeAsync"/>; the Story 3-3 inline <c>Ctrl+,</c> branch is
    /// RETIRED per the AC8 migration contract.
    /// </summary>
    /// <param name="e">The keyboard event.</param>
    /// <returns>A task representing the asynchronous dispatch.</returns>
    protected async Task HandleGlobalKeyDown(KeyboardEventArgs e) {
        ArgumentNullException.ThrowIfNull(e);

        // The JS-side filter (`fc-keyboard.js:registerShellKeyFilter`) already suppresses bare-letter
        // keys that target editable elements before the event reaches Blazor, so we no longer pay a
        // circuit round-trip to `isEditableElementActive` on every keystroke. Modifier-bearing
        // shortcuts stay global regardless of focus target.
        _ = await Shortcuts.TryInvokeAsync(e).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            await ApplyThemeAsync().ConfigureAwait(false);
            await RegisterBeforeUnloadAsync().ConfigureAwait(false);
            await RegisterKeyboardInteropAsync().ConfigureAwait(false);
            RegisterLocationTracking();
            SyncCurrentBoundedContext(NavigationManager.Uri);
            await Registrar.RegisterShellDefaultsAsync().ConfigureAwait(false);
            _initialRenderUri = NavigationManager.Uri;
        }

        TryRestoreSession();
    }

    /// <summary>
    /// Story 3-6 Task 4 / D3 / ADR-048 — one-shot session restoration. If the user landed on the
    /// home route AND a valid, still-registered <c>LastActiveRoute</c> is in state, navigates to
    /// it. Guarded by <see cref="_sessionRestoreAttempted"/> so a later hydrate-race or
    /// <c>StorageReadyAction</c> cannot double-fire.
    /// </summary>
    private void TryRestoreSession() {
        if (_sessionRestoreAttempted) {
            return;
        }

        string currentUri = NavigationManager.Uri;
        if (!IsOnHomeRoute(currentUri)) {
            // Deep-link — respect the user's intent.
            _sessionRestoreAttempted = true;
            return;
        }

        // If the URL shifted since first render (user initiated navigation while we were waiting
        // for hydrate), do not overwrite their click — set the flag and exit.
        if (_initialRenderUri is not null
            && !string.Equals(currentUri, _initialRenderUri, StringComparison.Ordinal)) {
            _sessionRestoreAttempted = true;
            return;
        }

        FrontComposerNavigationState snapshot = NavigationState.Value;
        if (snapshot.HydrationState != NavigationHydrationState.Hydrated) {
            return;
        }

        _sessionRestoreAttempted = true;
        if (!SessionRouteHelper.TryNormalizePersistedRoute(snapshot.LastActiveRoute, NavigationManager, out string candidate)) {
            return;
        }

        string? bc = BoundedContextRouteParser.Parse(candidate);
        if (bc is null || !IsBoundedContextRegistered(bc)) {
            return;
        }

        try {
            NavigationManager.NavigateTo(candidate, forceLoad: false);
        }
        catch (InvalidOperationException) {
            // NavigationManager rejects the target (pre-init or disposed). Fail-silent per D3/D17.
        }
    }

    private static bool IsOnHomeRoute(string uri) {
        if (string.IsNullOrWhiteSpace(uri)) {
            return false;
        }

        string path;
        if (Uri.TryCreate(uri, UriKind.Absolute, out Uri? absolute)) {
            path = absolute.AbsolutePath;
        }
        else {
            int queryIndex = uri.IndexOfAny(['?', '#']);
            path = queryIndex >= 0 ? uri[..queryIndex] : uri;
        }

        path = path.Trim('/');
        return path.Length == 0 || string.Equals(path, "home", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsBoundedContextRegistered(string boundedContext) {
        try {
            // BoundedContextRouteParser.Parse lowercases its output; manifests use natural casing.
            // Case-insensitive comparison so PascalCase manifests match parser output.
            foreach (DomainManifest manifest in Registry.GetManifests()) {
                if (string.Equals(manifest.BoundedContext, boundedContext, StringComparison.OrdinalIgnoreCase)) {
                    return true;
                }
            }
        }
        catch (Exception) {
            return false;
        }

        return false;
    }

    /// <inheritdoc />
    public new async ValueTask DisposeAsync() {
        // P14 (Pass-6): serialize the field write + event -= against HandleLocationChanged's
        // exception-recovery path. Both paths write _locationTrackingRegistered and detach the
        // handler; without the lock a racing call could observe true → both call -= (idempotent
        // but fragile) → leave the field in a transient inconsistent state.
        DetachLocationTracking();

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

        if (_keyboardModule is not null) {
            // P9 (2026-04-21 pass-3): release the keydown handler attached by registerShellKeyFilter
            // before dropping the module so hot-reload / reconnect paths don't accumulate stale
            // handlers on the shell root element.
            try { await _keyboardModule.InvokeVoidAsync("unregisterShellKeyFilter", _shellRoot).ConfigureAwait(false); }
            catch (OperationCanceledException) { }
            catch (JSDisconnectedException) { }
            catch (JSException) { }

            try { await _keyboardModule.DisposeAsync().ConfigureAwait(false); } catch (OperationCanceledException) { } catch (JSDisconnectedException) { }
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

    private async Task RegisterKeyboardInteropAsync() {
        IJSObjectReference? keyboardModule = await EnsureKeyboardModuleAsync().ConfigureAwait(false);
        if (keyboardModule is null) {
            return;
        }

        try {
            await keyboardModule.InvokeVoidAsync("registerShellKeyFilter", _shellRoot).ConfigureAwait(false);
        }
        catch (OperationCanceledException) { }
        catch (JSDisconnectedException) { }
        catch (JSException) {
            // Non-fatal — shortcut routing still works; only browser-default suppression is skipped.
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

    private void RegisterLocationTracking() {
        lock (_locationTrackingSync) {
            if (_locationTrackingRegistered) {
                return;
            }

            _locationTrackingRegistered = true;
            NavigationManager.LocationChanged += HandleLocationChanged;
        }
    }

    private void DetachLocationTracking() {
        lock (_locationTrackingSync) {
            if (!_locationTrackingRegistered) {
                return;
            }

            _locationTrackingRegistered = false;
            NavigationManager.LocationChanged -= HandleLocationChanged;
        }
    }

    private void HandleLocationChanged(object? sender, LocationChangedEventArgs e) {
        try {
            SyncCurrentBoundedContext(e.Location);
        }
        catch (Exception ex) when (ex is ObjectDisposedException or InvalidOperationException) {
            // An aborted circuit (network disconnect / tab close) may dispose the Fluxor dispatcher
            // before DisposeAsync runs. Some Fluxor versions throw InvalidOperationException once
            // the store is disposed; others throw ObjectDisposedException. Detach the handler
            // defensively so future LocationChanged events do not re-enter a disposed scope.
            // Lock-protected detach (P14) prevents a racing DisposeAsync from leaving the field
            // in a transient inconsistent state.
            DetachLocationTracking();
        }
    }

    private void SyncCurrentBoundedContext(string uri)
        => Dispatcher.Dispatch(new BoundedContextChangedAction(BoundedContextRouteParser.Parse(uri)));

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
