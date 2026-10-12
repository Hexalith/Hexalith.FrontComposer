using Fluxor;

using Hexalith.FrontComposer.Shell.Services;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.PendingCommands;
using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>
/// Epoch-scoped reconciliation coordinator. It owns orchestration only; visible lane ownership
/// remains in <see cref="IProjectionFallbackRefreshScheduler"/>.
/// </summary>
public sealed class ReconnectionReconciliationCoordinator : IReconnectionReconciliationCoordinator, IDisposable {
    /// <summary>Default sweep TTL applied to <see cref="MarkReconciliationSweepAction"/>. AC3: ≤ 700ms.</summary>
    private static readonly TimeSpan SweepTtl = TimeSpan.FromMilliseconds(700);
    private static readonly TimeSpan IncompleteReadWaitLimit = TimeSpan.FromSeconds(30);

    private readonly IProjectionFallbackRefreshScheduler _refreshScheduler;
    private readonly IReconnectionReconciliationState _state;
    private readonly IDispatcher _dispatcher;
    private readonly TimeProvider _timeProvider;
    private readonly IPendingCommandPollingCoordinator? _pendingCommandPolling;
    private readonly ILogger<ReconnectionReconciliationCoordinator> _logger;
    private readonly object _sync = new();
    private CancellationTokenSource? _activeCts;
    private ITimer? _sweepCleanupTimer;
    private long _latestEpoch;
    private int _disposed;

    public ReconnectionReconciliationCoordinator(
        IProjectionFallbackRefreshScheduler refreshScheduler,
        IReconnectionReconciliationState state,
        IDispatcher dispatcher,
        TimeProvider timeProvider,
        ILogger<ReconnectionReconciliationCoordinator> logger,
        IPendingCommandPollingCoordinator? pendingCommandPolling = null) {
        ArgumentNullException.ThrowIfNull(refreshScheduler);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _refreshScheduler = refreshScheduler;
        _state = state;
        _dispatcher = dispatcher;
        _timeProvider = timeProvider;
        _pendingCommandPolling = pendingCommandPolling;
        _logger = logger;
    }

    public async Task<ProjectionReconciliationRefreshResult> ReconcileAsync(CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        CancellationTokenSource? previous;
        CancellationTokenSource linked;
        long epoch;
        lock (_sync) {
            // P10 — cancel-then-detach. Defer Dispose of the previous CTS until the new one is
            // installed so an in-flight pass observing _activeCts.Token cannot hit
            // ObjectDisposedException between Cancel() and Dispose().
            previous = _activeCts;
            _activeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked = _activeCts;
            epoch = ++_latestEpoch;
        }

        if (previous is not null) {
            try {
                previous.Cancel();
            }
            catch (ObjectDisposedException) {
                // Already disposed — observed token is already cancelled.
            }
        }

        // P12 — wrap state.Start so a buggy subscriber can't strand the coordinator with a
        // bumped epoch that never completes.
        try {
            _state.Start(epoch);
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            FrontComposerHotPathLog.ReconciliationStateStartFailed(
                _logger,
                epoch,
                ex.GetType().Name);
        }

        ProjectionReconciliationRefreshResult result;
        try {
            DateTimeOffset waitDeadline = _timeProvider.GetUtcNow() + IncompleteReadWaitLimit;
            using CancellationTokenSource scopedInitial = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
            Task<ProjectionReconciliationRefreshResult> initialRead = _refreshScheduler
                .TriggerReconciliationOnceAsync(epoch, scopedInitial.Token);
            try {
                result = await initialRead.WaitAsync(IncompleteReadWaitLimit, _timeProvider, linked.Token)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException) {
                Task cancellation = scopedInitial.CancelAsync();
                if (_refreshScheduler is IProjectionAbandonedReadRetirer retirer) {
                    retirer.RetireAbandonedReads(epoch);
                }
                _ = cancellation.ContinueWith(static task => _ = task.Exception,
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                _ = initialRead.ContinueWith(static task => _ = task.Exception,
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                result = new ProjectionReconciliationRefreshResult(0, []) { HasIncompleteEligibleReads = true };
            }
            int refreshedCount = result.RefreshedCount;
            HashSet<string> changed = new(result.ChangedViewKeys, StringComparer.Ordinal);
            HashSet<string> failed = new(result.FailedViewKeys, StringComparer.Ordinal);
            HashSet<string> skipped = new(result.SkippedViewKeys, StringComparer.Ordinal);
            HashSet<string> incomplete = new(result.IncompleteViewKeys, StringComparer.Ordinal);
            bool unknownIncomplete = result.HasIncompleteEligibleReads && incomplete.Count == 0;
            // An eligible lane already owned by another read is still unresolved. Keep the
            // reconciliation episode open until that read (or its pending replay) releases the
            // lane, then run one newly budgeted pass against current scope and epoch state.
            while (incomplete.Count > 0 && _refreshScheduler is IProjectionIncompleteReadWaiter waiter) {
                TimeSpan remaining = waitDeadline - _timeProvider.GetUtcNow();
                if (remaining <= TimeSpan.Zero) {
                    break;
                }
                CancellationTokenSource scopedWait = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
                bool cancellationDetached = false;
                bool released;
                try {
                    released = result.IncompleteReadTasks.Count == 0
                        || await waiter.WaitForIncompleteReadAsync(epoch, result, scopedWait.Token)
                            .WaitAsync(remaining, _timeProvider, linked.Token).ConfigureAwait(false);
                }
                catch (TimeoutException) {
                    Task cancellation = scopedWait.CancelAsync();
                    cancellationDetached = true;
                    _ = CompleteTimedOutWaitCancellationAsync(scopedWait, cancellation);
                    break;
                }
                finally {
                    if (!cancellationDetached) {
                        scopedWait.Dispose();
                    }
                }
                if (!released) {
                    break;
                }
                TimeSpan recheckRemaining = waitDeadline - _timeProvider.GetUtcNow();
                if (recheckRemaining <= TimeSpan.Zero) {
                    break;
                }
                using CancellationTokenSource scopedPass = CancellationTokenSource.CreateLinkedTokenSource(linked.Token);
                Task<ProjectionReconciliationRefreshResult> recheck = waiter.RecheckIncompleteReadsAsync(
                    epoch, incomplete, scopedPass.Token);
                try {
                    result = await recheck.WaitAsync(recheckRemaining, _timeProvider, linked.Token)
                        .ConfigureAwait(false);
                }
                catch (TimeoutException) {
                    Task cancellation = scopedPass.CancelAsync();
                    if (_refreshScheduler is IProjectionAbandonedReadRetirer retirer) {
                        retirer.RetireAbandonedReads(epoch);
                    }
                    _ = cancellation.ContinueWith(static task => _ = task.Exception,
                        CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                    _ = recheck.ContinueWith(static task => _ = task.Exception,
                        CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
                    break;
                }
                refreshedCount += result.RefreshedCount;
                changed.UnionWith(result.ChangedViewKeys);
                failed.ExceptWith(result.RefreshedViewKeys);
                failed.UnionWith(result.FailedViewKeys);
                skipped.ExceptWith(result.RefreshedViewKeys);
                skipped.UnionWith(result.SkippedViewKeys);
                HashSet<string> unresolvedBefore = new(incomplete, StringComparer.Ordinal);
                incomplete.ExceptWith(result.RefreshedViewKeys);
                incomplete.ExceptWith(result.FailedViewKeys);
                incomplete.UnionWith(result.IncompleteViewKeys);
                if (incomplete.SetEquals(unresolvedBefore) && result.IncompleteReadTasks.Count == 0) {
                    // No eligible held read was found for these exact keys. Keep the
                    // recovery unresolved instead of spinning or claiming freshness.
                    break;
                }
            }
            if (incomplete.Count > 0 && _timeProvider.GetUtcNow() >= waitDeadline
                && _refreshScheduler is IProjectionAbandonedReadRetirer expiredReadRetirer) {
                // A held nudge/fallback read was not started with scopedPass's token.
                // Retire the exact observed admission before a later current sweep.
                expiredReadRetirer.RetireAbandonedReads(epoch);
            }
            result = result with {
                RefreshedCount = refreshedCount,
                ChangedViewKeys = changed.ToArray(),
                FailedViewKeys = failed.ToArray(),
                SkippedViewKeys = skipped.ToArray(),
                HasIncompleteEligibleReads = unknownIncomplete || incomplete.Count > 0,
                IncompleteViewKeys = incomplete.ToArray(),
            };
        }
        // P11 — accept caller-side cancellation as well as linked cancellation.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || linked.IsCancellationRequested) {
            if (IsCurrent(epoch)) {
                _state.Complete(epoch, changed: false, succeeded: false);
            }

            DisposeCompletedCts(linked);
            return ProjectionReconciliationRefreshResult.Empty;
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            FrontComposerHotPathLog.ReconciliationFailed(
                _logger,
                epoch,
                ex.GetType().Name);
            if (IsCurrent(epoch)) {
                _state.Complete(epoch, changed: false, succeeded: false);
            }

            DisposeCompletedCts(linked);
            return ProjectionReconciliationRefreshResult.Empty;
        }

        if (IsCurrent(epoch)) {
            // DN1=a — dispatch sweep markers for changed lanes BEFORE Complete so subscribers
            // see Refreshed and the marker state in the same render cycle.
            if (result.ChangedViewKeys.Count > 0) {
                DateTimeOffset now = _timeProvider.GetUtcNow();
                DateTimeOffset expiresAt = now + SweepTtl;
                try {
                    _dispatcher.Dispatch(new MarkReconciliationSweepAction(epoch, result.ChangedViewKeys, expiresAt, now));
                }
                catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                    FrontComposerHotPathLog.ReconciliationSweepMarkerFailed(
                        _logger,
                        epoch,
                        ex.GetType().Name);
                }
            }

            _state.Complete(epoch, result.ChangedViewKeys.Count > 0,
                succeeded: result.FailedViewKeys.Count == 0 && result.SkippedViewKeys.Count == 0 && !result.HasIncompleteEligibleReads,
                dataRead: result.RefreshedCount > 0);

            ScheduleSweepCleanup(epoch);

            // Story 5-5 DN1 — feed the shared pending-command resolver from the reconnect path so
            // commands accepted during the disconnected window resolve through the SAME path the
            // polling fallback uses. Failures are logged and swallowed; the reconciliation result
            // is independent of pending-command outcome.
            if (_pendingCommandPolling is not null) {
                try {
                    _ = await _pendingCommandPolling.PollOnceAsync(linked.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (linked.IsCancellationRequested) {
                    // Disposal or epoch supersession; nothing to do.
                }
                catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                    FrontComposerHotPathLog.ReconciliationPendingResolutionFailed(
                        _logger,
                        epoch,
                        ex.GetType().Name);
                }
            }
        }

        DisposeCompletedCts(linked);
        return result;
    }

    private static async Task CompleteTimedOutWaitCancellationAsync(CancellationTokenSource source, Task cancellation) {
        _ = cancellation.ContinueWith(static task => _ = task.Exception,
            CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted, TaskScheduler.Default);
        try {
            await cancellation.WaitAsync(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)
            && cancellation.Exception?.Flatten().InnerExceptions.Any(ExceptionGuard.IsFatal) != true) {
            // The visible deadline has expired; a held callback cannot retain cleanup forever.
        }
        finally {
            source.Dispose();
        }
    }

    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        CancellationTokenSource? toDispose;
        lock (_sync) {
            toDispose = _activeCts;
            _activeCts = null;
        }

        if (toDispose is not null) {
            try {
                toDispose.Cancel();
            }
            catch (ObjectDisposedException) {
                // No-op.
            }

            toDispose.Dispose();
        }

        ITimer? cleanup = Interlocked.Exchange(ref _sweepCleanupTimer, null);
        cleanup?.Dispose();

        // W9 — Reset() publishes Idle to subscribers; per-circuit scoping makes the off-thread
        // notification safe in practice. Wrapped to keep dispose robust.
        try {
            _state.Reset();
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            FrontComposerHotPathLog.ReconciliationStateResetFailed(
                _logger,
                ex.GetType().Name);
        }
    }

    private bool IsCurrent(long epoch) {
        lock (_sync) {
            return epoch == _latestEpoch && _disposed == 0;
        }
    }

    private void ScheduleSweepCleanup(long epoch) {
        ITimer timer = _timeProvider.CreateTimer(
            _ => {
                if (_disposed != 0 || !IsCurrent(epoch)) {
                    return;
                }

                try {
                    _dispatcher.Dispatch(new ClearExpiredReconciliationSweepsAction(_timeProvider.GetUtcNow()));
                }
                catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
                    FrontComposerHotPathLog.ReconciliationSweepCleanupFailed(
                        _logger,
                        epoch,
                        ex.GetType().Name);
                }
            },
            state: null,
            dueTime: SweepTtl,
            period: Timeout.InfiniteTimeSpan);

        ITimer? previous = Interlocked.Exchange(ref _sweepCleanupTimer, timer);
        previous?.Dispose();
    }

    private void DisposeCompletedCts(CancellationTokenSource linked) {
        lock (_sync) {
            if (ReferenceEquals(_activeCts, linked)) {
                _activeCts = null;
            }
        }

        linked.Dispose();
    }

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(_disposed != 0, typeof(ReconnectionReconciliationCoordinator));
}
