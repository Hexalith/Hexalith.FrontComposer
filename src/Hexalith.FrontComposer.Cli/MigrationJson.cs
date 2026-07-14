namespace Hexalith.FrontComposer.Cli;

internal static class MigrationJson {
    // Top-level cap on accumulated diff bytes across all entries; once exceeded subsequent diffs
    // are emitted as a `[diff omitted: budget]` placeholder so JSON cannot grow unbounded.
    internal const int MaxAggregateDiffChars = 64_000;
    internal const int MaxPerEntryDiffChars = 8_000;

    public static object From(MigrationResult result) {
        int diffBudget = MaxAggregateDiffChars;
        return new {
            schemaVersion = "frontcomposer.cli.migrate.v1",
            applied = result.Applied,
            summary = new {
                changed = result.Summary.Changed,
                unchanged = result.Summary.Unchanged,
                skipped = result.Summary.Skipped,
                failed = result.Summary.Failed,
                manualOnly = result.Summary.ManualOnly,
                conflicts = result.Summary.Conflicts,
            },
            entries = result.Entries
                .OrderBy(x => x.Path, StringComparer.Ordinal)
                .ThenBy(x => x.DiagnosticId, StringComparer.Ordinal)
                .Select(x => new {
                    diagnosticId = x.DiagnosticId,
                    kind = x.Kind,
                    path = OutputSanitizer.Sanitize(x.Path),
                    what = OutputSanitizer.Sanitize(x.What),
                    expected = OutputSanitizer.Sanitize(x.Expected),
                    got = OutputSanitizer.Sanitize(x.Got),
                    fix = OutputSanitizer.Sanitize(x.Fix),
                    docsLink = OutputSanitizer.Sanitize(x.DocsLink),
                    diff = TakeDiffWithBudget(x.Diff, ref diffBudget),
                    formattingApplied = x.FormattingApplied,
                })
                .ToArray(),
        };
    }

    private static string TakeDiffWithBudget(string? diff, ref int budget) {
        if (string.IsNullOrEmpty(diff)) {
            return string.Empty;
        }

        if (budget <= 0) {
            return "[diff omitted: aggregate diff budget exceeded]";
        }

        int allowed = Math.Min(budget, MaxPerEntryDiffChars);
        string sanitized = OutputSanitizer.SanitizeMultiLine(diff, allowed);
        budget -= sanitized.Length;
        return sanitized;
    }
}
