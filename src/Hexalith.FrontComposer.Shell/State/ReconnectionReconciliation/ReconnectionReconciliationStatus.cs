namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Transient status for the active reconnect reconciliation pass.</summary>
public enum ReconnectionReconciliationStatus {
    Idle,
    Reconciling,
    Refreshed,
}
