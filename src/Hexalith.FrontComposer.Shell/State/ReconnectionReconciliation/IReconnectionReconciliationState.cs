namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

/// <summary>Scoped per-circuit reconciliation state.</summary>
public interface IReconnectionReconciliationState {
    ReconnectionReconciliationSnapshot Current { get; }

    IDisposable Subscribe(Action<ReconnectionReconciliationSnapshot> handler, bool replay = true);

    void Start(long epoch);

    /// <summary>Completes a successful reconciliation using the original public contract.</summary>
    void Complete(long epoch, bool changed);

    /// <summary>Completes a reconciliation with its outcome and data-read result.</summary>
    void Complete(long epoch, bool changed, bool succeeded = true, bool dataRead = true)
        => Complete(epoch, changed);

    void Reset(long? expectedEpoch = null);
}
