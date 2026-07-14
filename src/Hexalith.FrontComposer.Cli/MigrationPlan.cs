namespace Hexalith.FrontComposer.Cli;

internal sealed record MigrationPlan(
    string ProjectDirectory,
    MigrationEdge Edge,
    IReadOnlyList<PlannedFileEdit> FileEdits,
    IReadOnlyList<MigrationEntry> Entries) {
    public MigrationSummary Summary => MigrationSummary.From(Entries);
}
