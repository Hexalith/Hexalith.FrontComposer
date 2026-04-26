using Fluxor;

using Hexalith.FrontComposer.Shell.State.ProjectionConnection;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Runs one visible-lane catch-up pass after the projection hub reconnects.</summary>
public interface IReconnectionReconciliationCoordinator {
    Task<ProjectionReconciliationRefreshResult> ReconcileAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Epoch-scoped reconciliation coordinator. It owns orchestration only; visible lane ownership
/// remains in <see cref="IProjectionFallbackRefreshScheduler"/>.
/// </summary>
public sealed class ReconnectionReconciliationCoordinator : IReconnectionReconciliationCoordinator, IDisposable {
    /// <summary>Default sweep TTL applied to <see cref="MarkReconciliationSweepAction"/>. AC3: ≤ 700ms.</summary>
    private static readonly TimeSpan SweepTtl = TimeSpan.FromMilliseconds(700);

    private readonly IProjectionFallbackRefreshScheduler _refreshScheduler;
    private readonly IReconnectionReconciliationState _state;
    private readonly IDispatcher _dispatcher;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ReconnectionReconciliationCoordinator> _logger;
    private readonly object _sync = new();
    private CancellationTokenSource? _activeCts;
    private long _latestEpoch;
    private int _disposed;

    public ReconnectionReconciliationCoordinator(
        IProjectionFallbackRefreshScheduler refreshScheduler,
        IReconnectionReconciliationState state,
        IDispatcher dispatcher,
        TimeProvider timeProvider,
        ILogger<ReconnectionReconciliationCoordinator> logger) {
        ArgumentNullException.ThrowIfNull(refreshScheduler);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(logger);
        _refreshScheduler = refreshScheduler;
        _state = state;
        _dispatcher = dispatcher;
        _timeProvider = timeProvider;
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
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            _logger.LogWarning(
                "Reconciliation state.Start threw. Epoch={Epoch}, FailureCategory={FailureCategory}",
                epoch,
                ex.GetType().Name);
        }

        ProjectionReconciliationRefreshResult result;
        try {
            result = await _refreshScheduler
                .TriggerReconciliationOnceAsync(epoch, linked.Token)
                .ConfigureAwait(false);
        }
        // P11 — accept caller-side cancellation as well as linked cancellation.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || linked.IsCancellationRequested) {
            if (IsCurrent(epoch)) {
                _state.Complete(epoch, changed: false);
            }

            // Best-effort dispose of the now-superseded previous CTS.
            previous?.Dispose();
            return ProjectionReconciliationRefreshResult.Empty;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            _logger.LogWarning(
                "Reconnection reconciliation failed. Epoch={Epoch}, FailureCategory={FailureCategory}",
                epoch,
                ex.GetType().Name);
            if (IsCurrent(epoch)) {
                _state.Complete(epoch, changed: false);
            }

            previous?.Dispose();
            return ProjectionReconciliationRefreshResult.Empty;
        }

        // Dispose the previous CTS now that the new pass has progressed past the await.
        previous?.Dispose();

        if (IsCurrent(epoch)) {
            // DN1=a — dispatch sweep markers for changed lanes BEFORE Complete so subscribers
            // see Refreshed and the marker state in the same render cycle.
            if (result.ChangedViewKeys.Count > 0) {
                DateTimeOffset expiresAt = _timeProvider.GetUtcNow() + SweepTtl;
                try {
                    _dispatcher.Dispatch(new MarkReconciliationSweepAction(epoch, result.ChangedViewKeys, expiresAt));
                }
                catch (Exception ex) when (ex is not OutOfMemoryException) {
                    _logger.LogWarning(
                        "Sweep marker dispatch failed. Epoch={Epoch}, FailureCategory={FailureCategory}",
                        epoch,
                        ex.GetType().Name);
                }
            }

            _state.Complete(epoch, result.ChangedViewKeys.Count > 0);

            // Schedule expired-sweep cleanup on the same render path so the marker state never
            // grows unbounded. The reducer is a no-op when no markers have expired (W8).
            try {
                _dispatcher.Dispatch(new ClearExpiredReconciliationSweepsAction(_timeProvider.GetUtcNow()));
            }
            catch (Exception ex) when (ex is not OutOfMemoryException) {
                _logger.LogWarning(
                    "Sweep cleanup dispatch failed. Epoch={Epoch}, FailureCategory={FailureCategory}",
                    epoch,
                    ex.GetType().Name);
            }
        }

        return result;
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

        // W9 — Reset() publishes Idle to subscribers; per-circuit scoping makes the off-thread
        // notification safe in practice. Wrapped to keep dispose robust.
        try {
            _state.Reset();
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            _logger.LogWarning(
                "Reconciliation state.Reset threw during dispose. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
    }

    private bool IsCurrent(long epoch) {
        lock (_sync) {
            return epoch == _latestEpoch && _disposed == 0;
        }
    }

    private void ThrowIfDisposed() {
        if (_disposed != 0) {
            throw new ObjectDisposedException(nameof(ReconnectionReconciliationCoordinator));
        }
    }
}
