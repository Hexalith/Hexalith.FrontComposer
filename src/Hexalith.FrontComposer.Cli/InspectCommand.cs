using System.Text.Json;

namespace Hexalith.FrontComposer.Cli;

internal static class InspectCommand {
    public static async Task<int> RunAsync(CommandOptions options, TextWriter output, TextWriter error, CancellationToken cancellationToken) {
        string format = options.Get("format", "text");
        if (format is not ("text" or "json")) {
            await error.WriteLineAsync("--format must be 'text' or 'json'.").ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        var project = ProjectSelection.Resolve(options, Environment.CurrentDirectory);
        if (!project.Success) {
            await error.WriteLineAsync(project.Error).ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        string configuration = options.Get("configuration", "Debug");
        string? framework = options.Get("framework");
        InspectLoadResult load = await GeneratedOutputLoader.LoadAsync(project.ProjectPath!, configuration, framework, options.Has("build"), options.Has("absolute-paths"), error, cancellationToken)
            .ConfigureAwait(false);
        if (!load.Success) {
            await error.WriteLineAsync(load.Error).ConfigureAwait(false);
            return load.ExitCode;
        }

        InspectReport report = load.Report!;
        string? severity = options.Get("severity");
        if (!string.IsNullOrWhiteSpace(severity)) {
            int minimumSeverity = NormalizeSeverityRank(severity);
            if (minimumSeverity < 0) {
                await error.WriteLineAsync("--severity must be one of hidden, info, warning, or error.").ConfigureAwait(false);
                return ExitCodes.InvalidArguments;
            }

            // AC2: `hidden` is the "show everything" level and must include all diagnostics, even
            // sidecar entries whose severity is non-canonical (rank -1, e.g. a malformed sidecar).
            // Higher levels keep strict threshold semantics, so a non-canonical severity stays
            // visible only under `hidden` and never satisfies info/warning/error thresholds.
            report = report with {
                Diagnostics = report.Diagnostics
                    .Where(x => minimumSeverity == 0 || SeverityRank(x.Severity) >= minimumSeverity)
                    .ToArray(),
            };
        }

        string? requestedType = options.Get("type");
        if (!string.IsNullOrWhiteSpace(requestedType)) {
            TypeMatchResult match = TypeMatcher.Filter(report, requestedType);
            if (!match.Success) {
                await error.WriteLineAsync(match.Error).ConfigureAwait(false);
                return match.ExitCode;
            }

            report = match.Report!;
        }

        if (format == "json") {
            string json = JsonSerializer.Serialize(InspectJson.From(report), JsonOptions.Stable);
            await output.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        else {
            RenderText(report, output);
        }

        if (options.Has("fail-on-warning") && report.Diagnostics.Any(x => x.Severity is "Warning" or "Error")) {
            return ExitCodes.ActionableFindings;
        }

        if (options.Has("fail-on-error") && report.Diagnostics.Any(x => x.Severity is "Error")) {
            return ExitCodes.ActionableFindings;
        }

        return ExitCodes.Success;
    }

    private static int NormalizeSeverityRank(string severity)
        => severity.ToLowerInvariant() switch {
            "hidden" => 0,
            "info" or "information" => 1,
            "warning" => 2,
            "error" => 3,
            _ => -1,
        };

    private static int SeverityRank(string severity)
        => severity switch {
            "Hidden" => 0,
            "Info" => 1,
            "Warning" => 2,
            "Error" => 3,
            _ => -1,
        };

    private static void RenderText(InspectReport report, TextWriter output) {
        output.WriteLine($"Project: {OutputSanitizer.Sanitize(report.ProjectName)}");
        output.WriteLine($"Configuration: {OutputSanitizer.Sanitize(report.Configuration)}");
        output.WriteLine($"Framework: {OutputSanitizer.Sanitize(report.Framework)}");
        output.WriteLine($"Generated files: {report.Files.Count}");
        output.WriteLine($"Forms: {report.Summary.Forms}; Grids: {report.Summary.Grids}; Registrations: {report.Summary.Registrations}; MCP manifests: {report.Summary.McpManifestEntries}; Warnings: {report.Summary.Warnings}; Errors: {report.Summary.Errors}");
        foreach (GeneratedFileInfo file in report.Files) {
            output.WriteLine($"- {file.Family}: {OutputSanitizer.Sanitize(file.RelativePath)}");
        }

        foreach (InspectDiagnostic diagnostic in report.Diagnostics) {
            output.WriteLine($"! {diagnostic.Id} {diagnostic.Severity}: What: {OutputSanitizer.Sanitize(diagnostic.What)} Expected: {OutputSanitizer.Sanitize(diagnostic.Expected)} Got: {OutputSanitizer.Sanitize(diagnostic.Got)} Fix: {OutputSanitizer.Sanitize(diagnostic.Fix)} DocsLink: {OutputSanitizer.Sanitize(diagnostic.DocsLink)}");
        }
    }
}
