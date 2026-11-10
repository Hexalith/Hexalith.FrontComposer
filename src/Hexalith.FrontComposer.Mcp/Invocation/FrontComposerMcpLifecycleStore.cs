using System.Collections.Concurrent;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

/// <summary>
/// Process-wide MCP lifecycle snapshot store used by scoped MCP request facades.
/// </summary>
public sealed partial class FrontComposerMcpLifecycleStore(
    IOptions<FrontComposerMcpOptions> options,
    TimeProvider? timeProvider = null) : IDisposable {
    private readonly ConcurrentDictionary<string, LifecycleEntry> _byCorrelation = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, LifecycleEntry> _byMessage = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private readonly ConcurrentQueue<string> _terminalOrder = new();
    private readonly object _capacityGate = new();
    private readonly TimeProvider _time = timeProvider ?? TimeProvider.System;
    private int _activeCount;
    private int _disposed;
    private int _terminalCount;

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
        CancellationToken cancellationToken) {
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
        (CommandLifecycleState State, string MessageId)[] validatedTransitions = [
            .. pendingTransitions.Select(transition => (
                transition.State,
                transition.MessageId is null
                    ? messageId
                    : RequireCanonicalIdentifier(transition.MessageId))),
        ];
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

        foreach ((CommandLifecycleState state, string transitionMessageId) in validatedTransitions) {
            cancellationToken.ThrowIfCancellationRequested();
            lifecycle.Transition(correlationId, state, transitionMessageId);
        }

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
            || (transition.MessageId is not null && !FrontComposerMcpUlid.IsCanonical(transition.MessageId))
            || !_byCorrelation.TryGetValue(transition.CorrelationId, out LifecycleEntry? entry)) {
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
            while (_insertionOrder.TryDequeue(out _)) {
            }

            while (_terminalOrder.TryDequeue(out _)) {
            }

            Volatile.Write(ref _activeCount, 0);
            Volatile.Write(ref _terminalCount, 0);
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
            LifecycleEntry created = new(
                descriptor,
                creator.TenantId,
                creator.UserId,
                correlationId,
                messageId,
                opts.MaxLifecycleTransitionHistory,
                TimeSpan.FromMilliseconds(Math.Max(1, opts.MaxLifecycleInProgressMs)),
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

            _insertionOrder.Enqueue(correlationId);
            _ = Interlocked.Increment(ref _activeCount);
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

    private void EnforceCapacity() {
        lock (_capacityGate) {
            EnforceCapacityLocked();
        }
    }

    private void EnforceCapacityLocked() {
        int max = Math.Max(1, options.Value.MaxActiveLifecycleEntries);
        while (Volatile.Read(ref _activeCount) > max && _insertionOrder.TryDequeue(out string? oldest)) {
            if (_byCorrelation.TryGetValue(oldest, out LifecycleEntry? entry)) {
                _ = entry.MarkNeedsReview(_time.GetUtcNow());
            }
        }

        EnforceTerminalRetention();
    }

    private void OnEntryTerminalized(string correlationId) {
        lock (_capacityGate) {
            _ = Interlocked.Decrement(ref _activeCount);
            _ = Interlocked.Increment(ref _terminalCount);
            _terminalOrder.Enqueue(correlationId);
            EnforceTerminalRetention();
        }
    }

    private void EnforceTerminalRetention() {
        int maxRetained = Math.Max(1, options.Value.MaxRetainedTerminalLifecycleEntries);
        while (Volatile.Read(ref _terminalCount) > maxRetained && _terminalOrder.TryDequeue(out string? oldest)) {
            if (!_byCorrelation.TryGetValue(oldest, out LifecycleEntry? entry) || !entry.IsTerminal) {
                continue;
            }

            if (_byCorrelation.TryRemove(oldest, out LifecycleEntry? removed)) {
                if (_byMessage.TryGetValue(removed.MessageId, out LifecycleEntry? mapped)
                    && ReferenceEquals(mapped, removed)) {
                    _ = _byMessage.TryRemove(removed.MessageId, out _);
                }

                _ = Interlocked.Decrement(ref _terminalCount);
                removed.Dispose();
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
        private readonly Action<string> _onTerminalized;
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
            Action<string> onTerminalized) {
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

        public void Observe(CommandLifecycleTransition transition) {
            if (!string.Equals(transition.CorrelationId, CorrelationId, StringComparison.Ordinal)
                || !FrontComposerMcpUlid.IsCanonical(transition.CorrelationId)
                || (transition.MessageId is not null && !FrontComposerMcpUlid.IsCanonical(transition.MessageId))) {
                return;
            }

            bool becameTerminal = false;
            lock (_gate) {
                bool currentTerminal = IsTerminalState(_state);
                if (currentTerminal) {
                    return;
                }

                _state = transition.NewState;
                if (transition.NewState == CommandLifecycleState.Confirmed) {
                    if (_outcome is null) {
                        _outcome = transition.IdempotencyResolved
                            ? McpTerminalOutcome.IdempotentSuccess(Descriptor.Title)
                            : McpTerminalOutcome.Success();
                    }
                }
                else if (transition.NewState == CommandLifecycleState.Rejected && _outcome is null) {
                    _outcome = McpTerminalOutcome.GenericRejection();
                }

                else if (transition.NewState == CommandLifecycleState.IdempotentConfirmed && _outcome is null)
                {
                    _outcome = McpTerminalOutcome.IdempotentSuccess(Descriptor.Title);
                }
                else if (transition.NewState is CommandLifecycleState.NeedsReview or CommandLifecycleState.Warning
                    or CommandLifecycleState.DegradedExhausted && _outcome is null)
                {
                    _outcome = McpTerminalOutcome.Unconfirmed(transition.NewState);
                }

                AppendHistory(transition.NewState, transition.MessageId, transition.TimestampUtc, transition.IdempotencyResolved);

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

                _onTerminalized(CorrelationId);
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
            if (Volatile.Read(ref _entryDisposed) != 0) {
                return false;
            }

            lock (_gate) {
                if (IsTerminalState(_state)) {
                    return false;
                }

                _state = CommandLifecycleState.Rejected;
                _outcome = outcome;
                _terminalRecorded = true;
                Volatile.Write(ref _isTerminal, 1);
                AppendHistory(CommandLifecycleState.Rejected, MessageId, observedAtUtc ?? _time.GetUtcNow(), idempotencyResolved: false);
            }

            try {
                _ = _timeoutTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException) {
            }

            _onTerminalized(CorrelationId);
            return true;
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
