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
        // P13/P15 — atomic epoch + status check inside the lock. Stale epoch results never
        // overwrite a fresh Start; once the pass has settled to Refreshed/Idle a duplicate
        // Complete is silently ignored.
        ReconnectionReconciliationSnapshot? next = null;
        Action<ReconnectionReconciliationSnapshot>[] handlers = [];
        lock (_sync) {
            if (epoch != _current.Epoch) {
                return;
            }

            if (_current.Status != ReconnectionReconciliationStatus.Reconciling) {
                return;
            }

            ReconnectionReconciliationSnapshot snapshot = new(
                changed ? ReconnectionReconciliationStatus.Refreshed : ReconnectionReconciliationStatus.Idle,
                epoch,
                changed,
                timeProvider.GetUtcNow());
            if (IsLogicalDuplicate(_current, snapshot)) {
                return;
            }

            _current = snapshot;
            next = snapshot;
            handlers = [.. _handlers];
        }

        foreach (Action<ReconnectionReconciliationSnapshot> handler in handlers) {
            InvokeSafe(handler, next!);
        }
    }

    public void Reset() {
        // P14 — read the latest epoch and publish under the same lock. A stale Reset that
        // races with a fresh Start cannot clobber the new pass because the snapshot it builds
        // carries the now-current epoch.
        ReconnectionReconciliationSnapshot? next = null;
        Action<ReconnectionReconciliationSnapshot>[] handlers = [];
        lock (_sync) {
            ReconnectionReconciliationSnapshot snapshot = new(
                ReconnectionReconciliationStatus.Idle,
                _current.Epoch,
                Changed: false,
                timeProvider.GetUtcNow());
            if (IsLogicalDuplicate(_current, snapshot)) {
                return;
            }

            _current = snapshot;
            next = snapshot;
            handlers = [.. _handlers];
        }

        foreach (Action<ReconnectionReconciliationSnapshot> handler in handlers) {
            InvokeSafe(handler, next!);
        }
    }

    private void Apply(ReconnectionReconciliationSnapshot snapshot) {
        Action<ReconnectionReconciliationSnapshot>[] handlers;
        lock (_sync) {
            if (IsLogicalDuplicate(_current, snapshot)) {
                return;
            }

            _current = snapshot;
            handlers = [.. _handlers];
        }

        foreach (Action<ReconnectionReconciliationSnapshot> handler in handlers) {
            InvokeSafe(handler, snapshot);
        }
    }

    private static bool IsLogicalDuplicate(ReconnectionReconciliationSnapshot current, ReconnectionReconciliationSnapshot next)
        => current.Status == next.Status
            && current.Epoch == next.Epoch
            && current.Changed == next.Changed;

    private void InvokeSafe(Action<ReconnectionReconciliationSnapshot> handler, ReconnectionReconciliationSnapshot snapshot) {
        try {
            handler(snapshot);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            // P16 — log message + stack so diagnosing a subscriber crash does not require a
            // debugger attach. Exception type is preserved as a structured field so log
            // aggregators can group failures.
            logger.LogWarning(
                ex,
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
