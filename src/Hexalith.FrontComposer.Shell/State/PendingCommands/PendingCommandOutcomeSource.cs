namespace Hexalith.FrontComposer.Shell.State.PendingCommands;

/// <summary>Delivery path that observed a terminal pending-command outcome.</summary>
public enum PendingCommandOutcomeSource {
    LiveNudgeRefresh,
    ReconnectReconciliation,
    FallbackPolling,
    IdempotencyStatusQuery,
}
