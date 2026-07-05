using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Delivery path that observed a terminal pending-command outcome.</summary>
public enum PendingCommandOutcomeSource {
    LiveNudgeRefresh,
    ReconnectReconciliation,
    FallbackPolling,
    IdempotencyStatusQuery,
}

/// <summary>Shared resolver status across live, reconnect, polling, and status-query inputs.</summary>
public enum PendingCommandOutcomeResolutionStatus {
    Resolved,
    DuplicateIgnored,
    Unknown,
    InvalidMessageId,
    AmbiguousMatch,
    LifecycleDispatchFailed,
}

/// <summary>
/// Transport-neutral terminal outcome metadata. Raw command payloads and form values must never be
/// used to populate this record.
/// </summary>
public sealed record PendingCommandOutcomeObservation(
    PendingCommandOutcomeSource Source,
    PendingCommandTerminalOutcome Outcome,
    string? MessageId = null,
    string? ProjectionTypeName = null,
    string? LaneKey = null,
    string? EntityKey = null,
    string? ExpectedStatusSlot = null,
    string? RejectionTitle = null,
    string? RejectionDetail = null,
    string? RejectionDataImpact = null,
    DateTimeOffset? ObservedAt = null);

/// <summary>Result returned by the shared pending-command outcome resolver.</summary>
public sealed record PendingCommandOutcomeResolutionResult(
    PendingCommandOutcomeResolutionStatus Status,
    PendingCommandEntry? Entry = null) {
    public static PendingCommandOutcomeResolutionResult From(PendingCommandResolutionResult result) {
        ArgumentNullException.ThrowIfNull(result);

        return result.Status switch {
            PendingCommandResolutionStatus.Resolved => new(PendingCommandOutcomeResolutionStatus.Resolved, result.Entry),
            PendingCommandResolutionStatus.DuplicateIgnored => new(PendingCommandOutcomeResolutionStatus.DuplicateIgnored, result.Entry),
            PendingCommandResolutionStatus.InvalidMessageId => new(PendingCommandOutcomeResolutionStatus.InvalidMessageId),
            PendingCommandResolutionStatus.LifecycleDispatchFailed => new(PendingCommandOutcomeResolutionStatus.LifecycleDispatchFailed, result.Entry),
            _ => new(PendingCommandOutcomeResolutionStatus.Unknown),
        };
    }
}

/// <summary>Shared pending-command outcome resolver used by nudge, reconnect, polling, and status-query paths.</summary>
public interface IPendingCommandOutcomeResolver {
    PendingCommandOutcomeResolutionResult Resolve(PendingCommandOutcomeObservation observation);
}

/// <inheritdoc />
public sealed class PendingCommandOutcomeResolver : IPendingCommandOutcomeResolver {
    private readonly IPendingCommandStateService _pendingCommands;
    private readonly INewItemIndicatorStateService? _newItemIndicators;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PendingCommandOutcomeResolver> _logger;

    public PendingCommandOutcomeResolver(
        IPendingCommandStateService pendingCommands,
        ILogger<PendingCommandOutcomeResolver>? logger = null,
        INewItemIndicatorStateService? newItemIndicators = null,
        TimeProvider? timeProvider = null) {
        _pendingCommands = pendingCommands ?? throw new ArgumentNullException(nameof(pendingCommands));
        _newItemIndicators = newItemIndicators;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<PendingCommandOutcomeResolver>.Instance;
    }

    /// <inheritdoc />
    public PendingCommandOutcomeResolutionResult Resolve(PendingCommandOutcomeObservation observation) {
        ArgumentNullException.ThrowIfNull(observation);

        if (!string.IsNullOrWhiteSpace(observation.MessageId)) {
            PendingCommandOutcomeResolutionResult result = PendingCommandOutcomeResolutionResult.From(_pendingCommands.ResolveTerminal(ToTerminalObservation(
                    observation,
                    observation.MessageId)));
            PublishNewItemIndicatorIfEligible(observation, result);
            return result;
        }

        // P2-P10 — both anchors absent means the upstream payload is structurally broken; warn so
        // the silent drop is observable in diagnostics. EntityKey alone is not sufficient
        // identification (Matches enforces that), but losing this without trace is the dangerous part.
        if (string.IsNullOrWhiteSpace(observation.EntityKey)) {
            _logger.LogWarning(
                "Pending command outcome dropped because both MessageId and EntityKey were absent. Source={Source} Outcome={Outcome}",
                observation.Source,
                observation.Outcome);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
        }

        if (string.IsNullOrWhiteSpace(observation.ProjectionTypeName)
            || string.IsNullOrWhiteSpace(observation.LaneKey)) {
            _logger.LogDebug(
                "Pending command outcome ignored because fallback row identity metadata was incomplete. Source={Source}",
                observation.Source);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
        }

        PendingCommandEntry[] matches = [.. _pendingCommands.Snapshot()
            .Where(entry => entry.Status == PendingCommandStatus.Pending)
            .Where(entry => Matches(entry, observation))];

        if (matches.Length == 0) {
            _logger.LogDebug(
                "Pending command outcome ignored because no framework-controlled match was found. Source={Source}",
                observation.Source);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
        }

        if (matches.Length > 1) {
            _logger.LogWarning(
                "Pending command outcome left unresolved because framework-controlled matching was ambiguous. Source={Source} CandidateCount={CandidateCount}",
                observation.Source,
                matches.Length);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.AmbiguousMatch);
        }

        PendingCommandOutcomeResolutionResult resolved = PendingCommandOutcomeResolutionResult.From(_pendingCommands.ResolveTerminal(ToTerminalObservation(
                observation,
                matches[0].MessageId)));
        PublishNewItemIndicatorIfEligible(observation, resolved);
        return resolved;
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

    private static bool Matches(PendingCommandEntry entry, PendingCommandOutcomeObservation observation) {
        if (string.IsNullOrWhiteSpace(observation.EntityKey)) {
            return false;
        }

        return string.Equals(entry.EntityKey, observation.EntityKey, StringComparison.Ordinal)
            && OptionalEquals(entry.ProjectionTypeName, observation.ProjectionTypeName)
            && OptionalEquals(entry.LaneKey, observation.LaneKey)
            && OptionalEquals(entry.ExpectedStatusSlot, observation.ExpectedStatusSlot);
    }

    private static bool OptionalEquals(string? entryValue, string? observationValue) =>
        string.IsNullOrWhiteSpace(observationValue)
        || string.Equals(entryValue, observationValue, StringComparison.Ordinal);

    private void PublishNewItemIndicatorIfEligible(
        PendingCommandOutcomeObservation observation,
        PendingCommandOutcomeResolutionResult result) {
        if (_newItemIndicators is null
            || result is not { Status: PendingCommandOutcomeResolutionStatus.Resolved, Entry: { } entry }
            || !IsConfirmedOutcome(observation.Outcome)) {
            return;
        }

        if (string.IsNullOrWhiteSpace(entry.ProjectionTypeName)
            || string.IsNullOrWhiteSpace(entry.LaneKey)
            || string.IsNullOrWhiteSpace(entry.EntityKey)
            || string.IsNullOrWhiteSpace(entry.MessageId)) {
            _logger.LogDebug(
                "New-item indicator skipped because pending command row metadata is incomplete. MessageId={MessageId}",
                entry.MessageId);
            return;
        }

        _newItemIndicators.Add(new NewItemIndicatorEntry(
            entry.LaneKey,
            entry.EntityKey,
            entry.MessageId,
            observation.ObservedAt ?? _timeProvider.GetUtcNow()));
    }

    private static bool IsConfirmedOutcome(PendingCommandTerminalOutcome outcome) =>
        outcome is PendingCommandTerminalOutcome.Confirmed or PendingCommandTerminalOutcome.IdempotentConfirmed;
}
