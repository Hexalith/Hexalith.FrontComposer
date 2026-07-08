using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

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
    private const int MaxIdentifierLength = 64;
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
    /// <param name="result">Dispatcher acknowledgement containing the agent-visible handles.</param>
    /// <param name="pendingTransitions">Transitions captured by the command dispatcher callback.</param>
    /// <param name="lifecycle">Scoped lifecycle service for the current command request.</param>
    /// <param name="cancellationToken">Cancellation token for pending transition recording.</param>
    /// <returns>The MCP acknowledgement payload.</returns>
    internal McpCommandAcknowledgement TrackAcknowledged(
        McpCommandDescriptor descriptor,
        CommandResult result,
        IReadOnlyList<(CommandLifecycleState State, string? MessageId)> pendingTransitions,
        ILifecycleStateService lifecycle,
        CancellationToken cancellationToken) {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(lifecycle);

        string messageId = NormalizeIdentifier(result.MessageId)
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        string correlationId = NormalizeIdentifier(result.CorrelationId) ?? messageId;
        LifecycleEntry entry = GetOrCreateEntry(descriptor, correlationId, messageId);
        int clampedRetryAfter = ClampRetryAfter(result.RetryAfter);
        entry.SetRetryAfterMs(clampedRetryAfter);

        using IDisposable subscription = lifecycle.Subscribe(correlationId, entry.Observe);
        if (entry.TryMarkAcknowledgmentEmitted()) {
            lifecycle.Transition(correlationId, CommandLifecycleState.Submitting);
            lifecycle.Transition(correlationId, CommandLifecycleState.Acknowledged, messageId);
        }

        foreach ((CommandLifecycleState state, string? transitionMessageId) in pendingTransitions) {
            cancellationToken.ThrowIfCancellationRequested();
            lifecycle.Transition(correlationId, state, NormalizeIdentifier(transitionMessageId) ?? messageId);
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

        string? correlationId = NormalizeIdentifier(transition.CorrelationId);
        if (correlationId is null || !_byCorrelation.TryGetValue(correlationId, out LifecycleEntry? entry)) {
            return false;
        }

        entry.Observe(transition);
        return true;
    }

    /// <summary>
    /// Attempts to read the lifecycle snapshot for an agent-supplied handle.
    /// </summary>
    /// <param name="handle">Canonical correlation or message identifier.</param>
    /// <param name="currentOptions">Current MCP options used for retry bounds.</param>
    /// <param name="descriptor">Descriptor associated with the lifecycle entry.</param>
    /// <param name="snapshot">Current lifecycle snapshot when the handle is known.</param>
    /// <returns><see langword="true"/> when the handle is known.</returns>
    internal bool TryReadSnapshot(
        string handle,
        FrontComposerMcpOptions currentOptions,
        out McpCommandDescriptor descriptor,
        out McpLifecycleSnapshot snapshot) {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(handle);
        ArgumentNullException.ThrowIfNull(currentOptions);

        if (!_byCorrelation.TryGetValue(handle, out LifecycleEntry? entry)) {
            _ = _byMessage.TryGetValue(handle, out entry);
        }

        if (entry is null) {
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

    /// <summary>
    /// Normalizes agent-supplied lifecycle handles to canonical ULID strings.
    /// </summary>
    /// <param name="value">Raw identifier value.</param>
    /// <returns>The canonical identifier, or <see langword="null"/> when the value is invalid.</returns>
    internal static string? NormalizeIdentifier(string? value) {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxIdentifierLength) {
            return null;
        }

        for (int i = 0; i < value.Length; i++) {
            if (value[i] > 0x7F) {
                return null;
            }
        }

        if (value.Length != value.AsSpan().Trim().Length) {
            return null;
        }

        string normalized = value.Normalize(NormalizationForm.FormC);
        if (!CanonicalUlidRegex().IsMatch(normalized)) {
            return null;
        }

        return normalized;
    }

    private LifecycleEntry GetOrCreateEntry(
        McpCommandDescriptor descriptor,
        string correlationId,
        string messageId) {
        if (_byCorrelation.TryGetValue(correlationId, out LifecycleEntry? existing)) {
            return existing;
        }

        lock (_capacityGate) {
            ThrowIfDisposed();
            if (_byCorrelation.TryGetValue(correlationId, out existing)) {
                return existing;
            }

            FrontComposerMcpOptions opts = options.Value;
            LifecycleEntry created = new(
                descriptor,
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

    private void ThrowIfDisposed() {
        if (Volatile.Read(ref _disposed) != 0) {
            throw new ObjectDisposedException(nameof(FrontComposerMcpLifecycleStore));
        }
    }

    [GeneratedRegex("^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalUlidRegex();

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
            string correlationId,
            string messageId,
            int maxHistory,
            TimeSpan timeout,
            TimeProvider time,
            Action<string> onTerminalized) {
            Descriptor = descriptor;
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

        public string CorrelationId { get; }

        public string MessageId { get; }

        public bool IsTerminal => Volatile.Read(ref _isTerminal) != 0;

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
            bool becameTerminal = false;
            lock (_gate) {
                bool currentTerminal = _state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;
                if (currentTerminal && transition.NewState != _state) {
                    return;
                }

                bool sameStateRedelivery = currentTerminal && transition.NewState == _state;

                _state = transition.NewState;
                if (transition.NewState == CommandLifecycleState.Confirmed) {
                    if (_outcome is null) {
                        _outcome = transition.IdempotencyResolved
                            ? McpTerminalOutcome.IdempotentSuccess(Descriptor.Title)
                            : McpTerminalOutcome.Success();
                    }
                    else if (transition.IdempotencyResolved
                        && _outcome.Kind == McpTerminalOutcomeKind.Confirmed) {
                        _outcome = McpTerminalOutcome.IdempotentSuccess(Descriptor.Title);
                    }
                }
                else if (transition.NewState == CommandLifecycleState.Rejected && _outcome is null) {
                    _outcome = McpTerminalOutcome.GenericRejection();
                }

                if (!sameStateRedelivery) {
                    AppendHistory(transition.NewState, NormalizeIdentifier(transition.MessageId), transition.TimestampUtc, transition.IdempotencyResolved);
                }

                if (_state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected && !_terminalRecorded) {
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
                bool terminal = _state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;
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

        private bool MarkSyntheticTerminal(McpTerminalOutcome outcome, DateTimeOffset? observedAtUtc = null) {
            if (Volatile.Read(ref _entryDisposed) != 0) {
                return false;
            }

            lock (_gate) {
                if (_state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected) {
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
