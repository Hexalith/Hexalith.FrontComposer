using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Mcp;

using Microsoft.Extensions.DependencyInjection;
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
    private readonly object _capacityGate = new();
    private int _disposed;

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
                ClampRetryAfter(result.RetryAfter)));
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
        catch {
            return FrontComposerMcpResult.Failure(FrontComposerMcpFailureCategory.DownstreamFailed);
        }
    }

    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        LifecycleEntry[] snapshot = [.. _byCorrelation.Values];
        _byCorrelation.Clear();
        _byMessage.Clear();
        while (_insertionOrder.TryDequeue(out _)) { }

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

        string normalized = value.Trim().Normalize(NormalizationForm.FormC);
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

        LifecycleEntry created = new(descriptor, correlationId, messageId, options.Value.MaxLifecycleTransitionHistory);
        if (!_byCorrelation.TryAdd(correlationId, created)) {
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

        // _byMessage uses TryAdd so a message-id collision does not silently overwrite.
        _byMessage.TryAdd(messageId, created);
        _insertionOrder.Enqueue(correlationId);
        EnforceCapacity();
        return created;
    }

    private void EnforceCapacity() {
        int max = Math.Max(1, options.Value.MaxActiveLifecycleEntries);
        lock (_capacityGate) {
            while (_byCorrelation.Count > max && _insertionOrder.TryDequeue(out string? oldest)) {
                if (_byCorrelation.TryRemove(oldest, out LifecycleEntry? removed)) {
                    if (_byMessage.TryGetValue(removed.MessageId, out LifecycleEntry? mapped)
                        && ReferenceEquals(mapped, removed)) {
                        _byMessage.TryRemove(removed.MessageId, out _);
                    }

                    removed.Dispose();
                }
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
        => FrontComposerMcpResult.Failure(
            FrontComposerMcpFailureCategory.UnknownTool,
            new JsonObject {
                ["category"] = "unknown_tool",
                ["suggestion"] = null,
                ["visibleTools"] = new JsonArray(),
                ["docsCode"] = "HFC-MCP-UNKNOWN-TOOL",
            });

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

    private sealed class LifecycleEntry(
        McpCommandDescriptor descriptor,
        string correlationId,
        string messageId,
        int maxHistory) : IDisposable {
        private readonly object _gate = new();
        private readonly Queue<McpLifecycleTransitionDto> _history = new();
        private long _nextSequence;
        private bool _historyTruncated;
        private CommandLifecycleState _state = CommandLifecycleState.Acknowledged;
        private McpTerminalOutcome? _outcome;

        public McpCommandDescriptor Descriptor { get; } = descriptor;

        public string CorrelationId { get; } = correlationId;

        public string MessageId { get; } = messageId;

        public IDisposable? Subscription { get; set; }

        public bool AcknowledgmentEmitted { get; set; }

        public CommandLifecycleState CurrentState {
            get {
                lock (_gate) {
                    return _state;
                }
            }
        }

        public void Observe(CommandLifecycleTransition transition) {
            lock (_gate) {
                // Defense in depth: terminal regression is impossible at the MCP edge regardless of
                // upstream behaviour. Once a terminal outcome is recorded, drop further observations
                // unless they are the same terminal kind (idempotent re-delivery).
                bool currentTerminal = _state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;
                if (currentTerminal && transition.NewState != _state) {
                    return;
                }

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

                _history.Enqueue(new McpLifecycleTransitionDto(
                    ++_nextSequence,
                    transition.NewState,
                    NormalizeIdentifier(transition.MessageId),
                    transition.TimestampUtc,
                    transition.IdempotencyResolved));

                int cap = Math.Max(1, maxHistory);
                while (_history.Count > cap) {
                    _ = _history.Dequeue();
                    _historyTruncated = true;
                }
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
                int min = Math.Max(1, opts.MinLifecycleRetryAfterMs);
                int max = Math.Max(min, opts.MaxLifecycleRetryAfterMs);
                int retryAfter = Math.Clamp(Math.Max(1, opts.DefaultLifecycleRetryAfterMs), min, max);
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

        public void Dispose() => Subscription?.Dispose();
    }
}
