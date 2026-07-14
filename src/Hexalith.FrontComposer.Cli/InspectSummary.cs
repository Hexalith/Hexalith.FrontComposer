namespace Hexalith.FrontComposer.Cli;

internal sealed record InspectSummary(
    int GeneratedFiles,
    int Forms,
    int Grids,
    int Registrations,
    int McpManifestEntries,
    int Warnings,
    int Errors) {
    public static InspectSummary From(IReadOnlyList<GeneratedFileInfo> files, IReadOnlyList<InspectDiagnostic> diagnostics)
        => new(
            files.Count,
            files.Count(x => x.Family is GeneratedSourceFamily.CommandForm),
            files.Count(x => x.Family is GeneratedSourceFamily.ProjectionRazor),
            files.Count(x => x.Family is GeneratedSourceFamily.Registration),
            files.Count(x => x.Family is GeneratedSourceFamily.McpManifest),
            diagnostics.Count(x => x.Severity == "Warning"),
            diagnostics.Count(x => x.Severity == "Error"));
}
