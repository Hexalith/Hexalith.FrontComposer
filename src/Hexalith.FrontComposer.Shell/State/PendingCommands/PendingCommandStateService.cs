using System.Diagnostics.CodeAnalysis;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Story 5-5 T1: bounded circuit-local pending command index. It records only framework metadata and
/// resolves terminal observations exactly once per ULID MessageId.
/// </summary>
public sealed class PendingCommandStateService : IPendingCommandStateService {
    private readonly object _gate = new();
    private readonly Dictionary<string, PendingCommandEntry> _byMessageId = new(StringComparer.Ordinal);
    private readonly Queue<string> _insertionOrder = new();
    private readonly FcShellOptions _options;
    private readonly ILifecycleStateService _lifecycle;
    private readonly IUserContextAccessor? _userContext;
    private readonly TimeProvider _time;
    private readonly ILogger<PendingCommandStateService> _logger;
    private (string? Tenant, string? User)? _scopeSnapshot;
    private bool _disposed;

    public PendingCommandStateService(
        IOptions<FcShellOptions> options,
        ILifecycleStateService lifecycle,
        TimeProvider? time = null,
        ILogger<PendingCommandStateService>? logger = null)
        : this(options, lifecycle, userContext: null, time, logger) {
    }

    public PendingCommandStateService(
        IOptions<FcShellOptions> options,
        ILifecycleStateService lifecycle,
        IUserContextAccessor? userContext,
        TimeProvider? time = null,
        ILogger<PendingCommandStateService>? logger = null) {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _userContext = userContext;
        _time = time ?? TimeProvider.System;
        _logger = logger ?? NullLogger<PendingCommandStateService>.Instance;
    }

    /// <inheritdoc />
    public PendingCommandRegistrationResult Register(PendingCommandRegistration registration) {
        ArgumentNullException.ThrowIfNull(registration);

        if (!TryNormalizeMessageId(registration.MessageId, out string? canonicalMessageId, out string? reason)) {
            _logger.LogWarning("Pending command registration rejected. Reason={Reason}", reason);
            return PendingCommandRegistrationResult.InvalidMessageId();
        }

        PendingCommandRegistration normalized = registration with { MessageId = canonicalMessageId };

        // DN3 — fail-closed on tenant/user transitions. Detected before mutation so the new
        // registration belongs to the new scope, not a leaked previous one.
        EnforceScopeBoundary();

        PendingCommandEntry registered;
        PendingCommandEntry? evicted;
        List<PendingCommandEntry> evictionList;
        lock (_gate) {
            if (_disposed) {
                return PendingCommandRegistrationResult.Disposed();
            }

            if (_byMessageId.TryGetValue(canonicalMessageId, out PendingCommandEntry? existing)) {
                if (!existing.HasSameFrameworkMetadata(normalized)) {
                    _logger.LogWarning(
                        "Pending command duplicate registration rejected because framework metadata conflicts. MessageId={MessageId}",
                        canonicalMessageId);
                    return PendingCommandRegistrationResult.ConflictingMetadata(existing);
                }

                // P17 — surface terminal merges separately so generated forms can suppress
                // duplicate AcknowledgedAction dispatches after the resolver has already moved
                // the entry to a terminal state.
                return existing.Status != PendingCommandStatus.Pending
                    ? PendingCommandRegistrationResult.MergedTerminal(existing)
                    : PendingCommandRegistrationResult.Merged(existing);
            }

            PendingCommandEntry entry = new(
                CorrelationId: normalized.CorrelationId,
                MessageId: canonicalMessageId,
                CommandTypeName: normalized.CommandTypeName,
                ProjectionTypeName: normalized.ProjectionTypeName,
                LaneKey: normalized.LaneKey,
                EntityKey: normalized.EntityKey,
                ExpectedStatusSlot: normalized.ExpectedStatusSlot,
                PriorStatusSlot: normalized.PriorStatusSlot,
                SubmittedAt: normalized.SubmittedAt ?? _time.GetUtcNow(),
                Status: PendingCommandStatus.Pending);

            _byMessageId.Add(entry.MessageId, entry);
            _insertionOrder.Enqueue(entry.MessageId);

            // P3/P4 — eviction may need to drain more than one entry when the cap is exceeded by
            // bursts; the most-recently evicted entry is reported to the caller so the generated
            // form / summary can reflect the unresolved state.
            evictionList = DrainEvictionsLocked();
            evicted = evictionList.Count > 0 ? evictionList[^1] : null;
            registered = entry;
        }

        // P2-P4 — dispatch lifecycle on the calling thread (typically the renderer dispatcher for
        // form submissions) instead of an off-thread `ThreadPool.UnsafeQueueUserWorkItem`. The
        // previous off-thread dispatch broke when subscribers called StateHasChanged and dropped
        // ExecutionContext for AsyncLocal accessors.
        if (evictionList.Count > 0) {
            DispatchEvictedLifecycle(evictionList);
        }

        return PendingCommandRegistrationResult.Registered(registered, evicted);
    }

    /// <inheritdoc />
    public PendingCommandResolutionResult ResolveTerminal(PendingCommandTerminalObservation observation) {
        ArgumentNullException.ThrowIfNull(observation);

        if (!TryNormalizeMessageId(observation.MessageId, out string? canonicalMessageId, out string? reason)) {
            _logger.LogWarning("Pending command terminal observation rejected. Reason={Reason}", reason);
            return PendingCommandResolutionResult.InvalidMessageId();
        }

        EnforceScopeBoundary();

        PendingCommandEntry terminal;
        bool duplicate;

        lock (_gate) {
            if (_disposed) {
                return PendingCommandResolutionResult.Disposed();
            }

            if (!_byMessageId.TryGetValue(canonicalMessageId, out PendingCommandEntry? entry)) {
                _logger.LogDebug(
                    "Pending command terminal observation ignored for unknown MessageId. MessageId={MessageId}",
                    canonicalMessageId);
                return PendingCommandResolutionResult.UnknownMessageId();
            }

            if (entry.Status != PendingCommandStatus.Pending) {
                terminal = entry with {
                    DuplicateTerminalObservations = entry.DuplicateTerminalObservations + 1,
                };
                _byMessageId[canonicalMessageId] = terminal;
                duplicate = true;
            }
            else {
                terminal = entry with {
                    Status = MapStatus(observation.Outcome),
                    RejectionTitle = observation.RejectionTitle,
                    RejectionDetail = observation.RejectionDetail,
                    RejectionDataImpact = observation.RejectionDataImpact,
                    TerminalAt = _time.GetUtcNow(),
                };
                _byMessageId[canonicalMessageId] = terminal;
                duplicate = false;

                // P2-P6 — purge insertion order under the same lock that wrote the terminal status.
                // Releasing then re-acquiring the gate (the previous PurgeFromInsertionOrder call)
                // exposed a TOCTOU window where a concurrent Register could mutate _insertionOrder
                // between the unlock/relock pair and break FIFO eviction guarantees.
                PurgeFromInsertionOrderLocked(canonicalMessageId);
            }
        }

        if (duplicate) {
            return PendingCommandResolutionResult.DuplicateIgnored(terminal);
        }

        try {
            CommandLifecycleState lifecycleState = terminal.Status == PendingCommandStatus.Rejected
                ? CommandLifecycleState.Rejected
                : CommandLifecycleState.Confirmed;
            // P8 — flag IdempotentConfirmed terminals as already-applied so the lifecycle
            // wrapper can render the Info bar instead of the success celebration.
            bool idempotencyResolved = terminal.Status == PendingCommandStatus.IdempotentConfirmed;
            _lifecycle.Transition(terminal.CorrelationId, lifecycleState, terminal.MessageId, idempotencyResolved);
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogError(
                ex,
                "Pending command lifecycle terminal dispatch failed. MessageId={MessageId} Outcome={Outcome}",
                terminal.MessageId,
                terminal.Status);
            return PendingCommandResolutionResult.LifecycleDispatchFailed(terminal);
        }

        return PendingCommandResolutionResult.Resolved(terminal);
    }

    /// <inheritdoc />
    public PendingCommandEntry? GetByMessageId(string messageId) {
        // P14 — validate at the boundary; the dictionary throws on null but returns silently on
        // empty/whitespace which previously hid bugs.
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (!TryNormalizeMessageId(messageId, out string? canonical, out _)) {
            return null;
        }

        lock (_gate) {
            if (_disposed) {
                return null;
            }

            return _byMessageId.TryGetValue(canonical, out PendingCommandEntry? entry) ? entry : null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PendingCommandEntry> Snapshot() {
        lock (_gate) {
            if (_disposed) {
                return [];
            }

            return [.. _byMessageId.Values.OrderBy(static e => e.SubmittedAt)];
        }
    }

    /// <inheritdoc />
    public void Clear(string reason) {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        List<PendingCommandEntry> outstanding;
        lock (_gate) {
            // P2 — explicit guard so racing Clear-after-Dispose does not surprise tests with a
            // mid-disposal flush.
            if (_disposed) {
                return;
            }

            outstanding = [.. _byMessageId.Values.Where(static e => e.Status == PendingCommandStatus.Pending)];
            _byMessageId.Clear();
            _insertionOrder.Clear();
        }

        // Dispatch lifecycle transitions OUTSIDE the gate to avoid deadlocking with subscribers
        // who synchronously call back into this service.
        foreach (PendingCommandEntry entry in outstanding) {
            if (!DispatchNeedsReviewLifecycle(entry, "Clear")) {
                break;
            }
        }

        _logger.LogInformation(
            "Pending command state cleared. Reason={Reason} OutstandingPendingCount={Count}",
            reason,
            outstanding.Count);
    }

    /// <inheritdoc />
    public void Dispose() {
        List<PendingCommandEntry> outstanding;
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            // P3 — outstanding pending commands cannot stay invisible after dispose; transition
            // each one to NeedsReview so any UI observer sees the unresolved tail.
            outstanding = [.. _byMessageId.Values.Where(static e => e.Status == PendingCommandStatus.Pending)];
            _byMessageId.Clear();
            _insertionOrder.Clear();
        }

        foreach (PendingCommandEntry entry in outstanding) {
            if (!DispatchNeedsReviewLifecycle(entry, "Dispose")) {
                break;
            }
        }
    }

    private List<PendingCommandEntry> DrainEvictionsLocked() {
        // The cap applies to PENDING entries only. Terminal entries (Confirmed / Rejected /
        // IdempotentConfirmed / NeedsReview) are immutable history and must remain visible to
        // Snapshot/FcPendingCommandSummary even after eviction (P3).
        int pendingCount = CountPendingLocked();
        List<PendingCommandEntry> evictedQueue = [];
        if (pendingCount <= _options.MaxPendingCommandEntries) {
            return evictedQueue;
        }

        // P4 — drain every excess entry, not just the first one.
        while (pendingCount > _options.MaxPendingCommandEntries
            && _insertionOrder.TryDequeue(out string? oldestMessageId)) {
            if (!_byMessageId.TryGetValue(oldestMessageId, out PendingCommandEntry? oldest)) {
                continue;
            }

            if (oldest.Status != PendingCommandStatus.Pending) {
                // Already terminal; not occupying a pending slot.
                continue;
            }

            PendingCommandEntry evicted = oldest with {
                Status = PendingCommandStatus.NeedsReview,
                TerminalAt = _time.GetUtcNow(),
            };

            // P3 — re-insert the evicted record as terminal so Snapshot()/FcPendingCommandSummary
            // surfaces the unresolved tail; lifecycle dispatch happens after the caller exits the
            // gate (P2-P4 — synchronous on the calling thread, no off-thread queue).
            _byMessageId[evicted.MessageId] = evicted;
            evictedQueue.Add(evicted);
            pendingCount--;
            _logger.LogWarning(
                "Pending command evicted unresolved because MaxPendingCommandEntries was exceeded. MessageId={MessageId}",
                evicted.MessageId);
        }

        return evictedQueue;
    }

    private int CountPendingLocked() {
        int count = 0;
        foreach (PendingCommandEntry entry in _byMessageId.Values) {
            if (entry.Status == PendingCommandStatus.Pending) {
                count++;
            }
        }

        return count;
    }

    private void DispatchEvictedLifecycle(IReadOnlyList<PendingCommandEntry> evicted) {
        foreach (PendingCommandEntry entry in evicted) {
            // P2-P5 — re-check the entry's current status before transitioning. A concurrent
            // ResolveTerminal may have moved this MessageId to Confirmed in the gap between the
            // gate-protected drain and this dispatch; in that case the transition would dispatch
            // Rejected over an already-Confirmed lifecycle.
            PendingCommandEntry? current;
            lock (_gate) {
                if (_disposed) {
                    return;
                }

                current = _byMessageId.TryGetValue(entry.MessageId, out PendingCommandEntry? c) ? c : null;
            }

            if (current is null || current.Status != PendingCommandStatus.NeedsReview) {
                _logger.LogDebug(
                    "Skipping evicted lifecycle dispatch because the entry is no longer in NeedsReview state. MessageId={MessageId} CurrentStatus={CurrentStatus}",
                    entry.MessageId,
                    current?.Status);
                continue;
            }

            if (!DispatchNeedsReviewLifecycle(entry, "Evicted")) {
                // Lifecycle service is disposed or unrecoverable; do not iterate further.
                return;
            }
        }
    }

    private bool DispatchNeedsReviewLifecycle(PendingCommandEntry entry, string reason) {
        try {
            // The lifecycle service treats Rejected as a terminal-only state; NeedsReview is
            // surfaced as Rejected to the lifecycle wrapper so the UI does not stay locked in
            // Acknowledged/Syncing forever. The pending-command summary still shows the explicit
            // NeedsReview status from PendingCommandStatus. The 3-arg overload forwards to the
            // 4-arg with idempotencyResolved=false; an explicit "evicted" reason flag is a
            // follow-up extension to the lifecycle API (P2-P18 is deferred — see deferred-work).
            _lifecycle.Transition(entry.CorrelationId, CommandLifecycleState.Rejected, entry.MessageId);
            return true;
        }
        catch (ObjectDisposedException) {
            // P2-P9 — the lifecycle service may have been disposed first during circuit teardown;
            // the rest of the iteration would only repeat the same warning per entry.
            _logger.LogDebug(
                "Pending command lifecycle dispatch skipped because LifecycleStateService is disposed. Reason={Reason}",
                reason);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            _logger.LogWarning(
                ex,
                "Pending command lifecycle dispatch failed during {Reason}. MessageId={MessageId}",
                reason,
                entry.MessageId);
            return true;
        }
    }

    private void EnforceScopeBoundary() {
        if (_userContext is null) {
            return;
        }

        bool needsClear;
        lock (_gate) {
            // P2-P8 — read tenant/user inside the lock so a concurrent transition cannot mutate
            // the values between the read and the snapshot comparison.
            (string? Tenant, string? User) current = (_userContext.TenantId, _userContext.UserId);

            // P2-P7 — fail-closed on missing tenant/user. (null, null) must NEVER be cached as a
            // baseline; otherwise the first real (tenant, user) value looks like a "transition"
            // and flushes legitimate pending state. memory:feedback_tenant_isolation_fail_closed.
            bool currentIsValid = !string.IsNullOrWhiteSpace(current.Tenant)
                && !string.IsNullOrWhiteSpace(current.User);
            if (!currentIsValid) {
                // If we previously held a valid scope, this is a transition out — flush.
                needsClear = _scopeSnapshot is not null;
                _scopeSnapshot = null;
            }
            else if (_scopeSnapshot is null) {
                _scopeSnapshot = current;
                return;
            }
            else {
                needsClear = !ScopeMatches(_scopeSnapshot.Value, current);
                if (needsClear) {
                    _scopeSnapshot = current;
                }
            }
        }

        if (needsClear) {
            // The gate is released before Clear() reacquires it; this preserves the rule that
            // lifecycle dispatch happens off the lock. Tenant/user transition is rare in the
            // scoped circuit but must fail-closed when it does occur.
            _logger.LogWarning("Pending command tenant/user transition detected; flushing pending state.");
            Clear("TenantOrUserTransition");
        }
    }

    private static bool ScopeMatches((string? Tenant, string? User) a, (string? Tenant, string? User) b)
        => string.Equals(a.Tenant, b.Tenant, StringComparison.Ordinal)
            && string.Equals(a.User, b.User, StringComparison.Ordinal);

    /// <summary>P2-P6 — must be invoked while holding <see cref="_gate"/>; the queue rebuild and the terminal-status write must be in the same critical section.</summary>
    private void PurgeFromInsertionOrderLocked(string messageId) {
        // The Queue<string> does not support O(1) removal; rebuild on demand. Cost is bounded by
        // MaxPendingCommandEntries and only paid on terminal resolution.
        if (_insertionOrder.Count == 0) {
            return;
        }

        int original = _insertionOrder.Count;
        for (int i = 0; i < original; i++) {
            if (!_insertionOrder.TryDequeue(out string? candidate)) {
                break;
            }

            if (string.Equals(candidate, messageId, StringComparison.Ordinal)) {
                continue;
            }

            _insertionOrder.Enqueue(candidate);
        }
    }

    private static PendingCommandStatus MapStatus(PendingCommandTerminalOutcome outcome) =>
        outcome switch {
            PendingCommandTerminalOutcome.Confirmed => PendingCommandStatus.Confirmed,
            PendingCommandTerminalOutcome.Rejected => PendingCommandStatus.Rejected,
            PendingCommandTerminalOutcome.IdempotentConfirmed => PendingCommandStatus.IdempotentConfirmed,
            PendingCommandTerminalOutcome.NeedsReview => PendingCommandStatus.NeedsReview,
            _ => PendingCommandStatus.NeedsReview,
        };

    /// <summary>
    /// DN7 — accept the entire 32-symbol Crockford alphabet, including the lower-case range. The
    /// canonical form stored in <c>_byMessageId</c> is uppercase so duplicate observations under
    /// either casing collapse to the same entry.
    /// </summary>
    private static bool TryNormalizeMessageId(
        [NotNullWhen(true)] string? messageId,
        [NotNullWhen(true)] out string? canonical,
        [NotNullWhen(false)] out string? reason) {
        if (string.IsNullOrWhiteSpace(messageId)) {
            canonical = null;
            reason = "empty";
            return false;
        }

        if (messageId.Length != 26) {
            canonical = null;
            reason = "invalid-length";
            return false;
        }

        Span<char> upper = stackalloc char[26];
        for (int i = 0; i < 26; i++) {
            char c = messageId[i];
            char normalized = c switch {
                >= 'a' and <= 'z' => (char)(c - 32),
                _ => c,
            };

            bool valid = (normalized >= '0' && normalized <= '9')
                || (normalized >= 'A' && normalized <= 'H')
                || (normalized >= 'J' && normalized <= 'K')
                || (normalized >= 'M' && normalized <= 'N')
                || (normalized >= 'P' && normalized <= 'T')
                || (normalized >= 'V' && normalized <= 'Z');

            if (!valid) {
                canonical = null;
                reason = "invalid-character";
                return false;
            }

            upper[i] = normalized;
        }

        canonical = new string(upper);
        reason = null;
        return true;
    }
}
