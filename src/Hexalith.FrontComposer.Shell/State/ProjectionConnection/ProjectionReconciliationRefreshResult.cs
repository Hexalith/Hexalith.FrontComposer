namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Summary returned by an epoch-scoped reconnect reconciliation pass.</summary>
public sealed record ProjectionReconciliationRefreshResult(int RefreshedCount, IReadOnlyList<string> ChangedViewKeys) {
    public static ProjectionReconciliationRefreshResult Empty { get; } = new(0, []);
}
