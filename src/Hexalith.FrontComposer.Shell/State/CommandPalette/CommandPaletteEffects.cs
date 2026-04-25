using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Registration;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Shortcuts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Components.Layout;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Routing;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.CommandPalette;

/// <summary>
/// Async side effects for the command palette: 150 ms debounced fuzzy scoring (D7 / D8 / D9), recent
/// route ring-buffer persistence with fail-closed scope guard (D10), discoverable shortcuts entry +
/// alias canonicalisation (D23), stale-result cancellation (D20), and FullPage-route filtering (D21).
/// </summary>
/// <remarks>
/// Implements <see cref="IDisposable"/> so the per-dispatch <see cref="CancellationTokenSource"/>
/// is disposed when the circuit tears down.
/// </remarks>
public sealed class CommandPaletteEffects : IDisposable {
    private const string FeatureSegment = "palette-recent";
    private const string DirectionHydrate = "hydrate";
    private const string DirectionPersist = "persist";
    private const int DebounceMilliseconds = 150;
    private const int TopResultCap = 50;
    private const int ContextualBonus = 15;

    /// <summary>
    /// Upper bound on how long <see cref="HandleRecentRouteVisited"/> waits for the persist gate
    /// (<see cref="_persistGate"/>) before dropping the payload as stale (Pass-1 P1). 2 seconds is
    /// generous for any reasonable browser-storage round-trip; persist failure is non-fatal — the
    /// next activation queues a fresher payload.
    /// </summary>
    private static readonly TimeSpan PersistGateTimeout = TimeSpan.FromSeconds(2);

    private static readonly string[] _shortcutAliases = ["?", "help", "keys", "kb", "shortcut"];
    private const string ShortcutsCanonicalQuery = "shortcuts";

    /// <summary>Sentinel <c>CommandTypeName</c> for the synthetic "Keyboard Shortcuts" entry (D23).</summary>
    public const string KeyboardShortcutsSentinel = "@shortcuts";

    private readonly IState<FrontComposerNavigationState> _navState;
    private readonly IState<FrontComposerCommandPaletteState> _paletteState;
    private readonly ILogger<CommandPaletteEffects> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeProvider _timeProvider;

    private readonly object _ctsSync = new();
    private readonly SemaphoreSlim _persistGate = new(1, 1);
    private CancellationTokenSource? _queryCts;
    private bool _disposed;

    /// <summary>
    /// Initialises a new instance of the <see cref="CommandPaletteEffects"/> class.
    /// </summary>
    /// <param name="navState">Cross-feature read of the current bounded context for the contextual bonus.</param>
    /// <param name="paletteState">Intra-feature read of the current results / IsOpen / recent buffer.</param>
    /// <param name="logger">Logger for HFC2105 / HFC2110 / HFC2111 diagnostics.</param>
    /// <param name="serviceProvider">Lazy-resolves all non-Fluxor-core dependencies so the effect survives in unit-test fixtures that bypass <c>AddHexalithFrontComposer</c>.</param>
    public CommandPaletteEffects(
        IState<FrontComposerNavigationState> navState,
        IState<FrontComposerCommandPaletteState> paletteState,
        ILogger<CommandPaletteEffects> logger,
        IServiceProvider serviceProvider) {
        ArgumentNullException.ThrowIfNull(navState);
        ArgumentNullException.ThrowIfNull(paletteState);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _navState = navState;
        _paletteState = paletteState;
        _logger = logger;
        _serviceProvider = serviceProvider;
        // P13 (2026-04-21 pass-3): cache the TimeProvider once at construction. The prior per-dispatch
        // GetService<TimeProvider>() resolution could return different providers across the lifetime
        // of a single debounce cycle if the test harness rewired DI mid-flight, and split the chord
        // timer (ShortcutService, ctor-injected) from the debounce timer (effect, previously
        // per-dispatch) on different clocks.
        _timeProvider = serviceProvider.GetService<TimeProvider>() ?? TimeProvider.System;
    }

    private TimeProvider Time => _timeProvider;

    private string NewCorrelationId()
        => TryGetService<IUlidFactory>()?.NewUlid() ?? Guid.NewGuid().ToString("N");

    private string ResolveLocalised(string key, string fallback) {
        IStringLocalizer<FcShellResources>? localizer = TryGetService<IStringLocalizer<FcShellResources>>();
        if (localizer is null) {
            return fallback;
        }

        Microsoft.Extensions.Localization.LocalizedString result = localizer[key];
        return result.ResourceNotFound ? fallback : result.Value;
    }

    // P5 (2026-04-21 pass-3): the scoped IServiceProvider can be disposed mid-effect on circuit
    // teardown — GetService then throws ObjectDisposedException. Previously only the per-manifest
    // scoring loop guarded exceptions; these accessors were naked. Wrap each resolution so effects
    // observe a null dependency (same shape as "unregistered service") instead of blowing up.
    private T? TryGetService<T>() where T : class {
        try {
            return _serviceProvider.GetService<T>();
        }
        catch (ObjectDisposedException) {
            return null;
        }
    }

    private IFrontComposerRegistry? Registry => TryGetService<IFrontComposerRegistry>();

    private IShortcutService? Shortcuts => TryGetService<IShortcutService>();

    private IStorageService? Storage => TryGetService<IStorageService>();

    private IUserContextAccessor? UserContextAccessor => TryGetService<IUserContextAccessor>();

    /// <summary>
    /// Hydrates the recent-route ring buffer from storage on app initialisation. Read-only — does
    /// NOT trigger re-persistence (ADR-038 mirror). Filters tampered URLs through
    /// <see cref="CommandRouteBuilder.IsInternalRoute"/> per D10.
    /// </summary>
    /// <param name="action">The app-initialised action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleAppInitialized(AppInitializedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        await HydrateRecentRoutesAsync(dispatcher).ConfigureAwait(false);
    }

    /// <summary>
    /// Re-runs hydrate on <see cref="Navigation.StorageReadyAction"/> iff the palette hydration
    /// state is still <see cref="CommandPaletteHydrationState.Idle"/> (Story 3-6 D19).
    /// </summary>
    /// <param name="action">The storage-ready action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleStorageReady(Navigation.StorageReadyAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);
        if (_paletteState.Value.HydrationState != CommandPaletteHydrationState.Idle) {
            return;
        }

        await HydrateRecentRoutesAsync(dispatcher).ConfigureAwait(false);
    }

    private async Task HydrateRecentRoutesAsync(IDispatcher dispatcher) {
        if (!TryResolveScope(out string tenantId, out string userId, DirectionHydrate)) {
            return;
        }

        dispatcher.Dispatch(new PaletteHydratingAction());

        IStorageService? storage = Storage;
        if (storage is null) {
            dispatcher.Dispatch(new PaletteHydratedCompletedAction());
            return;
        }

        string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);
        string[]? stored;
        try {
            stored = await storage.GetAsync<string[]>(key).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("Palette hydration cancelled — circuit disposing.");
            dispatcher.Dispatch(new PaletteHydratedCompletedAction());
            return;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException) {
            // P8 (Pass-6): distinguish deserialisation/schema corruption from transient I/O so
            // operators can triage HFC2111 without misdiagnosing a network blip as data corruption.
            // P6 (Pass-1 review): InvalidOperationException is too broad to classify as "Corrupt"
            // — Fluxor's dispatcher and IStorageService implementations both throw IOE on lifecycle
            // / disposal races. Map IOE to "Lifecycle" so operators paging on Reason=Corrupt do not
            // mistriage a circuit-teardown event as a data-corruption incident.
            string reason = ex switch {
                IOException => "TransientFailure",
                TimeoutException => "TransientFailure",
                System.Text.Json.JsonException => "Corrupt",
                FormatException => "Corrupt",
                InvalidOperationException => "Lifecycle",
                _ => "Unknown",
            };
            _logger.LogInformation(
                ex,
                "{DiagnosticId}: Palette hydration errored. Reason={Reason}.",
                FcDiagnosticIds.HFC2111_PaletteHydrationEmpty,
                reason);
            dispatcher.Dispatch(new PaletteHydratedCompletedAction());
            return;
        }

        if (stored is null || stored.Length == 0) {
            _logger.LogInformation(
                "{DiagnosticId}: Palette hydration found no stored value. Reason={Reason}.",
                FcDiagnosticIds.HFC2111_PaletteHydrationEmpty,
                "Empty");
            dispatcher.Dispatch(new PaletteHydratedCompletedAction());
            return;
        }

        // P6 (Pass-6): dedupe identical filtered entries — older code paths or storage corruption
        // could persist duplicates; the reducer dedupes on visit but NOT on hydrate.
        ImmutableArray<string> filtered = [.. stored
            .Where(CommandRouteBuilder.IsInternalRoute)
            .Distinct(StringComparer.OrdinalIgnoreCase)];
        int rejected = stored.Length - filtered.Length;
        if (rejected > 0) {
            _logger.LogInformation(
                "{DiagnosticId}: {RejectedCount} of {TotalCount} palette recent-route entries rejected. Reason={Reason}.",
                FcDiagnosticIds.HFC2111_PaletteHydrationEmpty,
                rejected,
                stored.Length,
                "Tampered");
        }

        // Cap tampered / schema-drifted blobs at RingBufferCap so untrusted storage contents
        // cannot seed an unbounded in-memory buffer.
        if (filtered.Length > FrontComposerCommandPaletteState.RingBufferCap) {
            filtered = [.. filtered.Take(FrontComposerCommandPaletteState.RingBufferCap)];
        }

        if (filtered.Length > 0) {
            dispatcher.Dispatch(new PaletteHydratedAction(filtered));
        }

        dispatcher.Dispatch(new PaletteHydratedCompletedAction());
    }

    /// <summary>
    /// Handles palette open — pre-populates the result list with the synthetic "Keyboard Shortcuts"
    /// entry (D23), recent routes, and a default top-20 projections preview.
    /// </summary>
    /// <param name="action">The palette-opened action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public Task HandlePaletteOpened(PaletteOpenedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        dispatcher.Dispatch(new PaletteResultsComputedAction(string.Empty, BuildDefaultResults()));
        return Task.CompletedTask;
    }

    /// <summary>
    /// Cancels the in-flight debounce when the palette closes (D20 stale-result guard upstream half).
    /// </summary>
    /// <param name="action">The palette-closed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused — required by Fluxor signature).</param>
    /// <returns>A completed task.</returns>
    [EffectMethod]
    public Task HandlePaletteClosed(PaletteClosedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        CancelInFlightQuery();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Debounces 150 ms then either bypasses scoring for the "shortcuts" alias (D23) or runs the
    /// pure scorer + contextual bonus + top-N pass (D7 / D8). Filters unreachable commands via
    /// <see cref="IFrontComposerRegistry.HasFullPageRoute"/> (D21).
    /// </summary>
    /// <param name="action">The query-changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandlePaletteQueryChanged(PaletteQueryChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        CancellationTokenSource cts = ReplaceQueryCts();

        try {
            await Task.Delay(TimeSpan.FromMilliseconds(DebounceMilliseconds), Time, cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            return;
        }

        string canonical = ResolveShortcutAliasQuery(action.Query).Trim();

        if (string.IsNullOrWhiteSpace(canonical)) {
            dispatcher.Dispatch(new PaletteResultsComputedAction(action.Query, BuildDefaultResults()));
            return;
        }

        if (string.Equals(canonical, ShortcutsCanonicalQuery, StringComparison.OrdinalIgnoreCase)) {
            IShortcutService? shortcuts = Shortcuts;
            if (shortcuts is null) {
                // P10 (Pass-6): log when the alias bypass falls through to an empty result set
                // because IShortcutService is unregistered. Adopters who forget the registration
                // saw silent UX degradation (typing "?" produced an empty list with no clue why).
                _logger.LogWarning(
                    "{DiagnosticId}: Palette shortcuts query bypassed scoring but IShortcutService is unregistered; dispatching empty result set.",
                    FcDiagnosticIds.HFC2110_PaletteScoringFault);
                dispatcher.Dispatch(new PaletteResultsComputedAction(action.Query, ImmutableArray<PaletteResult>.Empty));
                return;
            }

            ImmutableArray<PaletteResult> shortcutResults = [.. shortcuts.GetRegistrations()
                .Select(r => new PaletteResult(
                    Category: PaletteResultCategory.Shortcut,
                    DisplayLabel: r.NormalisedLabel,
                    BoundedContext: string.Empty,
                    RouteUrl: null,
                    CommandTypeName: null,
                    Score: 0,
                    IsInCurrentContext: false,
                    ProjectionType: null,
                    DescriptionKey: r.DescriptionKey))];
            dispatcher.Dispatch(new PaletteResultsComputedAction(action.Query, shortcutResults));
            return;
        }

        string? currentContext = _navState.Value.CurrentBoundedContext;
        ImmutableArray<PaletteResult>.Builder scored = ImmutableArray.CreateBuilder<PaletteResult>();
        IFrontComposerRegistry? scoringRegistry = Registry;

        // Per-manifest try/catch so one malformed manifest (e.g., null/empty BoundedContext) does
        // not blank the entire result set. Registry enumeration itself remains outside-guarded.
        IReadOnlyList<DomainManifest> manifests;
        try {
            manifests = (scoringRegistry?.GetManifests() ?? []).ToList();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException) {
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Registry enumeration threw during palette scoring — dispatching empty result set.",
                FcDiagnosticIds.HFC2110_PaletteScoringFault);
            dispatcher.Dispatch(new PaletteResultsComputedAction(action.Query, ImmutableArray<PaletteResult>.Empty));
            return;
        }

        foreach (DomainManifest manifest in manifests) {
            // P5 (Pass-6): narrow the per-manifest catch so fatal exceptions (OOM, StackOverflow,
            // ThreadAbort) propagate. Otherwise a real fatal becomes a LogWarning + the loop
            // continues, masking serious issues as routine HFC2110 noise.
            try {
                if (string.IsNullOrWhiteSpace(manifest.BoundedContext)) {
                    // Skip manifests with null/empty BoundedContext — BuildRoute would throw and any
                    // generated URL would be invalid. One malformed manifest does not blank the rest.
                    continue;
                }

                foreach (string projection in manifest.Projections) {
                    string label = FrontComposerNavigation.ProjectionLabel(projection);
                    int score = PaletteScorer.Score(canonical, label);
                    if (score <= 0) {
                        continue;
                    }

                    bool inContext = IsInCurrentContext(manifest.BoundedContext, currentContext);
                    int finalScore = score + (inContext ? ContextualBonus : 0);
                    scored.Add(CreateProjectionResult(manifest, projection, finalScore, inContext));
                }

                foreach (string command in manifest.Commands) {
                    string label = ShortName(command);
                    int score = PaletteScorer.Score(canonical, label);
                    if (score <= 0) {
                        continue;
                    }

                    if (scoringRegistry is not null && !scoringRegistry.HasFullPageRoute(command)) {
                        continue;
                    }

                    bool inContext = IsInCurrentContext(manifest.BoundedContext, currentContext);
                    int finalScore = score + (inContext ? ContextualBonus : 0);
                    scored.Add(new PaletteResult(
                        Category: PaletteResultCategory.Command,
                        DisplayLabel: label,
                        BoundedContext: manifest.BoundedContext,
                        RouteUrl: null,
                        CommandTypeName: command,
                        Score: finalScore,
                        IsInCurrentContext: inContext));
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException) {
                _logger.LogWarning(
                    ex,
                    "{DiagnosticId}: Manifest '{BoundedContext}' threw during palette scoring — skipping manifest, keeping other results.",
                    FcDiagnosticIds.HFC2110_PaletteScoringFault,
                    manifest.BoundedContext ?? "<unknown>");
            }
        }

        // Recent routes whose URL also scores against the query.
        foreach (string url in _paletteState.Value.RecentRouteUrls) {
            int score = PaletteScorer.Score(canonical, url);
            if (score > 0) {
                scored.Add(new PaletteResult(
                    Category: PaletteResultCategory.Recent,
                    DisplayLabel: url,
                    BoundedContext: string.Empty,
                    RouteUrl: url,
                    CommandTypeName: null,
                    Score: score,
                    IsInCurrentContext: false));
            }
        }

        ImmutableArray<PaletteResult> ranked = [.. scored
            .OrderByDescending(static r => r.Score)
            .Take(TopResultCap)];

        // Final stale-guard: if the palette closed (or the per-dispatch CTS was cancelled) while
        // the synchronous scoring loop ran, drop the dispatch so a torn-down dispatcher doesn't
        // raise `ObjectDisposedException`. Reducer-level `IsOpen` no-op covers the fast path, but
        // explicit check here also prevents the dispatch round-trip cost.
        if (!_paletteState.Value.IsOpen) {
            return;
        }

        try {
            dispatcher.Dispatch(new PaletteResultsComputedAction(action.Query, ranked));
        }
        catch (ObjectDisposedException) {
            // Circuit disposed between stale-guard and dispatch — Fluxor store is gone, safe to ignore.
        }
    }

    private void SafeDispatchClose(IDispatcher dispatcher) {
        try {
            dispatcher.Dispatch(new PaletteClosedAction(NewCorrelationId()));
        }
        catch (ObjectDisposedException) {
            // Circuit torn down — nothing to close.
        }
    }

    /// <summary>
    /// Routes a result activation to navigation OR (for the D23 sentinel) refills the palette with
    /// the shortcut reference view. Always closes the palette + records the recent route on a real
    /// navigation.
    /// </summary>
    /// <param name="action">The result-activated action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A completed task.</returns>
    [EffectMethod]
    public Task HandlePaletteResultActivated(PaletteResultActivatedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        FrontComposerCommandPaletteState snapshot = _paletteState.Value;
        if (action.SelectedIndex < 0 || action.SelectedIndex >= snapshot.Results.Length) {
            return Task.CompletedTask;
        }

        PaletteResult result = snapshot.Results[action.SelectedIndex];

        if (result.Category == PaletteResultCategory.Shortcut && string.IsNullOrEmpty(result.RouteUrl)) {
            return Task.CompletedTask;
        }

        // D23 sentinel — refill instead of navigating.
        if (string.Equals(result.CommandTypeName, KeyboardShortcutsSentinel, StringComparison.Ordinal)) {
            // P2 (Pass-6): IsOpen guard against double-activation race. If the palette already
            // closed (rapid double-click + interleaved PaletteClosedAction), dispatching a fresh
            // PaletteQueryChangedAction would be no-op'd by the reducer's IsOpen guard (P1)
            // anyway, but bailing early avoids the dispatch round-trip.
            if (!snapshot.IsOpen) {
                return Task.CompletedTask;
            }

            dispatcher.Dispatch(new PaletteQueryChangedAction(NewCorrelationId(), ShortcutsCanonicalQuery));
            return Task.CompletedTask;
        }

        string? targetUrl = result.Category switch {
            PaletteResultCategory.Projection or PaletteResultCategory.Recent or PaletteResultCategory.Shortcut => result.RouteUrl,
            // Guard against a Command result with null/empty CommandTypeName or BoundedContext;
            // BuildRoute's ThrowIfNullOrWhiteSpace would otherwise escalate to an uncaught effect
            // exception. Fall through to null targetUrl so the palette still closes cleanly.
            PaletteResultCategory.Command when !string.IsNullOrWhiteSpace(result.BoundedContext) && !string.IsNullOrWhiteSpace(result.CommandTypeName)
                => CommandRouteBuilder.BuildRoute(result.BoundedContext, result.CommandTypeName),
            _ => null,
        };

        if (!string.IsNullOrEmpty(targetUrl)) {
            // DN5 (2026-04-21 pass-3): re-validate Recent-category URLs against the open-redirect
            // filter at activation time. IsInternalRoute otherwise runs only at hydrate; any future
            // code path inserting directly into state (bypassing hydrate) would slip past the D10
            // contract. Cheap defence-in-depth — one predicate call on the happy path.
            // P5 (Pass-1 review): extend the revalidation to Projection and Command categories.
            // BuildRoute's output is only as safe as the manifest's BoundedContext + CommandTypeName;
            // a tampered/malformed manifest with backslashes or absolute schemes can otherwise smuggle
            // a non-internal target through the activation path. Shortcuts are reference rows
            // pre-validated at registration time and skip this gate.
            if (result.Category != PaletteResultCategory.Shortcut && !CommandRouteBuilder.IsInternalRoute(targetUrl)) {
                _logger.LogInformation(
                    "{DiagnosticId}: {Category} activation rejected by internal-route filter. Reason={Reason}.",
                    FcDiagnosticIds.HFC2111_PaletteHydrationEmpty,
                    result.Category,
                    "Tampered");
                SafeDispatchClose(dispatcher);
                return Task.CompletedTask;
            }

            NavigationManager? navigation;
            try {
                navigation = _serviceProvider.GetService<NavigationManager>();
            }
            catch (ObjectDisposedException) {
                // Scoped provider torn down mid-dispatch — circuit is gone, nothing to do.
                return Task.CompletedTask;
            }

            if (navigation is null) {
                // P6 (2026-04-21 pass-3): log when NavigationManager is unresolvable. Prior behaviour
                // silently closed the palette with no navigation and no diagnostic breadcrumb.
                _logger.LogWarning(
                    "{DiagnosticId}: Palette activation resolved no NavigationManager from the service provider; navigation dropped.",
                    FcDiagnosticIds.HFC2110_PaletteScoringFault);
                SafeDispatchClose(dispatcher);
                return Task.CompletedTask;
            }

            // Dispatch close BEFORE navigation so the dispatcher is guaranteed live. Blazor Server
            // `NavigateTo` can synchronously unwind the current render tree; any dispatch afterwards
            // may land on a disposed dispatcher and throw `ObjectDisposedException`.
            SafeDispatchClose(dispatcher);

            try {
                navigation.NavigateTo(targetUrl);
            }
            catch (InvalidOperationException ex) {
                // P6: NavigateTo throws on invalid / forced-external URLs with hostile shapes. Log
                // and let the RecentRouteVisitedAction dispatch below be skipped — the user sees the
                // palette close without navigating, which is the correct behaviour for a rejected URL.
                _logger.LogWarning(
                    ex,
                    "{DiagnosticId}: NavigationManager.NavigateTo refused target URL.",
                    FcDiagnosticIds.HFC2110_PaletteScoringFault);
                return Task.CompletedTask;
            }

            // Shortcut-category rows are reference entries — never record them in the recent-route
            // ring buffer, even when they carry a RouteUrl (e.g., g-h with RouteUrl="/").
            if (result.Category != PaletteResultCategory.Shortcut) {
                try {
                    dispatcher.Dispatch(new RecentRouteVisitedAction(targetUrl));
                }
                catch (ObjectDisposedException) {
                    // Navigation tore down the circuit synchronously; nothing to persist.
                }
            }
        }
        else {
            // P9 (Pass-6): targetUrl is null on either (a) informational shortcut row (already
            // returned earlier in this method); (b) Command result with empty BoundedContext or
            // CommandTypeName that fell through the switch's `when` clause. Differentiate the two
            // so operators can diagnose unreachable-command cases. The shortcut path returned
            // before reaching here, so this else branch is the unreachable-command case.
            _logger.LogInformation(
                "{DiagnosticId}: Palette command activation produced no target URL — Category={Category}, BoundedContext='{BoundedContext}', CommandTypeName='{CommandTypeName}'. Closing palette without navigation.",
                FcDiagnosticIds.HFC2110_PaletteScoringFault,
                result.Category,
                result.BoundedContext ?? "<null>",
                result.CommandTypeName ?? "<null>");
            SafeDispatchClose(dispatcher);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Persists the recent-route ring buffer after a successful activation (Story 3-4 D10).
    /// Fail-closed on missing tenant / user.
    /// </summary>
    /// <param name="action">The recent-route-visited action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused).</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandleRecentRouteVisited(RecentRouteVisitedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        if (!TryResolveScope(out string tenantId, out string userId, DirectionPersist)) {
            return;
        }

        IStorageService? persistStorage = Storage;
        if (persistStorage is null) {
            return;
        }

        // The reducer has already updated state.RecentRouteUrls — read the post-reduce snapshot
        // BEFORE awaiting the persist gate so the snapshot reflects the action that triggered
        // this effect. Without the synchronous read, two rapid activations could swap snapshots
        // before either persist completes.
        string[] payload = [.. _paletteState.Value.RecentRouteUrls];
        string key = StorageKeys.BuildKey(tenantId, userId, FeatureSegment);

        // P7 (Pass-6): serialise persist calls so an older payload cannot overwrite a newer one
        // when storage I/O latency exceeds the inter-action interval. Snapshot is captured above.
        // P1 (Pass-1 review): bound the wait with a timeout so a stuck `SetAsync` does not block
        // every subsequent persist indefinitely. Drop-stale on timeout — the next activation will
        // queue a fresher payload, and the gate-holder either eventually completes (good) or also
        // times out (storage is wedged; logging the timeout here surfaces the wedge to operators).
        bool gateAcquired;
        try {
            gateAcquired = await _persistGate.WaitAsync(PersistGateTimeout).ConfigureAwait(false);
        }
        catch (ObjectDisposedException) {
            // Effect ran after Dispose — semaphore is gone, nothing to do.
            return;
        }

        if (!gateAcquired) {
            _logger.LogInformation(
                "{DiagnosticId}: Palette recent-route persist gate timeout after {TimeoutMs}ms — dropping stale payload (a newer activation will retry).",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                (int)PersistGateTimeout.TotalMilliseconds);
            return;
        }

        try {
            await persistStorage.SetAsync(key, payload).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            _logger.LogDebug("Palette recent-route persist cancelled — circuit disposing.");
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException) {
            _logger.LogInformation(
                ex,
                "{DiagnosticId}: Palette recent-route persistence failed.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped);
        }
        finally {
            try {
                _persistGate.Release();
            }
            catch (ObjectDisposedException) {
                // Disposed mid-await — benign.
            }
        }
    }

    /// <summary>
    /// Read-only no-op test seam for <see cref="PaletteHydratedAction"/> (ADR-038 mirror — hydrate
    /// does NOT re-persist). Intentionally NOT decorated with <c>[EffectMethod]</c>; its existence
    /// anchors <c>CommandPaletteEffectsScopeTests.HydrateDoesNotRePersist</c>, which asserts that
    /// calling this entry point never writes to <see cref="IStorageService"/>.
    /// </summary>
    /// <param name="action">The hydrated action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher (unused).</param>
    /// <returns>A completed task.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822:Mark members as static",
        Justification = "Public instance API contract — symmetry with the persist handlers per ADR-038.")]
    public Task HandlePaletteHydrated(PaletteHydratedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Re-hydrates the palette for a new per-user persistence scope (DN2). The adopter wires this
    /// by dispatching <see cref="PaletteScopeChangedAction"/> when <see cref="IUserContextAccessor"/>
    /// surfaces a tenant/user change inside a live circuit. The reducer clears the ring buffer
    /// ahead of this effect; the effect reads from storage under the new scope's key.
    /// </summary>
    /// <param name="action">The scope-changed action.</param>
    /// <param name="dispatcher">The Fluxor dispatcher.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [EffectMethod]
    public async Task HandlePaletteScopeChanged(PaletteScopeChangedAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        // Cancel any in-flight debounced scoring before re-hydrating under the new scope. Without
        // this, a pending `Task.Delay` from the pre-switch tenant's query could resume and score
        // against the freshly hydrated post-switch state, mixing cross-scope results.
        CancelInFlightQuery();

        // P34 (Pass-6 C2-D4): call the extracted hydrate helper directly instead of dispatching
        // `AppInitializedAction` (which would broadcast to every app-init subscriber — theme,
        // navigation, capability discovery, expanded-row state — far broader than the palette
        // scope-change intent) or invoking the `HandleAppInitialized` effect by method (bypasses
        // Fluxor's middleware/observer pipeline). The shared `HydrateRecentRoutesAsync` is the
        // single source of truth for the hydrate path.
        await HydrateRecentRoutesAsync(dispatcher).ConfigureAwait(false);
    }

    /// <summary>
    /// Canonicalises the user query through the D23 alias table. Public for unit testing.
    /// </summary>
    /// <param name="query">The raw user query.</param>
    /// <returns>
    /// <c>"shortcuts"</c> when the query is a known alias or canonical; otherwise the input as
    /// supplied (raw — no trimming). Callers that need the trimmed shape must apply <c>.Trim()</c>
    /// themselves; the only call site (<see cref="HandlePaletteQueryChanged"/>) already does so.
    /// </returns>
    public static string ResolveShortcutAliasQuery(string query) {
        // P29 (Pass-6): single contract — return the canonical when matched, else return the raw
        // input. The caller still trims; returning trimmed-when-matched + raw-when-not collapses
        // the two-exit ambiguity that previously made readers wonder which branch trims.
        // P9 (Pass-1 review): clarified the XML doc to match the actual return shape on no-match
        // (raw, not trimmed) — the previous "input trimmed" wording was code/doc drift.
        if (string.IsNullOrWhiteSpace(query)) {
            return query;
        }

        string trimmed = query.Trim();
        foreach (string alias in _shortcutAliases) {
            if (string.Equals(trimmed, alias, StringComparison.OrdinalIgnoreCase)) {
                return ShortcutsCanonicalQuery;
            }
        }

        if (string.Equals(trimmed, ShortcutsCanonicalQuery, StringComparison.OrdinalIgnoreCase)) {
            return ShortcutsCanonicalQuery;
        }

        return query;
    }

    private ImmutableArray<PaletteResult> BuildDefaultResults() {
        ImmutableArray<PaletteResult>.Builder builder = ImmutableArray.CreateBuilder<PaletteResult>();
        AddShortcutSentinel(builder);
        AddRecentRoutes(builder);

        IFrontComposerRegistry? registry = Registry;
        IReadOnlyList<DomainManifest> manifests;
        try {
            manifests = (registry?.GetManifests() ?? []).OrderBy(static m => m.Name, StringComparer.Ordinal).ToList();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException) {
            _logger.LogWarning(
                ex,
                "{DiagnosticId}: Registry enumeration failed during palette open — falling back to empty projection preview.",
                FcDiagnosticIds.HFC2110_PaletteScoringFault);
            return builder.ToImmutable();
        }

        // P4 (Pass-6): per-manifest try/catch so a single malformed manifest cannot blank the
        // whole projections preview. Mirrors the pattern in HandlePaletteQueryChanged.
        int projectionCount = 0;
        foreach (DomainManifest manifest in manifests) {
            if (projectionCount >= 20) {
                break;
            }

            try {
                if (string.IsNullOrWhiteSpace(manifest.BoundedContext)) {
                    continue;
                }

                foreach (string projection in manifest.Projections.OrderBy(static p => p, StringComparer.Ordinal)) {
                    if (projectionCount >= 20) {
                        break;
                    }

                    builder.Add(CreateProjectionResult(manifest, projection, 0, isInCurrentContext: false));
                    projectionCount++;
                }
            }
            catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException) {
                _logger.LogWarning(
                    ex,
                    "{DiagnosticId}: Manifest '{BoundedContext}' threw during palette open preview — skipping manifest, keeping other projection rows.",
                    FcDiagnosticIds.HFC2110_PaletteScoringFault,
                    manifest.BoundedContext ?? "<unknown>");
            }
        }

        return builder.ToImmutable();
    }

    private void AddShortcutSentinel(ImmutableArray<PaletteResult>.Builder builder)
        => builder.Add(new PaletteResult(
            Category: PaletteResultCategory.Command,
            DisplayLabel: ResolveLocalised("KeyboardShortcutsCommandLabel", "Keyboard Shortcuts"),
            BoundedContext: string.Empty,
            RouteUrl: null,
            CommandTypeName: KeyboardShortcutsSentinel,
            Score: 1000,
            IsInCurrentContext: false,
            ProjectionType: null,
            DescriptionKey: "KeyboardShortcutsCommandDescription"));

    private void AddRecentRoutes(ImmutableArray<PaletteResult>.Builder builder) {
        foreach (string url in _paletteState.Value.RecentRouteUrls) {
            builder.Add(new PaletteResult(
                Category: PaletteResultCategory.Recent,
                DisplayLabel: url,
                BoundedContext: string.Empty,
                RouteUrl: url,
                CommandTypeName: null,
                Score: 0,
                IsInCurrentContext: false));
        }
    }

    private static PaletteResult CreateProjectionResult(DomainManifest manifest, string projection, int score, bool isInCurrentContext) {
        string label = FrontComposerNavigation.ProjectionLabel(projection);
        return new PaletteResult(
            Category: PaletteResultCategory.Projection,
            DisplayLabel: label,
            BoundedContext: manifest.BoundedContext,
            RouteUrl: FrontComposerNavigation.BuildRoute(manifest.BoundedContext, projection),
            CommandTypeName: null,
            Score: score,
            IsInCurrentContext: isInCurrentContext,
            ProjectionType: ProjectionTypeResolver.Resolve(projection));
    }

    private static bool IsInCurrentContext(string? boundedContext, string? currentContext)
        => !string.IsNullOrWhiteSpace(currentContext)
            && !string.IsNullOrWhiteSpace(boundedContext)
            && string.Equals(boundedContext, currentContext, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        CancelInFlightQuery();
        _persistGate.Dispose();
    }

    private CancellationTokenSource ReplaceQueryCts() {
        lock (_ctsSync) {
            CancellationTokenSource? previous = _queryCts;
            try {
                previous?.Cancel();
            }
            catch (ObjectDisposedException) {
                // Previous CTS already disposed — ignore.
            }

            previous?.Dispose();
            // P15 (Pass-6): when Dispose has run, return an already-cancelled CTS so the caller's
            // `await Task.Delay(_, _, cts.Token)` throws `OperationCanceledException` and unwinds
            // cleanly. Allocating a fresh active CTS post-dispose would leak (no consumer to dispose
            // it; subsequent CancelInFlightQuery is gated by Dispose's `_disposed` flag).
            if (_disposed) {
                CancellationTokenSource cancelled = new();
                cancelled.Cancel();
                _queryCts = null;
                return cancelled;
            }

            CancellationTokenSource fresh = new();
            _queryCts = fresh;
            return fresh;
        }
    }

    private void CancelInFlightQuery() {
        lock (_ctsSync) {
            CancellationTokenSource? cts = _queryCts;
            if (cts is null) {
                return;
            }

            _queryCts = null;
            try {
                cts.Cancel();
            }
            catch (ObjectDisposedException) {
                // Already disposed by a racing thread — ignore.
            }

            cts.Dispose();
        }
    }

    private bool TryResolveScope(out string tenantId, out string userId, string direction) {
        IUserContextAccessor? accessor = UserContextAccessor;
        // P16 (Pass-6): accessor property getters may throw (claims principal disposed, JWT parse
        // error in adopter implementation). Wrap reads so the fail-closed branch fires consistently
        // with HFC2105 instead of bubbling an unhandled effect exception to Fluxor's pipeline.
        string? rawTenant;
        string? rawUser;
        try {
            rawTenant = accessor?.TenantId;
            rawUser = accessor?.UserId;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException) {
            _logger.LogInformation(
                ex,
                "{DiagnosticId}: Palette {Direction} skipped — IUserContextAccessor threw on TenantId/UserId access. Reason=AccessorThrew.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                direction);
            tenantId = string.Empty;
            userId = string.Empty;
            return false;
        }

        if (string.IsNullOrWhiteSpace(rawTenant) || string.IsNullOrWhiteSpace(rawUser)) {
            _logger.LogInformation(
                "{DiagnosticId}: Palette {Direction} skipped — null/empty/whitespace tenant or user context.",
                FcDiagnosticIds.HFC2105_StoragePersistenceSkipped,
                direction);
            tenantId = string.Empty;
            userId = string.Empty;
            return false;
        }

        tenantId = rawTenant;
        userId = rawUser;
        return true;
    }

    private static string ShortName(string fullyQualifiedTypeName) {
        if (string.IsNullOrEmpty(fullyQualifiedTypeName)) {
            return fullyQualifiedTypeName;
        }

        int lastDot = fullyQualifiedTypeName.LastIndexOf('.');
        return lastDot >= 0 && lastDot + 1 < fullyQualifiedTypeName.Length
            ? fullyQualifiedTypeName[(lastDot + 1)..]
            : fullyQualifiedTypeName;
    }
}
