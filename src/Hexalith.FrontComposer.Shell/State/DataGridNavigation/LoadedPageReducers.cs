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
        foreach (ImmutableArray<TaskCompletionSource<object>> completions in state.AdditionalPendingCompletionsByKey.Values) {
            foreach (TaskCompletionSource<object> completion in completions) {
                _ = completion.TrySetCanceled();
            }
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
    /// Identical registrations share the current page result while each provider retains its
    /// own completion. A changed request cancels all completions from the previous request.
    /// </remarks>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPage(LoadedPageState state, LoadPageAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        if (action.OriginScopeGeneration is { } origin && origin != state.ScopeGeneration) {
            _ = action.Completion.TrySetCanceled();
            return state;
        }

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        long priorGeneration = state.ActiveRequestGenerationByView.TryGetValue(action.ViewKey, out long activeGeneration)
            ? activeGeneration : 0;
        if (action.RequestGeneration is { } requestedGeneration && requestedGeneration < priorGeneration) {
            _ = action.Completion.TrySetCanceled();
            return state;
        }
        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> nextPending = state.PendingCompletionsByKey;
        ImmutableDictionary<(string ViewKey, int Skip), ImmutableArray<TaskCompletionSource<object>>> nextAdditional = state.AdditionalPendingCompletionsByKey;
        ImmutableDictionary<(string ViewKey, int Skip), int> nextTake = state.PendingTakeByKey;
        bool changedRequest = state.ActiveRequestIdentityByView.TryGetValue(action.ViewKey, out string? priorIdentity)
            && !string.Equals(priorIdentity, action.RequestIdentity, StringComparison.Ordinal)
            || action.RequestGeneration is { } generation && generation > priorGeneration;
        long nextGeneration = action.RequestGeneration ?? (changedRequest ? priorGeneration + 1 : Math.Max(1, priorGeneration));
        if (changedRequest) {
            foreach (KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>> pending in nextPending) {
                if (string.Equals(pending.Key.ViewKey, action.ViewKey, StringComparison.Ordinal)) {
                    _ = pending.Value.TrySetCanceled();
                    foreach (TaskCompletionSource<object> additional in AdditionalFor(nextAdditional, pending.Key)) {
                        _ = additional.TrySetCanceled();
                    }
                    nextPending = nextPending.Remove(pending.Key);
                    nextAdditional = nextAdditional.Remove(pending.Key);
                    nextTake = nextTake.Remove(pending.Key);
                }
            }
        }

        bool changedPageSize = nextPending.ContainsKey(key) && nextTake.TryGetValue(key, out int previousTake)
            && previousTake != action.Take;
        if (changedPageSize) {
            TaskCompletionSource<object> old = nextPending[key];
            _ = old.TrySetCanceled();
            foreach (TaskCompletionSource<object> additional in AdditionalFor(nextAdditional, key)) {
                _ = additional.TrySetCanceled();
            }
            nextPending = nextPending.SetItem(key, action.Completion);
            nextAdditional = nextAdditional.Remove(key);
        }
        else if (nextPending.TryGetValue(key, out TaskCompletionSource<object>? existing)
            && !ReferenceEquals(existing, action.Completion)) {
            ImmutableArray<TaskCompletionSource<object>> additional = AdditionalFor(nextAdditional, key);
            if (!additional.Contains(action.Completion)) {
                nextAdditional = nextAdditional.SetItem(key, additional.Add(action.Completion));
            }
        }
        else if (!nextPending.ContainsKey(key)) {
            nextPending = nextPending.SetItem(key, action.Completion);
        }
        nextTake = nextTake.SetItem(key, action.Take);
        ImmutableDictionary<TaskCompletionSource<object>, DateTimeOffset> nextAnchors =
            KeepActiveAnchors(state.PendingStartedAtByCompletion, nextPending, nextAdditional);
        if (!nextAnchors.ContainsKey(action.Completion)) {
            nextAnchors = nextAnchors.SetItem(action.Completion, action.RegisteredAt ?? DateTimeOffset.UtcNow);
        }
        if (!nextAnchors.ContainsKey(nextPending[key])) {
            DateTimeOffset primaryAnchor = state.PendingStartedAtByKey.TryGetValue(key, out DateTimeOffset priorAnchor)
                ? priorAnchor : action.RegisteredAt ?? DateTimeOffset.UtcNow;
            nextAnchors = nextAnchors.SetItem(nextPending[key], primaryAnchor);
        }
        ImmutableDictionary<(string ViewKey, int Skip), string> nextSpeechIdentities = changedRequest
            ? state.PendingSpeechIdentityByKey.RemoveRange(state.PendingSpeechIdentityByKey.Keys.Where(entry => string.Equals(entry.ViewKey, action.ViewKey, StringComparison.Ordinal)))
            : state.PendingSpeechIdentityByKey;
        if (!nextSpeechIdentities.ContainsKey(key) || changedPageSize) {
            long resultAtRegistration = state.ResultSequenceByView.TryGetValue(action.ViewKey, out long lastResult)
                ? lastResult
                : state.LastResultByKey.TryGetValue(action.ViewKey, out LoadedPageResult? priorResult)
                    ? priorResult.Identity : 0;
            nextSpeechIdentities = nextSpeechIdentities.SetItem(key,
                resultAtRegistration.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":"
                + Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        }

        ImmutableDictionary<string, VirtualizationLane> nextLane = state.LaneByKey.ContainsKey(action.ViewKey)
            ? state.LaneByKey
            : state.LaneByKey.SetItem(action.ViewKey, VirtualizationLane.ServerSide);

        if (ReferenceEquals(nextPending, state.PendingCompletionsByKey)
            && ReferenceEquals(nextAdditional, state.AdditionalPendingCompletionsByKey)
            && ReferenceEquals(nextTake, state.PendingTakeByKey)
            && ReferenceEquals(nextLane, state.LaneByKey)
            && !changedRequest
            && state.ActiveRequestIdentityByView.ContainsKey(action.ViewKey)) {
            return state;
        }

        return state with {
            PendingCompletionsByKey = nextPending,
            AdditionalPendingCompletionsByKey = nextAdditional,
            PendingTakeByKey = nextTake,
            PendingStartedAtByCompletion = nextAnchors,
            PendingSpeechIdentityByKey = nextSpeechIdentities,
            PendingStartedAtByKey = (changedRequest
                ? state.PendingStartedAtByKey.RemoveRange(state.PendingStartedAtByKey.Keys.Where(entry => string.Equals(entry.ViewKey, action.ViewKey, StringComparison.Ordinal)))
                : state.PendingStartedAtByKey).SetItem(key, nextAnchors[nextPending[key]]),
            LaneByKey = nextLane,
            ActiveRequestIdentityByView = state.ActiveRequestIdentityByView.SetItem(action.ViewKey, action.RequestIdentity),
            ActiveRequestGenerationByView = state.ActiveRequestGenerationByView.SetItem(action.ViewKey, nextGeneration),
            ResultsByPage = changedRequest
                ? state.ResultsByPage.RemoveRange(state.ResultsByPage.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal)))
                : state.ResultsByPage,
            StalePages = changedRequest
                ? state.StalePages.Except(state.StalePages.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal)))
                : state.StalePages,
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
                || !MatchesCompletion(state, key, pending, action.Completion)) {
                return state;
            }
        }

        if (action.Items is null) {
            FrontComposerWarningLog.LoadedPageNullItems(_logger, action.ViewKey, action.Skip);

            if (state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? nullTcs)) {
                if (action.Completion is not null &&
                    (AdditionalFor(state.AdditionalPendingCompletionsByKey, key).Length > 0
                     || !ReferenceEquals(nullTcs, action.Completion))) {
                    _ = action.Completion.TrySetException(new InvalidOperationException("Data could not be loaded."));
                    return RemovePendingCompletion(state, key, action.Completion);
                }
                _ = nullTcs.TrySetException(new InvalidOperationException("Data could not be loaded."));
                foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, key)) {
                    _ = additional.TrySetException(new InvalidOperationException("Data could not be loaded."));
                }
                LoadedPageResult failed = new(
                    NextResultIdentity(state, action.ViewKey), action.RequestIdentity, 0, true,
                    action.OperatorInitiated, action.Skip, action.Take);
                return state with {
                    PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
                    AdditionalPendingCompletionsByKey = state.AdditionalPendingCompletionsByKey.Remove(key),
                    PendingTakeByKey = state.PendingTakeByKey.Remove(key),
                    PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
                    PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
                    PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
                    FailureByKey = state.FailureByKey.SetItem(action.ViewKey, "Am30QueryFailed"),
                    LastResultByKey = state.LastResultByKey.SetItem(action.ViewKey, failed),
                    ResultsByPage = TrimResultMetadata(state.ResultsByPage.SetItem(key, failed), state.ResultMetadataLimit, action.ViewKey),
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
            foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, key)) {
                _ = additional.TrySetResult(action.Items);
            }
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
        nextResults = TrimResultMetadata(nextResults, cap, action.ViewKey);
        string? remainingFailure = nextResults
            .Where(entry => string.Equals(entry.Key.ViewKey, action.ViewKey, StringComparison.Ordinal) && entry.Value.Failed)
            .Select(entry => entry.Value.ErrorMessage ?? "Am30QueryFailed")
            .FirstOrDefault();
        long successfulReadSequence = state.SuccessfulReadSequenceByView.GetValueOrDefault(action.ViewKey) + 1;
        return state with {
            StalePages = state.StalePages.Remove((action.ViewKey, action.Skip)),
            PagesByKey = nextPages,
            TotalCountByKey = nextTotal,
            LastElapsedMsByKey = nextElapsed,
            FailureByKey = remainingFailure is null
                ? state.FailureByKey.Remove(action.ViewKey)
                : state.FailureByKey.SetItem(action.ViewKey, remainingFailure),
            PageInsertionOrder = nextOrder,
            PendingCompletionsByKey = nextPending,
            AdditionalPendingCompletionsByKey = state.AdditionalPendingCompletionsByKey.Remove(key),
            PendingTakeByKey = state.PendingTakeByKey.Remove(key),
            PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
            PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
            PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
            LastResultByKey = state.LastResultByKey.SetItem(action.ViewKey, completed),
            ResultsByPage = nextResults,
            ResultMetadataLimit = cap,
            ResultSequenceByView = state.ResultSequenceByView.SetItem(action.ViewKey, completed.Identity),
            SuccessfulReadSequenceByView = state.SuccessfulReadSequenceByView.SetItem(action.ViewKey, successfulReadSequence),
            SuccessfulReadSequenceByPage = TrimSuccessfulReadSequences(
                state.SuccessfulReadSequenceByPage.SetItem(key, successfulReadSequence), nextPages, key),
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
        if (!state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? tcs)
            && (action.Completion is not null || action.OriginScopeGeneration is null))
        {
            return state;
        }

        if (action.Completion is not null && !MatchesCompletion(state, key, tcs!, action.Completion)) {
            return state;
        }

        if (!state.PagesByKey.ContainsKey(key)) {
            // Classification happened against an earlier snapshot. A missing page cannot
            // be certified fresh by an unchanged response. If the read carries usable
            // rows (or proves an empty page), restore them as a real completed read.
            if (action.TotalCount is { } totalCount && totalCount >= 0
                && (action.CachedItems.Count > 0 || action.Skip >= totalCount)) {
                return RestoreMissingNotModifiedPage(state, action, key, totalCount, tcs);
            }

            if (action.TotalCount is null) {
                _ = tcs?.TrySetResult(action.CachedItems);
                foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, key)) {
                    _ = additional.TrySetResult(action.CachedItems);
                }
            }
            else {
                _ = tcs?.TrySetException(new InvalidOperationException("Data could not be loaded."));
                foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, key)) {
                    _ = additional.TrySetException(new InvalidOperationException("Data could not be loaded."));
                }
            }
            return state with {
                StalePages = state.StalePages.Add(key),
                PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
                AdditionalPendingCompletionsByKey = state.AdditionalPendingCompletionsByKey.Remove(key),
                PendingTakeByKey = state.PendingTakeByKey.Remove(key),
                PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
                PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
                PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
            };
        }

        _ = tcs?.TrySetResult(action.CachedItems);
        foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, key)) {
            _ = additional.TrySetResult(action.CachedItems);
        }
        ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> nextResults = state.ResultsByPage;
        if (nextResults.TryGetValue(key, out LoadedPageResult? priorPage) && priorPage.Failed) {
            nextResults = nextResults.Remove(key);
        }
        string? remainingFailure = nextResults
            .Where(entry => string.Equals(entry.Key.ViewKey, action.ViewKey, StringComparison.Ordinal) && entry.Value.Failed)
            .Select(entry => entry.Value.ErrorMessage ?? "Am30QueryFailed")
            .FirstOrDefault();
        long successfulReadSequence = state.SuccessfulReadSequenceByView.GetValueOrDefault(action.ViewKey) + 1;
        return state with {
            StalePages = state.StalePages.Remove((action.ViewKey, action.Skip)),
            PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
            AdditionalPendingCompletionsByKey = state.AdditionalPendingCompletionsByKey.Remove(key),
            PendingTakeByKey = state.PendingTakeByKey.Remove(key),
            PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
            PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
            PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
            ResultsByPage = nextResults,
            SuccessfulReadSequenceByView = state.SuccessfulReadSequenceByView.SetItem(action.ViewKey, successfulReadSequence),
            SuccessfulReadSequenceByPage = TrimSuccessfulReadSequences(
                state.SuccessfulReadSequenceByPage.SetItem(key, successfulReadSequence), state.PagesByKey, key),
            FailureByKey = remainingFailure is null
                ? state.FailureByKey.Remove(action.ViewKey)
                : state.FailureByKey.SetItem(action.ViewKey, remainingFailure),
            LastResultByKey = state.LastResultByKey.TryGetValue(action.ViewKey, out LoadedPageResult? priorResult)
                && priorResult.Failed && priorResult.Skip == action.Skip
                ? state.LastResultByKey.Remove(action.ViewKey)
                : state.LastResultByKey,
        };
    }

    private static LoadedPageState RestoreMissingNotModifiedPage(LoadedPageState state, LoadPageNotModifiedAction action,
        (string ViewKey, int Skip) key, int totalCount, TaskCompletionSource<object>? tcs) {
        _ = tcs?.TrySetResult(action.CachedItems);
        foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, key)) {
            _ = additional.TrySetResult(action.CachedItems);
        }

        ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>> pages = state.PagesByKey.SetItem(key, action.CachedItems);
        ImmutableQueue<(string ViewKey, int Skip)> order = ImmutableQueue.CreateRange(
            state.PageInsertionOrder.Where(existing => existing != key)).Enqueue(key);
        int cap = Math.Max(1, action.MaxCachedPages);
        while (pages.Count > cap && !order.IsEmpty) {
            order = order.Dequeue(out (string ViewKey, int Skip) oldest);
            pages = pages.Remove(oldest);
        }
        if (!pages.ContainsKey(key)) {
            return state with { StalePages = state.StalePages.Add(key) };
        }

        long sequence = state.SuccessfulReadSequenceByView.GetValueOrDefault(action.ViewKey) + 1;
        LoadedPageResult completed = new(NextResultIdentity(state, action.ViewKey), action.RequestIdentity,
            totalCount, false, false, action.Skip);
        ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> results = TrimResultMetadata(
            state.ResultsByPage.SetItem(key, completed), state.ResultMetadataLimit, action.ViewKey);
        string? remainingFailure = results
            .Where(entry => string.Equals(entry.Key.ViewKey, action.ViewKey, StringComparison.Ordinal) && entry.Value.Failed)
            .Select(entry => entry.Value.ErrorMessage ?? "Am30QueryFailed")
            .FirstOrDefault();
        return state with {
            PagesByKey = pages,
            PageInsertionOrder = order,
            TotalCountByKey = state.TotalCountByKey.SetItem(action.ViewKey, totalCount),
            StalePages = state.StalePages.Remove(key),
            PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
            AdditionalPendingCompletionsByKey = state.AdditionalPendingCompletionsByKey.Remove(key),
            PendingTakeByKey = state.PendingTakeByKey.Remove(key),
            PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
            PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
            PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
            FailureByKey = remainingFailure is null
                ? state.FailureByKey.Remove(action.ViewKey)
                : state.FailureByKey.SetItem(action.ViewKey, remainingFailure),
            LastResultByKey = state.LastResultByKey.SetItem(action.ViewKey, completed),
            ResultsByPage = results,
            ResultSequenceByView = state.ResultSequenceByView.SetItem(action.ViewKey, completed.Identity),
            SuccessfulReadSequenceByView = state.SuccessfulReadSequenceByView.SetItem(action.ViewKey, sequence),
            SuccessfulReadSequenceByPage = TrimSuccessfulReadSequences(
                state.SuccessfulReadSequenceByPage.SetItem(key, sequence), pages, key),
            LastSuccessfulPrimaryByView = action.Skip == 0
                ? state.LastSuccessfulPrimaryByView.SetItem(action.ViewKey, completed)
                : state.LastSuccessfulPrimaryByView,
        };
    }

    /// <summary>Labels only the matching originating scope and request after a failed background read.</summary>
    [ReducerMethod]
    public static LoadedPageState ReduceMarkProjectionStale(LoadedPageState state, MarkProjectionStaleAction action)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);
        return state.ScopeGeneration != action.OriginScopeGeneration
            || IsStaleRequest(state, action.ViewKey, action.RequestIdentity, action.RequestGeneration)
            ? state : state with { StalePages = state.StalePages.Add((action.ViewKey, action.Skip)) };
    }

    /// <summary>Resolves the matching TCS via <c>TrySetException</c> and removes the entry.</summary>
    [ReducerMethod]
    public static LoadedPageState ReduceLoadPageFailed(LoadedPageState state, LoadPageFailedAction action) {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(action);

        if (action.OriginScopeGeneration is { } origin && origin != state.ScopeGeneration) {
            return state;
        }

        (string viewKey, int skip) key = (action.ViewKey, action.Skip);
        if (IsStaleRequest(state, action.ViewKey, action.RequestIdentity, action.RequestGeneration)) {
            return state;
        }
        if (!state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? tcs)) {
            return state;
        }

        if (action.Completion is not null && !MatchesCompletion(state, key, tcs, action.Completion)) {
            return state;
        }

        if (action.Completion is not null &&
            (AdditionalFor(state.AdditionalPendingCompletionsByKey, key).Length > 0
             || !ReferenceEquals(tcs, action.Completion))) {
            _ = action.Completion.TrySetException(new InvalidOperationException(action.ErrorMessage));
            return RemovePendingCompletion(state, key, action.Completion);
        }
        _ = tcs.TrySetException(new InvalidOperationException(action.ErrorMessage));
        foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, key)) {
            _ = additional.TrySetException(new InvalidOperationException(action.ErrorMessage));
        }
        LoadedPageResult failed = new(
            NextResultIdentity(state, action.ViewKey), action.RequestIdentity, 0, true,
            false, action.Skip, action.Take) { ErrorMessage = action.ErrorMessage };
        return state with {
            PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
            AdditionalPendingCompletionsByKey = state.AdditionalPendingCompletionsByKey.Remove(key),
            PendingTakeByKey = state.PendingTakeByKey.Remove(key),
            PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
            PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
            PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
            FailureByKey = state.FailureByKey.SetItem(action.ViewKey, action.ErrorMessage),
            LastResultByKey = state.LastResultByKey.SetItem(action.ViewKey, failed),
            ResultsByPage = TrimResultMetadata(state.ResultsByPage.SetItem(key, failed), state.ResultMetadataLimit, action.ViewKey),
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

        ImmutableArray<TaskCompletionSource<object>> additional = AdditionalFor(state.AdditionalPendingCompletionsByKey, key);
        if (action.Completion is null) {
            _ = tcs.TrySetCanceled();
            foreach (TaskCompletionSource<object> completion in additional) {
                _ = completion.TrySetCanceled();
            }
            return state with {
                PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
                AdditionalPendingCompletionsByKey = state.AdditionalPendingCompletionsByKey.Remove(key),
                PendingTakeByKey = state.PendingTakeByKey.Remove(key),
                PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
                PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
                PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
            };
        }

        if (ReferenceEquals(tcs, action.Completion)) {
            _ = tcs.TrySetCanceled();
            if (additional.IsEmpty) {
                return state with {
                    PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
                    PendingTakeByKey = state.PendingTakeByKey.Remove(key),
                    PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
                    PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
                    PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
                };
            }
            return state with {
                PendingCompletionsByKey = state.PendingCompletionsByKey.SetItem(key, additional[0]),
                AdditionalPendingCompletionsByKey = additional.Length == 1
                    ? state.AdditionalPendingCompletionsByKey.Remove(key)
                    : state.AdditionalPendingCompletionsByKey.SetItem(key, additional.RemoveAt(0)),
                PendingStartedAtByCompletion = state.PendingStartedAtByCompletion.Remove(tcs),
                PendingStartedAtByKey = state.PendingStartedAtByKey.SetItem(key, RegistrationAnchor(state, key, additional[0])),
            };
        }

        if (!additional.Contains(action.Completion)) {
            return state;
        }
        _ = action.Completion.TrySetCanceled();
        additional = additional.Remove(action.Completion);
        return state with {
            AdditionalPendingCompletionsByKey = additional.IsEmpty
                ? state.AdditionalPendingCompletionsByKey.Remove(key)
                : state.AdditionalPendingCompletionsByKey.SetItem(key, additional),
            PendingStartedAtByCompletion = state.PendingStartedAtByCompletion.Remove(action.Completion),
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

        if (action.StillFinalOwner is { } stillFinalOwner && !stillFinalOwner()) {
            return state;
        }

        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> next = state.PendingCompletionsByKey;
        foreach (KeyValuePair<(string ViewKey, int Skip), TaskCompletionSource<object>> kvp in state.PendingCompletionsByKey) {
            if (string.Equals(kvp.Key.ViewKey, action.ViewKey, StringComparison.Ordinal)) {
                _ = kvp.Value.TrySetCanceled();
                foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, kvp.Key)) {
                    _ = additional.TrySetCanceled();
                }
                next = next.Remove(kvp.Key);
            }
        }

        return ReferenceEquals(next, state.PendingCompletionsByKey)
            ? state
            : state with {
                PendingCompletionsByKey = next,
                AdditionalPendingCompletionsByKey = state.AdditionalPendingCompletionsByKey.RemoveRange(state.AdditionalPendingCompletionsByKey.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal))),
                PendingTakeByKey = state.PendingTakeByKey.RemoveRange(state.PendingTakeByKey.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal))),
                PendingStartedAtByKey = state.PendingStartedAtByKey.RemoveRange(state.PendingStartedAtByKey.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal))),
                PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.RemoveRange(state.PendingSpeechIdentityByKey.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal))),
                PendingStartedAtByCompletion = KeepActiveAnchors(state.PendingStartedAtByCompletion, next,
                    state.AdditionalPendingCompletionsByKey.RemoveRange(state.AdditionalPendingCompletionsByKey.Keys.Where(key => string.Equals(key.ViewKey, action.ViewKey, StringComparison.Ordinal)))),
            };
    }

    private static bool IsStaleRequest(LoadedPageState state, string viewKey, string requestIdentity, long? requestGeneration)
        => state.ActiveRequestIdentityByView.TryGetValue(viewKey, out string? active)
            && !string.Equals(active, requestIdentity, StringComparison.Ordinal)
            || requestGeneration is { } generation
                && state.ActiveRequestGenerationByView.TryGetValue(viewKey, out long current)
                && generation != current;

    private static ImmutableArray<TaskCompletionSource<object>> AdditionalFor(
        ImmutableDictionary<(string ViewKey, int Skip), ImmutableArray<TaskCompletionSource<object>>> pending,
        (string ViewKey, int Skip) key)
        => pending.TryGetValue(key, out ImmutableArray<TaskCompletionSource<object>> values) ? values : ImmutableArray<TaskCompletionSource<object>>.Empty;

    private static bool MatchesCompletion(LoadedPageState state, (string ViewKey, int Skip) key,
        TaskCompletionSource<object> primary, TaskCompletionSource<object> completion)
        => ReferenceEquals(primary, completion) || AdditionalFor(state.AdditionalPendingCompletionsByKey, key).Contains(completion);

    private static LoadedPageState RemovePendingCompletion(LoadedPageState state, (string ViewKey, int Skip) key,
        TaskCompletionSource<object> completion) {
        TaskCompletionSource<object> primary = state.PendingCompletionsByKey[key];
        ImmutableArray<TaskCompletionSource<object>> additional = AdditionalFor(state.AdditionalPendingCompletionsByKey, key);
        if (ReferenceEquals(primary, completion)) {
            return additional.IsEmpty
                ? state with {
                    PendingCompletionsByKey = state.PendingCompletionsByKey.Remove(key),
                    PendingTakeByKey = state.PendingTakeByKey.Remove(key),
                    PendingStartedAtByKey = state.PendingStartedAtByKey.Remove(key),
                    PendingStartedAtByCompletion = RemoveAnchorsForKey(state, key),
                    PendingSpeechIdentityByKey = state.PendingSpeechIdentityByKey.Remove(key),
                }
                : state with {
                    PendingCompletionsByKey = state.PendingCompletionsByKey.SetItem(key, additional[0]),
                    AdditionalPendingCompletionsByKey = additional.Length == 1
                        ? state.AdditionalPendingCompletionsByKey.Remove(key)
                        : state.AdditionalPendingCompletionsByKey.SetItem(key, additional.RemoveAt(0)),
                    PendingStartedAtByCompletion = state.PendingStartedAtByCompletion.Remove(primary),
                    PendingStartedAtByKey = state.PendingStartedAtByKey.SetItem(key, RegistrationAnchor(state, key, additional[0])),
                };
        }

        additional = additional.Remove(completion);
        return state with {
            AdditionalPendingCompletionsByKey = additional.IsEmpty
                ? state.AdditionalPendingCompletionsByKey.Remove(key)
                : state.AdditionalPendingCompletionsByKey.SetItem(key, additional),
            PendingStartedAtByCompletion = state.PendingStartedAtByCompletion.Remove(completion),
        };
    }

    private static ImmutableDictionary<TaskCompletionSource<object>, DateTimeOffset> RemoveAnchorsForKey(
        LoadedPageState state, (string ViewKey, int Skip) key) {
        ImmutableDictionary<TaskCompletionSource<object>, DateTimeOffset> anchors = state.PendingStartedAtByCompletion;
        if (state.PendingCompletionsByKey.TryGetValue(key, out TaskCompletionSource<object>? primary)) {
            anchors = anchors.Remove(primary);
        }
        foreach (TaskCompletionSource<object> additional in AdditionalFor(state.AdditionalPendingCompletionsByKey, key)) {
            anchors = anchors.Remove(additional);
        }
        return anchors;
    }

    private static DateTimeOffset RegistrationAnchor(LoadedPageState state, (string ViewKey, int Skip) key,
        TaskCompletionSource<object> completion)
        => state.PendingStartedAtByCompletion.TryGetValue(completion, out DateTimeOffset anchor)
            ? anchor : state.PendingStartedAtByKey.TryGetValue(key, out DateTimeOffset prior) ? prior : DateTimeOffset.UtcNow;

    private static ImmutableDictionary<TaskCompletionSource<object>, DateTimeOffset> KeepActiveAnchors(
        ImmutableDictionary<TaskCompletionSource<object>, DateTimeOffset> anchors,
        ImmutableDictionary<(string ViewKey, int Skip), TaskCompletionSource<object>> pending,
        ImmutableDictionary<(string ViewKey, int Skip), ImmutableArray<TaskCompletionSource<object>>> additional) {
        HashSet<TaskCompletionSource<object>> live = new(pending.Values, ReferenceEqualityComparer.Instance);
        foreach (ImmutableArray<TaskCompletionSource<object>> providers in additional.Values) {
            foreach (TaskCompletionSource<object> provider in providers) {
                _ = live.Add(provider);
            }
        }
        return anchors.RemoveRange(anchors.Keys.Where(provider => !live.Contains(provider)));
    }

    private static ImmutableDictionary<(string ViewKey, int Skip), long> TrimSuccessfulReadSequences(
        ImmutableDictionary<(string ViewKey, int Skip), long> sequences,
        ImmutableDictionary<(string ViewKey, int Skip), IReadOnlyList<object>> pages,
        (string ViewKey, int Skip) justRead)
        => sequences.RemoveRange(sequences.Keys.Where(key => !pages.ContainsKey(key) && key != justRead));

    private static ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> TrimResultMetadata(
        ImmutableDictionary<(string ViewKey, int Skip), LoadedPageResult> results, int limit, string viewKey) {
        int bounded = Math.Max(1, limit);
        int viewCount = 0;
        foreach (KeyValuePair<(string ViewKey, int Skip), LoadedPageResult> entry in results) {
            if (string.Equals(entry.Key.ViewKey, viewKey, StringComparison.Ordinal)) {
                viewCount++;
            }
        }

        // Each view has its own budget. A failed offset keeps in-place Retry until that page succeeds.
        while (viewCount > bounded) {
            bool found = false;
            (string ViewKey, int Skip) oldestKey = default;
            long oldestIdentity = long.MaxValue;
            foreach (KeyValuePair<(string ViewKey, int Skip), LoadedPageResult> entry in results) {
                if (!string.Equals(entry.Key.ViewKey, viewKey, StringComparison.Ordinal) || entry.Value.Failed) {
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
            viewCount--;
        }

        return results;
    }

    private static long NextResultIdentity(LoadedPageState state, string viewKey)
        => state.ResultSequenceByView.TryGetValue(viewKey, out long previous)
            ? previous + 1
            : state.LastResultByKey.TryGetValue(viewKey, out LoadedPageResult? last) ? last.Identity + 1 : 1;
}
