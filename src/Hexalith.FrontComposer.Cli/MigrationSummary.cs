namespace Hexalith.FrontComposer.Cli;

internal sealed record MigrationSummary(
    int Changed,
    int Unchanged,
    int Skipped,
    int Failed,
    int ManualOnly,
    int Conflicts) {
    public static MigrationSummary From(IReadOnlyList<MigrationEntry> entries)
        => new(
            entries.Count(x => x.Kind == "safe-fix"),
            entries.Count(x => x.Kind == "unchanged"),
            entries.Count(x => x.Kind == "skipped"),
            entries.Count(x => x.Kind == "failed"),
            entries.Count(x => x.Kind == "manual-only"),
            entries.Count(x => x.Kind == "conflict"));
}
