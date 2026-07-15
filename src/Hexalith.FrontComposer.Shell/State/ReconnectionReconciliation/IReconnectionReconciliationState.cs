namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Scoped per-circuit reconciliation state.</summary>
public interface IReconnectionReconciliationState {
    ReconnectionReconciliationSnapshot Current { get; }

    IDisposable Subscribe(Action<ReconnectionReconciliationSnapshot> handler, bool replay = true);

    void Start(long epoch);

    void Complete(long epoch, bool changed);

    void Reset(long? expectedEpoch = null);
}
