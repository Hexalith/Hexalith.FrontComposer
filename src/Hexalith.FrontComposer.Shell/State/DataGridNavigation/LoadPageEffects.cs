using System.Collections.Immutable;
using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Resources;

using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// Story 4-4 D3 — handles <see cref="LoadPageAction"/>. TCS lifecycle is owned by the
/// reducer (which stores the action-supplied TCS in <see cref="LoadedPageState.PendingCompletionsByKey"/>
/// and resolves it on success / failure / cancel). The effect itself does only the IO:
/// resolve the loader, invoke it with the action's filter / sort / search, measure elapsed,
/// and dispatch the terminal action. The <c>finally</c> clause defensively drains any
/// orphan TCS that survived an exception between the reducer and the terminal dispatch.
/// </summary>
public sealed class LoadPageEffects {
    private readonly IState<LoadedPageState> _state;
    private readonly IProjectionPageLoader _loader;
    private readonly ILogger<LoadPageEffects> _logger;
    private readonly IOptionsMonitor<FcShellOptions> _options;
    private readonly TimeProvider _timeProvider;
    private readonly IStringLocalizer<FcShellResources>? _localizer;

    /// <summary>Initializes a new instance of the <see cref="LoadPageEffects"/> class.</summary>
    /// <param name="state">Read-only <see cref="LoadedPageState"/> for the defensive-finally guard.</param>
    /// <param name="loader">Non-generic projection page loader (Story 4-4 D3 / D16 boundary).</param>
    /// <param name="logger">Logger for the defensive-finally breadcrumb.</param>
    /// <param name="options">Shell options monitor for virtualization caps.</param>
    /// <param name="timeProvider">Time source for deterministic elapsed measurement.</param>
    /// <param name="localizer">Optional shell-resources localizer; when present, schema-mismatch user copy is resolved through it.</param>
    public LoadPageEffects(
        IState<LoadedPageState> state,
        IProjectionPageLoader loader,
        ILogger<LoadPageEffects> logger,
        IOptionsMonitor<FcShellOptions> options,
        TimeProvider? timeProvider = null,
        IStringLocalizer<FcShellResources>? localizer = null) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(options);
        _state = state;
        _loader = loader;
        _logger = logger;
        _options = options;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _localizer = localizer;
    }

    /// <summary>Handles <see cref="LoadPageAction"/> per Story 4-4 D3 re-revised.</summary>
    [EffectMethod]
    public async Task HandleLoadPageAsync(LoadPageAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        CancellationTokenRegistration ctr = default;

        try {
            // Route cancellation through the Fluxor pipeline — the reducer handles TCS cleanup.
            ctr = action.CancellationToken.Register(() => {
                try {
                    dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip, action.Completion));
                }
                catch (ObjectDisposedException) {
                    // Store is tearing down; ClearPendingPagesAction bounds the pending TCS lifetime.
                }
            });

            string projectionTypeFqn = ExtractProjectionTypeFqn(action.ViewKey);
            bool hasRealFilter = HasRealFilter(action.Filters);
            int maxUnfilteredItems = Math.Max(0, _options.CurrentValue.MaxUnfilteredItems);
            int take = ResolveTake(action, hasRealFilter, maxUnfilteredItems);
            if (take <= 0) {
                dispatcher.Dispatch(new LoadPageSucceededAction(
                    viewKey: action.ViewKey,
                    skip: action.Skip,
                    items: Array.Empty<object>(),
                    totalCount: maxUnfilteredItems,
                    elapsedMs: 0,
                    completion: action.Completion));
                return;
            }

            long startTicks = _timeProvider.GetTimestamp();
            ProjectionPageResult result = await _loader.LoadPageAsync(
                projectionTypeFqn,
                action.Skip,
                take,
                action.Filters,
                action.SortColumn,
                action.SortDescending,
                action.SearchQuery,
                action.CancellationToken).ConfigureAwait(false);
            long elapsedMs = (long)_timeProvider.GetElapsedTime(startTicks).TotalMilliseconds;

            // Story 5-2 D4 / AC4 — 304 Not Modified takes the explicit no-change path. The
            // reducer resolves the TCS from cached items WITHOUT state mutation so the
            // DataGrid emits no loading flash / synthetic success / badge animation.
            if (result.IsNotModified) {
                dispatcher.Dispatch(new LoadPageNotModifiedAction(
                    viewKey: action.ViewKey,
                    skip: action.Skip,
                    cachedItems: result.Items,
                    completion: action.Completion));
                return;
            }

            int totalCount = hasRealFilter
                ? result.TotalCount
                : Math.Min(result.TotalCount, maxUnfilteredItems);

            dispatcher.Dispatch(new LoadPageSucceededAction(
                viewKey: action.ViewKey,
                skip: action.Skip,
                items: result.Items,
                totalCount: totalCount,
                elapsedMs: elapsedMs,
                completion: action.Completion));
        }
        catch (OperationCanceledException) {
            dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip, action.Completion));
        }
        catch (ProjectionSchemaMismatchException ex) {
            // P36 — resolve the user-visible copy through IStringLocalizer when registered so
            // French-locale users see the translated `SectionUpdatingText` instead of the
            // English literal. The localizer is optional: hosts that have not opted in to
            // shell localization still get a sensible English fallback.
            string sectionUpdatingCopy = _localizer is null
                ? "This section is being updated"
                : _localizer["SectionUpdatingText"];
            // P37 — structured Warning log on the schema-mismatch path. Diagnostic is bounded
            // to redacted projection type + exception type — no payload bodies, no stack-traces
            // (the EventStore client already logged the FailureCategory at the wire boundary).
            _logger.LogWarning(
                "Projection load failed schema check. ProjectionType={ProjectionType}, FailureCategory={FailureCategory}",
                ex.ProjectionType,
                ex.GetType().Name);
            dispatcher.Dispatch(new LoadPageFailedAction(action.ViewKey, action.Skip, sectionUpdatingCopy, action.Completion));
        }
        catch (Exception ex) {
            string message = string.IsNullOrWhiteSpace(ex.Message) ? ex.GetType().Name : ex.Message;
            dispatcher.Dispatch(new LoadPageFailedAction(action.ViewKey, action.Skip, message, action.Completion));
        }
        finally {
            ctr.Dispose();

            // Defensive guarantee-terminal-dispatch per D3 re-revised. If the TCS is still in
            // PendingCompletionsByKey the effect somehow exited without a terminal action.
            // Wrap the dispatch so a store-disposal ObjectDisposedException doesn't re-propagate.
            if (_state.Value.PendingCompletionsByKey.ContainsKey(key)) {
                try {
                    dispatcher.Dispatch(new LoadPageFailedAction(
                        viewKey: action.ViewKey,
                        skip: action.Skip,
                        errorMessage: "effect exited without terminal dispatch",
                        completion: action.Completion));
                }
                catch (Exception defensiveEx) {
                    _logger.LogWarning(
                        defensiveEx,
                        "Defensive terminal dispatch failed during finally — TCS may orphan. viewKey={ViewKey}, skip={Skip}",
                        action.ViewKey,
                        action.Skip);
                }
            }
        }
    }

    private static string ExtractProjectionTypeFqn(string viewKey) {
        int separator = viewKey.IndexOf(':');
        return separator > 0 && separator < viewKey.Length - 1
            ? viewKey[(separator + 1)..]
            : viewKey;
    }

    private static int ResolveTake(LoadPageAction action, bool hasRealFilter, int maxUnfilteredItems) {
        if (hasRealFilter) {
            return action.Take;
        }

        int remaining = maxUnfilteredItems - action.Skip;
        return remaining <= 0 ? 0 : Math.Min(action.Take, remaining);
    }

    private static bool HasRealFilter(IImmutableDictionary<string, string> filters) {
        foreach (KeyValuePair<string, string> filter in filters) {
            if (!filter.Key.StartsWith("__", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(filter.Value)) {
                return true;
            }
        }

        return false;
    }
}
