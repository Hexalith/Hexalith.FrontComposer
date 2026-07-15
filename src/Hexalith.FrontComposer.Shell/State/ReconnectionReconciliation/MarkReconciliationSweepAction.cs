namespace Hexalith.FrontComposer.Shell.State.ReconnectionReconciliation;

public sealed record MarkReconciliationSweepAction(
    long Epoch,
    IReadOnlyList<string> ViewKeys,
    DateTimeOffset ExpiresAt,
    DateTimeOffset Now);
