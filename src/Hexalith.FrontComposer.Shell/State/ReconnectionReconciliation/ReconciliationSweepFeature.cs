using Fluxor;

namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

public sealed class ReconciliationSweepFeature : Feature<ReconciliationSweepState> {
    public override string GetName() => typeof(ReconciliationSweepState).FullName!;

    protected override ReconciliationSweepState GetInitialState() => new();
}
