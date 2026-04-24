using System.Threading.Tasks;

using Fluxor;

using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Logging;

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
    private readonly TimeProvider _timeProvider;

    /// <summary>Initializes a new instance of the <see cref="LoadPageEffects"/> class.</summary>
    /// <param name="state">Read-only <see cref="LoadedPageState"/> for the defensive-finally guard.</param>
    /// <param name="loader">Non-generic projection page loader (Story 4-4 D3 / D16 boundary).</param>
    /// <param name="logger">Logger for the defensive-finally breadcrumb.</param>
    /// <param name="timeProvider">Time source for deterministic elapsed measurement.</param>
    public LoadPageEffects(
        IState<LoadedPageState> state,
        IProjectionPageLoader loader,
        ILogger<LoadPageEffects> logger,
        TimeProvider? timeProvider = null) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(logger);
        _state = state;
        _loader = loader;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
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
            ctr = action.CancellationToken.Register(() =>
                dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip)));

            string projectionTypeFqn = ExtractProjectionTypeFqn(action.ViewKey);
            long startTicks = _timeProvider.GetTimestamp();
            ProjectionPageResult result = await _loader.LoadPageAsync(
                projectionTypeFqn,
                action.Skip,
                action.Take,
                action.Filters,
                action.SortColumn,
                action.SortDescending,
                action.SearchQuery,
                action.CancellationToken).ConfigureAwait(false);
            long elapsedMs = (long)_timeProvider.GetElapsedTime(startTicks).TotalMilliseconds;

            dispatcher.Dispatch(new LoadPageSucceededAction(
                viewKey: action.ViewKey,
                skip: action.Skip,
                items: result.Items,
                totalCount: result.TotalCount,
                elapsedMs: elapsedMs));
        }
        catch (OperationCanceledException) {
            dispatcher.Dispatch(new LoadPageCancelledAction(action.ViewKey, action.Skip));
        }
        catch (Exception ex) {
            dispatcher.Dispatch(new LoadPageFailedAction(action.ViewKey, action.Skip, ex.Message));
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
                        errorMessage: "effect exited without terminal dispatch"));
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
}
