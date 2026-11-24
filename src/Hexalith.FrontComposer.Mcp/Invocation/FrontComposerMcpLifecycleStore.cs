using System.Collections.Concurrent;
using System.Diagnostics;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

/// <summary>
/// Process-wide MCP lifecycle snapshot store used by scoped MCP request facades.
/// </summary>
/// <remarks>
/// Lifecycle transition recording is non-reentrant for the entry currently being updated.
/// Recursion into that entry fails before recording the nested observation. Concurrent observations
/// on different threads are serialized per entry. Custom clocks must return a timestamp
/// without synchronously publishing another transition for that entry.
/// </remarks>
public sealed partial class FrontComposerMcpLifecycleStore(
    IOptions<FrontComposerMcpOptions> options,
    TimeProvider? timeProvider = null) : IDisposable {
    private readonly ConcurrentDictionary<string, LifecycleEntry> _byCorrelation = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, LifecycleEntry> _byMessage = new(StringComparer.Ordinal);
    private readonly LinkedList<LifecycleEntry> _activeOrder = new();
    private readonly Queue<LifecycleEntry> _terminalOrder = new();
    private readonly object _capacityGate = new();
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private int _disposed;

    /// <summary>
    /// Records a command acknowledgement and the lifecycle transitions emitted by the current command request.
    /// </summary>
    /// <param name="descriptor">Descriptor of the command tool that produced the acknowledgement.</param>
    /// <param name="creator">
    /// Agent context whose tenant and user were injected into the dispatched command. The entry records these tenant
    /// and user identifiers, and later reads must match both exactly (ordinal). The store does not check
    /// authentication; the caller (the lifecycle tracker) enforces it.
    /// </param>
    /// <param name="result">Dispatcher acknowledgement containing the agent-visible handles.</param>
    /// <param name="pendingTransitions">Transitions captured by the command dispatcher callback.</param>
    /// <param name="lifecycle">Scoped lifecycle service for the current command request.</param>
    /// <param name="cancellationToken">Cancellation token for pending transition recording.</param>
    /// <param name="observer">Optional sink for observations arriving after the captured batch.</param>
    /// <returns>The MCP acknowledgement payload.</returns>
    /// <exception cref="FrontComposerMcpException">
    /// Thrown with <see cref="FrontComposerMcpFailureCategory.UnsupportedSchema"/> when a handle is not canonical, the
    /// creator has no tenant or user, the correlation id is already recorded with different tenant or user
    /// identifiers, a retry changes its message or descriptor, or either alias belongs to another entry.
    /// </exception>
    internal McpCommandAcknowledgement TrackAcknowledged(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext creator,
        CommandResult result,
        IReadOnlyList<(CommandLifecycleState State, string? MessageId)> pendingTransitions,
        ILifecycleStateService lifecycle,
        CancellationToken cancellationToken,
        McpDispatchLifecycleObserver? observer = null) {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(creator);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(lifecycle);

        // E14R-AI-1: an entry must always have a resolvable owner. Without one it could never be read back,
        // so refuse it before it is stored, subscribed, or counted against capacity.
        if (!IsIdentifiedPrincipal(creator)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        }

        string messageId = RequireCanonicalIdentifier(result.MessageId);
        string correlationId = result.CorrelationId is null
            ? messageId
            : RequireCanonicalIdentifier(result.CorrelationId);
        if (pendingTransitions.Any(transition =>
            transition.MessageId is not null && !string.Equals(transition.MessageId, messageId, StringComparison.Ordinal))) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        }

        LifecycleEntry entry = GetOrCreateEntry(descriptor, creator, correlationId, messageId);

        // E14R-AI-1 D3: a dispatcher that re-acknowledges a correlation owned by another principal must not
        // reach that entry. Fail closed before any mutation, subscription, or handle disclosure.
        if (!entry.IsOwnedBy(creator)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        }

        int clampedRetryAfter = ClampRetryAfter(result.RetryAfter);
        entry.SetRetryAfterMs(clampedRetryAfter);

        using IDisposable subscription = lifecycle.Subscribe(correlationId, entry.Observe);
        if (entry.TryMarkAcknowledgmentEmitted()) {
            lifecycle.Transition(correlationId, CommandLifecycleState.Submitting);
            lifecycle.Transition(correlationId, CommandLifecycleState.Acknowledged, messageId);
        }

        foreach ((CommandLifecycleState state, string? transitionMessageId) in pendingTransitions) {
            cancellationToken.ThrowIfCancellationRequested();
            lifecycle.Transition(correlationId, state, transitionMessageId ?? messageId);
        }

        observer?.Bind(entry.CreateDispatchSink());
        return new McpCommandAcknowledgement(
            messageId,
            correlationId,
            CommandLifecycleState.Acknowledged,
            new McpLifecycleSubscription(
                options.Value.LifecycleToolName,
                options.Value.LifecycleUriPrefix + correlationId,
                clampedRetryAfter));
    }

    /// <summary>
    /// Records a transition that has already been observed by a lifecycle bridge without keeping the bridge alive.
    /// </summary>
    /// <param name="transition">Observed transition to append to an existing lifecycle entry.</param>
    /// <returns><see langword="true"/> when the correlation was known and the transition was recorded.</returns>
    internal bool TryRecordObservedTransition(CommandLifecycleTransition transition) {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(transition);

        if (!FrontComposerMcpUlid.IsCanonical(transition.CorrelationId)
            || !_byCorrelation.TryGetValue(transition.CorrelationId, out LifecycleEntry? entry)
            || (transition.MessageId is not null && !string.Equals(transition.MessageId, entry.MessageId, StringComparison.Ordinal))) {
            return false;
        }

        entry.Observe(transition);
        return true;
    }

    /// <summary>
    /// Attempts to read the lifecycle snapshot for an agent-supplied handle.
    /// </summary>
    /// <param name="handle">Canonical correlation or message identifier.</param>
    /// <param name="reader">Agent context of the caller polling the handle.</param>
    /// <param name="currentOptions">Current MCP options used for retry bounds.</param>
    /// <param name="descriptor">Descriptor associated with the lifecycle entry.</param>
    /// <param name="snapshot">Current lifecycle snapshot when the handle is known.</param>
    /// <returns>
    /// <see langword="true"/> when the handle is known and its recorded tenant and user identifiers exactly (ordinal)
    /// match those of <paramref name="reader"/>. Any mismatch returns <see langword="false"/>, exactly like a handle
    /// that was never issued. The store does not check authentication; the caller (the lifecycle tracker) enforces it.
    /// </returns>
    internal bool TryReadSnapshot(
        string handle,
        FrontComposerMcpAgentContext reader,
        FrontComposerMcpOptions currentOptions,
        out McpCommandDescriptor descriptor,
        out McpLifecycleSnapshot snapshot) {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(currentOptions);

        if (!FrontComposerMcpUlid.IsCanonical(handle)) {
            descriptor = null!;
            snapshot = null!;
            return false;
        }

        if (!_byCorrelation.TryGetValue(handle, out LifecycleEntry? entry)) {
            _ = _byMessage.TryGetValue(handle, out entry);
        }

        // E14R-AI-1 D1: ownership is part of the lookup, so a foreign principal takes the same miss path as a
        // never-issued handle and the caller cannot tell the two apart.
        if (entry is null || !entry.IsOwnedBy(reader)) {
            descriptor = null!;
            snapshot = null!;
            return false;
        }

        descriptor = entry.Descriptor;
        snapshot = entry.ToSnapshot(currentOptions);
        return true;
    }

    /// <inheritdoc/>
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        LifecycleEntry[] snapshot;
        lock (_capacityGate) {
            snapshot = [.. _byCorrelation.Values];
            _byCorrelation.Clear();
            _byMessage.Clear();
            _activeOrder.Clear();
            _terminalOrder.Clear();
        }

        foreach (LifecycleEntry entry in snapshot) {
            entry.Dispose();
        }
    }

    private LifecycleEntry GetOrCreateEntry(
        McpCommandDescriptor descriptor,
        FrontComposerMcpAgentContext creator,
        string correlationId,
        string messageId) {
        _ = RequireCanonicalIdentifier(correlationId);
        _ = RequireCanonicalIdentifier(messageId);
        lock (_capacityGate) {
            ThrowIfDisposed();
            if (_byCorrelation.TryGetValue(correlationId, out LifecycleEntry? existing)) {
                // Validate retries before changing retry guidance, subscribing or replaying transitions.
                if (!existing.IsOwnedBy(creator)
                    || !string.Equals(existing.MessageId, messageId, StringComparison.Ordinal)
                    || !DescriptorsEqual(existing.Descriptor, descriptor)) {
                    throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
                }

                return existing;
            }

            // Both identifiers share one namespace, including across the two index roles. Hold the
            // same gate as eviction so a refused reservation cannot publish or terminalize an entry.
            if (_byMessage.ContainsKey(correlationId)
                || _byCorrelation.ContainsKey(messageId)
                || _byMessage.ContainsKey(messageId)) {
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
            }

            FrontComposerMcpOptions opts = options.Value;
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(opts.MaxLifecycleInProgressMs);
            LifecycleEntry created = new(
                descriptor,
                creator.TenantId,
                creator.UserId,
                correlationId,
                messageId,
                opts.MaxLifecycleTransitionHistory,
                TimeSpan.FromMilliseconds(opts.MaxLifecycleInProgressMs),
                _time,
                OnEntryTerminalized);
            if (!_byCorrelation.TryAdd(correlationId, created)) {
                created.Dispose();
                return _byCorrelation[correlationId];
            }

            if (!_byMessage.TryAdd(messageId, created)) {
                _ = _byCorrelation.TryRemove(correlationId, out _);
                created.Dispose();
                throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
            }

            _activeOrder.AddLast(created.ActiveNode);
            created.Start();
            EnforceCapacityLocked();
            return created;
        }
    }

    private static bool DescriptorsEqual(McpCommandDescriptor left, McpCommandDescriptor right)
        => left with { Parameters = right.Parameters, DerivablePropertyNames = right.DerivablePropertyNames } == right
            && (left.DerivablePropertyNames ?? []).SequenceEqual(right.DerivablePropertyNames ?? [], StringComparer.Ordinal)
            && left.Parameters.Count == right.Parameters.Count
            && left.Parameters.Zip(right.Parameters).All(pair => ParametersEqual(pair.First, pair.Second));

    private static bool ParametersEqual(McpParameterDescriptor left, McpParameterDescriptor right)
        => left with { EnumValues = right.EnumValues, BadgeMappings = right.BadgeMappings } == right
            && left.EnumValues.SequenceEqual(right.EnumValues, StringComparer.Ordinal)
            && ((left.BadgeMappings is null && right.BadgeMappings is null)
                || (left.BadgeMappings is not null && right.BadgeMappings is not null
                    && left.BadgeMappings.Count == right.BadgeMappings.Count
                    && left.BadgeMappings.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                        .SequenceEqual(right.BadgeMappings.OrderBy(pair => pair.Key, StringComparer.Ordinal))));

    private void EnforceCapacityLocked() {
        Debug.Assert(Monitor.IsEntered(_capacityGate));
        int max = Math.Max(1, options.Value.MaxActiveLifecycleEntries);
        while (_activeOrder.Count > max) {
            LifecycleEntry oldest = _activeOrder.First!.Value;
            // A concurrent terminal callback may already be waiting for this gate.
            _activeOrder.RemoveFirst();
            _ = oldest.MarkNeedsReview(_time.GetUtcNow());
        }
    }

    private void OnEntryTerminalized(LifecycleEntry entry) {
        lock (_capacityGate) {
            if (Volatile.Read(ref _disposed) != 0) {
                return;
            }

            if (entry.ActiveNode.List is not null) {
                _activeOrder.Remove(entry.ActiveNode);
            }

            _terminalOrder.Enqueue(entry);
            int maxRetained = Math.Max(1, options.Value.MaxRetainedTerminalLifecycleEntries);
            while (_terminalOrder.Count > maxRetained) {
                LifecycleEntry oldest = _terminalOrder.Dequeue();
                _ = _byCorrelation.TryRemove(oldest.CorrelationId, out _);
                _ = _byMessage.TryRemove(oldest.MessageId, out _);
                oldest.Dispose();
            }
        }
    }

    private int ClampRetryAfter(TimeSpan? retryAfter) {
        FrontComposerMcpOptions o = options.Value;
        int min = Math.Max(1, o.MinLifecycleRetryAfterMs);
        int max = Math.Max(min, o.MaxLifecycleRetryAfterMs);
        int fallback = Math.Clamp(Math.Max(1, o.DefaultLifecycleRetryAfterMs), min, max);
        if (retryAfter is null) {
            return fallback;
        }

        double ms = retryAfter.Value.TotalMilliseconds;
        if (double.IsNaN(ms) || double.IsInfinity(ms) || ms <= 0) {
            return fallback;
        }

        if (ms >= max) {
            return max;
        }

        if (ms <= min) {
            return min;
        }

        return Math.Clamp((int)Math.Ceiling(ms), min, max);
    }

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private static bool IsIdentifiedPrincipal(FrontComposerMcpAgentContext context)
        => !string.IsNullOrWhiteSpace(context.TenantId) && !string.IsNullOrWhiteSpace(context.UserId);

    private static string RequireCanonicalIdentifier(string? value) {
        if (!FrontComposerMcpUlid.IsCanonical(value)) {
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        }

        return value!;
    }

    private sealed class LifecycleEntry : IDisposable {
        private readonly object _gate = new();
        private readonly Queue<McpLifecycleTransitionDto> _history = new();
        private readonly int _maxHistory;
        private readonly Action<LifecycleEntry> _onTerminalized;
        private readonly TimeProvider _time;
        private readonly TimeSpan _timeout;
        private readonly ITimer _timeoutTimer;
        private long _nextSequence;
        private bool _historyTruncated;
        private int _isTerminal;
        private int _entryDisposed;
        private int _retryAfterMs;
        private int _acknowledgmentEmitted;
        private CommandLifecycleState _state = CommandLifecycleState.Acknowledged;
        private bool _terminalRecorded;
        private McpTerminalOutcome? _outcome;

        public LifecycleEntry(
            McpCommandDescriptor descriptor,
            string ownerTenantId,
            string ownerUserId,
            string correlationId,
            string messageId,
            int maxHistory,
            TimeSpan timeout,
            TimeProvider time,
            Action<LifecycleEntry> onTerminalized) {
            // Descriptor collection interfaces can wrap mutable caller-owned lists and maps. Retain the
            // acknowledged structure so later caller mutation is checked as a conflicting retry.
            Descriptor = descriptor with {
                Parameters = [.. descriptor.Parameters.Select(parameter => parameter with {
                    EnumValues = [.. parameter.EnumValues],
                    BadgeMappings = parameter.BadgeMappings is null
                        ? null : new Dictionary<string, string>(parameter.BadgeMappings, StringComparer.Ordinal),
                })],
                DerivablePropertyNames = descriptor.DerivablePropertyNames is null ? null! : [.. descriptor.DerivablePropertyNames],
            };
            ActiveNode = new(this);
            OwnerTenantId = ownerTenantId;
            OwnerUserId = ownerUserId;
            CorrelationId = correlationId;
            MessageId = messageId;
            _maxHistory = Math.Max(1, maxHistory);
            _timeout = timeout;
            _time = time;
            _onTerminalized = onTerminalized;
            _timeoutTimer = time.CreateTimer(
                static state => ((LifecycleEntry)state!).MarkTimedOut(),
                this,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
        }

        public LinkedListNode<LifecycleEntry> ActiveNode { get; }

        public McpCommandDescriptor Descriptor { get; }

        public string OwnerTenantId { get; }

        public string OwnerUserId { get; }

        public string CorrelationId { get; }

        public string MessageId { get; }

        public bool IsTerminal => Volatile.Read(ref _isTerminal) != 0;

        // Exact ordinal match on both identifiers. Case variants, padded values, and blank readers never match.
        public bool IsOwnedBy(FrontComposerMcpAgentContext principal)
            => IsIdentifiedPrincipal(principal)
                && string.Equals(OwnerTenantId, principal.TenantId, StringComparison.Ordinal)
                && string.Equals(OwnerUserId, principal.UserId, StringComparison.Ordinal);

        public void SetRetryAfterMs(int retryAfterMs) => Volatile.Write(ref _retryAfterMs, retryAfterMs);

        public bool TryMarkAcknowledgmentEmitted() => Interlocked.Exchange(ref _acknowledgmentEmitted, 1) == 0;

        public void Start() {
            if (Volatile.Read(ref _entryDisposed) != 0) {
                return;
            }

            try {
                _ = _timeoutTimer.Change(_timeout, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException) {
            }
        }

        public Action<CommandLifecycleState, string?> CreateDispatchSink() {
            // A producer can retain its callback beyond terminal eviction. Do not keep this entry
            // or the store alive, or look up a replacement entry after these aliases are reused.
            WeakReference<LifecycleEntry> target = new(this);
            return (state, messageId) => {
                if (target.TryGetTarget(out LifecycleEntry? entry)
                    && (messageId is null || string.Equals(messageId, entry.MessageId, StringComparison.Ordinal))
                    && (state is CommandLifecycleState.Syncing or CommandLifecycleState.Degraded || IsTerminalState(state))) {
                    entry.RecordState(state, entry.MessageId, timestampUtc: null, state == CommandLifecycleState.IdempotentConfirmed);
                }
            };
        }

        public void Observe(CommandLifecycleTransition transition) {
            if (!string.Equals(transition.CorrelationId, CorrelationId, StringComparison.Ordinal)
                || !FrontComposerMcpUlid.IsCanonical(transition.CorrelationId)
                || (transition.MessageId is not null && !string.Equals(transition.MessageId, MessageId, StringComparison.Ordinal))) {
                return;
            }

            RecordState(transition.NewState, transition.MessageId, transition.TimestampUtc, transition.IdempotencyResolved);
        }

        private void RecordState(CommandLifecycleState state, string? messageId, DateTimeOffset? timestampUtc, bool idempotencyResolved) {
            ThrowIfRecordingRecursively();
            bool becameTerminal = false;
            lock (_gate) {
                bool currentTerminal = IsTerminalState(_state);
                if (currentTerminal || Volatile.Read(ref _entryDisposed) != 0) {
                    return;
                }

                DateTimeOffset timestamp = timestampUtc ?? _time.GetUtcNow();
                _state = state;
                if (state == CommandLifecycleState.Confirmed) {
                    if (_outcome is null) {
                        _outcome = idempotencyResolved
                            ? McpTerminalOutcome.IdempotentSuccess(Descriptor.Title)
                            : McpTerminalOutcome.Success();
                    }
                }
                else if (state == CommandLifecycleState.Rejected && _outcome is null) {
                    _outcome = McpTerminalOutcome.GenericRejection();
                }

                else if (state == CommandLifecycleState.IdempotentConfirmed && _outcome is null)
                {
                    _outcome = McpTerminalOutcome.IdempotentSuccess(Descriptor.Title);
                }
                else if (state is CommandLifecycleState.NeedsReview or CommandLifecycleState.Warning
                    or CommandLifecycleState.DegradedExhausted && _outcome is null)
                {
                    _outcome = McpTerminalOutcome.Unconfirmed(state);
                }

                AppendHistory(state, messageId, timestamp, idempotencyResolved);

                if (IsTerminalState(_state) && !_terminalRecorded) {
                    _terminalRecorded = true;
                    Volatile.Write(ref _isTerminal, 1);
                    becameTerminal = true;
                }
            }

            if (becameTerminal) {
                try {
                    _ = _timeoutTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException) {
                }

                _onTerminalized(this);
            }
        }

        public bool MarkTimedOut() => MarkSyntheticTerminal(McpTerminalOutcome.TimedOut());

        public bool MarkNeedsReview(DateTimeOffset observedAtUtc) => MarkSyntheticTerminal(McpTerminalOutcome.NeedsReview(), observedAtUtc);

        public McpLifecycleSnapshot ToSnapshot(FrontComposerMcpOptions opts) {
            lock (_gate) {
                bool terminal = IsTerminalState(_state);
                IReadOnlyList<McpLifecycleTransitionDto> agentHistory = [
                    .. _history.Where(t => t.State is not CommandLifecycleState.Idle
                        and not CommandLifecycleState.Submitting),
                ];
                int min = Math.Max(1, opts.MinLifecycleRetryAfterMs);
                int max = Math.Max(min, opts.MaxLifecycleRetryAfterMs);
                int persisted = Volatile.Read(ref _retryAfterMs);
                int retryAfter = persisted > 0
                    ? Math.Clamp(persisted, min, max)
                    : Math.Clamp(Math.Max(1, opts.DefaultLifecycleRetryAfterMs), min, max);
                return new McpLifecycleSnapshot(
                    MessageId,
                    CorrelationId,
                    _state,
                    terminal,
                    _outcome,
                    agentHistory,
                    retryAfter,
                    Math.Max(0, opts.MaxLifecycleLongPollMs),
                    _historyTruncated);
            }
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref _entryDisposed, 1) != 0) {
                return;
            }

            _timeoutTimer.Dispose();
        }

        private static bool IsTerminalState(CommandLifecycleState state)
            => state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected
                or CommandLifecycleState.IdempotentConfirmed or CommandLifecycleState.NeedsReview
                or CommandLifecycleState.Warning or CommandLifecycleState.DegradedExhausted;

        private bool MarkSyntheticTerminal(McpTerminalOutcome outcome, DateTimeOffset? observedAtUtc = null) {
            ThrowIfRecordingRecursively();
            if (Volatile.Read(ref _entryDisposed) != 0) {
                return false;
            }

            lock (_gate) {
                if (IsTerminalState(_state)) {
                    return false;
                }

                DateTimeOffset timestamp = observedAtUtc ?? _time.GetUtcNow();
                _state = CommandLifecycleState.Rejected;
                _outcome = outcome;
                _terminalRecorded = true;
                Volatile.Write(ref _isTerminal, 1);
                AppendHistory(CommandLifecycleState.Rejected, MessageId, timestamp, idempotencyResolved: false);
            }

            try {
                _ = _timeoutTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException) {
            }

            _onTerminalized(this);
            return true;
        }

        private void ThrowIfRecordingRecursively() {
            if (Monitor.IsEntered(_gate)) {
                throw new InvalidOperationException("A lifecycle entry cannot record a recursive transition.");
            }
        }

        private void AppendHistory(
            CommandLifecycleState state,
            string? transitionMessageId,
            DateTimeOffset observedAtUtc,
            bool idempotencyResolved) {
            _history.Enqueue(new McpLifecycleTransitionDto(
                ++_nextSequence,
                state,
                transitionMessageId,
                observedAtUtc,
                idempotencyResolved));

            while (_history.Count > _maxHistory) {
                _ = _history.Dequeue();
                _historyTruncated = true;
            }
        }
    }
}
