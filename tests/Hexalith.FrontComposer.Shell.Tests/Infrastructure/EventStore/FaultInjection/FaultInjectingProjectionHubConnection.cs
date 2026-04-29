using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using System.Text;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore.FaultInjection;

/// <summary>
/// Deterministic, payload-less fault-injection harness for <see cref="IProjectionHubConnection"/>.
/// The harness exposes the production seam (<c>StartAsync</c>/<c>JoinGroupAsync</c>/etc.) plus
/// test-side staging APIs (<c>BlockUntil</c>/<c>Release</c>/<c>FailNext</c>/<c>CancelNext</c>/
/// <c>WaitFor</c>, plus <c>RaiseStateAsync</c>/<c>PublishNudgeAsync</c>/queue helpers).
/// </summary>
/// <remarks>
/// <para>
/// Coordination uses <see cref="TaskCompletionSource"/> with
/// <see cref="TaskCreationOptions.RunContinuationsAsynchronously"/>. No real timers, sleeps,
/// wall-clock reads, or unbounded waits are used inside the harness; tests advance time via the
/// injected <see cref="TimeProvider"/> if they need timer-driven scenarios.
/// </para>
/// <para>
/// Harness diagnostics name checkpoints and bounded categories only. Tenant values, group strings,
/// payloads, exception messages, and connection identifiers are never included in disposal or
/// failure messages.
/// </para>
/// </remarks>
internal sealed class FaultInjectingProjectionHubConnection : IProjectionHubConnection {
    private static int _nextInstanceId;

    private readonly int _instanceId = Interlocked.Increment(ref _nextInstanceId);
    private readonly object _gate = new();
    private readonly Dictionary<HarnessCheckpoint, Queue<ScriptedAction>> _scripts = [];
    private readonly Dictionary<HarnessCheckpoint, Queue<TaskCompletionSource>> _activeBlocks = [];
    private readonly Dictionary<HarnessCheckpoint, int> _hits = [];
    private readonly Dictionary<HarnessCheckpoint, List<WaiterEntry>> _waiters = [];
    private readonly Dictionary<int, QueuedNudge> _nudgeQueue = [];
    private readonly List<int> _nudgeQueueOrder = [];
    private readonly Dictionary<string, Queue<NudgeSelector>> _nudgeSelectors = [];
    private readonly List<HandlerRegistration<Func<string, string, Task>>> _projectionHandlers = [];
    private readonly List<HandlerRegistration<Func<ProjectionHubConnectionStateChanged, Task>>> _stateHandlers = [];
    private readonly List<string> _activeQualifiers = [];
    private readonly List<string> _handlerFailureCategories = [];

    private int _nextSequence;
    private bool _isConnected;
    private bool _disposed;

    /// <summary>Capacity guard for staged scripts/queues. Default = 256 (per-scenario bound).</summary>
    public int MaxBoundedQueueDepth { get; init; } = 256;

    /// <inheritdoc />
    public bool IsConnected {
        get {
            lock (_gate) {
                return _isConnected;
            }
        }
    }

    /// <summary>Bounded list of subscriber failure category names captured during nudge dispatch.</summary>
    public IReadOnlyList<string> CapturedHandlerFailureCategories {
        get {
            lock (_gate) {
                return [.. _handlerFailureCategories];
            }
        }
    }

    // ---------- IProjectionHubConnection: subscriptions ----------

    public IDisposable OnProjectionChanged(Func<string, string, Task> handler) {
        ArgumentNullException.ThrowIfNull(handler);
        return RegisterHandler(_projectionHandlers, handler);
    }

    public IDisposable OnConnectionStateChanged(Func<ProjectionHubConnectionStateChanged, Task> handler) {
        ArgumentNullException.ThrowIfNull(handler);
        return RegisterHandler(_stateHandlers, handler);
    }

    // ---------- IProjectionHubConnection: lifecycle ----------

    public async Task StartAsync(CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        await CrossCheckpointAsync(HarnessCheckpoint.Start, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) {
            ThrowIfDisposedLocked();
            _isConnected = true;
        }

        await PublishStateAsync(HarnessConnectionStates.Connected()).ConfigureAwait(false);
    }

    public async Task JoinGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        HarnessCheckpoint checkpoint = HarnessCheckpoint.Join(projectionType, tenantId);
        await CrossCheckpointAsync(checkpoint, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) {
            ThrowIfDisposedLocked();
            string qualifier = checkpoint.Qualifier!;
            if (!_activeQualifiers.Contains(qualifier, StringComparer.Ordinal)) {
                _activeQualifiers.Add(qualifier);
            }
        }
    }

    public async Task LeaveGroupAsync(string projectionType, string tenantId, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        HarnessCheckpoint checkpoint = HarnessCheckpoint.Leave(projectionType, tenantId);
        await CrossCheckpointAsync(checkpoint, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) {
            ThrowIfDisposedLocked();
            _ = _activeQualifiers.Remove(checkpoint.Qualifier!);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ThrowIfDisposed();
        await CrossCheckpointAsync(HarnessCheckpoint.Stop, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate) {
            ThrowIfDisposedLocked();
            _isConnected = false;
        }
    }

    public async ValueTask DisposeAsync() {
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _isConnected = false;
            _projectionHandlers.Clear();
            _stateHandlers.Clear();
            _activeQualifiers.Clear();
        }

        // Cross the Dispose checkpoint after disposal flag flips so disposal-during-script
        // scenarios can observe the cancellation. We do NOT throw on a scripted Dispose
        // failure — disposal must be best-effort for production parity.
        try {
            await CrossCheckpointAsync(HarnessCheckpoint.Dispose, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException) {
            // Disposal must not throw on internal bookkeeping issues.
        }

        // Bounded-state diagnostic: any leftover scripted actions, queued nudges, blocked
        // checkpoints, queued nudges, or active group qualifiers indicate the test left
        // staged state behind. We surface a sanitized message naming the checkpoints / counts.
        string? diagnostic = BuildDisposalDiagnosticOrNull();
        if (diagnostic is not null) {
            throw new HarnessDisposalException(diagnostic);
        }
    }

    // ---------- Test-side staging: scripted lifecycle actions ----------

    /// <summary>Arms the next matching operation to block until <see cref="Release"/> is called.</summary>
    public void BlockUntil(HarnessCheckpoint checkpoint) {
        TaskCompletionSource tcs = NewBlockTcs();
        EnqueueScript(checkpoint, new ScriptedAction(ScriptedActionKind.Block, tcs, null));
    }

    /// <summary>
    /// Releases the oldest currently-blocked operation at <paramref name="checkpoint"/>. If no
    /// operation has crossed the checkpoint yet but a <see cref="BlockUntil"/> is queued, the
    /// queued block is consumed so the next crossing proceeds normally instead of blocking. If
    /// the block was already completed (e.g., by cancellation), <c>Release</c> is a no-op so
    /// tests can call it unconditionally in <c>finally</c>.
    /// </summary>
    public void Release(HarnessCheckpoint checkpoint) {
        TaskCompletionSource? toRelease = null;
        lock (_gate) {
            if (_activeBlocks.TryGetValue(checkpoint, out Queue<TaskCompletionSource>? active) && active.Count > 0) {
                toRelease = active.Dequeue();
                if (active.Count == 0) {
                    _ = _activeBlocks.Remove(checkpoint);
                }
            }
            else if (_scripts.TryGetValue(checkpoint, out Queue<ScriptedAction>? script) && script.Count > 0 && script.Peek().Kind == ScriptedActionKind.Block) {
                ScriptedAction action = script.Dequeue();
                if (script.Count == 0) {
                    _ = _scripts.Remove(checkpoint);
                }

                toRelease = action.Tcs;
            }
        }

        _ = toRelease?.TrySetResult();
    }

    /// <summary>Arms the next matching operation to fault with <paramref name="exception"/>.</summary>
    public void FailNext(HarnessCheckpoint checkpoint, Exception exception) {
        ArgumentNullException.ThrowIfNull(exception);
        EnqueueScript(checkpoint, new ScriptedAction(ScriptedActionKind.Fail, null, exception));
    }

    /// <summary>Arms the next matching operation to complete canceled.</summary>
    public void CancelNext(HarnessCheckpoint checkpoint)
        => EnqueueScript(checkpoint, new ScriptedAction(ScriptedActionKind.Cancel, null, null));

    /// <summary>
    /// Returns a task that completes when <paramref name="checkpoint"/> has been crossed at least
    /// <paramref name="count"/> times. Resolves immediately if the count is already reached.
    /// </summary>
    public Task WaitForAsync(HarnessCheckpoint checkpoint, int count = 1, CancellationToken cancellationToken = default) {
        if (count <= 0) {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        TaskCompletionSource tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_gate) {
            ThrowIfDisposedLocked();
            int hits = _hits.GetValueOrDefault(checkpoint);
            if (hits >= count) {
                _ = tcs.TrySetResult();
            }
            else {
                if (!_waiters.TryGetValue(checkpoint, out List<WaiterEntry>? list)) {
                    list = [];
                    _waiters[checkpoint] = list;
                }

                list.Add(new WaiterEntry(count, tcs));
            }
        }

        if (cancellationToken.CanBeCanceled) {
            CancellationTokenRegistration registration = cancellationToken.Register(static state => {
                TaskCompletionSource source = (TaskCompletionSource)state!;
                _ = source.TrySetCanceled();
            }, tcs);
            _ = tcs.Task.ContinueWith(_ => {
                registration.Dispose();
                RemoveCompletedWaiters(checkpoint);
            }, TaskScheduler.Default);
        }

        return tcs.Task;
    }

    /// <summary>Returns the recorded hit count for <paramref name="checkpoint"/>.</summary>
    public int GetHitCount(HarnessCheckpoint checkpoint) {
        lock (_gate) {
            return _hits.GetValueOrDefault(checkpoint);
        }
    }

    // ---------- Test-side staging: nudges ----------

    /// <summary>Fires <c>OnProjectionChanged</c> handlers immediately (after applying any scripted nudge selector).</summary>
    public async Task PublishNudgeAsync(string projectionType, string tenantId) {
        HarnessCheckpoint checkpoint = HarnessCheckpoint.Nudge(projectionType, tenantId);
        string qualifier = checkpoint.Qualifier!;
        if (IsDisposed()) {
            return;
        }

        await CrossCheckpointAsync(checkpoint, CancellationToken.None).ConfigureAwait(false);
        if (IsDisposed()) {
            return;
        }

        NudgeSelector? selector = ConsumeNudgeSelector(qualifier);
        switch (selector) {
            case { Kind: NudgeSelectorKind.Drop }:
                return;
            case { Kind: NudgeSelectorKind.Duplicate }:
                await DispatchDuplicatedAsync(projectionType, tenantId, selector.Count).ConfigureAwait(false);
                return;
            case { Kind: NudgeSelectorKind.Delay }:
                await DelayInternalAsync(projectionType, tenantId, selector.Token!.Value).ConfigureAwait(false);
                return;
            default:
                await DispatchNudgeAsync(projectionType, tenantId).ConfigureAwait(false);
                return;
        }
    }

    /// <summary>Suppresses the next call to <see cref="PublishNudgeAsync"/> matching the group.</summary>
    public void DropNextNudge(string projectionType, string tenantId)
        => RegisterNudgeSelector(projectionType, tenantId, new NudgeSelector(NudgeSelectorKind.Drop, 1, null));

    /// <summary>Causes the next matching publication to fire handlers <paramref name="count"/> times.</summary>
    public void DuplicateNextNudge(string projectionType, string tenantId, int count = 2) {
        if (count <= 1) {
            throw new ArgumentOutOfRangeException(nameof(count), "Duplicate count must be > 1.");
        }

        RegisterNudgeSelector(projectionType, tenantId, new NudgeSelector(NudgeSelectorKind.Duplicate, count, null));
    }

    /// <summary>Captures the next matching publication into the queue. Returns a token for release/discard.</summary>
    public NudgeQueueToken DelayNextNudge(string projectionType, string tenantId) {
        NudgeQueueToken token = NewQueueToken();
        RegisterNudgeSelector(projectionType, tenantId, new NudgeSelector(NudgeSelectorKind.Delay, 1, token));
        return token;
    }

    /// <summary>Queues a nudge with neither a synchronous publish nor a selector — pure deterministic queue.</summary>
    public NudgeQueueToken QueueNudge(string projectionType, string tenantId) {
        // Validate-then-store. A queued nudge has no scripted-selector dependency.
        EnsureValid(projectionType, tenantId);
        NudgeQueueToken token = NewQueueToken();
        lock (_gate) {
            EnsureBound(_nudgeQueue.Count + 1, "nudge queue");
            _nudgeQueue[token.Sequence] = new QueuedNudge(projectionType, tenantId);
            _nudgeQueueOrder.Add(token.Sequence);
        }

        return token;
    }

    /// <summary>Releases a queued nudge to the handlers <paramref name="count"/> times.</summary>
    public async Task ReleaseAsync(NudgeQueueToken token, int count = 1) {
        if (count <= 0) {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        QueuedNudge nudge = TakeQueuedNudge(token);
        for (int i = 0; i < count; i++) {
            await DispatchNudgeAsync(nudge.ProjectionType, nudge.TenantId).ConfigureAwait(false);
        }
    }

    /// <summary>Releases queued nudges in the specified order (each token may appear once).</summary>
    public async Task ReleaseInOrderAsync(IEnumerable<NudgeQueueToken> tokens) {
        ArgumentNullException.ThrowIfNull(tokens);
        List<QueuedNudge> nudges = TakeQueuedNudgesInOrder(tokens);
        foreach (QueuedNudge nudge in nudges) {
            await DispatchNudgeAsync(nudge.ProjectionType, nudge.TenantId).ConfigureAwait(false);
        }
    }

    /// <summary>Releases all queued nudges in original FIFO order.</summary>
    public async Task ReleaseAllQueuedAsync() {
        List<int> sequences;
        lock (_gate) {
            sequences = [.. _nudgeQueueOrder];
        }

        foreach (int sequence in sequences) {
            await ReleaseAsync(new NudgeQueueToken(_instanceId, sequence)).ConfigureAwait(false);
        }
    }

    /// <summary>Discards the queued nudge without firing handlers.</summary>
    public void Discard(NudgeQueueToken token) => _ = TakeQueuedNudge(token);

    // ---------- Test-side staging: connection states ----------

    /// <summary>Synchronously raises a connection-state change to all subscribers.</summary>
    public async Task RaiseStateAsync(ProjectionHubConnectionStateChanged change) {
        ArgumentNullException.ThrowIfNull(change);
        if (IsDisposed()) {
            return;
        }

        await CrossCheckpointAsync(HarnessCheckpoint.ConnectionState(change.State), CancellationToken.None).ConfigureAwait(false);
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _isConnected = change.State is ProjectionHubConnectionState.Connected or ProjectionHubConnectionState.Reconnected;
        }

        await PublishStateAsync(change).ConfigureAwait(false);
    }

    /// <summary>Crosses the named fallback-trigger checkpoint for tests that coordinate fallback seams.</summary>
    public Task TriggerFallbackCheckpointAsync(CancellationToken cancellationToken = default)
        => CrossCheckpointAsync(HarnessCheckpoint.FallbackTrigger, cancellationToken);

    /// <summary>Crosses a group-scoped fallback-trigger checkpoint for tests that coordinate fallback seams.</summary>
    public Task TriggerFallbackCheckpointAsync(string projectionType, string tenantId, CancellationToken cancellationToken = default)
        => CrossCheckpointAsync(HarnessCheckpoint.FallbackTriggerFor(projectionType, tenantId), cancellationToken);

    /// <summary>
    /// Snapshot of currently-active joined group qualifiers (for assertions only — production
    /// truth lives in <c>ProjectionSubscriptionService</c>).
    /// </summary>
    public IReadOnlyList<string> ObservedActiveGroups {
        get {
            lock (_gate) {
                return [.. _activeQualifiers];
            }
        }
    }

    // ---------- Internal coordination ----------

    private async Task CrossCheckpointAsync(HarnessCheckpoint checkpoint, CancellationToken cancellationToken) {
        cancellationToken.ThrowIfCancellationRequested();
        ScriptedAction? action;
        lock (_gate) {
            IncrementHitsLocked(checkpoint);
            CompleteWaitersLocked(checkpoint);
            action = TakeNextScriptedActionLocked(checkpoint);
        }

        if (action is null) {
            return;
        }

        switch (action.Kind) {
            case ScriptedActionKind.Block:
                lock (_gate) {
                    if (!_activeBlocks.TryGetValue(checkpoint, out Queue<TaskCompletionSource>? active)) {
                        active = new Queue<TaskCompletionSource>();
                        _activeBlocks[checkpoint] = active;
                    }

                    active.Enqueue(action.Tcs!);
                }

                using (cancellationToken.Register(static state => {
                    TaskCompletionSource source = (TaskCompletionSource)state!;
                    _ = source.TrySetCanceled();
                }, action.Tcs!)) {
                    try {
                        await action.Tcs!.Task.ConfigureAwait(false);
                    }
                    finally {
                        lock (_gate) {
                            if (_activeBlocks.TryGetValue(checkpoint, out Queue<TaskCompletionSource>? remaining) && remaining.Count > 0) {
                                Queue<TaskCompletionSource> filtered = new();
                                foreach (TaskCompletionSource tcs in remaining) {
                                    if (!ReferenceEquals(tcs, action.Tcs)) {
                                        filtered.Enqueue(tcs);
                                    }
                                }

                                if (filtered.Count == 0) {
                                    _ = _activeBlocks.Remove(checkpoint);
                                }
                                else {
                                    _activeBlocks[checkpoint] = filtered;
                                }
                            }
                        }
                    }
                }

                break;

            case ScriptedActionKind.Fail:
                throw action.Exception!;

            case ScriptedActionKind.Cancel:
                cancellationToken.ThrowIfCancellationRequested();
                throw new OperationCanceledException(cancellationToken);

            default:
                throw new InvalidOperationException($"Unknown scripted action: {action.Kind}");
        }
    }

    private void IncrementHitsLocked(HarnessCheckpoint checkpoint) {
        _hits[checkpoint] = _hits.GetValueOrDefault(checkpoint) + 1;
    }

    private void CompleteWaitersLocked(HarnessCheckpoint checkpoint) {
        if (!_waiters.TryGetValue(checkpoint, out List<WaiterEntry>? list)) {
            return;
        }

        int currentHits = _hits.GetValueOrDefault(checkpoint);
        for (int i = list.Count - 1; i >= 0; i--) {
            if (currentHits >= list[i].Count) {
                _ = list[i].Tcs.TrySetResult();
                list.RemoveAt(i);
            }
        }

        if (list.Count == 0) {
            _ = _waiters.Remove(checkpoint);
        }
    }

    private void RemoveCompletedWaiters(HarnessCheckpoint checkpoint) {
        lock (_gate) {
            if (!_waiters.TryGetValue(checkpoint, out List<WaiterEntry>? list)) {
                return;
            }

            _ = list.RemoveAll(static entry => entry.Tcs.Task.IsCompleted);
            if (list.Count == 0) {
                _ = _waiters.Remove(checkpoint);
            }
        }
    }

    private ScriptedAction? TakeNextScriptedActionLocked(HarnessCheckpoint checkpoint) {
        if (!_scripts.TryGetValue(checkpoint, out Queue<ScriptedAction>? queue) || queue.Count == 0) {
            return null;
        }

        ScriptedAction action = queue.Dequeue();
        if (queue.Count == 0) {
            _ = _scripts.Remove(checkpoint);
        }

        return action;
    }

    private ScriptedAction? TakeMatchingScript(HarnessCheckpoint checkpoint, ScriptedActionKind kind) {
        lock (_gate) {
            if (!_scripts.TryGetValue(checkpoint, out Queue<ScriptedAction>? queue) || queue.Count == 0) {
                return null;
            }

            // Match the first action of the requested kind. We must consume in FIFO order; if the
            // head is not of the requested kind, we surface a clear diagnostic so tests cannot
            // accidentally release a queued FailNext as if it were a Block.
            ScriptedAction head = queue.Peek();
            if (head.Kind != kind) {
                throw new InvalidOperationException(
                    $"Head scripted action at {checkpoint} is {head.Kind}, not {kind}.");
            }

            _ = queue.Dequeue();
            if (queue.Count == 0) {
                _ = _scripts.Remove(checkpoint);
            }

            return head;
        }
    }

    private void EnqueueScript(HarnessCheckpoint checkpoint, ScriptedAction action) {
        lock (_gate) {
            ThrowIfDisposedLocked();
            if (!_scripts.TryGetValue(checkpoint, out Queue<ScriptedAction>? queue)) {
                queue = new Queue<ScriptedAction>();
                _scripts[checkpoint] = queue;
            }

            EnsureBound(queue.Count + 1, "scripted-action queue");
            queue.Enqueue(action);
        }
    }

    private void RegisterNudgeSelector(string projectionType, string tenantId, NudgeSelector selector) {
        EnsureValid(projectionType, tenantId);
        string qualifier = $"{projectionType}:{tenantId}";
        lock (_gate) {
            ThrowIfDisposedLocked();
            if (!_nudgeSelectors.TryGetValue(qualifier, out Queue<NudgeSelector>? queue)) {
                queue = new Queue<NudgeSelector>();
                _nudgeSelectors[qualifier] = queue;
            }

            EnsureBound(queue.Count + 1, "nudge-selector queue");
            queue.Enqueue(selector);
        }
    }

    private NudgeSelector? ConsumeNudgeSelector(string qualifier) {
        lock (_gate) {
            if (!_nudgeSelectors.TryGetValue(qualifier, out Queue<NudgeSelector>? queue) || queue.Count == 0) {
                return null;
            }

            NudgeSelector selector = queue.Dequeue();
            if (queue.Count == 0) {
                _ = _nudgeSelectors.Remove(qualifier);
            }

            return selector;
        }
    }

    private async Task DispatchDuplicatedAsync(string projectionType, string tenantId, int count) {
        for (int i = 0; i < count; i++) {
            await DispatchNudgeAsync(projectionType, tenantId).ConfigureAwait(false);
        }
    }

    private Task DelayInternalAsync(string projectionType, string tenantId, NudgeQueueToken token) {
        lock (_gate) {
            EnsureBound(_nudgeQueue.Count + 1, "nudge queue");
            _nudgeQueue[token.Sequence] = new QueuedNudge(projectionType, tenantId);
            _nudgeQueueOrder.Add(token.Sequence);
        }

        return Task.CompletedTask;
    }

    private QueuedNudge TakeQueuedNudge(NudgeQueueToken token) {
        if (token.InstanceId != _instanceId) {
            throw new InvalidOperationException("Nudge token belongs to a different harness instance.");
        }

        lock (_gate) {
            if (!_nudgeQueue.Remove(token.Sequence, out QueuedNudge nudge)) {
                throw new InvalidOperationException(
                    $"Nudge {token} not present in queue (already released or discarded).");
            }

            _ = _nudgeQueueOrder.Remove(token.Sequence);
            return nudge;
        }
    }

    private List<QueuedNudge> TakeQueuedNudgesInOrder(IEnumerable<NudgeQueueToken> tokens) {
        List<NudgeQueueToken> materialized = [.. tokens];
        HashSet<int> seen = [];
        lock (_gate) {
            foreach (NudgeQueueToken token in materialized) {
                if (token.InstanceId != _instanceId) {
                    throw new InvalidOperationException("Nudge token belongs to a different harness instance.");
                }

                if (!seen.Add(token.Sequence)) {
                    throw new InvalidOperationException($"Nudge {token} was provided more than once.");
                }

                if (!_nudgeQueue.ContainsKey(token.Sequence)) {
                    throw new InvalidOperationException(
                        $"Nudge {token} not present in queue (already released or discarded).");
                }
            }

            List<QueuedNudge> nudges = new(materialized.Count);
            foreach (NudgeQueueToken token in materialized) {
                QueuedNudge nudge = _nudgeQueue[token.Sequence];
                _ = _nudgeQueue.Remove(token.Sequence);
                _ = _nudgeQueueOrder.Remove(token.Sequence);
                nudges.Add(nudge);
            }

            return nudges;
        }
    }

    private async Task DispatchNudgeAsync(string projectionType, string tenantId) {
        Func<string, string, Task>[] handlers;
        lock (_gate) {
            handlers = [.. _projectionHandlers.Select(static r => r.Handler)];
        }

        foreach (Func<string, string, Task> handler in handlers) {
            try {
                await handler(projectionType, tenantId).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException) {
                AddHandlerFailureCategory(ex);
            }
        }
    }

    private async Task PublishStateAsync(ProjectionHubConnectionStateChanged change) {
        Func<ProjectionHubConnectionStateChanged, Task>[] handlers;
        lock (_gate) {
            handlers = [.. _stateHandlers.Select(static r => r.Handler)];
        }

        foreach (Func<ProjectionHubConnectionStateChanged, Task> handler in handlers) {
            try {
                await handler(change).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OutOfMemoryException) {
                AddHandlerFailureCategory(ex);
            }
        }
    }

    private void AddHandlerFailureCategory(Exception exception) {
        lock (_gate) {
            if (MaxBoundedQueueDepth <= 0) {
                return;
            }

            if (_handlerFailureCategories.Count < MaxBoundedQueueDepth) {
                _handlerFailureCategories.Add(exception.GetType().Name);
            }
            else {
                _handlerFailureCategories[^1] = "Overflow";
            }
        }
    }

    private NudgeQueueToken NewQueueToken() {
        int sequence = Interlocked.Increment(ref _nextSequence);
        return new NudgeQueueToken(_instanceId, sequence);
    }

    private void EnsureBound(int proposedCount, string queueName) {
        if (proposedCount > MaxBoundedQueueDepth) {
            throw new InvalidOperationException(
                $"Harness {queueName} exceeded MaxBoundedQueueDepth ({MaxBoundedQueueDepth}).");
        }
    }

    private void ThrowIfDisposedLocked() {
        if (_disposed) {
            throw new ObjectDisposedException(nameof(FaultInjectingProjectionHubConnection));
        }
    }

    private void ThrowIfDisposed() {
        lock (_gate) {
            ThrowIfDisposedLocked();
        }
    }

    private bool IsDisposed() {
        lock (_gate) {
            return _disposed;
        }
    }

    private string? BuildDisposalDiagnosticOrNull() {
        StringBuilder? builder = null;
        lock (_gate) {
            int outstandingActiveBlocks = _activeBlocks.Sum(kv => kv.Value.Count);
            if (outstandingActiveBlocks > 0) {
                builder ??= new StringBuilder();
                _ = builder.Append("Outstanding blocked operations: ").Append(outstandingActiveBlocks);
                _ = builder.Append(" at [");
                bool first = true;
                foreach (KeyValuePair<HarnessCheckpoint, Queue<TaskCompletionSource>> kv in _activeBlocks) {
                    if (!first) {
                        _ = builder.Append(", ");
                    }

                    _ = builder.Append(kv.Key).Append(':').Append(kv.Value.Count);
                    first = false;
                }

                _ = builder.Append("]. ");

                foreach (Queue<TaskCompletionSource> blocked in _activeBlocks.Values) {
                    foreach (TaskCompletionSource tcs in blocked) {
                        _ = tcs.TrySetException(new ObjectDisposedException(nameof(FaultInjectingProjectionHubConnection)));
                    }
                }

                _activeBlocks.Clear();
            }

            int outstandingScripts = _scripts.Sum(kv => kv.Value.Count);
            if (outstandingScripts > 0) {
                builder ??= new StringBuilder();
                _ = builder.Append("Outstanding scripted actions: ").Append(outstandingScripts);
                _ = builder.Append(" at [");
                bool first = true;
                foreach (KeyValuePair<HarnessCheckpoint, Queue<ScriptedAction>> kv in _scripts) {
                    if (!first) {
                        _ = builder.Append(", ");
                    }

                    _ = builder.Append(kv.Key).Append(':').Append(kv.Value.Count);
                    first = false;
                }

                _ = builder.Append("]. ");

                // Fail any queued blocks so awaiters do not hang.
                foreach (Queue<ScriptedAction> queue in _scripts.Values) {
                    foreach (ScriptedAction action in queue) {
                        if (action.Kind == ScriptedActionKind.Block && action.Tcs is not null) {
                            _ = action.Tcs.TrySetException(new ObjectDisposedException(nameof(FaultInjectingProjectionHubConnection)));
                        }
                    }
                }
            }

            int outstandingSelectors = _nudgeSelectors.Sum(kv => kv.Value.Count);
            if (outstandingSelectors > 0) {
                builder ??= new StringBuilder();
                _ = builder.Append("Outstanding nudge selectors: ").Append(outstandingSelectors).Append(". ");
            }

            if (_nudgeQueue.Count > 0) {
                builder ??= new StringBuilder();
                _ = builder.Append("Outstanding queued nudges: ").Append(_nudgeQueue.Count).Append(". ");
            }

            foreach (HarnessCheckpoint checkpoint in _waiters.Keys.ToArray()) {
                if (_waiters.TryGetValue(checkpoint, out List<WaiterEntry>? list)) {
                    _ = list.RemoveAll(static entry => entry.Tcs.Task.IsCompleted);
                    if (list.Count == 0) {
                        _ = _waiters.Remove(checkpoint);
                    }
                }
            }

            if (_waiters.Count > 0) {
                int outstanding = _waiters.Sum(kv => kv.Value.Count);
                if (outstanding > 0) {
                    builder ??= new StringBuilder();
                    _ = builder.Append("Outstanding WaitFor awaiters: ").Append(outstanding).Append(". ");
                    foreach (List<WaiterEntry> list in _waiters.Values) {
                        foreach (WaiterEntry entry in list) {
                            _ = entry.Tcs.TrySetException(new ObjectDisposedException(nameof(FaultInjectingProjectionHubConnection)));
                        }
                    }
                }
            }
        }

        return builder?.ToString();
    }

    private static void EnsureValid(string projectionType, string tenantId) {
        _ = EventStoreValidation.RequireNonColonSegment(projectionType, nameof(projectionType));
        _ = EventStoreValidation.RequireNonColonSegment(tenantId, nameof(tenantId));
    }

    private static TaskCompletionSource NewBlockTcs() => new(TaskCreationOptions.RunContinuationsAsynchronously);

    private IDisposable RegisterHandler<TDelegate>(List<HandlerRegistration<TDelegate>> registry, TDelegate handler)
        where TDelegate : Delegate {
        HandlerRegistration<TDelegate> registration = new(handler);
        lock (_gate) {
            ThrowIfDisposedLocked();
            EnsureBound(registry.Count + 1, "handler registration");
            registry.Add(registration);
        }

        return new HandlerDisposer(() => {
            lock (_gate) {
                _ = registry.Remove(registration);
            }
        });
    }

    // ---------- Inner types ----------

    private sealed record ScriptedAction(ScriptedActionKind Kind, TaskCompletionSource? Tcs, Exception? Exception);

    private enum ScriptedActionKind { Block, Fail, Cancel }

    private readonly record struct WaiterEntry(int Count, TaskCompletionSource Tcs);

    private readonly record struct QueuedNudge(string ProjectionType, string TenantId);

    private sealed record NudgeSelector(NudgeSelectorKind Kind, int Count, NudgeQueueToken? Token);

    private enum NudgeSelectorKind { Drop, Duplicate, Delay }

    private sealed record HandlerRegistration<TDelegate>(TDelegate Handler);

    private sealed class HandlerDisposer(Action onDispose) : IDisposable {
        public void Dispose() => onDispose();
    }
}
