using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;

using Hexalith.FrontComposer.Contracts.Lifecycle;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Lifecycle;

/// <summary>
/// Story 2-3 — scoped cross-command correlation-keyed lifecycle state index (ADR-017). Bridges forward
/// typed Fluxor actions via <see cref="Transition(string, CommandLifecycleState, string?)"/>; consumers
/// <see cref="Subscribe(string, System.Action{CommandLifecycleTransition})"/> to a CorrelationId to observe
/// transitions. Enforces the five-state machine, exactly-one-outcome invariant (FR30), and deterministic
/// duplicate detection (FR36) via a bounded LRU of seen MessageIds.
/// </summary>
public sealed class LifecycleStateService : ILifecycleStateService, IAsyncDisposable {
    /// <summary>
    /// Decision D20 singleton-resolve guard (Winston review 2026-04-16). Detects mis-registration as
    /// <see cref="Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton"/> — two instances
    /// for the same scope provider trips the constructor throw. <see cref="ConditionalWeakTable{TKey, TValue}"/>
    /// lets GC reclaim the entry when the scope is collected.
    /// </summary>
    private static readonly ConditionalWeakTable<IServiceProvider, LifecycleStateService> _perScope = new();

    private readonly ConcurrentDictionary<string, LifecycleEntry> _entries = new(StringComparer.Ordinal);

    /// <summary>
    /// Per-correlation subscriber lists (Decision D6). Mutated only via
    /// <see cref="ImmutableInterlocked"/> so concurrent Subscribe/Dispose races cannot lose updates.
    /// </summary>
    private ImmutableDictionary<string, ImmutableList<Subscription>> _subs =
        ImmutableDictionary<string, ImmutableList<Subscription>>.Empty.WithComparers(StringComparer.Ordinal);

    private readonly ConcurrentDictionary<string, byte> _seenMessageIds = new(StringComparer.Ordinal);
    private readonly ConcurrentQueue<string> _seenOrder = new();
    private readonly object _seenLock = new();

    private readonly TimeProvider _time;
    private readonly LifecycleOptions _options;
    private readonly ILogger<LifecycleStateService> _logger;

    private int _disposed;

    public LifecycleStateService(
        IOptions<LifecycleOptions> options,
        TimeProvider? time = null,
        ILogger<LifecycleStateService>? logger = null) {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _time = time ?? TimeProvider.System;
        _logger = logger ?? NullLogger<LifecycleStateService>.Instance;
    }

    /// <summary>
    /// Constructor with an <see cref="IServiceProvider"/> scope for the Decision D20 singleton-resolve
    /// guard. DI wires this overload when the service is resolved from a scope, so two resolves against
    /// the same scope provider raise <see cref="InvalidOperationException"/> with a Fix-framed message.
    /// </summary>
    public LifecycleStateService(
        IServiceProvider scopeProvider,
        IOptions<LifecycleOptions> options,
        TimeProvider? time = null,
        ILogger<LifecycleStateService>? logger = null)
        : this(options, time, logger) {
        if (scopeProvider is null) {
            return;
        }

        if (_perScope.TryGetValue(scopeProvider, out _)) {
            throw new InvalidOperationException(
                "LifecycleStateService must be registered as Scoped, not Singleton. " +
                "Fix: services.TryAddScoped<ILifecycleStateService, LifecycleStateService>()");
        }

        _perScope.Add(scopeProvider, this);
    }

    /// <inheritdoc/>
    public IDisposable Subscribe(string correlationId, Action<CommandLifecycleTransition> onTransition) {
        if (correlationId is null) {
            throw new ArgumentNullException(nameof(correlationId));
        }

        if (onTransition is null) {
            throw new ArgumentNullException(nameof(onTransition));
        }

        if (Volatile.Read(ref _disposed) != 0) {
            throw new ObjectDisposedException(nameof(LifecycleStateService));
        }

        Subscription subscription = new(correlationId, onTransition);

        _ = ImmutableInterlocked.AddOrUpdate(
            ref _subs,
            correlationId,
            _ => ImmutableList.Create(subscription),
            (_, existing) => existing.Add(subscription));

        if (_entries.TryGetValue(correlationId, out LifecycleEntry? entry)) {
            CommandLifecycleState current;
            string? messageId;
            DateTimeOffset originalAt;
            lock (entry) {
                current = entry.State;
                messageId = entry.MessageId;
                originalAt = entry.OriginalTransitionAt;
            }

            try {
                onTransition(new CommandLifecycleTransition(
                    CorrelationId: correlationId,
                    PreviousState: CommandLifecycleState.Idle,
                    NewState: current,
                    MessageId: messageId,
                    TimestampUtc: _time.GetUtcNow(),
                    LastTransitionAt: originalAt,
                    IdempotencyResolved: false));
            }
            catch (Exception ex) when (ex is not OperationCanceledException) {
                _logger.LogError(
                    ex,
                    "Lifecycle subscribe replay callback faulted. CorrelationId={CorrelationId}",
                    correlationId);
            }
        }

        return new Unsubscriber(this, subscription);
    }

    /// <inheritdoc/>
    public CommandLifecycleState GetState(string correlationId) {
        if (correlationId is null) {
            throw new ArgumentNullException(nameof(correlationId));
        }

        if (_entries.TryGetValue(correlationId, out LifecycleEntry? entry)) {
            lock (entry) {
                return entry.State;
            }
        }

        return CommandLifecycleState.Idle;
    }

    /// <inheritdoc/>
    public string? GetMessageId(string correlationId) {
        if (correlationId is null) {
            throw new ArgumentNullException(nameof(correlationId));
        }

        if (_entries.TryGetValue(correlationId, out LifecycleEntry? entry)) {
            lock (entry) {
                return entry.MessageId;
            }
        }

        return null;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetActiveCorrelationIds() => [.. _entries.Keys];

    /// <inheritdoc/>
    public void Transition(string correlationId, CommandLifecycleState newState, string? messageId = null)
        => Transition(correlationId, newState, messageId, idempotencyResolved: false);

    /// <inheritdoc/>
    public void Transition(
        string correlationId,
        CommandLifecycleState newState,
        string? messageId,
        bool idempotencyResolved) {
        if (correlationId is null) {
            throw new ArgumentNullException(nameof(correlationId));
        }

        if (Volatile.Read(ref _disposed) != 0) {
            throw new ObjectDisposedException(nameof(LifecycleStateService));
        }

        DateTimeOffset now = _time.GetUtcNow();
        bool entryExistedBefore = _entries.TryGetValue(correlationId, out _);
        bool crossCorrelationCollision = messageId is not null
            && !entryExistedBefore
            && _seenMessageIds.ContainsKey(messageId);

        if (crossCorrelationCollision) {
            _logger.LogWarning(
                "HFC2005: duplicate MessageId detected across CorrelationIds (treated as fresh submission). " +
                "CorrelationId={CorrelationId} MessageId={MessageId}",
                correlationId,
                messageId);
        }

        LifecycleEntry entry = _entries.GetOrAdd(
            correlationId,
            static (_, ctx) => new LifecycleEntry {
                State = CommandLifecycleState.Idle,
                OriginalTransitionAt = ctx.now,
                LastUpdated = ctx.now,
            },
            (now, correlationId));

        CommandLifecycleState previous;
        CommandLifecycleState applied;
        bool isTerminalFirstEntry;
        bool computedIdempotencyResolved;
        bool dropped;
        DateTimeOffset originalAt;

        lock (entry) {
            previous = entry.State;

            if (!IsValidTransition(previous, newState)) {
                _logger.LogError(
                    "HFC2004: invalid lifecycle transition dropped. " +
                    "CorrelationId={CorrelationId} From={From} To={To} MessageId={MessageId}",
                    correlationId,
                    previous,
                    newState,
                    messageId);
                return;
            }

            if (!entryExistedBefore && previous == CommandLifecycleState.Idle
                && newState != CommandLifecycleState.Submitting
                && newState != CommandLifecycleState.Idle) {
                _logger.LogWarning(
                    "HFC2007: transition arrived for a CorrelationId without a prior Submitted observation. " +
                    "CorrelationId={CorrelationId} State={State}",
                    correlationId,
                    newState);
            }

            applied = newState;
            entry.State = newState;
            entry.LastUpdated = now;
            if (messageId is not null) {
                entry.MessageId = messageId;
            }

            originalAt = entry.OriginalTransitionAt;

            if (newState == CommandLifecycleState.Idle) {
                entry.MessageId = null;
                entry.OutcomeNotifications = 0;
                entry.OriginalTransitionAt = now;
                originalAt = now;
            }

            if (IsTerminal(newState)) {
                int prior = Interlocked.CompareExchange(ref entry.OutcomeNotifications, 1, 0);
                isTerminalFirstEntry = prior == 0;
                computedIdempotencyResolved = !isTerminalFirstEntry;
                // Decision D8 — duplicate terminal for same CorrelationId is silently absorbed
                // (no second outcome notification). FR30 "≤1 user-visible outcome" invariant.
                dropped = prior != 0 && previous == newState;
            }
            else {
                isTerminalFirstEntry = false;
                computedIdempotencyResolved = false;
                dropped = false;
            }
        }

        if (dropped) {
            return;
        }

        if (messageId is not null) {
            RecordMessageId(messageId);
        }

        // P8 — caller-supplied flag wins over the auto-detected duplicate-terminal flag so the
        // pending-command resolver can mark IdempotentConfirmed outcomes as already-applied even
        // on the first observed terminal for this correlation.
        bool effectiveIdempotencyResolved = computedIdempotencyResolved || idempotencyResolved;

        CommandLifecycleTransition transition = new(
            CorrelationId: correlationId,
            PreviousState: previous,
            NewState: applied,
            MessageId: messageId ?? entry.MessageId,
            TimestampUtc: now,
            LastTransitionAt: originalAt,
            IdempotencyResolved: effectiveIdempotencyResolved);

        InvokeSubscribers(correlationId, transition);
    }

    private void InvokeSubscribers(string correlationId, CommandLifecycleTransition transition) {
        ImmutableDictionary<string, ImmutableList<Subscription>> subs = _subs;
        if (!subs.TryGetValue(correlationId, out ImmutableList<Subscription>? snapshot) || snapshot.Count == 0) {
            return;
        }

        foreach (Subscription sub in snapshot) {
            if (Volatile.Read(ref sub.Disposed) != 0) {
                continue;
            }

            try {
                sub.Callback(transition);
            }
            catch (Exception ex) when (ex is not OperationCanceledException) {
                _logger.LogError(
                    ex,
                    "Lifecycle subscriber callback faulted. CorrelationId={CorrelationId} NewState={NewState}",
                    correlationId,
                    transition.NewState);
            }
        }
    }

    private void RecordMessageId(string messageId) {
        if (!_seenMessageIds.TryAdd(messageId, 0)) {
            return;
        }

        _seenOrder.Enqueue(messageId);

        if (_seenMessageIds.Count <= _options.MessageIdCacheCapacity) {
            return;
        }

        lock (_seenLock) {
            while (_seenMessageIds.Count > _options.MessageIdCacheCapacity
                && _seenOrder.TryDequeue(out string? oldest)) {
                _ = _seenMessageIds.TryRemove(oldest, out _);
                _logger.LogDebug("Lifecycle MessageId cache evicted oldest. Evicted={Evicted}", oldest);
            }
        }
    }

    private static bool IsTerminal(CommandLifecycleState state) =>
        state is CommandLifecycleState.Confirmed or CommandLifecycleState.Rejected;

    private static bool IsValidTransition(CommandLifecycleState from, CommandLifecycleState to) {
        if (to == CommandLifecycleState.Idle) {
            return true;
        }

        if (from == to) {
            return true;
        }

        return from switch {
            CommandLifecycleState.Idle => to == CommandLifecycleState.Submitting,
            CommandLifecycleState.Submitting => to is CommandLifecycleState.Acknowledged
                or CommandLifecycleState.Confirmed
                or CommandLifecycleState.Rejected,
            CommandLifecycleState.Acknowledged => to is CommandLifecycleState.Syncing
                or CommandLifecycleState.Confirmed
                or CommandLifecycleState.Rejected,
            CommandLifecycleState.Syncing => to is CommandLifecycleState.Confirmed
                or CommandLifecycleState.Rejected,
            CommandLifecycleState.Confirmed => false,
            CommandLifecycleState.Rejected => false,
            _ => false,
        };
    }

    /// <inheritdoc/>
    public void Dispose() {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) {
            return;
        }

        _entries.Clear();
        _subs = ImmutableDictionary<string, ImmutableList<Subscription>>.Empty.WithComparers(StringComparer.Ordinal);
        _seenMessageIds.Clear();
        while (_seenOrder.TryDequeue(out _)) { }
    }

    /// <inheritdoc/>
    public ValueTask DisposeAsync() {
        Dispose();
        return ValueTask.CompletedTask;
    }

    internal sealed class LifecycleEntry {
        public CommandLifecycleState State;
        public string? MessageId;
        public DateTimeOffset LastUpdated;
        public DateTimeOffset OriginalTransitionAt;
        public int OutcomeNotifications;
    }

    internal sealed class Subscription {
        public readonly string CorrelationId;
        public readonly Action<CommandLifecycleTransition> Callback;
        public int Disposed;

        public Subscription(string correlationId, Action<CommandLifecycleTransition> callback) {
            CorrelationId = correlationId;
            Callback = callback;
        }
    }

    private sealed class Unsubscriber : IDisposable {
        private readonly LifecycleStateService _service;
        private readonly Subscription _subscription;
        private int _disposed;

        public Unsubscriber(LifecycleStateService service, Subscription subscription) {
            _service = service;
            _subscription = subscription;
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) {
                return;
            }

            Volatile.Write(ref _subscription.Disposed, 1);

            _ = ImmutableInterlocked.Update(
                ref _service._subs,
                static (dict, sub) => {
                    if (!dict.TryGetValue(sub.CorrelationId, out ImmutableList<Subscription>? list)) {
                        return dict;
                    }

                    ImmutableList<Subscription> next = list.Remove(sub);
                    return next.IsEmpty
                        ? dict.Remove(sub.CorrelationId)
                        : dict.SetItem(sub.CorrelationId, next);
                },
                _subscription);
        }
    }
}
