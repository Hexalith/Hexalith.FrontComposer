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
public sealed class ReconnectionReconciliationCoordinator(
    IProjectionFallbackRefreshScheduler refreshScheduler,
    IReconnectionReconciliationState state,
    ILogger<ReconnectionReconciliationCoordinator> logger) : IReconnectionReconciliationCoordinator, IDisposable {
    private readonly object _sync = new();
    private CancellationTokenSource? _activeCts;
    private long _latestEpoch;
    private int _disposed;

    public async Task<ProjectionReconciliationRefreshResult> ReconcileAsync(CancellationToken cancellationToken = default) {
        ThrowIfDisposed();
        CancellationTokenSource linked;
        long epoch;
        lock (_sync) {
            _activeCts?.Cancel();
            _activeCts?.Dispose();
            _activeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            linked = _activeCts;
            epoch = ++_latestEpoch;
        }

        state.Start(epoch);
        try {
            ProjectionReconciliationRefreshResult result = await refreshScheduler
                .TriggerReconciliationOnceAsync(epoch, linked.Token)
                .ConfigureAwait(false);
            if (IsCurrent(epoch)) {
                state.Complete(epoch, result.ChangedViewKeys.Count > 0);
            }

            return result;
        }
        catch (OperationCanceledException) when (linked.IsCancellationRequested) {
            if (IsCurrent(epoch)) {
                state.Complete(epoch, changed: false);
            }

            return ProjectionReconciliationRefreshResult.Empty;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            logger.LogWarning(
                "Reconnection reconciliation failed. Epoch={Epoch}, FailureCategory={FailureCategory}",
                epoch,
                ex.GetType().Name);
            if (IsCurrent(epoch)) {
                state.Complete(epoch, changed: false);
            }

            return ProjectionReconciliationRefreshResult.Empty;
        }
    }

    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        lock (_sync) {
            _activeCts?.Cancel();
            _activeCts?.Dispose();
            _activeCts = null;
        }

        state.Reset();
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
