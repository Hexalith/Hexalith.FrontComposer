namespace Hexalith.FrontComposer.Shell.Services;

/// <summary>
/// Scoped replayable snapshot pub/sub primitive (Story 11.15, M19 cluster 5). Consolidates the
/// handler-list / current-and-replay / per-subscriber fault-isolation / idempotent-unsubscribe
/// mechanics that <c>ProjectionConnectionStateService</c> and
/// <c>ReconnectionReconciliationStateService</c> previously hand-rolled as near-identical copies, so
/// those behaviors are defined once and reused by both.
/// </summary>
/// <remarks>
/// <para>
/// Owner-specific transition rules (epoch / reconnect-attempt accumulation / dedup / logging /
/// telemetry) are deliberately NOT baked in here — each owner passes its decision as the
/// <c>transition</c> delegate to <see cref="TryApply"/> (evaluated atomically under the lock) and
/// performs its own logging/telemetry between the state advance and <see cref="Deliver"/>, preserving
/// its "mutate under lock → log → fan-out" ordering.
/// </para>
/// <para>
/// Concurrency: <see cref="Current"/>, <see cref="Subscribe"/>, <see cref="Publish"/> and
/// <see cref="TryApply"/> serialize on a single lock; replay (<see cref="Subscribe"/> with
/// <c>replay:true</c>) runs UNDER the lock so a concurrent advance can never deliver a fresher
/// snapshot before a joining subscriber has replayed the value current at join time (no
/// fresh-then-stale ordering). The subscriber list is snapshotted under the lock and invoked outside
/// it, so a handler may re-enter <see cref="Subscribe"/> / <see cref="Publish"/> without deadlocking.
/// </para>
/// </remarks>
/// <typeparam name="T">The immutable snapshot type (a reference type / record).</typeparam>
internal sealed class SnapshotPublisher<T>
    where T : class {
    private readonly object _sync = new();
    private readonly List<Subscription> _subscriptions = [];
    private readonly Action<Exception> _subscriberFaultHandler;
    private T _current;

    /// <summary>Initializes a new instance of the <see cref="SnapshotPublisher{T}"/> class.</summary>
    /// <param name="initial">The initial current snapshot replayed to eager subscribers.</param>
    /// <param name="subscriberFaultHandler">Owner callback used to report non-fatal subscriber faults.</param>
    public SnapshotPublisher(T initial, Action<Exception> subscriberFaultHandler) {
        ArgumentNullException.ThrowIfNull(initial);
        ArgumentNullException.ThrowIfNull(subscriberFaultHandler);
        _current = initial;
        _subscriberFaultHandler = subscriberFaultHandler;
    }

    /// <summary>Gets the current snapshot.</summary>
    public T Current {
        get {
            lock (_sync) {
                return _current;
            }
        }
    }

    /// <summary>
    /// Subscribes <paramref name="handler"/>, optionally replaying the current snapshot under the lock.
    /// </summary>
    /// <param name="handler">The subscriber callback.</param>
    /// <param name="replay">When <see langword="true"/> (default), replays <see cref="Current"/> immediately.</param>
    /// <returns>A disposable whose disposal idempotently unsubscribes the handler.</returns>
    public IDisposable Subscribe(Action<T> handler, bool replay = true) {
        ArgumentNullException.ThrowIfNull(handler);
        Subscription subscription = new(this, handler);

        // Replay under the lock so a concurrent advance that runs between add-to-handlers and replay
        // cannot deliver fresh-then-stale ordering. Subscribers must not call back into
        // Publish/TryApply/Subscribe inside their replay handler.
        lock (_sync) {
            _subscriptions.Add(subscription);
            if (replay) {
                InvokeSafe(subscription.Dispatch, _current);
            }
        }

        return subscription;
    }

    /// <summary>Unconditionally advances <see cref="Current"/> to <paramref name="snapshot"/> and fans out.</summary>
    /// <param name="snapshot">The new snapshot.</param>
    public void Publish(T snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);
        Action<T>[] handlers;
        lock (_sync) {
            _current = snapshot;
            handlers = CaptureHandlers();
        }

        foreach (Action<T> handler in handlers) {
            InvokeSafe(handler, snapshot);
        }
    }

    /// <summary>
    /// Atomically evaluates <paramref name="transition"/> against the current snapshot under the lock.
    /// When it returns <see langword="null"/> the call is a no-op (returns <see langword="false"/>; no
    /// state change, no delivery). Otherwise <see cref="Current"/> is advanced and the subscribers are
    /// snapshotted into <paramref name="handlers"/> WITHOUT fanning out, so the caller can run its own
    /// logging/telemetry before invoking <see cref="Deliver"/>.
    /// </summary>
    /// <param name="transition">Owner decision run under the lock; returns the next snapshot, or <see langword="null"/> to skip.</param>
    /// <param name="snapshot">When this returns <see langword="true"/>, the published snapshot; otherwise the unchanged current.</param>
    /// <param name="handlers">When this returns <see langword="true"/>, the subscriber snapshot to deliver; otherwise empty.</param>
    /// <returns><see langword="true"/> when a new snapshot was published; otherwise <see langword="false"/>.</returns>
    public bool TryApply(Func<T, T?> transition, out T snapshot, out Action<T>[] handlers) {
        ArgumentNullException.ThrowIfNull(transition);
        lock (_sync) {
            T? next = transition(_current);
            if (next is null) {
                snapshot = _current;
                handlers = [];
                return false;
            }

            _current = next;
            snapshot = next;
            handlers = CaptureHandlers();
        }

        return true;
    }

    /// <summary>Fans out <paramref name="snapshot"/> to a captured <paramref name="handlers"/> set with per-handler fault isolation.</summary>
    /// <param name="handlers">The subscriber snapshot captured by <see cref="TryApply"/>.</param>
    /// <param name="snapshot">The snapshot to deliver.</param>
    public void Deliver(Action<T>[] handlers, T snapshot) {
        ArgumentNullException.ThrowIfNull(handlers);
        foreach (Action<T> handler in handlers) {
            InvokeSafe(handler, snapshot);
        }
    }

    /// <summary>Reads the current snapshot while holding the publisher lock.</summary>
    /// <typeparam name="TResult">The result returned by <paramref name="reader"/>.</typeparam>
    /// <param name="reader">Owner callback that derives a result from the current snapshot.</param>
    /// <returns>The result produced by <paramref name="reader"/>.</returns>
    public TResult ReadCurrent<TResult>(Func<T, TResult> reader) {
        ArgumentNullException.ThrowIfNull(reader);
        lock (_sync) {
            return reader(_current);
        }
    }

    private void InvokeSafe(Action<T> handler, T snapshot) {
        try {
            handler(snapshot);
        }
        catch (Exception ex) when (!ExceptionGuard.IsFatal(ex)) {
            // Per-subscriber fault isolation stays shared, while the owner controls the exact logging
            // contract (ProjectionConnection redacts the exception; Reconciliation preserves its
            // existing diagnostic shape).
            try {
                _subscriberFaultHandler(ex);
            }
            catch (Exception faultEx) when (!ExceptionGuard.IsFatal(faultEx)) {
                // A faulting fault handler (e.g. a throwing logger) must not abort the remaining
                // fan-out or escalate into the caller's Apply/Publish/Deliver path; swallow it so
                // per-subscriber isolation holds even when reporting the fault itself fails.
            }
        }
    }

    private Action<T>[] CaptureHandlers() {
        Action<T>[] handlers = new Action<T>[_subscriptions.Count];
        for (int i = 0; i < _subscriptions.Count; i++) {
            handlers[i] = _subscriptions[i].Dispatch;
        }

        return handlers;
    }

    private void Unsubscribe(Subscription subscription) {
        lock (_sync) {
            _ = _subscriptions.Remove(subscription);
        }
    }

    private sealed class Subscription : IDisposable {
        private readonly object _sync = new();
        private readonly SnapshotPublisher<T> _owner;
        private readonly Action<T> _handler;
        private int _disposed;

        public Subscription(SnapshotPublisher<T> owner, Action<T> handler) {
            _owner = owner;
            _handler = handler;
            Dispatch = Invoke;
        }

        public Action<T> Dispatch { get; }

        public void Dispose() {
            lock (_sync) {
                if (Interlocked.Exchange(ref _disposed, 1) != 0) {
                    return;
                }
            }

            _owner.Unsubscribe(this);
        }

        private void Invoke(T snapshot) {
            lock (_sync) {
                if (_disposed != 0) {
                    return;
                }

                _handler(snapshot);
            }
        }
    }
}
