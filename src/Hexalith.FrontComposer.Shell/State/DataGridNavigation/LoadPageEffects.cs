using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Resources;
using Hexalith.FrontComposer.Shell.Services;

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
    private readonly IUserContextAccessor? _userContextAccessor;

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
        IStringLocalizer<FcShellResources>? localizer = null)
        : this(state, loader, logger, options, timeProvider, localizer, null) {
    }

    /// <summary>Initializes an effect with authenticated page-origin validation.</summary>
    /// <param name="state">Read-only loaded-page state.</param>
    /// <param name="loader">Projection page loader.</param>
    /// <param name="logger">Effect logger.</param>
    /// <param name="options">Shell virtualization options.</param>
    /// <param name="timeProvider">Optional time source for elapsed measurement.</param>
    /// <param name="localizer">Optional shell-resources localizer.</param>
    /// <param name="userContextAccessor">Authenticated tenant and user context.</param>
    public LoadPageEffects(
        IState<LoadedPageState> state,
        IProjectionPageLoader loader,
        ILogger<LoadPageEffects> logger,
        IOptionsMonitor<FcShellOptions> options,
        TimeProvider? timeProvider,
        IStringLocalizer<FcShellResources>? localizer,
        IUserContextAccessor? userContextAccessor) {
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
        _userContextAccessor = userContextAccessor;
    }

    /// <summary>Handles <see cref="LoadPageAction"/> per Story 4-4 D3 re-revised.</summary>
    [EffectMethod]
    public async Task HandleLoadPageAsync(LoadPageAction action, IDispatcher dispatcher) {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(dispatcher);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        CancellationTokenRegistration ctr = default;

        try {
            if (!IsCurrentOrigin(action)) {
                dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip, action.Completion));
                return;
            }
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
            bool hasRealFilter = HasRealFilter(action.Filters) || !string.IsNullOrWhiteSpace(action.SearchQuery);
            int maxUnfilteredItems = Math.Max(0, _options.CurrentValue.MaxUnfilteredItems);
            int take = ResolveTake(action, hasRealFilter, maxUnfilteredItems);
            if (take <= 0) {
                dispatcher.Dispatch(new LoadPageSucceededAction(
                    viewKey: action.ViewKey,
                    skip: action.Skip,
                    items: Array.Empty<object>(),
                    totalCount: maxUnfilteredItems,
                    elapsedMs: 0,
                    completion: action.Completion) { RequestIdentity = action.RequestIdentity, RequestGeneration = action.RequestGeneration, OperatorInitiated = action.OperatorInitiated, Take = action.Take, OriginScopeGeneration = action.OriginScopeGeneration });
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

            if (!IsCurrentOrigin(action)) {
                dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip, action.Completion));
                return;
            }

            // Story 5-2 D4 / AC4 — 304 Not Modified takes the explicit no-change path. The
            // reducer resolves the TCS from cached items WITHOUT state mutation so the
            // DataGrid emits no loading flash / synthetic success / badge animation.
            if (result.IsNotModified) {
                dispatcher.Dispatch(new LoadPageNotModifiedAction(
                    viewKey: action.ViewKey,
                    skip: action.Skip,
                    cachedItems: result.Items,
                    completion: action.Completion) { RequestIdentity = action.RequestIdentity, RequestGeneration = action.RequestGeneration, OriginScopeGeneration = action.OriginScopeGeneration, TotalCount = result.TotalCount, MaxCachedPages = _options.CurrentValue.MaxCachedPages });
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
                completion: action.Completion) { RequestIdentity = action.RequestIdentity, RequestGeneration = action.RequestGeneration, OperatorInitiated = action.OperatorInitiated, Take = action.Take, OriginScopeGeneration = action.OriginScopeGeneration });
        }
        catch (OperationCanceledException) {
            dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip, action.Completion));
        }
        catch (ProjectionSchemaMismatchException ex) {
            if (!IsCurrentOrigin(action)) {
                dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip, action.Completion));
                return;
            }
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
            FrontComposerWarningLog.ProjectionLoadSchemaFailed(
                _logger,
                ex.ProjectionType,
                ex.GetType().Name);
            dispatcher.Dispatch(new LoadPageFailedAction(action.ViewKey, action.Skip, sectionUpdatingCopy, action.Completion) { RequestIdentity = action.RequestIdentity, RequestGeneration = action.RequestGeneration, Take = action.Take, OriginScopeGeneration = action.OriginScopeGeneration });
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            if (!IsCurrentOrigin(action)) {
                dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip, action.Completion));
                return;
            }
            // Transport exception text can contain endpoint, tenant, or response fragments.
            string message = _localizer is null ? "Data could not be loaded." : _localizer["Am30QueryFailed"];
            dispatcher.Dispatch(new LoadPageFailedAction(action.ViewKey, action.Skip, message, action.Completion) { RequestIdentity = action.RequestIdentity, RequestGeneration = action.RequestGeneration, Take = action.Take, OriginScopeGeneration = action.OriginScopeGeneration });
        }
        finally {
            ctr.Dispose();

            // Defensive guarantee-terminal-dispatch per D3 re-revised. If the TCS is still in
            // PendingCompletionsByKey the effect somehow exited without a terminal action.
            // Wrap the dispatch so a store-disposal ObjectDisposedException doesn't re-propagate.
            bool ownsPending = _state.Value.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? primary)
                && (ReferenceEquals(primary, action.Completion)
                    || _state.Value.AdditionalPendingCompletionsByKey.TryGetValue(key, out var additional)
                        && additional.Contains(action.Completion));
            if (ownsPending) {
                try {
                    if (!IsCurrentOrigin(action)) {
                        dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip, action.Completion));
                    }
                    else {
                        dispatcher.Dispatch(new LoadPageFailedAction(
                            viewKey: action.ViewKey,
                            skip: action.Skip,
                            errorMessage: _localizer is null ? "Data could not be loaded." : _localizer["Am30QueryFailed"].Value,
                            completion: action.Completion) { RequestIdentity = action.RequestIdentity, RequestGeneration = action.RequestGeneration, Take = action.Take, OriginScopeGeneration = action.OriginScopeGeneration });
                    }
                }
                catch (Exception defensiveEx) when (!ExceptionGuard.IsFatal(defensiveEx)) {
                    FrontComposerWarningLog.ProjectionLoadTerminalDispatchFailed(
                        _logger,
                        action.ViewKey,
                        action.Skip,
                        defensiveEx);
                }
            }
        }
    }

    private bool IsCurrentOrigin(LoadPageAction action) {
        if (action.OriginScopeGeneration is { } generation && _state.Value.ScopeGeneration != generation) {
            return false;
        }
        if (action.OriginTenantId is null && action.OriginUserId is null) {
            return true;
        }
        if (string.IsNullOrWhiteSpace(action.OriginTenantId)
            || string.IsNullOrWhiteSpace(action.OriginUserId)
            || _userContextAccessor is null) {
            return false;
        }
        try {
            return string.Equals(_userContextAccessor.TenantId, action.OriginTenantId, StringComparison.Ordinal)
                && string.Equals(_userContextAccessor.UserId, action.OriginUserId, StringComparison.Ordinal);
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            return false;
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
            if ((!filter.Key.StartsWith("__", StringComparison.Ordinal)
                    || string.Equals(filter.Key, ReservedFilterKeys.StatusKey, StringComparison.Ordinal))
                && !string.IsNullOrWhiteSpace(filter.Value)) {
                return true;
            }
        }

        return false;
    }
}
