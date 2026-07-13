using Hexalith.FrontComposer.Shell.Services;

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

    void Reset(long? expectedEpoch = null);
}

/// <summary>In-memory reconciliation status service. History is intentionally not persisted.</summary>
/// <remarks>
/// Story 11.15 (M19 cluster 5): the handler-list / current-and-replay / fault-isolation /
/// idempotent-unsubscribe mechanics are delegated to the shared <see cref="SnapshotPublisher{T}"/>
/// primitive. This service retains its distinct semantics: epoch + status staleness gating,
/// logical-duplicate dedup, and atomic current-then-replay ordering (the guards run inside the
/// publisher's lock via <see cref="SnapshotPublisher{T}.TryApply"/>). It intentionally does NOT
/// implement <see cref="IDisposable"/>.
/// </remarks>
public sealed class ReconnectionReconciliationStateService(
    TimeProvider timeProvider,
    ILogger<ReconnectionReconciliationStateService> logger) : IReconnectionReconciliationState {
    private readonly SnapshotPublisher<ReconnectionReconciliationSnapshot> _publisher = new(
        new ReconnectionReconciliationSnapshot(
            ReconnectionReconciliationStatus.Idle,
            Epoch: 0,
            Changed: false,
            timeProvider.GetUtcNow()),
        ex => logger.LogWarning(
            ex,
            "Reconnection reconciliation state subscriber threw. FailureCategory={FailureCategory}",
            ex.GetType().Name));

    public ReconnectionReconciliationSnapshot Current => _publisher.Current;

    public IDisposable Subscribe(Action<ReconnectionReconciliationSnapshot> handler, bool replay = true)
        => _publisher.Subscribe(handler, replay);

    public void Start(long epoch)
        => Apply(new ReconnectionReconciliationSnapshot(
            ReconnectionReconciliationStatus.Reconciling,
            epoch,
            Changed: false,
            timeProvider.GetUtcNow()));

    public void Complete(long epoch, bool changed) {
        // P13/P15 — atomic epoch + status check inside the lock. Stale epoch results never overwrite
        // a fresh Start; once the pass has settled to Refreshed/Idle a duplicate Complete is silently
        // ignored.
        if (!_publisher.TryApply(
                current => {
                    if (epoch != current.Epoch) {
                        return null;
                    }

                    if (current.Status != ReconnectionReconciliationStatus.Reconciling) {
                        return null;
                    }

                    ReconnectionReconciliationSnapshot candidate = new(
                        changed ? ReconnectionReconciliationStatus.Refreshed : ReconnectionReconciliationStatus.Idle,
                        epoch,
                        changed,
                        timeProvider.GetUtcNow());
                    return IsLogicalDuplicate(current, candidate) ? null : candidate;
                },
                out ReconnectionReconciliationSnapshot snapshot,
                out Action<ReconnectionReconciliationSnapshot>[] handlers)) {
            return;
        }

        _publisher.Deliver(handlers, snapshot);
    }

    public void Reset(long? expectedEpoch = null) {
        // P14 — read the latest epoch and publish under the same lock. A stale Reset that races with a
        // fresh Start cannot clobber the new pass because the snapshot it builds carries the
        // now-current epoch.
        if (!_publisher.TryApply(
                current => {
                    if (expectedEpoch.HasValue && expectedEpoch.Value != current.Epoch) {
                        return null;
                    }

                    ReconnectionReconciliationSnapshot candidate = new(
                        ReconnectionReconciliationStatus.Idle,
                        current.Epoch,
                        Changed: false,
                        timeProvider.GetUtcNow());
                    return IsLogicalDuplicate(current, candidate) ? null : candidate;
                },
                out ReconnectionReconciliationSnapshot snapshot,
                out Action<ReconnectionReconciliationSnapshot>[] handlers)) {
            return;
        }

        _publisher.Deliver(handlers, snapshot);
    }

    private void Apply(ReconnectionReconciliationSnapshot snapshot) {
        if (!_publisher.TryApply(
                current => IsLogicalDuplicate(current, snapshot) ? null : snapshot,
                out ReconnectionReconciliationSnapshot published,
                out Action<ReconnectionReconciliationSnapshot>[] handlers)) {
            return;
        }

        _publisher.Deliver(handlers, published);
    }

    private static bool IsLogicalDuplicate(ReconnectionReconciliationSnapshot current, ReconnectionReconciliationSnapshot next)
        => current.Status == next.Status
            && current.Epoch == next.Epoch
            && current.Changed == next.Changed;
}
