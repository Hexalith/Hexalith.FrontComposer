using System.Diagnostics.CodeAnalysis;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Lifecycle;

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
    private readonly TimeProvider _time;
    private readonly ILogger<PendingCommandStateService> _logger;
    private bool _disposed;

    public PendingCommandStateService(
        IOptions<FcShellOptions> options,
        ILifecycleStateService lifecycle,
        TimeProvider? time = null,
        ILogger<PendingCommandStateService>? logger = null) {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
        _time = time ?? TimeProvider.System;
        _logger = logger ?? NullLogger<PendingCommandStateService>.Instance;
    }

    /// <inheritdoc />
    public PendingCommandRegistrationResult Register(PendingCommandRegistration registration) {
        ArgumentNullException.ThrowIfNull(registration);

        if (!TryValidateMessageId(registration.MessageId, out string? reason)) {
            _logger.LogWarning("Pending command registration rejected. Reason={Reason}", reason);
            return PendingCommandRegistrationResult.InvalidMessageId();
        }

        lock (_gate) {
            if (_disposed) {
                return PendingCommandRegistrationResult.Disposed();
            }

            if (_byMessageId.TryGetValue(registration.MessageId, out PendingCommandEntry? existing)) {
                if (existing.HasSameFrameworkMetadata(registration)) {
                    return PendingCommandRegistrationResult.Merged(existing);
                }

                _logger.LogWarning(
                    "Pending command duplicate registration rejected because framework metadata conflicts. MessageId={MessageId}",
                    registration.MessageId);
                return PendingCommandRegistrationResult.ConflictingMetadata(existing);
            }

            PendingCommandEntry entry = new(
                CorrelationId: registration.CorrelationId,
                MessageId: registration.MessageId,
                CommandTypeName: registration.CommandTypeName,
                ProjectionTypeName: registration.ProjectionTypeName,
                LaneKey: registration.LaneKey,
                EntityKey: registration.EntityKey,
                ExpectedStatusSlot: registration.ExpectedStatusSlot,
                PriorStatusSlot: registration.PriorStatusSlot,
                SubmittedAt: registration.SubmittedAt ?? _time.GetUtcNow(),
                Status: PendingCommandStatus.Pending);

            _byMessageId.Add(entry.MessageId, entry);
            _insertionOrder.Enqueue(entry.MessageId);

            PendingCommandEntry? evicted = EvictIfNeeded();
            return PendingCommandRegistrationResult.Registered(entry, evicted);
        }
    }

    /// <inheritdoc />
    public PendingCommandResolutionResult ResolveTerminal(PendingCommandTerminalObservation observation) {
        ArgumentNullException.ThrowIfNull(observation);

        if (!TryValidateMessageId(observation.MessageId, out string? reason)) {
            _logger.LogWarning("Pending command terminal observation rejected. Reason={Reason}", reason);
            return PendingCommandResolutionResult.InvalidMessageId();
        }

        PendingCommandEntry terminal;
        bool duplicate;

        lock (_gate) {
            if (_disposed) {
                return PendingCommandResolutionResult.Disposed();
            }

            if (!_byMessageId.TryGetValue(observation.MessageId, out PendingCommandEntry? entry)) {
                _logger.LogDebug(
                    "Pending command terminal observation ignored for unknown MessageId. MessageId={MessageId}",
                    observation.MessageId);
                return PendingCommandResolutionResult.UnknownMessageId();
            }

            if (entry.Status != PendingCommandStatus.Pending) {
                terminal = entry with {
                    DuplicateTerminalObservations = entry.DuplicateTerminalObservations + 1,
                };
                _byMessageId[observation.MessageId] = terminal;
                duplicate = true;
            }
            else {
                terminal = entry with {
                    Status = MapStatus(observation.Outcome),
                    RejectionTitle = observation.RejectionTitle,
                    RejectionDetail = observation.RejectionDetail,
                    TerminalAt = _time.GetUtcNow(),
                };
                _byMessageId[observation.MessageId] = terminal;
                duplicate = false;
            }
        }

        if (duplicate) {
            return PendingCommandResolutionResult.DuplicateIgnored(terminal);
        }

        try {
            CommandLifecycleState lifecycleState = terminal.Status == PendingCommandStatus.Rejected
                ? CommandLifecycleState.Rejected
                : CommandLifecycleState.Confirmed;
            _lifecycle.Transition(terminal.CorrelationId, lifecycleState, terminal.MessageId);
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
        lock (_gate) {
            return _byMessageId.TryGetValue(messageId, out PendingCommandEntry? entry) ? entry : null;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<PendingCommandEntry> Snapshot() {
        lock (_gate) {
            return [.. _byMessageId.Values.OrderBy(static e => e.SubmittedAt)];
        }
    }

    /// <inheritdoc />
    public void Clear(string reason) {
        lock (_gate) {
            _byMessageId.Clear();
            _insertionOrder.Clear();
        }

        _logger.LogInformation("Pending command state cleared. Reason={Reason}", reason);
    }

    /// <inheritdoc />
    public void Dispose() {
        lock (_gate) {
            if (_disposed) {
                return;
            }

            _disposed = true;
            _byMessageId.Clear();
            _insertionOrder.Clear();
        }
    }

    private PendingCommandEntry? EvictIfNeeded() {
        if (_byMessageId.Count <= _options.MaxPendingCommandEntries) {
            return null;
        }

        while (_byMessageId.Count > _options.MaxPendingCommandEntries
            && _insertionOrder.TryDequeue(out string? oldestMessageId)) {
            if (!_byMessageId.Remove(oldestMessageId, out PendingCommandEntry? oldest)) {
                continue;
            }

            PendingCommandEntry evicted = oldest with {
                Status = PendingCommandStatus.NeedsReview,
                TerminalAt = _time.GetUtcNow(),
            };
            _logger.LogWarning(
                "Pending command evicted unresolved because MaxPendingCommandEntries was exceeded. MessageId={MessageId}",
                evicted.MessageId);
            return evicted;
        }

        return null;
    }

    private static PendingCommandStatus MapStatus(PendingCommandTerminalOutcome outcome) =>
        outcome switch {
            PendingCommandTerminalOutcome.Confirmed => PendingCommandStatus.Confirmed,
            PendingCommandTerminalOutcome.Rejected => PendingCommandStatus.Rejected,
            PendingCommandTerminalOutcome.IdempotentConfirmed => PendingCommandStatus.IdempotentConfirmed,
            PendingCommandTerminalOutcome.NeedsReview => PendingCommandStatus.NeedsReview,
            _ => PendingCommandStatus.NeedsReview,
        };

    private static bool TryValidateMessageId(
        [NotNullWhen(true)] string? messageId,
        [NotNullWhen(false)] out string? reason) {
        if (string.IsNullOrWhiteSpace(messageId)) {
            reason = "empty";
            return false;
        }

        if (messageId.Length != 26) {
            reason = "invalid-length";
            return false;
        }

        foreach (char c in messageId) {
            bool valid = (c >= '0' && c <= '9')
                || (c >= 'A' && c <= 'H')
                || (c >= 'J' && c <= 'K')
                || (c >= 'M' && c <= 'N')
                || (c >= 'P' && c <= 'T')
                || (c >= 'V' && c <= 'Z');

            if (!valid) {
                reason = "invalid-character";
                return false;
            }
        }

        reason = null;
        return true;
    }
}
