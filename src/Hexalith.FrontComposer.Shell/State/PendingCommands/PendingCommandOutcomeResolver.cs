using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

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
            FrontComposerHotPathLog.PendingOutcomeMissingIdentity(
                _logger,
                observation.Source,
                observation.Outcome);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
        }

        if (string.IsNullOrWhiteSpace(observation.ProjectionTypeName)
            || string.IsNullOrWhiteSpace(observation.LaneKey)) {
            FrontComposerHotPathLog.PendingOutcomeFallbackIdentityIncomplete(
                _logger,
                observation.Source);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
        }

        PendingCommandEntry[] matches = [.. _pendingCommands.Snapshot()
            .Where(entry => entry.Status == PendingCommandStatus.Pending)
            .Where(entry => Matches(entry, observation))];

        if (matches.Length == 0) {
            FrontComposerHotPathLog.PendingOutcomeNoMatch(
                _logger,
                observation.Source);
            return new PendingCommandOutcomeResolutionResult(PendingCommandOutcomeResolutionStatus.Unknown);
        }

        if (matches.Length > 1) {
            FrontComposerHotPathLog.PendingOutcomeAmbiguous(
                _logger,
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
            FrontComposerHotPathLog.NewItemMetadataIncomplete(
                _logger,
                entry.MessageId);
            return;
        }

        _newItemIndicators.Add(new NewItemIndicatorEntry(
            entry.LaneKey,
            entry.EntityKey,
            entry.MessageId,
            // Treat a default/MinValue observation timestamp as absent: a non-nullable EventStore
            // Timestamp DTO field that deserializes to default(DateTimeOffset) would otherwise be
            // trusted as a real stamp and always sort first in Snapshot's OrderBy(CreatedAt).
            observation.ObservedAt is { } observedAt && observedAt > DateTimeOffset.MinValue
                ? observedAt
                : _timeProvider.GetUtcNow()));
    }

    private static bool IsConfirmedOutcome(PendingCommandTerminalOutcome outcome) =>
        outcome is PendingCommandTerminalOutcome.Confirmed or PendingCommandTerminalOutcome.IdempotentConfirmed;
}
