namespace Hexalith.FrontComposer.Cli;

internal sealed record InspectReport(
    string ProjectName,
    string ProjectRelativePath,
    string Configuration,
    string Framework,
    IReadOnlyList<GeneratedFileInfo> Files,
    IReadOnlyList<InspectDiagnostic> Diagnostics) {
    public InspectSummary Summary => InspectSummary.From(Files, Diagnostics);
}
