namespace Hexalith.FrontComposer.Contracts.Freshness;

/// <summary>Routine presentation transitions during optimistic reconciliation.</summary>
public enum FcReconcileState
{
    /// <summary>The optimistic command is being sent.</summary>
    Saving,
    /// <summary>The projection has confirmed the current record.</summary>
    Reconciled,
    /// <summary>The projection still exposes the last known record.</summary>
    Degraded,
}
