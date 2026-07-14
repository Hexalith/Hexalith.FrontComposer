namespace Hexalith.FrontComposer.Cli;

internal static class InspectJson {
    public static object From(InspectReport report)
        => new {
            schemaVersion = "frontcomposer.cli.inspect.v1",
            project = new {
                name = OutputSanitizer.Sanitize(report.ProjectName),
                path = OutputSanitizer.Sanitize(report.ProjectRelativePath),
                configuration = OutputSanitizer.Sanitize(report.Configuration),
                framework = OutputSanitizer.Sanitize(report.Framework),
            },
            summary = new {
                generatedFiles = report.Summary.GeneratedFiles,
                forms = report.Summary.Forms,
                grids = report.Summary.Grids,
                registrations = report.Summary.Registrations,
                mcpManifestEntries = report.Summary.McpManifestEntries,
                warnings = report.Summary.Warnings,
                errors = report.Summary.Errors,
            },
            // AC21: text and JSON share the same iteration order. `report.Files` is already
            // sorted tri-key (RelatedType -> Family -> RelativePath) at load time, which the
            // text renderer relies on; do not re-sort here.
            generatedFiles = report.Files
                .Select(x => new {
                    path = OutputSanitizer.Sanitize(x.RelativePath),
                    family = x.Family.ToString(),
                    relatedType = OutputSanitizer.Sanitize(x.RelatedType),
                })
                .ToArray(),
            diagnostics = report.Diagnostics.Select(x => new {
                id = x.Id,
                severity = x.Severity,
                relatedType = x.RelatedType,
                path = x.RelativePath,
                what = x.What,
                expected = x.Expected,
                got = x.Got,
                fix = x.Fix,
                docsLink = x.DocsLink,
            }).ToArray(),
        };
}
