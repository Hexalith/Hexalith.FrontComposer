using System.Text.Json;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationCommand {
    public static async Task<int> RunAsync(CommandOptions options, TextWriter output, TextWriter error, CancellationToken cancellationToken) {
        string format = options.Get("format", "text");
        if (format is not ("text" or "json")) {
            await error.WriteLineAsync("--format must be 'text' or 'json'.").ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        bool apply = options.Has("apply");
        if (apply && options.Has("dry-run")) {
            await error.WriteLineAsync("Choose either --dry-run or --apply, not both.").ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        string? from = options.Get("from");
        string? to = options.Get("to");
        MigrationEdge? edge = MigrationCatalog.Resolve(from, to);
        if (edge is null) {
            await error.WriteLineAsync(MigrationCatalog.UnsupportedMessage(from, to)).ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        var project = ProjectSelection.Resolve(options, Environment.CurrentDirectory);
        if (!project.Success) {
            await error.WriteLineAsync(project.Error).ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        if (ProjectDocumentLoader.HasTopLevelImports(project.ProjectPath!)) {
            await error.WriteLineAsync(
                "Warning: MSBuild <Import> items are not evaluated by frontcomposer migrate; files contributed only by imports are not migrated. DocsLink: src/Hexalith.FrontComposer.Cli/README.md#migration-output-notes.")
                .ConfigureAwait(false);
        }

        MigrationPlan plan = await MigrationPlanner.PlanAsync(project.ProjectPath!, edge, cancellationToken).ConfigureAwait(false);
        MigrationResult result = apply
            ? await MigrationApplier.ApplyAsync(plan, cancellationToken).ConfigureAwait(false)
            : MigrationResult.FromPlan(plan, applied: false);

        if (format == "json") {
            string json = JsonSerializer.Serialize(MigrationJson.From(result), JsonOptions.Stable);
            await output.WriteLineAsync(json.AsMemory(), cancellationToken).ConfigureAwait(false);
        }
        else {
            RenderText(result, output);
        }

        if (result.Summary.Failed > 0) {
            return ExitCodes.ApplyWriteFailure;
        }

        return options.Has("fail-on-findings") && result.Summary.Changed + result.Summary.ManualOnly + result.Summary.Conflicts > 0
            ? ExitCodes.ActionableFindings
            : ExitCodes.Success;
    }

    internal static void RenderText(MigrationResult result, TextWriter output) {
        output.WriteLine(result.Applied ? "Migration apply completed." : "Migration dry-run completed.");
        output.WriteLine($"Changed: {result.Summary.Changed}; Unchanged: {result.Summary.Unchanged}; Skipped: {result.Summary.Skipped}; Failed: {result.Summary.Failed}; Manual-only: {result.Summary.ManualOnly}; Conflicts: {result.Summary.Conflicts}");
        // AC6: text output honors the same per-entry (8,000) and aggregate (64,000) diff budgets as JSON,
        // so "many changed files" cannot grow terminal output beyond the contracted aggregate cap.
        int diffBudget = MigrationJson.MaxAggregateDiffChars;
        foreach (MigrationEntry entry in result.Entries.OrderBy(x => x.Path, StringComparer.Ordinal).ThenBy(x => x.DiagnosticId, StringComparer.Ordinal)) {
            output.WriteLine($"- {entry.Kind} {entry.DiagnosticId} {OutputSanitizer.Sanitize(entry.Path)}: {OutputSanitizer.Sanitize(entry.What)}");
            if (string.IsNullOrWhiteSpace(entry.Diff)) {
                continue;
            }

            if (diffBudget <= 0) {
                output.WriteLine("[diff omitted: aggregate diff budget exceeded]");
                continue;
            }

            string rendered = OutputSanitizer.SanitizeMultiLine(entry.Diff, Math.Min(diffBudget, MigrationJson.MaxPerEntryDiffChars));
            output.Write(rendered);
            diffBudget -= rendered.Length;
        }
    }
}
