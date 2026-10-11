namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Summary returned by an epoch-scoped reconnect reconciliation pass.</summary>
public sealed record ProjectionReconciliationRefreshResult(int RefreshedCount, IReadOnlyList<string> ChangedViewKeys) {
    /// <summary>Eligible lanes whose attempted reads failed.</summary>
    public IReadOnlyList<string> FailedViewKeys { get; init; } = [];

    public static ProjectionReconciliationRefreshResult Empty { get; } = new(0, []);
}
