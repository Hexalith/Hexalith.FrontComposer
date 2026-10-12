namespace Hexalith.FrontComposer.Shell.State.ProjectionConnection;

/// <summary>Summary returned by an epoch-scoped reconnect reconciliation pass.</summary>
public sealed record ProjectionReconciliationRefreshResult(int RefreshedCount, IReadOnlyList<string> ChangedViewKeys) {
    /// <summary>Eligible lanes whose attempted reads failed.</summary>
    public IReadOnlyList<string> FailedViewKeys { get; init; } = [];

    /// <summary>Whether an eligible lane could not complete because another read still owns it.</summary>
    public bool HasIncompleteEligibleReads { get; init; }

    /// <summary>Exact eligible lane keys that this pass could not read.</summary>
    internal IReadOnlyList<string> IncompleteViewKeys { get; init; } = [];

    /// <summary>The exact eligible reads skipped by this pass, including reads that have since completed.</summary>
    internal IReadOnlyList<Task> IncompleteReadTasks { get; init; } = [];

    /// <summary>Eligible lanes successfully read in this pass, used to resolve earlier failures.</summary>
    internal IReadOnlyList<string> RefreshedViewKeys { get; init; } = [];

    /// <summary>Admitted eligible reads that produced no current freshness evidence.</summary>
    internal IReadOnlyList<string> SkippedViewKeys { get; init; } = [];

    public static ProjectionReconciliationRefreshResult Empty { get; } = new(0, []);
}
