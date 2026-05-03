using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Mcp.Invocation;

public sealed partial class FrontComposerMcpLifecycleTracker(
    FrontComposerMcpToolAdmissionService admissionService,
    IServiceProvider services,
    IOptions<FrontComposerMcpOptions> options) : IDisposable {
    private const int MaxIdentifierLength = 64;
    private readonly ConcurrentDictionary<string, LifecycleEntry> _byCorrelation = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, LifecycleEntry> _byMessage = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _insertionOrder = new();
    private readonly ConcurrentQueue<string> _terminalOrder = new();
    private readonly object _capacityGate = new();
    private readonly TimeProvider _time = services.GetService<TimeProvider>() ?? TimeProvider.System;
    // P46: optional logger so the bare `catch` in ReadCoreAsync surfaces sanitized failure
    // categories instead of disappearing. Resolved laxly so test hosts without logging still work.
    private readonly ILogger<FrontComposerMcpLifecycleTracker>? _logger
        = services.GetService<ILogger<FrontComposerMcpLifecycleTracker>>();
    private int _disposed;
    // P36: O(1) counters mutated under _capacityGate; replace per-iteration dictionary scans.
    private int _activeCount;
    private int _terminalCount;

    internal McpCommandAcknowledgement TrackAcknowledged(
        McpCommandDescriptor descriptor,
        CommandResult result,
        IReadOnlyList<(CommandLifecycleState State, string? MessageId)> pendingTransitions,
        CancellationToken cancellationToken) {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(result);

        string messageId = NormalizeIdentifier(result.MessageId)
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        string correlationId = NormalizeIdentifier(result.CorrelationId) ?? messageId;
        ILifecycleStateService lifecycle = services.GetService<ILifecycleStateService>()
            ?? throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);

        LifecycleEntry entry = GetOrCreateEntry(descriptor, correlationId, messageId, lifecycle);
        // P48: persist the dispatcher-supplied retry hint on the entry so subsequent snapshots
        // return the same value as the acknowledgement. Without this, ack and any later snapshot
        // for the same command disagree on retry guidance.
        int clampedRetryAfter = ClampRetryAfter(result.RetryAfter);
        entry.SetRetryAfterMs(clampedRetryAfter);
        if (!entry.AcknowledgmentEmitted) {
            lifecycle.Transition(correlationId, CommandLifecycleState.Submitting);
            lifecycle.Transition(correlationId, CommandLifecycleState.Acknowledged, messageId);
            entry.AcknowledgmentEmitted = true;
        }

        foreach ((CommandLifecycleState state, string? transitionMessageId) in pendingTransitions) {
            cancellationToken.ThrowIfCancellationRequested();
            lifecycle.Transition(correlationId, state, NormalizeIdentifier(transitionMessageId) ?? messageId);
        }

        // AC2 — the first call always reports Acknowledged. The agent uses the lifecycle subscribe
        // surface to learn the current state, even if dispatch progressed synchronously.
        return new McpCommandAcknowledgement(
            messageId,
            correlationId,
            CommandLifecycleState.Acknowledged,
            new McpLifecycleSubscription(
                options.Value.LifecycleToolName,
                options.Value.LifecycleUriPrefix + correlationId,
                clampedRetryAfter));
    }

    public Task<FrontComposerMcpResult> ReadAsync(
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken = default) => ReadCoreAsync(arguments, cancellationToken);

    private async Task<FrontComposerMcpResult> ReadCoreAsync(
        IReadOnlyDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken) {
        try {
            ThrowIfDisposed();
            if (!TryReadHandle(arguments, out string? handle)) {
                return HiddenUnknown();
            }

            string handleValue = handle!;
            if (!_byCorrelation.TryGetValue(handleValue, out LifecycleEntry? entry)) {
                _byMessage.TryGetValue(handleValue, out entry);
            }

            if (entry is null) {
                return HiddenUnknown();
            }

            McpToolResolutionResult current = await admissionService
                .ResolveAsync(entry.Descriptor.ProtocolName, cancellationToken)
                .ConfigureAwait(false);
            if (!current.Accepted
                || current.Tool is null
                || !string.Equals(current.Tool.Descriptor.ProtocolName, entry.Descriptor.ProtocolName, StringComparison.Ordinal)) {
                return HiddenUnknown();
            }

            McpLifecycleSnapshot snapshot = entry.ToSnapshot(options.Value);
            return FrontComposerMcpResult.Success("Lifecycle snapshot.", snapshot.ToJson());
        }
        catch (OperationCanceledException) {
            return FrontComposerMcpResult.Failure(
                cancellationToken.IsCancellationRequested
                    ? FrontComposerMcpFailureCategory.Canceled
                    : FrontComposerMcpFailureCategory.DownstreamFailed);
        }
        catch (FrontComposerMcpException ex) {
            return FrontComposerMcpResult.Failure(ex.Category);
        }
        catch (Exception ex) {
            // P46: surface a sanitized failure category so the operator can correlate the
            // negative outcome to a code path. We never log the exception's stack/message — only
            // the type name — to preserve the AC13 redaction contract for hidden-unknown reads.
            LogReadFailure(ex);
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.DownstreamFailed);
        }
    }

    private void LogReadFailure(Exception ex) {
        if (_logger is null || !_logger.IsEnabled(LogLevel.Warning)) {
            return;
        }

        LogReadFailureMessage(_logger, ex.GetType().FullName ?? "Exception");
    }

    [LoggerMessage(EventId = 8300, Level = LogLevel.Warning,
        Message = "MCP lifecycle read failed with sanitized category. ExceptionType={ExceptionType}.")]
    private static partial void LogReadFailureMessage(ILogger logger, string exceptionType);

    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        LifecycleEntry[] snapshot = [.. _byCorrelation.Values];
        _byCorrelation.Clear();
        _byMessage.Clear();
        while (_insertionOrder.TryDequeue(out _)) { }
        while (_terminalOrder.TryDequeue(out _)) { }
        Volatile.Write(ref _activeCount, 0);
        Volatile.Write(ref _terminalCount, 0);

        foreach (LifecycleEntry entry in snapshot) {
            entry.Dispose();
        }
    }

    internal static string? NormalizeIdentifier(string? value) {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaxIdentifierLength) {
            return null;
        }

        // Reject non-ASCII before NFKC so fullwidth / Unicode-confusable digits/letters cannot
        // fold into the canonical alphabet (AC23 / D13).
        for (int i = 0; i < value.Length; i++) {
            if (value[i] > 0x7F) {
                return null;
            }
        }

        // P41: reject leading/trailing ASCII whitespace before normalization. Otherwise two
        // distinct caller inputs (`" 01J..."` and `"01J..."`) collapse onto the same canonical
        // handle, opening a small canonicalization-mismatch surface in audit/logs.
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
        string messageId,
        ILifecycleStateService lifecycle) {
        if (_byCorrelation.TryGetValue(correlationId, out LifecycleEntry? existing)) {
            return existing;
        }

        FrontComposerMcpOptions opts = options.Value;
        // P34: timer is constructed in a quiescent state (Timeout.InfiniteTimeSpan) and armed via
        // Start() only after the entry is fully wired; loser path disposes before timer can fire.
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

        // Subscribe AFTER TryAdd succeeds so the loser path never wires a live callback.
        try {
            created.Subscription = lifecycle.Subscribe(correlationId, created.Observe);
        }
        catch {
            _byCorrelation.TryRemove(correlationId, out _);
            created.Dispose();
            throw;
        }

        // P50: fail-closed on _byMessage collision so a second registration cannot become
        // unreachable by messageId lookup. Roll back the correlation registration and surface
        // UnsupportedSchema; the framework-issued ULID factory makes this case configuration-only.
        if (!_byMessage.TryAdd(messageId, created)) {
            _byCorrelation.TryRemove(correlationId, out _);
            created.Dispose();
            throw new FrontComposerMcpException(FrontComposerMcpFailureCategory.UnsupportedSchema);
        }

        _insertionOrder.Enqueue(correlationId);
        Interlocked.Increment(ref _activeCount);
        created.Start();
        EnforceCapacity();
        return created;
    }

    private void EnforceCapacity() {
        int max = Math.Max(1, options.Value.MaxActiveLifecycleEntries);
        lock (_capacityGate) {
            while (Volatile.Read(ref _activeCount) > max && _insertionOrder.TryDequeue(out string? oldest)) {
                if (_byCorrelation.TryGetValue(oldest, out LifecycleEntry? entry)) {
                    entry.MarkNeedsReview(_time.GetUtcNow());
                }
            }

            EnforceTerminalRetention();
        }
    }

    private void OnEntryTerminalized(string correlationId) {
        lock (_capacityGate) {
            Interlocked.Decrement(ref _activeCount);
            Interlocked.Increment(ref _terminalCount);
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
                    _byMessage.TryRemove(removed.MessageId, out _);
                }

                Interlocked.Decrement(ref _terminalCount);
                removed.Dispose();
            }
        }
    }

    private static bool TryReadHandle(IReadOnlyDictionary<string, JsonElement>? arguments, out string? handle) {
        handle = null;
        if (arguments is null || arguments.Count != 1) {
            return false;
        }

        KeyValuePair<string, JsonElement> pair = arguments.Single();
        if (pair.Key is not "correlationId" and not "messageId"
            || pair.Value.ValueKind != JsonValueKind.String) {
            return false;
        }

        handle = NormalizeIdentifier(pair.Value.GetString());
        return handle is not null;
    }

    private static FrontComposerMcpResult HiddenUnknown()
        // P45: route through the shared admission helper so the lifecycle hidden-unknown shape
        // tracks any future change to the AC9 unknown-tool response contract automatically.
        => FrontComposerMcpResult.Failure(
            FrontComposerMcpFailureCategory.UnknownTool,
            FrontComposerMcpToolAdmissionService.BuildHiddenUnknownStructuredContent());

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
            throw new ObjectDisposedException(nameof(FrontComposerMcpLifecycleTracker));
        }
    }

    [GeneratedRegex("^[0-9A-HJKMNP-TV-Z]{26}$", RegexOptions.CultureInvariant)]
    private static partial Regex CanonicalUlidRegex();

    private sealed class LifecycleEntry : IDisposable {
        private readonly object _gate = new();
        private readonly Queue<McpLifecycleTransitionDto> _history = new();
        private readonly int _maxHistory;
        private readonly TimeSpan _timeout;
        private readonly TimeProvider _time;
        private readonly Action<string> _onTerminalized;
        private readonly ITimer _timeoutTimer;
        private long _nextSequence;
        private bool _historyTruncated;
        private bool _terminalRecorded;
        private int _isTerminal; // P35: lock-free terminal flag for capacity counters and IsTerminal.
        private int _entryDisposed; // P33: short-circuit timer callbacks racing Dispose().
        private int _retryAfterMs; // P48: dispatcher-supplied retry hint, surfaced from snapshots.
        private CommandLifecycleState _state = CommandLifecycleState.Acknowledged;
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
            // P34: construct timer in quiescent state. Tracker calls Start() after wiring.
            _timeoutTimer = time.CreateTimer(
                static state => ((LifecycleEntry)state!).MarkTimedOut(),
                this,
                Timeout.InfiniteTimeSpan,
                Timeout.InfiniteTimeSpan);
        }

        public McpCommandDescriptor Descriptor { get; }

        public string CorrelationId { get; }

        public string MessageId { get; }

        public IDisposable? Subscription { get; set; }

        public bool AcknowledgmentEmitted { get; set; }

        public bool IsTerminal => Volatile.Read(ref _isTerminal) != 0;

        public CommandLifecycleState CurrentState {
            get {
                lock (_gate) {
                    return _state;
                }
            }
        }

        // P48: persist the dispatcher-supplied retry hint so subsequent snapshots return the
        // same value as the acknowledgement. The tracker calls SetRetryAfterMs after Clamp.
        public void SetRetryAfterMs(int retryAfterMs) => Volatile.Write(ref _retryAfterMs, retryAfterMs);

        // P34: arm the timeout timer once the tracker has fully registered the entry.
        public void Start() {
            if (Volatile.Read(ref _entryDisposed) != 0) {
                return;
            }

            try {
                _timeoutTimer.Change(_timeout, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException) {
                // P33: Start lost a race with Dispose; nothing to arm.
            }
        }

        public void Observe(CommandLifecycleTransition transition) {
            bool becameTerminal = false;
            lock (_gate) {
                // Defense in depth: terminal regression is impossible at the MCP edge regardless of
                // upstream behaviour. Once a terminal outcome is recorded, drop further observations
                // unless they are the same terminal kind (idempotent re-delivery).
                bool currentTerminal = _state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;
                if (currentTerminal && transition.NewState != _state) {
                    return;
                }

                // P52: same-state idempotent re-delivery (Confirmed → Confirmed, Rejected → Rejected)
                // must not append a duplicate row to the bounded history; the truncation loop would
                // eventually evict the original Acknowledged frame. We still allow the
                // IdempotencyResolved upgrade in `_outcome` so agents see the duplicate-detection
                // signal when it arrives after the initial Confirmed observation.
                bool sameStateRedelivery = currentTerminal && transition.NewState == _state;

                _state = transition.NewState;
                if (transition.NewState == CommandLifecycleState.Confirmed) {
                    // Outcome is set once. An incoming Confirmed flagged IdempotencyResolved is a
                    // clarification of the same terminal — upgrade Success → IdempotentSuccess so
                    // agents see the duplicate-detection signal even when it arrives after the
                    // initial Confirmed observation.
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
                    _timeoutTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                }
                catch (ObjectDisposedException) {
                    // P33: timer disposed concurrently; nothing to cancel.
                }

                _onTerminalized(CorrelationId);
            }
        }

        public bool MarkTimedOut() => MarkSyntheticTerminal(McpTerminalOutcome.TimedOut());

        public bool MarkNeedsReview(DateTimeOffset observedAtUtc) => MarkSyntheticTerminal(McpTerminalOutcome.NeedsReview(), observedAtUtc);

        private bool MarkSyntheticTerminal(McpTerminalOutcome outcome, DateTimeOffset? observedAtUtc = null) {
            // P33: refuse terminalization if the entry has been disposed; the timer callback can fire
            // after Dispose() because ITimer.Dispose() is non-blocking on in-flight callbacks.
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
                _timeoutTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }
            catch (ObjectDisposedException) {
                // P33: timer disposed between flag check and Change call; benign.
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

        public McpLifecycleSnapshot ToSnapshot(FrontComposerMcpOptions opts) {
            lock (_gate) {
                bool terminal = _state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;
                // AC3: agent-visible history starts at Acknowledged. Pre-Acknowledged web-only
                // intermediate states (Idle / Submitting) are filtered from the snapshot.
                IReadOnlyList<McpLifecycleTransitionDto> agentHistory = [
                    .. _history.Where(t => t.State is not CommandLifecycleState.Idle
                        and not CommandLifecycleState.Submitting),
                ];
                // P48: prefer the dispatcher-supplied (clamped) retry hint persisted on this
                // entry; fall back to the configured default for entries that never carried one.
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
            Subscription?.Dispose();
        }
    }
}
