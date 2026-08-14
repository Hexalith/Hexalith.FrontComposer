using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <inheritdoc />
public sealed class PendingCommandOutcomeResolver : IPendingCommandOutcomeCoordinator, IDisposable {
    private static readonly TimeSpan MaximumFutureSkew = TimeSpan.FromSeconds(5);
    private readonly object _gate = new();
    private readonly Dictionary<string, PendingCommandOutcomeObservation> _earlyByOwner = new(StringComparer.Ordinal);
    private readonly Queue<string> _earlyOrder = new();
    private readonly HashSet<string> _indicatorDecisions = new(StringComparer.Ordinal);
    private readonly IPendingCommandStateService _pendingCommands;
    private readonly INewItemIndicatorStateService? _newItemIndicators;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PendingCommandOutcomeResolver> _logger;
    private readonly FcShellOptions _options;
    private readonly IPendingCommandOutcomeResolver? _terminalResolver;
    private readonly IUserContextAccessor? _userContext;
    private (string Tenant, string User)? _scopeSnapshot;
    private bool _scopeLost;
    private bool _disposed;

    internal int BufferedObservationCount {
        get {
            lock (_gate) {
                return _earlyByOwner.Count;
            }
        }
    }

    internal int BufferedOrderCount {
        get {
            lock (_gate) {
                return _earlyOrder.Count;
            }
        }
    }

    /// <summary>Initializes the pending-command outcome resolver using baseline dependencies.</summary>
    public PendingCommandOutcomeResolver(
        IPendingCommandStateService pendingCommands,
        ILogger<PendingCommandOutcomeResolver>? logger = null,
        INewItemIndicatorStateService? newItemIndicators = null,
        TimeProvider? timeProvider = null)
        : this(pendingCommands, logger, newItemIndicators, timeProvider, null, null) {
    }

    /// <summary>Initializes the bounded terminal-outcome producer boundary.</summary>
    public PendingCommandOutcomeResolver(
        IPendingCommandStateService pendingCommands,
        ILogger<PendingCommandOutcomeResolver>? logger,
        INewItemIndicatorStateService? newItemIndicators,
        TimeProvider? timeProvider,
        IOptions<FcShellOptions>? options,
        IUserContextAccessor? userContext)
        : this(null, pendingCommands, logger, newItemIndicators, timeProvider, options, userContext, false) {
    }

    internal PendingCommandOutcomeResolver(
        IPendingCommandOutcomeResolver terminalResolver,
        IPendingCommandStateService pendingCommands,
        ILogger<PendingCommandOutcomeResolver>? logger,
        INewItemIndicatorStateService? newItemIndicators,
        TimeProvider? timeProvider,
        IOptions<FcShellOptions>? options,
        IUserContextAccessor? userContext)
        : this(terminalResolver, pendingCommands, logger, newItemIndicators, timeProvider, options, userContext, true) {
    }

    private PendingCommandOutcomeResolver(
        IPendingCommandOutcomeResolver? terminalResolver,
        IPendingCommandStateService pendingCommands,
        ILogger<PendingCommandOutcomeResolver>? logger,
        INewItemIndicatorStateService? newItemIndicators,
        TimeProvider? timeProvider,
        IOptions<FcShellOptions>? options,
        IUserContextAccessor? userContext,
        bool useTerminalResolver) {
        _ = useTerminalResolver;
        _terminalResolver = terminalResolver;
        _pendingCommands = pendingCommands ?? throw new ArgumentNullException(nameof(pendingCommands));
        _newItemIndicators = newItemIndicators;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<PendingCommandOutcomeResolver>.Instance;
        _options = options?.Value ?? new FcShellOptions();
        _userContext = userContext;
    }

    /// <inheritdoc />
    public PendingCommandOutcomeResolutionResult BufferBeforeAccepted(
        string ownerId,
        PendingCommandOutcomeObservation observation) {
        ArgumentNullException.ThrowIfNull(observation);

        lock (_gate) {
            if (_disposed) {
                return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
            }

            EnforceScopeBoundaryLocked();
            if (!TryCanonicalUlid(observation.MessageId, out string? canonicalMessageId)) {
                return new PendingCommandOutcomeResolutionResult(
                    string.IsNullOrWhiteSpace(observation.MessageId)
                        ? PendingCommandOutcomeResolutionStatus.Unknown
                        : PendingCommandOutcomeResolutionStatus.InvalidMessageId);
            }

            if (_scopeLost
                || !TryCanonicalUlid(ownerId, out string? canonicalOwner)) {
                return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
            }

            GetBufferScope(out string? tenant, out string? user);
            string key = BuildBufferKey(canonicalMessageId!, canonicalOwner!, tenant, user);
            if (_earlyByOwner.ContainsKey(key)) {
                return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Buffered);
            }

            int capacity = Math.Max(1, _options.MaxPendingCommandEntries);
            while (_earlyByOwner.Count >= capacity && _earlyOrder.TryDequeue(out string? oldest)) {
                if (_earlyByOwner.Remove(oldest)) {
                    FrontComposerHotPathLog.PendingOutcomeBufferOverflow(_logger);
                    break;
                }
            }

            _earlyByOwner.Add(key, observation with { MessageId = canonicalMessageId });
            _earlyOrder.Enqueue(key);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Buffered);
        }
    }

    /// <inheritdoc />
    public PendingCommandRegistrationResult AssociateAccepted(PendingCommandRegistration registration) {
        ArgumentNullException.ThrowIfNull(registration);

        lock (_gate) {
            if (_disposed) {
                return PendingCommandRegistrationResult.Disposed();
            }

            EnforceScopeBoundaryLocked();
            PendingCommandRegistrationResult result;
            try {
                result = _pendingCommands.Register(registration);
            }
            catch (Exception ex) {
                PendingCommandEntry? committed = TryGetCommittedRegistration(registration);
                if (committed is null) {
                    throw;
                }

                FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                    _logger,
                    "AssociationReconciled",
                    committed.MessageId,
                    ex.GetType().Name);
                result = committed.Status == PendingCommandStatus.Pending
                    ? PendingCommandRegistrationResult.Merged(committed)
                    : PendingCommandRegistrationResult.MergedTerminal(committed);
            }
            bool hasBufferKey = TryBuildCurrentBufferKey(
                registration.MessageId,
                registration.CorrelationId,
                out string? key);

            if (result.Status is PendingCommandRegistrationStatus.InvalidMessageId
                or PendingCommandRegistrationStatus.InvalidCorrelationId
                or PendingCommandRegistrationStatus.ConflictingMetadata
                or PendingCommandRegistrationStatus.Disposed) {
                if (hasBufferKey) {
                    _ = _earlyByOwner.Remove(key!);
                    PurgeEarlyOrderLocked(key!);
                }

                return result;
            }

            if (!hasBufferKey) {
                return result;
            }

            if (_earlyByOwner.Remove(key!, out PendingCommandOutcomeObservation? early)) {
                PurgeEarlyOrderLocked(key!);
                try {
                    PendingCommandOutcomeResolutionResult replay = ApplyObservation(early);
                    if (replay.Entry is { Status: not PendingCommandStatus.Pending } terminal) {
                        return PendingCommandRegistrationResult.MergedTerminal(terminal);
                    }
                }
                catch (Exception ex) {
                    PendingCommandEntry? committed = TryGetCommittedRegistration(registration);
                    if (committed is null) {
                        throw;
                    }

                    FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                        _logger,
                        "AssociationReplayReconciled",
                        committed.MessageId,
                        ex.GetType().Name);
                    return committed.Status == PendingCommandStatus.Pending
                        ? result
                        : PendingCommandRegistrationResult.MergedTerminal(committed);
                }
            }

            return result;
        }
    }

    /// <inheritdoc />
    public PendingCommandOutcomeResolutionResult Resolve(PendingCommandOutcomeObservation observation) {
        ArgumentNullException.ThrowIfNull(observation);

        lock (_gate) {
            if (_disposed) {
                return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
            }

            EnforceScopeBoundaryLocked();
            return ApplyObservation(observation);
        }
    }

    /// <inheritdoc />
    public void DiscardBuffered(string? messageId) {
        if (string.IsNullOrWhiteSpace(messageId)) {
            return;
        }

        lock (_gate) {
            if (!TryCanonicalUlid(messageId, out string? canonicalMessageId)) {
                return;
            }

            string prefix = BuildBufferPrefix(canonicalMessageId!);
            string[] keys = [.. _earlyByOwner.Keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal))];
            foreach (string key in keys) {
                _ = _earlyByOwner.Remove(key);
                PurgeEarlyOrderLocked(key);
            }
        }
    }

    /// <inheritdoc />
    public void DiscardBufferedByOwner(string ownerId) {
        lock (_gate) {
            if (_disposed
                || !TryCanonicalUlid(ownerId, out string? canonicalOwner)) {
                return;
            }

            EnforceScopeBoundaryLocked();
            if (_scopeLost) {
                return;
            }

            GetBufferScope(out string? tenant, out string? user);
            string suffix = string.Concat(
                EncodeBufferSegment(canonicalOwner),
                EncodeBufferSegment(tenant),
                EncodeBufferSegment(user));
            string[] keys = [.. _earlyByOwner.Keys.Where(
                key => key.EndsWith(suffix, StringComparison.Ordinal))];
            foreach (string key in keys) {
                _ = _earlyByOwner.Remove(key);
                PurgeEarlyOrderLocked(key);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose() {
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _earlyByOwner.Clear();
            _earlyOrder.Clear();
            _indicatorDecisions.Clear();
        }
    }

    private PendingCommandOutcomeResolutionResult ApplyObservation(PendingCommandOutcomeObservation observation) {
        if (string.IsNullOrWhiteSpace(observation.MessageId)) {
            FrontComposerHotPathLog.PendingOutcomeMissingIdentity(
                _logger,
                observation.Source,
                observation.Outcome);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
        }

        if (!TryCanonicalUlid(observation.MessageId, out string? canonicalMessageId)) {
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.InvalidMessageId);
        }

        PendingCommandOutcomeObservation canonicalObservation = observation with { MessageId = canonicalMessageId };
        if (_terminalResolver is not null) {
            PendingCommandOutcomeResolutionResult delegated = _terminalResolver.Resolve(canonicalObservation);
            if (delegated.Status is PendingCommandOutcomeResolutionStatus.Resolved
                    or PendingCommandOutcomeResolutionStatus.LifecycleDispatchFailed
                && delegated.Entry is null) {
                return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
            }

            if (delegated.Entry is { } delegatedEntry
                && (!TryCanonicalUlid(delegatedEntry.MessageId, out string? delegatedMessageId)
                    || !string.Equals(delegatedMessageId, canonicalMessageId, StringComparison.Ordinal))) {
                return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
            }

            try {
                DecideNewItemIndicator(canonicalObservation, delegated);
            }
            catch (Exception ex) {
                FrontComposerHotPathLog.PendingOutcomePublicationFailed(_logger, ex.GetType().Name);
            }

            return delegated;
        }

        PendingCommandOutcomeResolutionResult result = PendingCommandOutcomeResolutionResult.From(
            _pendingCommands.ResolveTerminal(ToTerminalObservation(canonicalObservation, canonicalMessageId!)));
        try {
            DecideNewItemIndicator(canonicalObservation, result);
        }
        catch (Exception ex) {
            FrontComposerHotPathLog.PendingOutcomePublicationFailed(_logger, ex.GetType().Name);
        }

        return result;
    }

    private static PendingCommandTerminalObservation ToTerminalObservation(
        PendingCommandOutcomeObservation observation,
        string messageId) =>
        new(
            messageId,
            observation.Outcome,
            observation.RejectionTitle,
            observation.RejectionDetail,
            observation.RejectionDataImpact);

    private void DecideNewItemIndicator(
        PendingCommandOutcomeObservation observation,
        PendingCommandOutcomeResolutionResult result) {
        if (result is not {
            Status: PendingCommandOutcomeResolutionStatus.Resolved
                    or PendingCommandOutcomeResolutionStatus.LifecycleDispatchFailed,
            Entry: { } entry,
        }
            || !_indicatorDecisions.Add(entry.MessageId)
            || _newItemIndicators is null
            || entry.TargetSnapshot is not { } target
            || observation.Materiality != CommandMateriality.Material
            || !IsConfirmedOutcome(observation.Outcome)
            || target.ChangeKind == CommandTargetChangeKind.Delete
            || target.ChangeKind == CommandTargetChangeKind.StatusMove && string.IsNullOrWhiteSpace(target.ExpectedStatus)
            || !ScopeMatches(target)) {
            return;
        }

        if (!TryResolveObservedAt(target, observation.ObservedAt, out DateTimeOffset observedAt)) {
            FrontComposerHotPathLog.PendingOutcomeTimestampRejected(_logger);
            return;
        }

        try {
            _newItemIndicators.Add(new NewItemIndicatorEntry(
                target.ViewKey,
                target.EntityKey,
                entry.MessageId,
                observedAt));
        }
        catch (Exception ex) {
            FrontComposerHotPathLog.PendingOutcomePublicationFailed(_logger, ex.GetType().Name);
        }
    }

    private bool TryResolveObservedAt(
        CommandTargetSnapshot target,
        DateTimeOffset? candidate,
        out DateTimeOffset observedAt) {
        DateTimeOffset now;
        try {
            now = _timeProvider.GetUtcNow();
        }
        catch (Exception ex) {
            observedAt = default;
            FrontComposerHotPathLog.PendingOutcomePublicationFailed(_logger, ex.GetType().Name);
            return false;
        }

        observedAt = candidate is { } supplied && supplied > DateTimeOffset.MinValue ? supplied : now;
        if (observedAt > now) {
            if (observedAt - now > MaximumFutureSkew) {
                return false;
            }

            observedAt = now;
        }

        TimeSpan maximumAge = TimeSpan.FromMilliseconds(_options.MaxPendingCommandPollingDurationMs);
        return observedAt >= target.CapturedAt
            && observedAt - target.CapturedAt <= maximumAge
            && now - target.CapturedAt <= maximumAge
            && now - observedAt <= maximumAge;
    }

    private bool ScopeMatches(CommandTargetSnapshot target) {
        if (!TryGetCurrentScope(out string tenant, out string user)) {
            return false;
        }

        return string.Equals(target.TenantId, tenant, StringComparison.Ordinal)
            && string.Equals(target.UserId, user, StringComparison.Ordinal);
    }

    private void EnforceScopeBoundaryLocked() {
        if (!TryGetCurrentScope(out string tenant, out string user)) {
            if (_scopeSnapshot is not null) {
                _scopeSnapshot = null;
                _scopeLost = true;
                _earlyByOwner.Clear();
                _earlyOrder.Clear();
                _indicatorDecisions.Clear();
            }

            return;
        }

        (string Tenant, string User) current = (tenant, user);
        if ((_scopeSnapshot is null && _earlyByOwner.Count > 0)
            || (_scopeSnapshot is not null
                && (!string.Equals(_scopeSnapshot.Value.Tenant, current.Tenant, StringComparison.Ordinal)
                    || !string.Equals(_scopeSnapshot.Value.User, current.User, StringComparison.Ordinal)))) {
            _earlyByOwner.Clear();
            _earlyOrder.Clear();
            _indicatorDecisions.Clear();
        }

        _scopeSnapshot = current;
        _scopeLost = false;
    }

    private bool TryGetCurrentScope(out string tenant, out string user) {
        tenant = string.Empty;
        user = string.Empty;
        try {
            string? candidateTenant = _userContext?.TenantId;
            string? candidateUser = _userContext?.UserId;
            if (string.IsNullOrWhiteSpace(candidateTenant) || string.IsNullOrWhiteSpace(candidateUser)) {
                return false;
            }

            tenant = candidateTenant.Trim();
            user = candidateUser.Trim();
            return true;
        }
        catch (Exception) {
            return false;
        }
    }

    private static string BuildBufferKey(
        string messageId,
        string ownerId,
        string? tenantId,
        string? userId) =>
        string.Concat(
            BuildBufferPrefix(messageId),
            EncodeBufferSegment(ownerId),
            EncodeBufferSegment(tenantId),
            EncodeBufferSegment(userId));

    private static string BuildBufferPrefix(string messageId) => EncodeBufferSegment(messageId);

    private static string EncodeBufferSegment(string? value) => value is null
        ? "-:"
        : string.Concat(value.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), ":", value);

    private void GetBufferScope(out string? tenant, out string? user) {
        if (TryGetCurrentScope(out string currentTenant, out string currentUser)) {
            tenant = currentTenant;
            user = currentUser;
            return;
        }

        tenant = null;
        user = null;
    }

    private bool TryBuildCurrentBufferKey(
        string? messageId,
        string? ownerId,
        out string? key) {
        if (_scopeLost
            || !TryCanonicalUlid(messageId, out string? canonicalMessageId)
            || !TryCanonicalUlid(ownerId, out string? canonicalOwner)) {
            key = null;
            return false;
        }

        GetBufferScope(out string? tenant, out string? user);
        key = BuildBufferKey(canonicalMessageId!, canonicalOwner!, tenant, user);
        return true;
    }

    private static bool TryCanonicalUlid(string? value, out string? canonical) {
        string? candidate = value?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(candidate)
            || !NUlid.Ulid.TryParse(candidate, out NUlid.Ulid parsed)) {
            canonical = null;
            return false;
        }

        canonical = parsed.ToString();
        return string.Equals(candidate, canonical, StringComparison.Ordinal);
    }

    private void PurgeEarlyOrderLocked(string key) {
        int count = _earlyOrder.Count;
        for (int index = 0; index < count; index++) {
            string candidate = _earlyOrder.Dequeue();
            if (!string.Equals(candidate, key, StringComparison.Ordinal)
                && _earlyByOwner.ContainsKey(candidate)) {
                _earlyOrder.Enqueue(candidate);
            }
        }
    }

    private PendingCommandEntry? TryGetCommittedRegistration(PendingCommandRegistration registration) {
        try {
            PendingCommandEntry? committed = _pendingCommands.GetByMessageId(registration.MessageId);
            return committed is not null && committed.HasSameFrameworkMetadata(registration)
                ? committed
                : null;
        }
        catch (Exception ex) {
            FrontComposerHotPathLog.PendingLifecycleDispatchFailed(
                _logger,
                "AssociationReconciliationFailed",
                registration.MessageId,
                ex.GetType().Name);
            return null;
        }
    }

    private static bool IsConfirmedOutcome(PendingCommandTerminalOutcome outcome) =>
        outcome is PendingCommandTerminalOutcome.Confirmed or PendingCommandTerminalOutcome.IdempotentConfirmed;
}
