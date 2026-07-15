namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Registration result including any unresolved entry evicted by a bounded cap.</summary>
public sealed record PendingCommandRegistrationResult(
    PendingCommandRegistrationStatus Status,
    PendingCommandEntry? Entry = null,
    PendingCommandEntry? EvictedEntry = null) {
    public static PendingCommandRegistrationResult Registered(PendingCommandEntry entry, PendingCommandEntry? evicted = null) =>
        new(PendingCommandRegistrationStatus.Registered, entry, evicted);

    public static PendingCommandRegistrationResult Merged(PendingCommandEntry entry) =>
        new(PendingCommandRegistrationStatus.Merged, entry);

    /// <summary>P17 — the existing entry is already terminal; callers should skip duplicate acknowledged dispatch.</summary>
    public static PendingCommandRegistrationResult MergedTerminal(PendingCommandEntry entry) =>
        new(PendingCommandRegistrationStatus.MergedTerminal, entry);

    public static PendingCommandRegistrationResult InvalidCorrelationId() =>
        new(PendingCommandRegistrationStatus.InvalidCorrelationId);

    public static PendingCommandRegistrationResult InvalidMessageId() =>
        new(PendingCommandRegistrationStatus.InvalidMessageId);

    public static PendingCommandRegistrationResult ConflictingMetadata(PendingCommandEntry entry) =>
        new(PendingCommandRegistrationStatus.ConflictingMetadata, entry);

    public static PendingCommandRegistrationResult Disposed() =>
        new(PendingCommandRegistrationStatus.Disposed);
}
