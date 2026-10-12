using Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

namespace Hexalith.FrontComposer.Shell.Tests.State.ReconnectionReconciliation;

/// <summary>Exercises the original two-argument reconciliation-state implementation contract.</summary>
internal sealed class LegacyReconciliationState(ReconnectionReconciliationStateService inner) : IReconnectionReconciliationState
{
    /// <summary>Gets the number of legacy success completions.</summary>
    internal int SuccessfulCompletions { get; private set; }

    /// <inheritdoc />
    public ReconnectionReconciliationSnapshot Current => inner.Current;

    /// <inheritdoc />
    public IDisposable Subscribe(Action<ReconnectionReconciliationSnapshot> handler, bool replay = true)
        => inner.Subscribe(handler, replay);

    /// <inheritdoc />
    public void Start(long epoch) => inner.Start(epoch);

    /// <inheritdoc />
    public void Complete(long epoch, bool changed)
    {
        SuccessfulCompletions++;
        inner.Complete(epoch, changed);
    }

    /// <inheritdoc />
    public void Reset(long? expectedEpoch = null) => inner.Reset(expectedEpoch);
}
