using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;

using Fluxor;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.State.DataGridNavigation;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Infrastructure.ProjectionConnection;

/// <summary>Bounded fallback refresh scheduler that reuses the Story 5-2 page loader seam.</summary>
public sealed class ProjectionFallbackRefreshScheduler(
    IProjectionConnectionState connectionState,
    IProjectionPageLoader loader,
    IDispatcher dispatcher,
    IState<LoadedPageState> loadedPages,
    IOptionsMonitor<FcShellOptions> options,
    ILogger<ProjectionFallbackRefreshScheduler> logger,
    IFrontComposerTenantContextAccessor? tenantContextAccessor = null,
    TimeProvider? timeProvider = null) : IProjectionFallbackRefreshScheduler, IProjectionIncompleteReadWaiter, IProjectionAbandonedReadRetirer {
    private static readonly TimeSpan CustomSweepReadLimit = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<string, LaneEntry> _lanes = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, TaskCompletionSource<bool>> _inFlight = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<TaskCompletionSource<bool>, (long Epoch, LaneEntry Entry)> _reconciliationAdmissions = new();
    private readonly ConcurrentDictionary<string, (LaneEntry Entry, bool RequiresDisconnected)> _pendingRetry = new(StringComparer.Ordinal);
    private ImmutableDictionary<ProjectionFallbackGroupKey, bool> _activeGroups = ImmutableDictionary<ProjectionFallbackGroupKey, bool>.Empty;
    // DN4=b — last-known ETag per lane key. Compared against the response ETag on each refresh
    // to detect a wire-level data change without relying on per-item Equals (which falls back
    // to reference equality for class-typed adopter projections).
    private readonly ConcurrentDictionary<string, string> _lastEtagByLane = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, string> _lastNoEtagSignatureByLane = new(StringComparer.Ordinal);
    private readonly object _laneGate = new();
    private TaskCompletionSource<bool> _replayQueueChanged = NewReplaySignal();
    private long _latestObservedEpoch;
    private long _fallbackSweepCursor;
    private long _nudgeSweepCursor;
    private long _reconciliationSweepCursor;
    private int _replayPumpRunning;

    public ProjectionFallbackRefreshScheduler(
        IProjectionConnectionState connectionState,
        IProjectionPageLoader loader,
        IOptionsMonitor<FcShellOptions> options,
        ILogger<ProjectionFallbackRefreshScheduler> logger)
        : this(connectionState, loader, new NoopDispatcher(), new StaticLoadedPageState(), options, logger) {
    }

    /// <inheritdoc />
    public IDisposable RegisterLane(ProjectionFallbackLane lane) => RegisterLaneCore(lane, allowReplacement: false);

    /// <inheritdoc />
    public IDisposable RegisterReplacingLane(ProjectionFallbackLane lane) => RegisterLaneCore(lane, allowReplacement: true);

    private Registration RegisterLaneCore(ProjectionFallbackLane lane, bool allowReplacement) {
        ArgumentNullException.ThrowIfNull(lane);
        ValidateLane(lane);
        (long registeredGeneration, string? registeredUser, bool hasScopeProof) = CaptureRegistrationScope(lane);

        while (true) {
            LaneEntry? observed;
            lock (_laneGate) {
                if (!_lanes.TryGetValue(lane.ViewKey, out observed)) {
                    LaneEntry added = new(lane, registeredGeneration, registeredUser, hasScopeProof);
                    _lanes[lane.ViewKey] = added;
                    return new Registration(() => DecrementLane(lane.ViewKey, added));
                }
                if (LaneContractsAreEquivalent(observed.Lane, lane)
                    && observed.RegisteredGeneration == registeredGeneration
                    && string.Equals(observed.RegisteredUser, registeredUser, StringComparison.Ordinal)
                    && observed.HasScopeProof == hasScopeProof) {
                    observed.RefCount++;
                    return new Registration(() => DecrementLane(lane.ViewKey, observed));
                }
                if (!allowReplacement) {
                    throw new InvalidOperationException("A conflicting fallback lane contract is already registered for this view.");
                }
            }

            // A co-mounted view can move to new criteria before its other owner does.
            // Lock the old dispatch gate before replacing it so an old read cannot
            // publish after the replacement becomes current.
            lock (observed.DispatchGate) {
                lock (_laneGate) {
                    if (!_lanes.TryGetValue(lane.ViewKey, out LaneEntry? current) || !ReferenceEquals(current, observed)) {
                        continue;
                    }
                    LaneEntry replacement = new(lane, registeredGeneration, registeredUser, hasScopeProof);
                    _lanes[lane.ViewKey] = replacement;
                    _ = _lastEtagByLane.TryRemove(lane.ViewKey, out _);
                    _ = _lastNoEtagSignatureByLane.TryRemove(lane.ViewKey, out _);
                    _ = _pendingRetry.TryRemove(lane.ViewKey, out _);
                    if (_inFlight.TryRemove(lane.ViewKey, out TaskCompletionSource<bool>? inFlight)) {
                        inFlight.TrySetResult(true);
                    }
                    return new Registration(() => DecrementLane(lane.ViewKey, replacement));
                }
            }
        }
    }

    /// <inheritdoc />
    public void SetReconciliationGroupHealth(IReadOnlyDictionary<ProjectionFallbackGroupKey, bool> activeGroups) {
        ArgumentNullException.ThrowIfNull(activeGroups);
        Volatile.Write(ref _activeGroups, activeGroups.ToImmutableDictionary());
    }

    /// <inheritdoc />
    public async Task<int> TriggerFallbackOnceAsync(CancellationToken cancellationToken = default) {
        using Activity? activity = FrontComposerTelemetry.StartProjectionFallbackPoll();
        FcShellOptions current = options.CurrentValue;
        if (current.ProjectionFallbackPollingIntervalSeconds <= 0 || !connectionState.Current.IsDisconnected) {
            FrontComposerTelemetry.SetOutcome(activity, "skipped");
            return 0;
        }

        int refreshed = 0;
        int attempted = 0;
        int budget = Math.Max(0, current.MaxProjectionFallbackPollingLanes);
        // F12 — track whether the hub reconnected mid-loop so the outer span can be tagged
        // `outcome=stale_after_reconnect` when an in-progress sweep stopped due to
        // reconnection rather than hitting the budget cap.
        bool reconnectedDuringLoop = false;
        foreach (LaneEntry entry in RotateLanes(_lanes.Values, ref _fallbackSweepCursor, budget)) {
            if (attempted >= budget) {
                break;
            }

            // P20 — re-check disconnected per-lane so an in-progress sweep stops promptly when
            // the hub reconnects mid-loop. AC7 explicitly requires "stops when the hub reconnects".
            if (!connectionState.Current.IsDisconnected) {
                reconnectedDuringLoop = true;
                break;
            }

            IReadOnlyList<ProjectionLaneRefreshResult> results = await RefreshLaneSequenceAsync(
                entry, budget - attempted, cancellationToken, () => connectionState.Current.IsDisconnected, requiresDisconnected: true).ConfigureAwait(false);
            attempted += results.Count;
            if (results.Any(static result => result is ProjectionLaneRefreshResult.Changed or ProjectionLaneRefreshResult.NotModified)) {
                refreshed++;
            }
            if (!connectionState.Current.IsDisconnected) {
                reconnectedDuringLoop = true;
                break;
            }
        }

        string finalOutcome = reconnectedDuringLoop
            ? "stale_after_reconnect"
            : refreshed > 0 ? "refreshed" : "empty";
        FrontComposerTelemetry.SetOutcome(activity, finalOutcome);
        return refreshed;
    }

    /// <inheritdoc />
    public async Task<int> TriggerNudgeRefreshAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default) {
        string safeProjectionType = EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType));
        string safeTenantId = EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId));
        using Activity? activity = FrontComposerTelemetry.StartProjectionNudge(
            safeProjectionType,
            FrontComposerTelemetry.TenantMarker(safeTenantId));
        int budget = Math.Max(0, options.CurrentValue.MaxProjectionFallbackPollingLanes);
        int refreshed = 0;
        int attempted = 0;
        IEnumerable<LaneEntry> matches = _lanes.Values
            .Where(entry => string.Equals(entry.Lane.ProjectionType, safeProjectionType, StringComparison.Ordinal)
                && string.Equals(entry.Lane.TenantId, safeTenantId, StringComparison.Ordinal));
        foreach (LaneEntry entry in RotateLanes(matches, ref _nudgeSweepCursor, budget)) {
            if (attempted >= budget) {
                break;
            }

            IReadOnlyList<ProjectionLaneRefreshResult> results = await RefreshLaneSequenceAsync(entry, budget - attempted, cancellationToken).ConfigureAwait(false);
            attempted += results.Count;
            if (results.Any(static result => result is ProjectionLaneRefreshResult.Changed or ProjectionLaneRefreshResult.NotModified)) {
                refreshed++;
            }
        }

        FrontComposerTelemetry.SetOutcome(activity, refreshed > 0 ? "refreshed" : "empty");
        return refreshed;
    }

    /// <inheritdoc />
    public Task<ProjectionReconciliationRefreshResult> TriggerReconciliationOnceAsync(long epoch, CancellationToken cancellationToken = default)
        => TriggerReconciliationCoreAsync(epoch, null, cancellationToken);

    Task<ProjectionReconciliationRefreshResult> IProjectionIncompleteReadWaiter.RecheckIncompleteReadsAsync(
        long epoch, IReadOnlyCollection<string> viewKeys, CancellationToken cancellationToken)
        => TriggerReconciliationCoreAsync(epoch, viewKeys, cancellationToken);

    private async Task<ProjectionReconciliationRefreshResult> TriggerReconciliationCoreAsync(
        long epoch, IReadOnlyCollection<string>? priorityViewKeys, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        if (!TryObserveEpoch(epoch)) {
            return ProjectionReconciliationRefreshResult.Empty;
        }

        int budget = Math.Max(0, options.CurrentValue.MaxProjectionFallbackPollingLanes);
        // P22 — log when the configured budget is zero so misconfiguration is visible instead
        // of silently suppressing every reconcile pass.
        if (budget == 0) {
            FrontComposerHotPathLog.ReconciliationBudgetZero(
                logger,
                budget);
            return ProjectionReconciliationRefreshResult.Empty;
        }

        // Registrations already share one entry per view key. Distinct views must each receive
        // their own current dispatch and freshness evidence, even when query criteria match.
        HashSet<string> priority = priorityViewKeys is null
            ? new(StringComparer.Ordinal) : new(priorityViewKeys, StringComparer.Ordinal);
        LaneEntry[] orderedLanes = priority.Count == 0
            ? RotateLanes(_lanes.Values, ref _reconciliationSweepCursor, budget)
            : [.. RotateLanes(_lanes.Values.Where(entry => priority.Contains(entry.Lane.ViewKey)),
                    ref _reconciliationSweepCursor, budget),
                .. _lanes.Values.Where(entry => !priority.Contains(entry.Lane.ViewKey))
                    .OrderBy(static entry => entry.Lane.ViewKey, StringComparer.Ordinal)];

        List<string> changed = [];
        List<string> failed = [];
        List<string> successful = [];
        List<string> skipped = [];
        int refreshed = 0;
        bool incompleteEligibleRead = false;
        List<string> incompleteViewKeys = [];
        ConcurrentBag<Task> incompleteReadTasks = [];
        List<LaneEntry> eligible = [];
        foreach (LaneEntry entry in orderedLanes) {
            ProjectionFallbackLane lane = entry.Lane;
            cancellationToken.ThrowIfCancellationRequested();
            // P29 — fail-closed: lanes without a tenant must not enter the reconciliation pass.
            // The cache layer would itself fail-closed on missing tenant, but we want this to
            // surface as a structured "lane skipped" diagnostic rather than as a silent miss.
            if (string.IsNullOrWhiteSpace(lane.TenantId)) {
                FrontComposerHotPathLog.ReconciliationLaneMissingTenant(
                    logger,
                    lane.ViewKey,
                    lane.ProjectionType);
                continue;
            }

            if (!IsGroupEligibleForReconciliation(lane)) {
                FrontComposerHotPathLog.ReconciliationLaneDegraded(
                    logger,
                    lane.ViewKey,
                    lane.ProjectionType);
                continue;
            }

            if (eligible.Count < budget) {
                eligible.Add(entry);
            }
            else if (priorityViewKeys is null || priority.Contains(lane.ViewKey)) {
                // Reconciliation must not certify recovery while eligible views outside
                // this finite sweep still have no current read evidence. The coordinator
                // rechecks these exact keys through newly budgeted passes.
                incompleteEligibleRead = true;
                incompleteViewKeys.Add(lane.ViewKey);
            }
        }

        // Admit each selected view before awaiting any provider. Reserve one physical attempt
        // for every view and give the remaining finite budget to the first view's same-pass replay.
        Task<IReadOnlyList<ProjectionLaneRefreshResult>>[] reads = new Task<IReadOnlyList<ProjectionLaneRefreshResult>>[eligible.Count];
        for (int index = 0; index < eligible.Count; index++) {
            int laneBudget = index == 0 ? budget - eligible.Count + 1 : 1;
            reads[index] = RefreshLaneSequenceAsync(eligible[index], laneBudget, cancellationToken,
                onSkippedRead: incompleteReadTasks.Add, reconciliationEpoch: epoch,
                queueSkippedReconciliationReplay: priorityViewKeys is null);
        }
        IReadOnlyList<ProjectionLaneRefreshResult>[] completed = await Task.WhenAll(reads).ConfigureAwait(false);
        for (int index = 0; index < eligible.Count; index++) {
            ProjectionFallbackLane lane = eligible[index].Lane;
            IReadOnlyList<ProjectionLaneRefreshResult> results = completed[index];
            if (results.Count == 0) {
                incompleteEligibleRead = true;
                incompleteViewKeys.Add(lane.ViewKey);
                continue;
            }
            ProjectionLaneRefreshResult final = results[^1];
            if (final is ProjectionLaneRefreshResult.Failed) {
                failed.Add(lane.ViewKey);
            }
            if (final is ProjectionLaneRefreshResult.ReadSkipped or ProjectionLaneRefreshResult.StaleRead) {
                skipped.Add(lane.ViewKey);
            }
            if (final is ProjectionLaneRefreshResult.Changed or ProjectionLaneRefreshResult.NotModified) {
                refreshed++;
                successful.Add(lane.ViewKey);
                if (results.Contains(ProjectionLaneRefreshResult.Changed)) {
                    changed.Add(lane.ViewKey);
                }
            }
        }

        return refreshed == 0 && changed.Count == 0 && failed.Count == 0 && skipped.Count == 0 && !incompleteEligibleRead
            ? ProjectionReconciliationRefreshResult.Empty
            : new ProjectionReconciliationRefreshResult(refreshed, changed) {
                FailedViewKeys = failed,
                RefreshedViewKeys = successful,
                SkippedViewKeys = skipped,
                HasIncompleteEligibleReads = incompleteEligibleRead,
                IncompleteViewKeys = incompleteViewKeys,
                IncompleteReadTasks = incompleteReadTasks.ToArray(),
            };
    }

    async Task<bool> IProjectionIncompleteReadWaiter.WaitForIncompleteReadAsync(long epoch, ProjectionReconciliationRefreshResult pass, CancellationToken cancellationToken) {
        if (epoch != Volatile.Read(ref _latestObservedEpoch)) {
            return false;
        }
        Task[] held = pass.IncompleteReadTasks.ToArray();
        if (held.Length > 0) {
            await Task.WhenAny(held).WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        return epoch == Volatile.Read(ref _latestObservedEpoch);
    }

    void IProjectionAbandonedReadRetirer.RetireAbandonedReads(long epoch) {
        foreach (KeyValuePair<TaskCompletionSource<bool>, (long Epoch, LaneEntry Entry)> admission in _reconciliationAdmissions) {
            if (admission.Value.Epoch == epoch) {
                lock (admission.Value.Entry.DispatchGate) {
                    if (_reconciliationAdmissions.TryGetValue(admission.Key, out var current)
                        && current.Epoch == epoch && ReferenceEquals(current.Entry, admission.Value.Entry)) {
                        RetireInFlight(admission.Value.Entry, admission.Key);
                    }
                }
            }
        }
    }

    private static LaneEntry[] RotateLanes(IEnumerable<LaneEntry> lanes, ref long cursor, int budget) {
        LaneEntry[] ordered = lanes.OrderBy(static entry => entry.Lane.ViewKey, StringComparer.Ordinal).ToArray();
        if (ordered.Length <= 1) {
            return ordered;
        }
        long ticket = Interlocked.Add(ref cursor, Math.Max(1, budget)) - Math.Max(1, budget);
        int start = (int)((ulong)ticket % (ulong)ordered.Length);
        return [.. ordered.Skip(start), .. ordered.Take(start)];
    }

    private async Task<IReadOnlyList<ProjectionLaneRefreshResult>> RefreshLaneSequenceAsync(
        LaneEntry entry, int remainingBudget, CancellationToken cancellationToken, Func<bool>? mayContinue = null,
        Action<Task>? onSkippedRead = null, bool requiresDisconnected = false, long? reconciliationEpoch = null,
        bool queueSkippedReconciliationReplay = true) {
        List<ProjectionLaneRefreshResult> results = [];
        while (remainingBudget > 0) {
            if (mayContinue is not null && !mayContinue()) {
                break;
            }
            if (reconciliationEpoch is null && results.Count == 0
                && _pendingRetry.TryGetValue(entry.Lane.ViewKey, out var queued)
                && ReferenceEquals(queued.Entry, entry)) {
                _ = _pendingRetry.TryRemove(new KeyValuePair<string, (LaneEntry Entry, bool RequiresDisconnected)>(entry.Lane.ViewKey, queued));
            }
            ProjectionLaneRefreshResult result;
            try {
                if (reconciliationEpoch is null && entry.Lane.RefreshAsync is not null)
                {
                    // A custom callback may ignore cancellation. Bound only that
                    // callback; a standard loader read keeps its own transport budget.
                    CancellationTokenSource bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    TaskCompletionSource<bool>? admission = null;
                    Task<ProjectionLaneRefreshResult> read = RefreshLaneOnceAsync(entry, bounded.Token,
                        onSkippedRead, requiresDisconnected, reconciliationEpoch,
                        current => Volatile.Write(ref admission, current), queueSkippedReconciliationReplay);
                    bool abandonedRead = false;
                    try
                    {
                        result = await read.WaitAsync(CustomSweepReadLimit, timeProvider ?? TimeProvider.System,
                            cancellationToken).ConfigureAwait(false);
                    }
                    catch (TimeoutException)
                    {
                        abandonedRead = true;
                        if (Volatile.Read(ref admission) is { } abandoned)
                        {
                            RetireInFlight(entry, abandoned);
                        }
                        _ = CancelAndDisposeAsync(bounded);
                        _ = ObserveAbandonedReadAsync(read);
                        result = ProjectionLaneRefreshResult.StaleRead;
                    }
                    catch (OperationCanceledException)
                    {
                        abandonedRead = true;
                        if (Volatile.Read(ref admission) is { } abandoned)
                        {
                            RetireInFlight(entry, abandoned);
                        }
                        _ = CancelAndDisposeAsync(bounded);
                        _ = ObserveAbandonedReadAsync(read);
                        throw;
                    }
                    finally
                    {
                        if (!abandonedRead)
                        {
                            bounded.Dispose();
                        }
                    }
                }
                else
                {
                    // Reconciliation keeps its 30-second deadline. A standard loader
                    // read is not cut by the custom-callback bound. The lane admission
                    // completes before this Task is returned. A custom callback itself
                    // runs separately so a synchronous provider cannot hold the
                    // coordinator before it applies the reconciliation deadline.
                    Task<ProjectionLaneRefreshResult> read = RefreshLaneOnceAsync(entry,
                        cancellationToken, onSkippedRead, requiresDisconnected, reconciliationEpoch,
                        queueSkippedReconciliationReplay: queueSkippedReconciliationReplay);
                    result = await read.ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) {
                // Cancellation of the original read must not strand a nudge from another
                // caller. The replay still rechecks current lane and request ownership.
                if (_pendingRetry.ContainsKey(entry.Lane.ViewKey)) {
                    SchedulePendingReplay();
                }
                throw;
            }
            if (result is ProjectionLaneRefreshResult.Skipped) {
                if (!_inFlight.ContainsKey(entry.Lane.ViewKey) && _pendingRetry.ContainsKey(entry.Lane.ViewKey)) {
                    SchedulePendingReplay();
                }
                break;
            }
            results.Add(result);
            remainingBudget--;
            if (result is ProjectionLaneRefreshResult.ReadSkipped or ProjectionLaneRefreshResult.StaleRead) {
                // A skipped read has no successful lane result to continue from. Preserve
                // the pending retry for a new sweep, especially when ownership changed.
                if (_pendingRetry.ContainsKey(entry.Lane.ViewKey)) {
                    SchedulePendingReplay();
                }
                break;
            }
            if (!_pendingRetry.TryRemove(entry.Lane.ViewKey, out var pending) || !IsLaneActive(pending.Entry)) {
                break;
            }
            if (pending.RequiresDisconnected && !connectionState.Current.IsDisconnected) {
                break;
            }
            if (remainingBudget == 0 || (mayContinue is not null && !mayContinue())) {
                _pendingRetry[entry.Lane.ViewKey] = pending;
                SchedulePendingReplay();
                break;
            }
            entry = pending.Entry;
            requiresDisconnected = pending.RequiresDisconnected;
        }
        return results;
    }

    private static async Task ObserveAbandonedReadAsync(Task read)
    {
        try
        {
            await read.ConfigureAwait(false);
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
        {
            // The bounded sweep has already retired this admission. Its late
            // exception must not become an unobserved task failure.
        }
    }

    private async Task CancelAndDisposeAsync(CancellationTokenSource source)
    {
        Task? cancellation = null;
        try
        {
            cancellation = source.CancelAsync();
            await cancellation.WaitAsync(CustomSweepReadLimit, timeProvider ?? TimeProvider.System).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            if (cancellation is not null)
            {
                _ = ObserveAbandonedReadAsync(cancellation);
            }
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex))
        {
            // An adopter callback may reject cancellation after this admission
            // has already been retired from the visible sweep.
        }
        finally
        {
            source.Dispose();
        }
    }

    private void SchedulePendingReplay() {
        Interlocked.Exchange(ref _replayQueueChanged, NewReplaySignal()).TrySetResult(true);
        if (Interlocked.CompareExchange(ref _replayPumpRunning, 1, 0) != 0) {
            return;
        }
        _ = Task.Run(async () => {
            try {
                // A replay is a separately budgeted sweep, never a recursive continuation of
                // the read that received the nudge. A short yield also coalesces later nudges.
                await Task.Delay(50).ConfigureAwait(false);
                int budget = Math.Max(0, options.CurrentValue.MaxProjectionFallbackPollingLanes);
                int attempted = 0;
                while (attempted < budget && !_pendingRetry.IsEmpty) {
                    Task queueChanged = Volatile.Read(ref _replayQueueChanged).Task;
                    List<Task> held = [];
                    bool processedReady = false;
                    foreach (KeyValuePair<string, (LaneEntry Entry, bool RequiresDisconnected)> pending in _pendingRetry.OrderBy(static pair => pair.Key, StringComparer.Ordinal)) {
                        if (attempted >= budget) {
                            break;
                        }
                        if (_inFlight.TryGetValue(pending.Key, out TaskCompletionSource<bool>? inFlight)) {
                            held.Add(inFlight.Task);
                            continue;
                        }
                        if (!_pendingRetry.TryRemove(pending) || !IsLaneActive(pending.Value.Entry)
                            || (pending.Value.RequiresDisconnected && !connectionState.Current.IsDisconnected)) {
                            continue;
                        }
                        processedReady = true;
                        // Admit before awaiting. A newly stalled B must not prevent a ready
                        // C from entering this finite sweep's remaining physical slot.
                        attempted++;
                        _ = ObservePendingReplayAsync(pending.Value);
                    }
                    if (!processedReady) {
                        if (held.Count == 0) {
                            break;
                        }
                        // Await one completion only after every ready lane has been serviced.
                        // A distinct ready lane can arrive after this scan while every
                        // currently known read remains held.
                        held.Add(queueChanged);
                        await Task.WhenAny(held).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) {
                // A lane can cancel its own read after admission. Its completion has
                // already woken the pump; the next sweep rechecks lane ownership.
            }
            catch (Exception ex) when (ex is not OperationCanceledException && !ExceptionGuard.IsFatal(ex)) {
                FrontComposerLog.ProjectionRefreshFailed(logger, "pending-nudge", ex.GetType().Name);
            }
            finally {
                Volatile.Write(ref _replayPumpRunning, 0);
                if (_pendingRetry.Any(pending => !_inFlight.ContainsKey(pending.Key))
                    && options.CurrentValue.MaxProjectionFallbackPollingLanes > 0) {
                    SchedulePendingReplay();
                }
            }
        });
    }

    private static TaskCompletionSource<bool> NewReplaySignal()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private async Task ObservePendingReplayAsync((LaneEntry Entry, bool RequiresDisconnected) pending) {
        try {
            _ = await RefreshLaneSequenceAsync(pending.Entry, 1, CancellationToken.None,
                pending.RequiresDisconnected ? () => connectionState.Current.IsDisconnected : null,
                requiresDisconnected: pending.RequiresDisconnected).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            // The replay was explicitly canceled by its lane; pending ownership was
            // already reconsidered by RefreshLaneSequenceAsync.
        }
        catch (Exception ex) when (ex is not OperationCanceledException && !ExceptionGuard.IsFatal(ex)) {
            FrontComposerLog.ProjectionRefreshFailed(logger, "pending-nudge", ex.GetType().Name);
        }
    }

    private async Task<ProjectionLaneRefreshResult> RefreshLaneOnceAsync(LaneEntry entry, CancellationToken cancellationToken,
        Action<Task>? onSkippedRead = null, bool requiresDisconnected = false, long? reconciliationEpoch = null,
        Action<TaskCompletionSource<bool>>? onAdmission = null, bool queueSkippedReconciliationReplay = true) {
        cancellationToken.ThrowIfCancellationRequested();
        ProjectionFallbackLane lane = entry.Lane;
        if (!IsLaneActive(entry) || !IsLaneCurrentScope(entry) || (requiresDisconnected && !connectionState.Current.IsDisconnected)) {
            return ProjectionLaneRefreshResult.Skipped;
        }

        TaskCompletionSource<bool> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!_inFlight.TryAdd(lane.ViewKey, completion)) {
            if (_inFlight.TryGetValue(lane.ViewKey, out TaskCompletionSource<bool>? owner)) {
                if (reconciliationEpoch is { } observedEpoch) {
                    lock (entry.DispatchGate) {
                        if (_inFlight.TryGetValue(lane.ViewKey, out TaskCompletionSource<bool>? current)
                            && ReferenceEquals(current, owner) && IsLaneActive(entry)) {
                            _reconciliationAdmissions[owner] = (observedEpoch, entry);
                        }
                    }
                }
                onSkippedRead?.Invoke(owner.Task);
            }
            if (reconciliationEpoch is null || queueSkippedReconciliationReplay) {
                // P1 — mark pending so the final nudge gets a replay after the in-flight refresh
                // resolves. The first reconciliation pass can queue one recovery replay;
                // targeted rechecks wait for it without queuing a competing chain.
                lock (_laneGate) {
                    if (IsLaneActiveWithoutLock(entry)) {
                        _pendingRetry.AddOrUpdate(lane.ViewKey,
                            (entry, requiresDisconnected),
                            (_, prior) => ReferenceEquals(prior.Entry, entry)
                                ? (entry, prior.RequiresDisconnected && requiresDisconnected)
                                : (entry, requiresDisconnected));
                    }
                }

                SchedulePendingReplay();
            }

            return ProjectionLaneRefreshResult.Skipped;
        }
        if (reconciliationEpoch is { } admittedEpoch) {
            _reconciliationAdmissions[completion] = (admittedEpoch, entry);
        }
        onAdmission?.Invoke(completion);

        // Reconciliation has a finite deadline even when a provider ignores its
        // token. Retire this exact admission on cancellation so a later current
        // read can use the still-mounted lane. The old continuation remains
        // canceled and cannot remove a replacement admission in its finally.
        using CancellationTokenRegistration cancellationRegistration = cancellationToken.Register(
            () => RetireInFlight(entry, completion));

        entry.OriginScopeGeneration = entry.RegisteredGeneration;
        entry.OriginRequestIdentity = loadedPages.Value.ActiveRequestIdentityByView.TryGetValue(lane.ViewKey, out string? identity)
            ? identity : string.Empty;
        entry.OriginRequestGeneration = loadedPages.Value.ActiveRequestGenerationByView.TryGetValue(lane.ViewKey, out long generation)
            ? generation : null;

        ProjectionLaneRefreshResult outcome;
        try
        {
            try {
                if (requiresDisconnected && !connectionState.Current.IsDisconnected) {
                    return ProjectionLaneRefreshResult.ReadSkipped;
                }
                if (lane.RefreshAsync is not null) {
                    ProjectionFallbackLaneRefreshOutcome refreshed = await Task.Run(async () => {
                        cancellationToken.ThrowIfCancellationRequested();
                        return await lane.RefreshAsync(cancellationToken).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    outcome = MapCustomOutcome(refreshed);
                }
                else {
                    ProjectionPageResult result = await loader.LoadPageAsync(
                        lane.ProjectionType,
                        lane.Skip,
                        lane.Take,
                        lane.Filters,
                        lane.SortColumn,
                        lane.SortDescending,
                        lane.SearchQuery,
                        cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!IsCurrentAdmission(entry, completion)) {
                        return ProjectionLaneRefreshResult.StaleRead;
                    }
                    outcome = ClassifyRefreshResult(entry, result, completion);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException && !ExceptionGuard.IsFatal(ex)) {
                // P5 — log only the redacted exception type. Raw exception messages can carry
                // tenant/group/payload data; structured `FailureCategory` keeps logs bounded.
                FrontComposerLog.ProjectionRefreshFailed(logger, lane.ProjectionType, ex.GetType().Name);
                outcome = ProjectionLaneRefreshResult.Failed;
            }
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsCurrentAdmission(entry, completion) || !IsLaneActive(entry) || !IsCurrentRequest(entry))
            {
                // The read was admitted and must consume this sweep's physical-read budget,
                // even though its result no longer belongs to the active lane/request.
                outcome = ProjectionLaneRefreshResult.StaleRead;
            }
            else if (outcome is ProjectionLaneRefreshResult.Skipped)
            {
                // Callback and loader classification skips still followed an admitted read.
                outcome = ProjectionLaneRefreshResult.ReadSkipped;
            }
            else if (outcome is ProjectionLaneRefreshResult.Failed)
            {
                lock (entry.DispatchGate)
                {
                    if (IsCurrentAdmission(entry, completion) && IsLaneActive(entry) && IsCurrentRequest(entry))
                    {
                        dispatcher.Dispatch(new MarkProjectionStaleAction(lane.ViewKey, lane.Skip, entry.OriginScopeGeneration,
                            entry.OriginRequestIdentity, entry.OriginRequestGeneration));
                    }
                }
            }

        }
        finally
        {
            RetireInFlight(entry, completion);
        }

        return outcome;
    }

    private bool IsCurrentAdmission(LaneEntry entry, TaskCompletionSource<bool> completion)
        => _inFlight.TryGetValue(entry.Lane.ViewKey, out TaskCompletionSource<bool>? current)
            && ReferenceEquals(current, completion);

    private void RetireInFlight(LaneEntry entry, TaskCompletionSource<bool> completion) {
        lock (entry.DispatchGate) {
            _ = _reconciliationAdmissions.TryRemove(completion, out _);
            string viewKey = entry.Lane.ViewKey;
            if (!_inFlight.TryRemove(new KeyValuePair<string, TaskCompletionSource<bool>>(viewKey, completion))) {
                return;
            }

            completion.TrySetResult(true);
            if (_pendingRetry.ContainsKey(viewKey)) {
                SchedulePendingReplay();
            }
        }
    }

    /// <summary>
    /// DN4=b — classify the refresh result as Changed/NotModified using the wire-level ETag as
    /// the canonical change signal. P25 — guard against negative TotalCount which indicates a
    /// protocol failure rather than data, and never let a malformed value drive a Changed.
    /// P23 — null-guard before any dereference of result.Items in the comparison branches.
    /// </summary>
    private ProjectionLaneRefreshResult ClassifyRefreshResult(LaneEntry entry, ProjectionPageResult result,
        TaskCompletionSource<bool> completion) {
        ProjectionFallbackLane lane = entry.Lane;
        if (!IsLaneActive(entry)) {
            return ProjectionLaneRefreshResult.Skipped;
        }

        // P25 — negative TotalCount is a protocol issue, not a Changed signal.
        if (result.TotalCount < 0) {
            FrontComposerHotPathLog.ProjectionRefreshNegativeCount(
                logger,
                lane.ViewKey);
            return ProjectionLaneRefreshResult.Failed;
        }

        if (result.Items is null) {
            FrontComposerLog.ProjectionRefreshFailed(logger, lane.ProjectionType, "InvalidItems");
            return ProjectionLaneRefreshResult.Failed;
        }

        bool reducerPageMissing = !HasReducerPage(lane);
        if (reducerPageMissing && result.Items.Count == 0 && lane.Skip < result.TotalCount) {
            return ProjectionLaneRefreshResult.Failed;
        }
        if (result.IsNotModified) {
            if (reducerPageMissing) {
                return TryDispatchPageSuccess(entry, result, completion)
                    ? ProjectionLaneRefreshResult.Changed
                    : ProjectionLaneRefreshResult.Skipped;
            }

            return TryDispatchPageNotModified(entry, result, completion)
                ? ProjectionLaneRefreshResult.NotModified
                : ProjectionLaneRefreshResult.Skipped;
        }

        string laneIdentity = lane.ViewKey;
        string? newEtag = result.ETag;
        if (string.IsNullOrEmpty(newEtag)) {
            string signature = ProjectionFallbackRowSignature.Create(result, out bool signatureReliable);
            bool signatureChanged;
            lock (_laneGate) {
                if (!IsLaneActiveWithoutLock(entry)) {
                    return ProjectionLaneRefreshResult.Skipped;
                }

                signatureChanged = !signatureReliable
                    || !_lastNoEtagSignatureByLane.TryGetValue(laneIdentity, out string? previousSignature)
                    || !string.Equals(previousSignature, signature, StringComparison.Ordinal);
            }

            if (signatureChanged || reducerPageMissing) {
                return TryDispatchPageSuccess(entry, result, completion, signature)
                    ? ProjectionLaneRefreshResult.Changed
                    : ProjectionLaneRefreshResult.Skipped;
            }

            return TryDispatchPageNotModified(entry, result, completion)
                ? ProjectionLaneRefreshResult.NotModified
                : ProjectionLaneRefreshResult.Skipped;
        }

        bool etagChanged;
        lock (_laneGate) {
            if (!IsLaneActiveWithoutLock(entry)) {
                return ProjectionLaneRefreshResult.Skipped;
            }

            bool hadPrevious = _lastEtagByLane.TryGetValue(laneIdentity, out string? previousEtag);
            etagChanged = !hadPrevious || !string.Equals(previousEtag, newEtag, StringComparison.Ordinal);
        }

        if (etagChanged || reducerPageMissing) {
            return TryDispatchPageSuccess(entry, result, completion)
                ? ProjectionLaneRefreshResult.Changed
                : ProjectionLaneRefreshResult.Skipped;
        }

        return TryDispatchPageNotModified(entry, result, completion)
            ? ProjectionLaneRefreshResult.NotModified
            : ProjectionLaneRefreshResult.Skipped;
    }

    private void DecrementLane(string viewKey, LaneEntry expected) {
        lock (expected.DispatchGate) {
            lock (_laneGate) {
                expected.RefCount--;
                if (!_lanes.TryGetValue(viewKey, out LaneEntry? current) || !ReferenceEquals(current, expected)) {
                    return;
                }

                if (current.RefCount == 0) {
                    _ = _lanes.TryRemove(new KeyValuePair<string, LaneEntry>(viewKey, current));
                    _ = _lastEtagByLane.TryRemove(viewKey, out _);
                    _ = _lastNoEtagSignatureByLane.TryRemove(viewKey, out _);
                    _ = _pendingRetry.TryRemove(viewKey, out _);
                    // A transport may ignore cancellation indefinitely. Wake a replay pump
                    // waiting on this lane's completion when its final owner leaves.
                    if (_inFlight.TryGetValue(viewKey, out TaskCompletionSource<bool>? inFlight)) {
                        _ = _inFlight.TryRemove(new KeyValuePair<string, TaskCompletionSource<bool>>(viewKey, inFlight));
                        inFlight.TrySetResult(true);
                    }
                }
            }
        }
    }

    private LaneEntry? GetActiveLane(string viewKey) {
        lock (_laneGate) {
            return _lanes.TryGetValue(viewKey, out LaneEntry? entry) ? entry : null;
        }
    }

    private bool IsLaneActive(LaneEntry entry) {
        lock (_laneGate) {
            return IsLaneActiveWithoutLock(entry);
        }
    }

    private bool IsLaneActiveWithoutLock(LaneEntry entry)
        => _lanes.TryGetValue(entry.Lane.ViewKey, out LaneEntry? current)
            && ReferenceEquals(current, entry);

    private static bool LaneContractsAreEquivalent(ProjectionFallbackLane first, ProjectionFallbackLane second)
        => string.Equals(first.ViewKey, second.ViewKey, StringComparison.Ordinal)
            && string.Equals(first.ProjectionType, second.ProjectionType, StringComparison.Ordinal)
            && string.Equals(first.TenantId, second.TenantId, StringComparison.Ordinal)
            && string.Equals(first.UserId, second.UserId, StringComparison.Ordinal)
            && first.ScopeGeneration == second.ScopeGeneration
            && first.Skip == second.Skip
            && first.Take == second.Take
            && FiltersAreEquivalent(first.Filters, second.Filters)
            && string.Equals(first.SortColumn, second.SortColumn, StringComparison.Ordinal)
            && first.SortDescending == second.SortDescending
            && string.Equals(first.SearchQuery, second.SearchQuery, StringComparison.Ordinal)
            && ReferenceEquals(first.RefreshAsync, second.RefreshAsync);

    private static bool FiltersAreEquivalent(
        IImmutableDictionary<string, string> first,
        IImmutableDictionary<string, string> second) {
        if (first.Count != second.Count) {
            return false;
        }

        using IEnumerator<KeyValuePair<string, string>> firstEnumerator = first
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .GetEnumerator();
        using IEnumerator<KeyValuePair<string, string>> secondEnumerator = second
            .OrderBy(static pair => pair.Key, StringComparer.Ordinal)
            .GetEnumerator();
        while (firstEnumerator.MoveNext() && secondEnumerator.MoveNext()) {
            if (!string.Equals(firstEnumerator.Current.Key, secondEnumerator.Current.Key, StringComparison.Ordinal)
                || !string.Equals(firstEnumerator.Current.Value, secondEnumerator.Current.Value, StringComparison.Ordinal)) {
                return false;
            }
        }

        return true;
    }

    private static void ValidateLane(ProjectionFallbackLane lane) {
        ArgumentNullException.ThrowIfNull(lane.Filters);

        if (string.IsNullOrWhiteSpace(lane.ViewKey)) {
            throw new ArgumentException("View key cannot be null, empty, or whitespace.", nameof(lane));
        }

        if (string.IsNullOrWhiteSpace(lane.ProjectionType)) {
            throw new ArgumentException("ProjectionType cannot be null, empty, or whitespace.", nameof(lane));
        }

        if (lane.ProjectionType.Contains(':', StringComparison.Ordinal)) {
            throw new ArgumentException("ProjectionType must not contain ':' (reserved by EventStore group format).", nameof(lane));
        }

        if (lane.TenantId is not null && lane.TenantId.Contains(':', StringComparison.Ordinal)) {
            throw new ArgumentException("TenantId must not contain ':' (reserved by EventStore group format).", nameof(lane));
        }

        if (lane.Skip < 0) {
            throw new ArgumentOutOfRangeException(nameof(lane), lane.Skip, "Skip must be non-negative.");
        }

        if (lane.Take <= 0) {
            throw new ArgumentOutOfRangeException(nameof(lane), lane.Take, "Take must be positive.");
        }
    }

    private bool TryObserveEpoch(long epoch) {
        while (true) {
            long observed = Volatile.Read(ref _latestObservedEpoch);
            if (epoch < observed) {
                return false;
            }

            if (Interlocked.CompareExchange(ref _latestObservedEpoch, epoch, observed) == observed) {
                return true;
            }
        }
    }

    private bool IsGroupEligibleForReconciliation(ProjectionFallbackLane lane) {
        ImmutableDictionary<ProjectionFallbackGroupKey, bool> groups = Volatile.Read(ref _activeGroups);
        if (string.IsNullOrWhiteSpace(lane.TenantId) || groups.IsEmpty) {
            return true;
        }

        ProjectionFallbackGroupKey key = new(lane.ProjectionType, lane.TenantId);
        return !groups.TryGetValue(key, out bool active) || active;
    }

    private bool HasReducerPage(ProjectionFallbackLane lane) {
        (string ViewKey, int Skip) pageKey = (lane.ViewKey, lane.Skip);
        return loadedPages.Value.PagesByKey.ContainsKey(pageKey);
    }

    private bool IsCurrentRequest(LaneEntry entry)
        => IsLaneCurrentScope(entry)
            && loadedPages.Value.ScopeGeneration == entry.OriginScopeGeneration
            && string.Equals(
                loadedPages.Value.ActiveRequestIdentityByView.TryGetValue(entry.Lane.ViewKey, out string? identity) ? identity : string.Empty,
                entry.OriginRequestIdentity,
                StringComparison.Ordinal)
            && (!loadedPages.Value.ActiveRequestGenerationByView.TryGetValue(entry.Lane.ViewKey, out long generation)
                ? entry.OriginRequestGeneration is null
                : generation == entry.OriginRequestGeneration);

    private (long Generation, string? User, bool HasProof) CaptureRegistrationScope(ProjectionFallbackLane lane) {
        long generation = loadedPages.Value.ScopeGeneration;
        if (tenantContextAccessor is null) {
            return (generation, null, true);
        }
        try {
            TenantContextResult current = tenantContextAccessor.TryGetContext(lane.TenantId, "projection-fallback");
            return current.Succeeded && current.Context is not null
                ? (generation, current.Context.UserId, true)
                : (generation, null, false);
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            return (generation, null, false);
        }
    }

    private bool IsLaneCurrentScope(LaneEntry entry) {
        ProjectionFallbackLane lane = entry.Lane;
        if (!entry.HasScopeProof || loadedPages.Value.ScopeGeneration != entry.RegisteredGeneration
            || lane.ScopeGeneration is { } generation && loadedPages.Value.ScopeGeneration != generation) {
            return false;
        }
        if (tenantContextAccessor is null) {
            return lane.UserId is null;
        }
        try {
            TenantContextResult current = tenantContextAccessor.TryGetContext(lane.TenantId, "projection-fallback");
            return current.Succeeded && current.Context is not null
                && string.Equals(current.Context.UserId, entry.RegisteredUser, StringComparison.Ordinal)
                && (lane.UserId is null || string.Equals(current.Context.UserId, lane.UserId, StringComparison.Ordinal));
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            return false;
        }
    }

    private bool TryRecordValidatorState(
        LaneEntry entry,
        ProjectionPageResult result,
        string? noEtagSignature = null) {
        string? newEtag = result.ETag;
        string? signature = string.IsNullOrEmpty(newEtag)
            ? noEtagSignature ?? ProjectionFallbackRowSignature.Create(result, out _)
            : null;
        lock (_laneGate) {
            if (!IsLaneActiveWithoutLock(entry) || !IsCurrentRequest(entry)) {
                return false;
            }

            string laneIdentity = entry.Lane.ViewKey;
            if (string.IsNullOrEmpty(newEtag)) {
                _ = _lastEtagByLane.TryRemove(laneIdentity, out _);
                _lastNoEtagSignatureByLane[laneIdentity] = signature!;
            }
            else {
                _lastEtagByLane[laneIdentity] = newEtag;
                _ = _lastNoEtagSignatureByLane.TryRemove(laneIdentity, out _);
            }

            return true;
        }
    }

    private bool TryDispatchPageSuccess(
        LaneEntry entry,
        ProjectionPageResult result,
        TaskCompletionSource<bool> completion,
        string? noEtagSignature = null) {
        lock (entry.DispatchGate) {
            lock (_laneGate) {
                if (!IsCurrentAdmission(entry, completion) || !IsLaneActiveWithoutLock(entry) || !IsCurrentRequest(entry)) {
                    return false;
                }
            }

            DispatchPageSuccess(entry, result);
            return TryRecordValidatorState(entry, result, noEtagSignature);
        }
    }

    private bool TryDispatchPageNotModified(LaneEntry entry, ProjectionPageResult result,
        TaskCompletionSource<bool> completion) {
        lock (entry.DispatchGate) {
            bool hadValidatorState;
            lock (_laneGate) {
                if (!IsCurrentAdmission(entry, completion) || !IsLaneActiveWithoutLock(entry) || !IsCurrentRequest(entry)) {
                    return false;
                }

                string laneIdentity = entry.Lane.ViewKey;
                hadValidatorState = _lastEtagByLane.ContainsKey(laneIdentity)
                    || _lastNoEtagSignatureByLane.ContainsKey(laneIdentity);
            }

            ProjectionFallbackLane lane = entry.Lane;
            dispatcher.Dispatch(new LoadPageNotModifiedAction(lane.ViewKey, lane.Skip, result.Items) {
                OriginScopeGeneration = entry.OriginScopeGeneration,
                RequestIdentity = entry.OriginRequestIdentity,
                RequestGeneration = entry.OriginRequestGeneration,
                TotalCount = result.TotalCount,
                MaxCachedPages = options.CurrentValue.MaxCachedPages,
            });
            if (!HasReducerPage(lane)) {
                return false;
            }
            return !hadValidatorState || TryRecordValidatorState(entry, result);
        }
    }

    private void DispatchPageSuccess(LaneEntry entry, ProjectionPageResult result)
        => dispatcher.Dispatch(new LoadPageSucceededAction(
            entry.Lane.ViewKey,
            entry.Lane.Skip,
            result.Items,
            result.TotalCount,
            elapsedMs: 0) { OriginScopeGeneration = entry.OriginScopeGeneration, RequestIdentity = entry.OriginRequestIdentity, RequestGeneration = entry.OriginRequestGeneration });

    private static ProjectionLaneRefreshResult MapCustomOutcome(ProjectionFallbackLaneRefreshOutcome outcome)
        => outcome switch {
            ProjectionFallbackLaneRefreshOutcome.Changed => ProjectionLaneRefreshResult.Changed,
            ProjectionFallbackLaneRefreshOutcome.NotModified => ProjectionLaneRefreshResult.NotModified,
            _ => ProjectionLaneRefreshResult.Skipped,
        };

    private sealed class LaneEntry(ProjectionFallbackLane lane, long registeredGeneration, string? registeredUser, bool hasScopeProof) {
        public object DispatchGate { get; } = new();
        public ProjectionFallbackLane Lane { get; } = lane;
        public long RegisteredGeneration { get; } = registeredGeneration;
        public string? RegisteredUser { get; } = registeredUser;
        public bool HasScopeProof { get; } = hasScopeProof;
        public long OriginScopeGeneration { get; set; }
        public string OriginRequestIdentity { get; set; } = string.Empty;
        public long? OriginRequestGeneration { get; set; }
        public int RefCount = 1;
    }

    private enum ProjectionLaneRefreshResult {
        Skipped,
        ReadSkipped,
        StaleRead,
        NotModified,
        Changed,
        Failed,
    }

    private sealed class Registration(Action dispose) : IDisposable {
        private int _disposed;

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                dispose();
            }
        }
    }

    private sealed class NoopDispatcher : IDispatcher {
#pragma warning disable CS0067 // Required by Fluxor IDispatcher; no subscribers for fallback constructor.
        public event EventHandler<ActionDispatchedEventArgs>? ActionDispatched;
#pragma warning restore CS0067

        public void Dispatch(object action) {
        }
    }

    private sealed class StaticLoadedPageState : IState<LoadedPageState> {
        public LoadedPageState Value { get; } = new();

#pragma warning disable CS0067 // Static state never changes.
        public event EventHandler? StateChanged;
#pragma warning restore CS0067
    }
}
