namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Result code for command registration.</summary>
public enum PendingCommandRegistrationStatus {
    Registered,
    Merged,
    /// <summary>P17 — second registration observed after the entry already reached a terminal outcome.</summary>
    MergedTerminal,
    InvalidCorrelationId,
    InvalidMessageId,
    ConflictingMetadata,
    Disposed,
}
