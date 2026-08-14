using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Story 3.3 T4: bounded circuit-local pending command index. It records only framework metadata and
/// resolves terminal observations exactly once per ULID MessageId.
/// </summary>
public sealed class PendingCommandStateService : IPendingCommandStateService {
    private readonly object _gate = new();
    private readonly Dictionary<string, PendingCommandEntry> _byMessageId = new(StringComparer.Ordinal);
    private readonly Queue<string> _insertionOrder = new();
    private readonly Dictionary<string, DateTimeOffset> _lifecycleConvergenceDeadlines = new(StringComparer.Ordinal);
    private readonly Queue<string> _lifecycleConvergenceOrder = new();
    private readonly FcShellOptions _options;
    private readonly ILifecycleStateService _lifecycle;
    private readonly IUserContextAccessor? _userContext;
    private readonly TimeProvider _time;
    private readonly ILogger<PendingCommandStateService> _logger;
    private (string? Tenant, string? User)? _scopeSnapshot;
    private bool _disposed;

    public event EventHandler? Changed;

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

        if (!TryNormalizeUlid(registration.MessageId, out string? canonicalMessageId, out string? reason)) {
            FrontComposerHotPathLog.PendingRegistrationMessageRejected(_logger, reason);
            return PendingCommandRegistrationResult.InvalidMessageId();
        }

        if (!TryNormalizeUlid(registration.CorrelationId, out string? canonicalCorrelationId, out reason)) {
            FrontComposerHotPathLog.PendingRegistrationCorrelationRejected(_logger, reason);
            return PendingCommandRegistrationResult.InvalidCorrelationId();
        }

        PendingCommandRegistration normalized = registration with {
            CorrelationId = canonicalCorrelationId,
            MessageId = canonicalMessageId,
        };

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
                    FrontComposerHotPathLog.PendingRegistrationMetadataConflict(
                        _logger,
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
                Status: PendingCommandStatus.Pending) {
                TargetSnapshot = normalized.TargetSnapshot,
            };

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

        NotifyChanged();
        return PendingCommandRegistrationResult.Registered(registered, evicted);
    }

    /// <inheritdoc />
    public PendingCommandResolutionResult ResolveTerminal(PendingCommandTerminalObservation observation) {
        ArgumentNullException.ThrowIfNull(observation);

        if (!TryNormalizeUlid(observation.MessageId, out string? canonicalMessageId, out string? reason)) {
            FrontComposerHotPathLog.PendingTerminalRejected(_logger, reason);
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
                FrontComposerHotPathLog.PendingTerminalUnknown(
                    _logger,
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
            // F27 — explicitly tag the duplicate path's activity with `outcome=duplicate_ignored`
            // so dashboards see a paired (operation, outcome) on every emitted span. Without
            // this, the activity carried only the constructor tags and looked unfinished.
            using Activity? duplicateActivity = FrontComposerTelemetry.StartPendingCommandOutcome(
                "duplicate_ignored",
                terminal.CommandTypeName,
                terminal.MessageId,
                terminal.CorrelationId);
            FrontComposerTelemetry.SetOutcome(duplicateActivity, "duplicate_ignored");
            _ = TryConvergeLifecycle(canonicalMessageId);
            NotifyChanged();
            return PendingCommandResolutionResult.DuplicateIgnored(terminal);
        }

        NotifyChanged();
        using Activity? activity = FrontComposerTelemetry.StartPendingCommandOutcome(
            terminal.Status.ToString(),
            terminal.CommandTypeName,
            terminal.MessageId,
            terminal.CorrelationId);
        if (!TryDispatchTerminalLifecycle(terminal, activity)) {
            EnqueueLifecycleConvergence(terminal);
            return PendingCommandResolutionResult.LifecycleDispatchFailed(terminal);
        }

        FrontComposerTelemetry.SetOutcome(activity, "resolved");
        return PendingCommandResolutionResult.Resolved(terminal);
    }

    /// <inheritdoc />
    public PendingCommandEntry? GetByMessageId(string messageId) {
        // P14 — validate at the boundary; the dictionary throws on null but returns silently on
        // empty/whitespace which previously hid bugs.
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        if (!TryNormalizeUlid(messageId, out string? canonical, out _)) {
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

    internal (int Attempts, int Converged) ConvergeLifecycle(int maximumAttempts) {
        if (maximumAttempts <= 0) {
            return (0, 0);
        }

        List<string> candidates = SnapshotLifecycleConvergenceMessageIds(maximumAttempts);
        int attempts = 0;
        int converged = 0;
        foreach (string messageId in candidates) {
            if (!TryTakeLifecycleConvergence(messageId, out PendingCommandEntry? terminal, out DateTimeOffset deadline)) {
                continue;
            }

            attempts++;
            DateTimeOffset now;
            try {
                now = _time.GetUtcNow();
            }
            catch (Exception ex) {
                FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                    _logger,
                    "ConvergenceClock",
                    terminal.MessageId,
                    ex.GetType().Name);
                RequeueLifecycleConvergence(terminal.MessageId, deadline);
                continue;
            }

            if (now > deadline) {
                FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                    _logger,
                    "ConvergenceExpired",
                    terminal.MessageId,
                    "DeadlineExceeded");
                continue;
            }

            if (TryDispatchTerminalLifecycle(terminal, activity: null)) {
                converged++;
            }
            else {
                RequeueLifecycleConvergence(terminal.MessageId, deadline);
            }
        }

        return (attempts, converged);
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
            _lifecycleConvergenceDeadlines.Clear();
            _lifecycleConvergenceOrder.Clear();
        }

        // Dispatch lifecycle transitions OUTSIDE the gate to avoid deadlocking with subscribers
        // who synchronously call back into this service.
        foreach (PendingCommandEntry entry in outstanding) {
            if (!DispatchNeedsReviewLifecycle(entry, "Clear")) {
                break;
            }
        }

        FrontComposerHotPathLog.PendingStateCleared(
            _logger,
            reason,
            outstanding.Count);
        NotifyChanged();
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
            _lifecycleConvergenceDeadlines.Clear();
            _lifecycleConvergenceOrder.Clear();
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
            FrontComposerHotPathLog.PendingEvictedUnresolved(
                _logger,
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
                FrontComposerHotPathLog.PendingEvictedDispatchSkipped(
                    _logger,
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
            FrontComposerHotPathLog.PendingLifecycleDisposed(
                _logger,
                reason);
            return false;
        }
        catch (Exception ex) when (ex is not OperationCanceledException) {
            FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                _logger,
                reason,
                entry.MessageId,
                ex.GetType().Name);
            return true;
        }
    }

    private void EnqueueLifecycleConvergence(PendingCommandEntry terminal) {
        DateTimeOffset terminalAt = terminal.TerminalAt ?? terminal.SubmittedAt;
        DateTimeOffset deadline = AddMillisecondsSaturating(
            terminalAt,
            _options.MaxPendingCommandPollingDurationMs);
        RequeueLifecycleConvergence(terminal.MessageId, deadline);
    }

    private static DateTimeOffset AddMillisecondsSaturating(DateTimeOffset value, int milliseconds) {
        long deltaTicks = TimeSpan.FromMilliseconds(milliseconds).Ticks;
        long remainingTicks = Math.Min(
            DateTimeOffset.MaxValue.Ticks - value.Ticks,
            DateTimeOffset.MaxValue.UtcTicks - value.UtcTicks);
        return deltaTicks > remainingTicks
            ? DateTimeOffset.MaxValue
            : value.AddTicks(deltaTicks);
    }

    private void RequeueLifecycleConvergence(string messageId, DateTimeOffset deadline) {
        lock (_gate) {
            if (_disposed || _lifecycleConvergenceDeadlines.ContainsKey(messageId)) {
                return;
            }

            int capacity = Math.Max(1, _options.MaxPendingCommandEntries);
            while (_lifecycleConvergenceDeadlines.Count >= capacity
                && _lifecycleConvergenceOrder.TryDequeue(out string? oldest)) {
                if (_lifecycleConvergenceDeadlines.Remove(oldest)) {
                    FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                        _logger,
                        "ConvergenceOverflow",
                        oldest,
                        "CapacityExceeded");
                    break;
                }
            }

            _lifecycleConvergenceDeadlines.Add(messageId, deadline);
            _lifecycleConvergenceOrder.Enqueue(messageId);
        }
    }

    private bool TryConvergeLifecycle(string messageId) {
        if (!TryTakeLifecycleConvergence(messageId, out PendingCommandEntry? terminal, out DateTimeOffset deadline)) {
            return false;
        }

        DateTimeOffset now;
        try {
            now = _time.GetUtcNow();
        }
        catch (Exception ex) {
            FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                _logger,
                "ConvergenceClock",
                messageId,
                ex.GetType().Name);
            RequeueLifecycleConvergence(messageId, deadline);
            return false;
        }

        if (now > deadline) {
            FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                _logger,
                "ConvergenceExpired",
                messageId,
                "DeadlineExceeded");
            return false;
        }

        if (TryDispatchTerminalLifecycle(terminal, activity: null)) {
            return true;
        }

        RequeueLifecycleConvergence(messageId, deadline);
        return false;
    }

    private List<string> SnapshotLifecycleConvergenceMessageIds(int maximumAttempts) {
        lock (_gate) {
            if (_disposed) {
                return [];
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var candidates = new List<string>(Math.Min(maximumAttempts, _lifecycleConvergenceDeadlines.Count));
            foreach (string messageId in _lifecycleConvergenceOrder) {
                if (!_lifecycleConvergenceDeadlines.ContainsKey(messageId) || !seen.Add(messageId)) {
                    continue;
                }

                candidates.Add(messageId);
                if (candidates.Count == maximumAttempts) {
                    break;
                }
            }

            return candidates;
        }
    }

    private bool TryTakeLifecycleConvergence(
        string messageId,
        [NotNullWhen(true)] out PendingCommandEntry? terminal,
        out DateTimeOffset deadline) {
        lock (_gate) {
            if (!_lifecycleConvergenceDeadlines.Remove(messageId, out deadline)
                || !_byMessageId.TryGetValue(messageId, out terminal)
                || terminal.Status == PendingCommandStatus.Pending) {
                terminal = null;
                deadline = default;
                return false;
            }

            PurgeLifecycleConvergenceOrderLocked(messageId);
            return true;
        }
    }

    private bool TryDispatchTerminalLifecycle(PendingCommandEntry terminal, Activity? activity) {
        CommandLifecycleState lifecycleState = terminal.Status is PendingCommandStatus.Rejected or PendingCommandStatus.NeedsReview
            ? CommandLifecycleState.Rejected
            : CommandLifecycleState.Confirmed;
        bool idempotencyResolved = terminal.Status == PendingCommandStatus.IdempotentConfirmed;
        try {
            if (LifecycleMatches(terminal, lifecycleState)) {
                return true;
            }

            _lifecycle.Transition(terminal.CorrelationId, lifecycleState, terminal.MessageId, idempotencyResolved);
            if (LifecycleMatches(terminal, lifecycleState)) {
                return true;
            }

            FrontComposerLog.PendingCommandLifecycleTerminalDispatchFailed(
                _logger,
                terminal.MessageId,
                terminal.Status.ToString(),
                "StateNotConverged");
            return false;
        }
        catch (Exception ex) {
            FrontComposerTelemetry.SetFailure(activity, ex.GetType().Name);
            FrontComposerLog.PendingCommandLifecycleTerminalDispatchFailed(
                _logger,
                terminal.MessageId,
                terminal.Status.ToString(),
                ex.GetType().Name);
            return false;
        }
    }

    private bool LifecycleMatches(PendingCommandEntry terminal, CommandLifecycleState lifecycleState) =>
        _lifecycle.GetState(terminal.CorrelationId) == lifecycleState
        && string.Equals(
            _lifecycle.GetMessageId(terminal.CorrelationId),
            terminal.MessageId,
            StringComparison.Ordinal);

    private void NotifyChanged() {
        EventHandler? changed = Changed;
        if (changed is null) {
            return;
        }

        foreach (EventHandler handler in changed.GetInvocationList().Cast<EventHandler>()) {
            try {
                handler(this, EventArgs.Empty);
            }
            catch (Exception ex) {
                FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                    _logger,
                    "StateNotification",
                    "redacted",
                    ex.GetType().Name);
            }
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
            (string? Tenant, string? User) current;
            try {
                current = (_userContext.TenantId, _userContext.UserId);
            }
            catch (Exception) {
                // Scope access is an FC-NIP eligibility seam, not a transport/lifecycle gate.
                // Treat an unavailable accessor like an unknown scope so an accepted command can
                // still be registered and resolved without carrying a target into publication.
                current = (null, null);
            }

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
            FrontComposerHotPathLog.PendingScopeTransition(_logger);
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

    private void PurgeLifecycleConvergenceOrderLocked(string messageId) {
        int original = _lifecycleConvergenceOrder.Count;
        for (int i = 0; i < original; i++) {
            if (!_lifecycleConvergenceOrder.TryDequeue(out string? candidate)) {
                break;
            }

            if (!string.Equals(candidate, messageId, StringComparison.Ordinal)
                && _lifecycleConvergenceDeadlines.ContainsKey(candidate)) {
                _lifecycleConvergenceOrder.Enqueue(candidate);
            }
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
    /// DN7 — delegate ULID validation to NUlid so Crockford overflow encodings are rejected. The
    /// canonical form stored in <c>_byMessageId</c> collapses equivalent input casing.
    /// </summary>
    private static bool TryNormalizeUlid(
        [NotNullWhen(true)] string? value,
        [NotNullWhen(true)] out string? canonical,
        [NotNullWhen(false)] out string? reason) {
        if (string.IsNullOrWhiteSpace(value)) {
            canonical = null;
            reason = "empty";
            return false;
        }

        string candidate = value.ToUpperInvariant();
        if (!NUlid.Ulid.TryParse(candidate, out NUlid.Ulid parsed)) {
            canonical = null;
            reason = "invalid-ulid";
            return false;
        }

        canonical = parsed.ToString();
        if (!string.Equals(candidate, canonical, StringComparison.Ordinal)) {
            canonical = null;
            reason = "non-canonical-ulid";
            return false;
        }

        reason = null;
        return true;
    }
}
