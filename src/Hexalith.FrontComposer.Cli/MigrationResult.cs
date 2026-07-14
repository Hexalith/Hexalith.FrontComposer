namespace Hexalith.FrontComposer.Cli;

internal sealed record MigrationResult(bool Applied, IReadOnlyList<MigrationEntry> Entries, MigrationSummary Summary) {
    public static MigrationResult FromPlan(MigrationPlan plan, bool applied)
        => new(applied, plan.Entries, MigrationSummary.From(plan.Entries));
}
