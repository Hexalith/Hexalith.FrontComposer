using Fluxor;
using Fluxor.Blazor.Web.Components;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Services.Diagnostics;
using Hexalith.FrontComposer.Shell.Shortcuts;
using Hexalith.FrontComposer.Shell.State.Navigation;
using Hexalith.FrontComposer.Shell.State.Theme;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.JSInterop;

// Blazor component: awaited tasks must resume on the component's sync context, so ConfigureAwait(false) is the wrong choice here.

namespace Hexalith.FrontComposer.Shell.Components.Layout;

/// <summary>
/// Framework-owned application shell composing Fluent UI v5's <c>FluentLayout</c> into the
/// spec-pinned Header / Navigation / Content / Footer regions (Story 3-1 D3 / D4 / D6 / D20).
/// Adopters' <c>MainLayout.razor</c> collapses to <c>&lt;FrontComposerShell&gt;@Body&lt;/FrontComposerShell&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// <b>Parameter ordering (Story 3-2 D10 / Story 8.3):</b> the original shell parameters keep their
/// metadata order and later additions are append-only: <see cref="HeaderStart"/>, <see cref="HeaderCenter"/>,
/// <see cref="HeaderEnd"/>, <see cref="Navigation"/>, <see cref="Footer"/>, <see cref="ChildContent"/>,
/// <see cref="AppTitle"/>, landmark parameters, then brand/logo parameters. The snapshot test
/// <c>FrontComposerShellParameterSurfaceTests</c> locks this list — any addition must be append-only,
/// no parameter may be removed/renamed/retyped without a major bump.
/// </para>
/// <para>
/// <b>Brand/logo lockup (Story 8.3):</b> <see cref="HeaderLogo"/> renders adopter-supplied logo markup
/// between the header-start slot and the app title. When it is <see langword="null"/> and
/// <see cref="ShowDefaultHeaderLogo"/> is <see langword="false"/> (the default), the shell emits no
/// logo markup so zero-config adopters keep the existing header.
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

    // DN2 — build-symbol flag for dev-mode markup gating in FrontComposerShell.razor. Pairs with
    // the runtime IsDevelopment() gate on AddFrontComposerDevMode() so a Release build with an
    // inadvertent symbol leak still cannot expose the dev-mode shell-header icon or overlay.
#if DEBUG
    private static bool IsDevModeBuild => true;
#else
    private static bool IsDevModeBuild => false;
#endif

    private IJSObjectReference? _beforeUnloadModule;
    private IJSObjectReference? _beforeUnloadSubscription;
    private IJSObjectReference? _keyboardModule;
    private DotNetObjectReference<FrontComposerShell>? _selfRef;
    private bool _themeBootstrapped;
    private bool _interactiveReady;
    private bool _locationTrackingRegistered;
    private readonly object _locationTrackingSync = new();
    private bool _sessionRestoreAttempted;
    private string? _initialRenderUri;
    private ElementReference _shellRoot;
    private readonly LayoutHamburgerCoordinator _hamburgerCoordinator = new();

    // FC-LYT (Story 1.2) — instance-per-shell page-layout coordinator cascaded to @ChildContent so a
    // page's <FcPageLayout> can declare its measure. Mirrors _hamburgerCoordinator (ADR-030 single-writer
    // field, not DI). The shell subscribes to Changed to re-render #fc-main-content's mode attribute/class.
    private readonly FcPageLayoutCoordinator _pageLayoutCoordinator = new();

    // Handoff frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening (outcome 1/2) —
    // instance-per-shell coordinator cascaded to @ChildContent so a page's <FcContentLabel> can name
    // the #fc-main-content `main` landmark by its route heading (no orphaned page-level aria-labelledby).
    // Mirrors _pageLayoutCoordinator. The shell subscribes to Changed to re-render the landmark's
    // aria-label / aria-labelledby.
    private readonly FcContentLabelCoordinator _contentLabelCoordinator = new();

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
    /// Header content rendered AFTER the theme toggle (right-aligned). When <see langword="null"/>
    /// the shell renders its default right-side actions — <c>FcPaletteTriggerButton</c> followed by
    /// <c>FcSettingsButton</c> (Story 3-4 D18). Supply a fragment to replace those defaults.
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
    /// Application title shown in the header. When <see langword="null"/> (default), the shell
    /// resolves <see cref="FcShellOptions.AppTitle"/> and then falls back to
    /// <c>FcShellResources.AppTitle</c> — the framework-owned product name string.
    /// </summary>
    [Parameter] public string? AppTitle { get; set; }

    /// <summary>
    /// Accessible name for the shell's single content <c>main</c> landmark (<c>#fc-main-content</c>),
    /// emitted as <c>aria-label</c> (handoff
    /// <c>frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening</c>, Requested outcome 1).
    /// Prefer <see cref="ContentLabelledBy"/> when the page renders a visible route heading so the
    /// landmark is named by that heading; use <see cref="ContentLabel"/> only when no visible heading
    /// exists. Ignored when <see cref="ContentLabelledBy"/> is set (a labelledby reference wins). When
    /// both are <see langword="null"/> the landmark carries no accessible name and the implicit
    /// "main" label is used, matching the pre-handoff behavior.
    /// </summary>
    [Parameter] public string? ContentLabel { get; set; }

    /// <summary>
    /// Id reference that names the shell's content <c>main</c> landmark (<c>#fc-main-content</c>),
    /// emitted as <c>aria-labelledby</c> (Requested outcome 1). Pass the <c>HeadingId</c> of the
    /// route's <see cref="FcPageHeader"/> so the shell-owned main landmark is named by the visible
    /// page heading <b>without</b> an orphaned page-level <c>aria-labelledby</c> on a non-landmark
    /// wrapper. Takes precedence over <see cref="ContentLabel"/>. When <see langword="null"/> the
    /// landmark is not labelled by reference.
    /// </summary>
    [Parameter] public string? ContentLabelledBy { get; set; }

    /// <summary>
    /// Optional adopter-supplied brand/logo markup rendered between <see cref="HeaderStart"/> (or the
    /// default <see cref="FcHamburgerToggle"/>) and <see cref="AppTitle"/>. When <see langword="null"/>,
    /// the shell emits no logo unless <see cref="ShowDefaultHeaderLogo"/> is explicitly enabled.
    /// </summary>
    [Parameter] public RenderFragment? HeaderLogo { get; set; }

    /// <summary>
    /// Opts into the framework default decorative header logo when <see cref="HeaderLogo"/> is
    /// <see langword="null"/>. The default is <see langword="false"/> so zero-config shells emit no logo.
    /// </summary>
    [Parameter] public bool ShowDefaultHeaderLogo { get; set; }

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

    /// <summary>Injected root service provider used for optional dev-mode-only services.</summary>
    [Inject] private IServiceProvider Services { get; set; } = default!;

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

    /// <summary>Application title resolved from the explicit parameter, settings, then resources.</summary>
    protected string ResolvedAppTitle {
        get {
            if (AppTitle is not null) {
                return AppTitle;
            }

            string? configuredTitle = Options.Value.AppTitle;
            return configuredTitle ?? Localizer["AppTitle"].Value;
        }
    }

    /// <summary>Whether the header should render the optional brand/logo cell.</summary>
    protected bool ShouldRenderHeaderLogo => HeaderLogo is not null || ShowDefaultHeaderLogo;

    /// <summary>
    /// Gap applied only when the optional brand/logo cell exists, preserving the zero-config header spacing.
    /// </summary>
    protected string? HeaderStartHorizontalGap => ShouldRenderHeaderLogo ? "6px" : null;

    /// <summary>Marks only the framework default logo as decorative.</summary>
    protected string? HeaderLogoAriaHidden => HeaderLogo is null && ShowDefaultHeaderLogo ? "true" : null;

    /// <summary>Global stylesheet that owns shell chrome primitives that must apply before scoped CSS is available.</summary>
    protected string ShellStylesheetPath => NavigationManager
        .ToAbsoluteUri("_content/Hexalith.FrontComposer.Shell/css/fc-shell.css")
        .ToString();

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
    /// Global stylesheet for projection empty states (<c>FcProjectionEmptyPlaceholder</c>) and
    /// field placeholders (<c>FcFieldPlaceholder</c>). Loaded globally because the classes render
    /// on Fluent components and generated output where component-scoped selectors would not apply.
    /// </summary>
    protected string EmptyStateStylesheetPath => NavigationManager
        .ToAbsoluteUri("_content/Hexalith.FrontComposer.Shell/css/fc-empty-state.css")
        .ToString();

    /// <summary>
    /// E2E-observable marker that flips only after Blazor has attached interactive event handlers.
    /// </summary>
    protected string InteractiveReadyAttribute => _interactiveReady ? "true" : "false";

    /// <summary>
    /// Whether the shell should render the Navigation area. Adopter-supplied content always wins;
    /// framework auto-navigation appears when at least one manifest has projections OR a domain has
    /// registered explicit navigation entries.
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
            return tier is ViewportTier.Tablet or ViewportTier.Phone;
        }
    }

    /// <summary>
    /// Width of the Navigation area for framework-provided navigation. Story 8.5 renders one rail:
    /// 72 px labeled on expanded Desktop, 48 px icon-only when collapsed or CompactDesktop.
    /// </summary>
    protected string NavigationWidth => Navigation is null && ShouldUseCollapsedRailWidth()
        ? "48px"
        : "72px";

    /// <summary>
    /// FC-LYT (Story 1.2) — the <c>data-fc-page-layout</c> attribute value for <c>#fc-main-content</c>,
    /// driven by the cascaded <see cref="FcPageLayoutCoordinator"/>: <c>"constrained"</c> when a page
    /// declared it via <see cref="FcPageLayout"/>, otherwise the default <c>"full-width"</c>.
    /// </summary>
    protected string PageLayoutAttribute => _pageLayoutCoordinator.Mode == FcPageLayoutMode.Constrained
        ? "constrained"
        : "full-width";

    /// <summary>
    /// FC-LYT (Story 1.2) — the <c>#fc-main-content</c> class list. Always carries the base
    /// <c>fc-page-layout</c> marker; adds <c>fc-page-layout--constrained</c> (the
    /// <c>max-inline-size</c> rule) only when a page declared the constrained measure.
    /// </summary>
    protected string PageLayoutCssClass => _pageLayoutCoordinator.Mode == FcPageLayoutMode.Constrained
        ? "fc-page-layout fc-page-layout--constrained"
        : "fc-page-layout";

    /// <summary>
    /// Handoff <c>frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening</c>
    /// (outcome 1/2) — the resolved <c>aria-labelledby</c> for the <c>#fc-main-content</c> landmark,
    /// or <see langword="null"/>. A page-declared <see cref="FcContentLabel"/> wins over the shell
    /// <see cref="ContentLabelledBy"/> parameter; <c>aria-labelledby</c> wins over <c>aria-label</c>.
    /// Returns <see langword="null"/> when an <c>aria-label</c> is resolved instead, so only one of the
    /// two attributes is ever emitted.
    /// </summary>
    protected string? ContentLabelledByValue {
        get {
            if (_contentLabelCoordinator.HasDeclaration) {
                return _contentLabelCoordinator.LabelledBy;
            }

            return string.IsNullOrWhiteSpace(ContentLabelledBy) ? null : ContentLabelledBy;
        }
    }

    /// <summary>
    /// Handoff <c>frontcomposer-2026-06-19-page-header-landmarks-and-contract-hardening</c>
    /// (outcome 1/2) — the resolved <c>aria-label</c> for the <c>#fc-main-content</c> landmark, or
    /// <see langword="null"/>. Suppressed when an <c>aria-labelledby</c> is in effect
    /// (see <see cref="ContentLabelledByValue"/>) so the two attributes never compete.
    /// </summary>
    protected string? ContentLabelValue {
        get {
            // aria-labelledby always wins; never emit aria-label alongside it.
            if (ContentLabelledByValue is not null) {
                return null;
            }

            if (_contentLabelCoordinator.HasDeclaration) {
                return _contentLabelCoordinator.Label;
            }

            return string.IsNullOrWhiteSpace(ContentLabel) ? null : ContentLabel;
        }
    }

    /// <summary>Development-only customization contract mismatch diagnostics.</summary>
    protected IReadOnlyList<CustomizationDiagnostic> ContractMismatchDiagnostics {
        get {
            if (!IsDevModeBuild) {
                return [];
            }

            ICustomizationContractMismatchDiagnosticProvider? provider =
                Services.GetService<ICustomizationContractMismatchDiagnosticProvider>();
            return provider?.GetDiagnostics() ?? [];
        }
    }

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
    protected override void OnInitialized() {
        base.OnInitialized();
        // FC-LYT (Story 1.2) — re-render #fc-main-content's mode attribute/class when a child
        // <FcPageLayout> flips the coordinator (it registers in its OnAfterRender, after the shell's
        // first paint). SetMode no-ops on an unchanged mode, so this cannot loop the render cycle.
        _pageLayoutCoordinator.Changed += OnPageLayoutChanged;
        // Handoff outcome 1/2 — re-render #fc-main-content's accessible name when a child
        // <FcContentLabel> declares/clears it (same render-cycle safety as the page-layout coordinator).
        _contentLabelCoordinator.Changed += OnContentLabelChanged;
    }

    private void OnPageLayoutChanged() => _ = InvokeAsync(StateHasChanged);

    private void OnContentLabelChanged() => _ = InvokeAsync(StateHasChanged);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender) {
        if (firstRender) {
            _interactiveReady = true;
            _ = InvokeAsync(StateHasChanged);
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

        // FC-LYT (Story 1.2) — drop the coordinator subscription so the shell is not rooted by it.
        _pageLayoutCoordinator.Changed -= OnPageLayoutChanged;
        _contentLabelCoordinator.Changed -= OnContentLabelChanged;

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

        // A domain that declares only explicit navigation entries (e.g. bespoke pages rather than
        // projection list views) still contributes a renderable global menu.
        return Registry.GetNavEntries().Count > 0;
    }

    private bool ShouldUseCollapsedRailWidth() {
        FrontComposerNavigationState snapshot = NavigationState.Value;
        return snapshot.CurrentViewport == ViewportTier.CompactDesktop
            || (snapshot.CurrentViewport == ViewportTier.Desktop && snapshot.SidebarCollapsed);
    }
}
