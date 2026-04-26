using Hexalith.FrontComposer.Contracts.Lifecycle;

namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>
/// Registration metadata for an accepted command. This intentionally excludes raw command payloads,
/// form values, tenant IDs, user IDs, and validation messages.
/// </summary>
public sealed record PendingCommandRegistration(
    string CorrelationId,
    string MessageId,
    string CommandTypeName,
    string? ProjectionTypeName = null,
    string? LaneKey = null,
    string? EntityKey = null,
    string? ExpectedStatusSlot = null,
    string? PriorStatusSlot = null,
    DateTimeOffset? SubmittedAt = null);

/// <summary>Runtime state for a pending command entry.</summary>
public enum PendingCommandStatus {
    Pending,
    Confirmed,
    Rejected,
    IdempotentConfirmed,
    NeedsReview,
}

/// <summary>Terminal observation type accepted by the pending-command resolver.</summary>
public enum PendingCommandTerminalOutcome {
    Confirmed,
    Rejected,
    IdempotentConfirmed,
    NeedsReview,
}

/// <summary>Result code for command registration.</summary>
public enum PendingCommandRegistrationStatus {
    Registered,
    Merged,
    InvalidMessageId,
    ConflictingMetadata,
    Disposed,
}

/// <summary>Result code for terminal command resolution.</summary>
public enum PendingCommandResolutionStatus {
    Resolved,
    DuplicateIgnored,
    InvalidMessageId,
    UnknownMessageId,
    LifecycleDispatchFailed,
    Disposed,
}

/// <summary>Immutable snapshot of a circuit-local pending command.</summary>
public sealed record PendingCommandEntry(
    string CorrelationId,
    string MessageId,
    string CommandTypeName,
    string? ProjectionTypeName,
    string? LaneKey,
    string? EntityKey,
    string? ExpectedStatusSlot,
    string? PriorStatusSlot,
    DateTimeOffset SubmittedAt,
    PendingCommandStatus Status,
    string? RejectionTitle = null,
    string? RejectionDetail = null,
    DateTimeOffset? TerminalAt = null,
    int DuplicateTerminalObservations = 0) {
    internal bool HasSameFrameworkMetadata(PendingCommandRegistration registration) =>
        string.Equals(CorrelationId, registration.CorrelationId, StringComparison.Ordinal)
        && string.Equals(CommandTypeName, registration.CommandTypeName, StringComparison.Ordinal)
        && string.Equals(ProjectionTypeName, registration.ProjectionTypeName, StringComparison.Ordinal)
        && string.Equals(LaneKey, registration.LaneKey, StringComparison.Ordinal)
        && string.Equals(EntityKey, registration.EntityKey, StringComparison.Ordinal)
        && string.Equals(ExpectedStatusSlot, registration.ExpectedStatusSlot, StringComparison.Ordinal)
        && string.Equals(PriorStatusSlot, registration.PriorStatusSlot, StringComparison.Ordinal);
}

/// <summary>Registration result including any unresolved entry evicted by a bounded cap.</summary>
public sealed record PendingCommandRegistrationResult(
    PendingCommandRegistrationStatus Status,
    PendingCommandEntry? Entry = null,
    PendingCommandEntry? EvictedEntry = null) {
    public static PendingCommandRegistrationResult Registered(PendingCommandEntry entry, PendingCommandEntry? evicted = null) =>
        new(PendingCommandRegistrationStatus.Registered, entry, evicted);

    public static PendingCommandRegistrationResult Merged(PendingCommandEntry entry) =>
        new(PendingCommandRegistrationStatus.Merged, entry);

    public static PendingCommandRegistrationResult InvalidMessageId() =>
        new(PendingCommandRegistrationStatus.InvalidMessageId);

    public static PendingCommandRegistrationResult ConflictingMetadata(PendingCommandEntry entry) =>
        new(PendingCommandRegistrationStatus.ConflictingMetadata, entry);

    public static PendingCommandRegistrationResult Disposed() =>
        new(PendingCommandRegistrationStatus.Disposed);
}

/// <summary>Terminal observation produced by live nudge refresh, reconnect reconciliation, polling, or status lookup.</summary>
public sealed record PendingCommandTerminalObservation(
    string MessageId,
    PendingCommandTerminalOutcome Outcome,
    string? RejectionTitle = null,
    string? RejectionDetail = null) {
    public static PendingCommandTerminalObservation Confirmed(string messageId) =>
        new(messageId, PendingCommandTerminalOutcome.Confirmed);

    public static PendingCommandTerminalObservation IdempotentConfirmed(string messageId) =>
        new(messageId, PendingCommandTerminalOutcome.IdempotentConfirmed);

    public static PendingCommandTerminalObservation Rejected(string messageId, string title, string detail) =>
        new(messageId, PendingCommandTerminalOutcome.Rejected, title, detail);

    public static PendingCommandTerminalObservation NeedsReview(string messageId) =>
        new(messageId, PendingCommandTerminalOutcome.NeedsReview);
}

/// <summary>Terminal resolution result.</summary>
public sealed record PendingCommandResolutionResult(
    PendingCommandResolutionStatus Status,
    PendingCommandEntry? Entry = null) {
    public static PendingCommandResolutionResult Resolved(PendingCommandEntry entry) =>
        new(PendingCommandResolutionStatus.Resolved, entry);

    public static PendingCommandResolutionResult DuplicateIgnored(PendingCommandEntry entry) =>
        new(PendingCommandResolutionStatus.DuplicateIgnored, entry);

    public static PendingCommandResolutionResult InvalidMessageId() =>
        new(PendingCommandResolutionStatus.InvalidMessageId);

    public static PendingCommandResolutionResult UnknownMessageId() =>
        new(PendingCommandResolutionStatus.UnknownMessageId);

    public static PendingCommandResolutionResult LifecycleDispatchFailed(PendingCommandEntry entry) =>
        new(PendingCommandResolutionStatus.LifecycleDispatchFailed, entry);

    public static PendingCommandResolutionResult Disposed() =>
        new(PendingCommandResolutionStatus.Disposed);
}
