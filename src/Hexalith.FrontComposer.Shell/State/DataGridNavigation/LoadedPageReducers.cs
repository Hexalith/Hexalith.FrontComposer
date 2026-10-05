using System.Collections.Immutable;

using Fluxor;

using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.Navigation;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.DataGridNavigation;

/// <summary>
/// PURE reducers for <see cref="LoadedPageState"/> (Story 4-4 D3 / D4 / D6 / D7 / D10 / D16).
/// Reducers NEVER chain dispatches — persistence effects listen separately. TCS resolution
/// always goes through <c>TrySet*</c> variants so rapid-scroll races are absorbed silently.
/// </summary>
/// <remarks>
/// The reducer class is non-static because <see cref="FcShellOptions.MaxCachedPages"/> and the
/// logger must be injected — Fluxor supports DI-backed reducer classes via its reducer discovery.
/// </remarks>
public sealed class LoadedPageReducers {
    /// <summary>Cancels pending providers and removes every page from the previous scope.</summary>
    [ReducerMethod]
    public static LoadedPageState ReduceScopeChanged(LoadedPageState state, ScopeChangedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        foreach (TaskCompletionSource<object> completion in state.PendingCompletionsByKey.Values) {
            _ = completion.TrySetCanceled();
        }

        return new LoadedPageState { ScopeGeneration = state.ScopeGeneration + 1 };
    }
    private readonly IOptionsMonitor<FcShellOptions> _options;
    private readonly ILogger<LoadedPageReducers> _logger;

    /// <summary>Initializes a new instance of the <see cref="LoadedPageReducers"/> class.</summary>
    /// <param name="options">Shell options monitor; <c>MaxCachedPages</c> governs FIFO eviction (Story 4-4 D10).</param>
    /// <param name="logger">Logger for the Information-level eviction breadcrumb.</param>
    public LoadedPageReducers(
        IOptionsMonitor<FcShellOptions> options,
        ILogger<LoadedPageReducers> logger) {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Registers the provider-supplied TCS in <see cref="LoadedPageState.PendingCompletionsByKey"/>
    /// and latches the server-side virtualization lane for the view key on first dispatch
    /// (Story 4-4 D2 / D3 double-registration idempotency).
    /// </summary>
    /// <remarks>
    /// Double-registration idempotency: if an entry already exists for <c>(viewKey, skip)</c>,
    /// <c>TrySetCanceled</c> is called on the existing TCS before it is replaced — preventing
    /// silent orphan of the first TCS under rapid-scroll-and-bounce.
    /// </remarks>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPage(LoadedPageState state, LoadPageAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        long priorGeneration = state.ActiveRequestGenerationByView.TryGetValue(action.ViewKey, out long activeGeneration)
            ? activeGeneration : 0;
        if (action.RequestGeneration is { } requestedGeneration && requestedGeneration < priorGeneration) {
            _ = action.Completion.TrySetCanceled();
            return state;
        }
        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> nextPending = state.PendingCompletionsByKey;
        bool changedRequest = state.ActiveRequestIdentityByView.TryGetValue(action.ViewKey, out string? priorIdentity)
            && !string.Equals(priorIdentity, action.RequestIdentity, StringComparison.Ordinal)
            || action.RequestGeneration is { } generation && generation > priorGeneration;
        long nextGeneration = action.RequestGeneration ?? (changedRequest ? priorGeneration + 1 : Math.Max(1, priorGeneration));
        if (changedRequest) {
            foreach (KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>> pending in nextPending) {
                if (string.Equals(pending.Key.ViewKey, action.ViewKey, StringComparison.Ordinal)) {
                    _ = pending.Value.TrySetCanceled();
                    nextPending = nextPending.Remove(pending.Key);
                }
            }
        }

        if (nextPending.TryGetValue(key, out TaskCompletionSource<object>? existing)
            && !ReferenceEquals(existing, action.Completion)) {
            _ = existing.TrySetCanceled();
            nextPending = nextPending.SetItem(key, action.Completion);
        }
        else if (!nextPending.ContainsKey(key)) {
            nextPending = nextPending.SetItem(key, action.Completion);
        }

        ImmutableDictionary<string, VirtualizationLane> nextLane = state.LaneByKey.ContainsKey(action.ViewKey)
            ? state.LaneByKey
            : state.LaneByKey.SetItem(action.ViewKey, VirtualizationLane.ServerSide);

        if (ReferenceEquals(nextPending, state.PendingCompletionsByKey)
            && ReferenceEquals(nextLane, state.LaneByKey)
            && !changedRequest
            && state.ActiveRequestIdentityByView.ContainsKey(action.ViewKey)) {
            return state;
        }

        return state with {
            PendingCompletionsByKey = nextPending,
            PendingStartedAtByKey = (changedRequest
                ? state.PendingStartedAtByKey.RemoveRange(state.PendingStartedAtByKey.Keys.Where(entry => string.Equals(entry.ViewKey, action.ViewKey, StringComparison.Ordinal)))
                : state.PendingStartedAtByKey).SetItem(key, action.RegisteredAt ?? DateTimeOffset.UtcNow),
            LaneByKey = nextLane,
            ActiveRequestIdentityByView = state.ActiveRequestIdentityByView.SetItem(action.ViewKey, action.RequestIdentity),
            ActiveRequestGenerationByView = state.ActiveRequestGenerationByView.SetItem(action.ViewKey, nextGeneration),
            ResultsByPage = changedRequest
                ? state.ResultsByPage.RemoveRange(state.ResultsByPage.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal)))
                : state.ResultsByPage,
            FailureByKey = changedRequest ? state.FailureByKey.Remove(action.ViewKey) : state.FailureByKey,
            LastResultByKey = changedRequest ? state.LastResultByKey.Remove(action.ViewKey) : state.LastResultByKey,
            LastSuccessfulPrimaryByView = changedRequest ? state.LastSuccessfulPrimaryByView.Remove(action.ViewKey) : state.LastSuccessfulPrimaryByView,
            PagesByKey = changedRequest
                ? state.PagesByKey.RemoveRange(state.PagesByKey.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal)))
                : state.PagesByKey,
            PageInsertionOrder = changedRequest
                ? ImmutableQueue.CreateRange(state.PageInsertionOrder.Where(key => !string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal)))
                : state.PageInsertionOrder,
            TotalCountByKey = changedRequest ? state.TotalCountByKey.Remove(action.ViewKey) : state.TotalCountByKey,
            LastElapsedMsByKey = changedRequest ? state.LastElapsedMsByKey.Remove(action.ViewKey) : state.LastElapsedMsByKey,
        };
    }

    /// <summary>
    /// Writes the loaded page into <see cref="LoadedPageState.PagesByKey"/>, updates
    /// <see cref="LoadedPageState.TotalCountByKey"/> + <see cref="LoadedPageState.LastElapsedMsByKey"/>,
    /// enqueues the insertion-order token, resolves the matching TCS via <c>TrySetResult</c>,
    /// and evicts the oldest entry when <c>MaxCachedPages</c> is exceeded.
    /// </summary>
    /// <remarks>
    /// <b>Null-items guard (D3 chaos-monkey):</b> a null <c>Items</c> payload is treated as a
    /// failure — the TCS receives <c>TrySetException</c>, the entry is removed, and the log
    /// warning is emitted. <see cref="LoadedPageState.PagesByKey"/> is untouched.
    /// </remarks>
    [ReducerMethod]
    public LoadedPageState ReduceLoadPageSucceeded(LoadedPageState state, LoadPageSucceededAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        if (action.OriginScopeGeneration is { } origin && origin != state.ScopeGeneration) {
            return state;
        }

        if (IsStaleRequest(state, action.ViewKey, action.RequestIdentity, action.RequestGeneration)) {
            return state;
        }

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        if (action.Completion is not null) {
            if (!state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? pending)
                || !ReferenceEquals(pending, action.Completion)) {
                return state;
            }
        }

        if (action.Items is null) {
            FrontComposerWarningLog.LoadedPageNullItems(_logger, action.ViewKey, action.Skip);

            if (state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? nullTcs)) {
                _ = nullTcs.TrySetException(new InvalidOperationException("Data could not be loaded."));
                LoadedPageResult failed = new(
                    NextResultIdentity(state, action.ViewKey), action.RequestIdentity, 0, true,
                    action.OperatorInitiated, action.Skip);
                return state with {
                    PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
                    PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
                    FailureByKey = state.FailureByKey.SetItem(action.ViewKey, "Am30QueryFailed"),
                    LastResultByKey = state.LastResultByKey.SetItem(action.ViewKey, failed),
                    ResultsByPage = TrimResultMetadata(state.ResultsByPage.SetItem(key, failed), state.ResultMetadataLimit),
                    ResultSequenceByView = state.ResultSequenceByView.SetItem(action.ViewKey, failed.Identity),
                };
            }

            return state;
        }

        bool refreshedExistingPage = state.PagesByKey.ContainsKey(key);
        ImmutableDictionary<(string, int), IReadOnlyList<object>> nextPages =
            state.PagesByKey.SetItem(key, action.Items);
        ImmutableDictionary<string, int> nextTotal = state.TotalCountByKey.SetItem(action.ViewKey, action.TotalCount);
        ImmutableDictionary<string, long> nextElapsed = state.LastElapsedMsByKey.SetItem(action.ViewKey, action.ElapsedMs);
        ImmutableQueue<(string ViewKey, int Skip)> nextOrder = refreshedExistingPage
            ? state.PageInsertionOrder
            : state.PageInsertionOrder.Enqueue(key);
        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> nextPending = state.PendingCompletionsByKey;

        if (nextPending.TryGetValue(key, out TaskCompletionSource<object>? tcs)) {
            _ = tcs.TrySetResult(action.Items);
            nextPending = nextPending.Remove(key);
        }

        int cap = _options.CurrentValue.MaxCachedPages;
        while (nextPages.Count > cap && !nextOrder.IsEmpty) {
            nextOrder = nextOrder.Dequeue(out (string ViewKey, int Skip) evicted);
            if (!nextPages.ContainsKey(evicted)) {
                continue;
            }

            nextPages = nextPages.Remove(evicted);
            FrontComposerDiagnosticLog.LoadedPageEvicted(
                _logger,
                cap,
                evicted.ViewKey,
                evicted.Skip);
        }

        LoadedPageResult completed = new(
            NextResultIdentity(state, action.ViewKey), action.RequestIdentity,
            action.TotalCount, false, action.OperatorInitiated, action.Skip);
        ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> nextResults = state.ResultsByPage.SetItem(key, completed);
        nextResults = nextResults.RemoveRange(nextResults
            .Where(entry => !entry.Value.Failed && !nextPages.ContainsKey(entry.Key))
            .Select(entry => entry.Key));
        nextResults = TrimResultMetadata(nextResults, cap);
        string? remainingFailure = nextResults
            .Where(entry => string.Equals(entry.Key.ViewKey, action.ViewKey, StringComparison.Ordinal) && entry.Value.Failed)
            .Select(entry => entry.Value.ErrorMessage ?? "Am30QueryFailed")
            .FirstOrDefault();
        return state with {
            PagesByKey = nextPages,
            TotalCountByKey = nextTotal,
            LastElapsedMsByKey = nextElapsed,
            FailureByKey = remainingFailure is null
                ? state.FailureByKey.Remove(action.ViewKey)
                : state.FailureByKey.SetItem(action.ViewKey, remainingFailure),
            PageInsertionOrder = nextOrder,
            PendingCompletionsByKey = nextPending,
            PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
            LastResultByKey = state.LastResultByKey.SetItem(action.ViewKey, completed),
            ResultsByPage = nextResults,
            ResultMetadataLimit = cap,
            ResultSequenceByView = state.ResultSequenceByView.SetItem(action.ViewKey, completed.Identity),
            LastSuccessfulPrimaryByView = action.Skip == 0
                ? state.LastSuccessfulPrimaryByView.SetItem(action.ViewKey, completed)
                : state.LastSuccessfulPrimaryByView,
        };
    }

    /// <summary>
    /// Story 5-2 D4 / AC4 — explicit no-change path for 304 Not Modified responses with a
    /// compatible cached payload. Resolves the matching TCS from the cached items and
    /// returns the state UNCHANGED (no <see cref="LoadedPageState.PagesByKey"/> /
    /// <see cref="LoadedPageState.TotalCountByKey"/> /
    /// <see cref="LoadedPageState.LastElapsedMsByKey"/> mutation) so the DataGrid emits no
    /// loading flash, no synthetic success, and no badge animation.
    /// </summary>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPageNotModified(LoadedPageState state, LoadPageNotModifiedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        if (action.OriginScopeGeneration is { } origin && origin != state.ScopeGeneration) {
            return state;
        }

        if (IsStaleRequest(state, action.ViewKey, action.RequestIdentity, action.RequestGeneration)) {
            return state;
        }

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        if (!state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? tcs)) {
            return state;
        }

        if (action.Completion is not null && !ReferenceEquals(tcs, action.Completion)) {
            return state;
        }

        _ = tcs.TrySetResult(action.CachedItems);
        ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> nextResults = state.ResultsByPage;
        if (nextResults.TryGetValue(key, out LoadedPageResult? priorPage) && priorPage.Failed) {
            nextResults = nextResults.Remove(key);
        }
        string? remainingFailure = nextResults
            .Where(entry => string.Equals(entry.Key.ViewKey, action.ViewKey, StringComparison.Ordinal) && entry.Value.Failed)
            .Select(entry => entry.Value.ErrorMessage ?? "Am30QueryFailed")
            .FirstOrDefault();
        return state with {
            PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
            PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
            ResultsByPage = nextResults,
            FailureByKey = remainingFailure is null
                ? state.FailureByKey.Remove(action.ViewKey)
                : state.FailureByKey.SetItem(action.ViewKey, remainingFailure),
            LastResultByKey = state.LastResultByKey.TryGetValue(action.ViewKey, out LoadedPageResult? priorResult)
                && priorResult.Failed && priorResult.Skip == action.Skip
                ? state.LastResultByKey.Remove(action.ViewKey)
                : state.LastResultByKey,
        };
    }

    /// <summary>Resolves the matching TCS via <c>TrySetException</c> and removes the entry.</summary>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPageFailed(LoadedPageState state, LoadPageFailedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        if (IsStaleRequest(state, action.ViewKey, action.RequestIdentity, action.RequestGeneration)) {
            return state;
        }
        if (!state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? tcs)) {
            return state;
        }

        if (action.Completion is not null && !ReferenceEquals(tcs, action.Completion)) {
            return state;
        }

        _ = tcs.TrySetException(new InvalidOperationException(action.ErrorMessage));
        LoadedPageResult failed = new(
            NextResultIdentity(state, action.ViewKey), action.RequestIdentity, 0, true,
            false, action.Skip, action.Take) { ErrorMessage = action.ErrorMessage };
        return state with {
            PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
            PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
            FailureByKey = state.FailureByKey.SetItem(action.ViewKey, action.ErrorMessage),
            LastResultByKey = state.LastResultByKey.SetItem(action.ViewKey, failed),
            ResultsByPage = TrimResultMetadata(state.ResultsByPage.SetItem(key, failed), state.ResultMetadataLimit),
            ResultSequenceByView = state.ResultSequenceByView.SetItem(action.ViewKey, failed.Identity),
        };
    }

    /// <summary>Resolves the matching TCS via <c>TrySetCanceled</c> and removes the entry.</summary>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPageCancelled(LoadedPageState state, LoadPageCancelledAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        if (!state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? tcs)) {
            return state;
        }

        if (action.Completion is not null && !ReferenceEquals(tcs, action.Completion)) {
            return state;
        }

        _ = tcs.TrySetCanceled();
        return state with {
            PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
            PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
        };
    }

    /// <summary>
    /// Sweeps every <see cref="LoadedPageState.PendingCompletionsByKey"/> entry whose
    /// view-key component matches — invoked from the generated view's <c>DisposeAsync</c>.
    /// </summary>
    [ReducerMethod]
    public static LoadedPageState ReduceClearPendingPages(LoadedPageState state, ClearPendingPagesAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> next = state.PendingCompletionsByKey;
        foreach (KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>> kvp in state.PendingCompletionsByKey) {
            if (string.Equals(kvp.Key.ViewKey, action.ViewKey, StringComparison.Ordinal)) {
                _ = kvp.Value.TrySetCanceled();
                next = next.Remove(kvp.Key);
            }
        }

        return ReferenceEquals(next, state.PendingCompletionsByKey)
            ? state
            : state with {
                PendingCompletionsByKey = next,
                PendingStartedAtByKey = state.PendingStartedAtByKey.RemoveRange(state.PendingStartedAtByKey.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal))),
            };
    }

    private static bool IsStaleRequest(LoadedPageState state, string viewKey, string requestIdentity, long? requestGeneration)
        => state.ActiveRequestIdentityByView.TryGetValue(viewKey, out string? active)
            && !string.Equals(active, requestIdentity, StringComparison.Ordinal)
            || requestGeneration is { } generation
                && state.ActiveRequestGenerationByView.TryGetValue(viewKey, out long current)
                && generation != current;

    private static ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> TrimResultMetadata(
        ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> results, int limit) {
        int bounded = Math.Max(1, limit);
        while (results.Count > bounded) {
            // Successful metadata stays bounded. A failed offset keeps in-place Retry until that page succeeds.
            bool found = false;
            (string ViewKey, int Skip) oldestKey = default;
            long oldestIdentity = long.MaxValue;
            foreach (KeyValuePair<(string ViewKey, int Skip), LoadedPageResult> entry in results) {
                if (entry.Value.Failed) {
                    continue;
                }

                if (!found || entry.Value.Identity < oldestIdentity) {
                    found = true;
                    oldestKey = entry.Key;
                    oldestIdentity = entry.Value.Identity;
                }
            }

            if (!found) {
                break;
            }

            results = results.Remove(oldestKey);
        }
        return results;
    }

    private static long NextResultIdentity(LoadedPageState state, string viewKey)
        => state.ResultSequenceByView.TryGetValue(viewKey, out long previous)
            ? previous + 1
            : state.LastResultByKey.TryGetValue(viewKey, out LoadedPageResult? last) ? last.Identity + 1 : 1;
}
