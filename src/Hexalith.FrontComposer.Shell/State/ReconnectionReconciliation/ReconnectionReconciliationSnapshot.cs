namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Immutable snapshot consumed by Shell status components.</summary>
public sealed record ReconnectionReconciliationSnapshot(
    ReconnectionReconciliationStatus Status,
    long Epoch,
    bool Changed,
    DateTimeOffset LastTransitionAt);
