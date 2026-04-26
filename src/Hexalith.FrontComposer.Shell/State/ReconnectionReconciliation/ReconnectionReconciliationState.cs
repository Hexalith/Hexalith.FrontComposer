using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Transient status for the active reconnect reconciliation pass.</summary>
public enum ReconnectionReconciliationStatus {
    Idle,
    Reconciling,
    Refreshed,
}

/// <summary>Immutable snapshot consumed by Shell status components.</summary>
public sealed record ReconnectionReconciliationSnapshot(
    ReconnectionReconciliationStatus Status,
    long Epoch,
    bool Changed,
    DateTimeOffset LastTransitionAt);

/// <summary>Scoped per-circuit reconciliation state.</summary>
public interface IReconnectionReconciliationState {
    ReconnectionReconciliationSnapshot Current { get; }

    IDisposable Subscribe(Action<ReconnectionReconciliationSnapshot> handler, bool replay = true);

    void Start(long epoch);

    void Complete(long epoch, bool changed);

    void Reset();
}

/// <summary>In-memory reconciliation status service. History is intentionally not persisted.</summary>
public sealed class ReconnectionReconciliationStateService(
    TimeProvider timeProvider,
    ILogger<ReconnectionReconciliationStateService> logger) : IReconnectionReconciliationState {
    private readonly object _sync = new();
    private readonly List<Action<ReconnectionReconciliationSnapshot>> _handlers = [];
    private ReconnectionReconciliationSnapshot _current = new(
        ReconnectionReconciliationStatus.Idle,
        Epoch: 0,
        Changed: false,
        LastTransitionAt: timeProvider.GetUtcNow());

    public ReconnectionReconciliationSnapshot Current {
        get {
            lock (_sync) {
                return _current;
            }
        }
    }

    public IDisposable Subscribe(Action<ReconnectionReconciliationSnapshot> handler, bool replay = true) {
        ArgumentNullException.ThrowIfNull(handler);
        lock (_sync) {
            _handlers.Add(handler);
            if (replay) {
                InvokeSafe(handler, _current);
            }
        }

        return new Subscription(this, handler);
    }

    public void Start(long epoch)
        => Apply(new ReconnectionReconciliationSnapshot(
            ReconnectionReconciliationStatus.Reconciling,
            epoch,
            Changed: false,
            timeProvider.GetUtcNow()));

    public void Complete(long epoch, bool changed) {
        lock (_sync) {
            if (epoch != _current.Epoch) {
                return;
            }
        }

        Apply(new ReconnectionReconciliationSnapshot(
            changed ? ReconnectionReconciliationStatus.Refreshed : ReconnectionReconciliationStatus.Idle,
            epoch,
            changed,
            timeProvider.GetUtcNow()));
    }

    public void Reset()
        => Apply(new ReconnectionReconciliationSnapshot(
            ReconnectionReconciliationStatus.Idle,
            _current.Epoch,
            Changed: false,
            timeProvider.GetUtcNow()));

    private void Apply(ReconnectionReconciliationSnapshot snapshot) {
        Action<ReconnectionReconciliationSnapshot>[] handlers;
        lock (_sync) {
            if (_current.Status == snapshot.Status
                && _current.Epoch == snapshot.Epoch
                && _current.Changed == snapshot.Changed) {
                return;
            }

            _current = snapshot;
            handlers = [.. _handlers];
        }

        foreach (Action<ReconnectionReconciliationSnapshot> handler in handlers) {
            InvokeSafe(handler, snapshot);
        }
    }

    private void InvokeSafe(Action<ReconnectionReconciliationSnapshot> handler, ReconnectionReconciliationSnapshot snapshot) {
        try {
            handler(snapshot);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            logger.LogWarning(
                "Reconnection reconciliation state subscriber threw. FailureCategory={FailureCategory}",
                ex.GetType().Name);
        }
    }

    private void Unsubscribe(Action<ReconnectionReconciliationSnapshot> handler) {
        lock (_sync) {
            _ = _handlers.Remove(handler);
        }
    }

    private sealed class Subscription(ReconnectionReconciliationStateService owner, Action<ReconnectionReconciliationSnapshot> handler) : IDisposable {
        private int _disposed;

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) == 0) {
                owner.Unsubscribe(handler);
            }
        }
    }
}
