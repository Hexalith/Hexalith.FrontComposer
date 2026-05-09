using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationCommand
{
    public static async Task<int> RunAsync(CommandOptions options, TextWriter output, TextWriter error, CancellationToken cancellationToken)
    {
        string format = options.Get("format", "text");
        if (format is not ("text" or "json")) {
            await error.WriteLineAsync("--format must be 'text' or 'json'.").ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        bool apply = options.Has("apply");
        bool dryRun = options.Has("dry-run") || !apply;
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

        ProjectSelection project = ProjectSelection.Resolve(options, Environment.CurrentDirectory);
        if (!project.Success) {
            await error.WriteLineAsync(project.Error).ConfigureAwait(false);
            return ExitCodes.InvalidArguments;
        }

        MigrationPlan plan = MigrationPlanner.Plan(project.ProjectPath!, edge);
        MigrationResult result = apply
            ? await MigrationApplier.ApplyAsync(plan, cancellationToken).ConfigureAwait(false)
            : MigrationResult.FromPlan(plan, applied: false);

        if (format == "json") {
            string json = JsonSerializer.Serialize(MigrationJson.From(result), JsonOptions.Stable);
            await output.WriteLineAsync(json).ConfigureAwait(false);
        }
        else {
            RenderText(result, output);
        }

        return result.Summary.Failed > 0 ? ExitCodes.ApplyWriteFailure : ExitCodes.Success;
    }

    private static void RenderText(MigrationResult result, TextWriter output)
    {
        output.WriteLine(result.Applied ? "Migration apply completed." : "Migration dry-run completed.");
        output.WriteLine($"Changed: {result.Summary.Changed}; Unchanged: {result.Summary.Unchanged}; Skipped: {result.Summary.Skipped}; Failed: {result.Summary.Failed}; Manual-only: {result.Summary.ManualOnly}; Conflicts: {result.Summary.Conflicts}");
        foreach (MigrationEntry entry in result.Entries.OrderBy(x => x.Path, StringComparer.Ordinal).ThenBy(x => x.DiagnosticId, StringComparer.Ordinal)) {
            output.WriteLine($"- {entry.Kind} {entry.DiagnosticId} {OutputSanitizer.Sanitize(entry.Path)}: {OutputSanitizer.Sanitize(entry.What)}");
            if (!string.IsNullOrWhiteSpace(entry.Diff)) {
                output.WriteLine(OutputSanitizer.Sanitize(entry.Diff, 2_000));
            }
        }
    }
}

internal sealed record MigrationEdge(string FromVersion, string ToVersion, string DocsLink);

internal static class MigrationCatalog
{
    private static readonly MigrationEdge[] Edges = [
        new("9.1.0", "9.2.0", "docs/migrations/9.1-to-9.2.md"),
    ];

    public static MigrationEdge? Resolve(string? from, string? to)
        => Edges.SingleOrDefault(edge => string.Equals(edge.FromVersion, from, StringComparison.Ordinal)
            && string.Equals(edge.ToVersion, to, StringComparison.Ordinal));

    public static string UnsupportedMessage(string? from, string? to)
        => "Unsupported FrontComposer migration edge '"
            + OutputSanitizer.Sanitize(from)
            + "' -> '"
            + OutputSanitizer.Sanitize(to)
            + "'. Supported edges: "
            + string.Join(", ", Edges.Select(edge => edge.FromVersion + " -> " + edge.ToVersion))
            + ". DocsLink: docs/migrations/index.md.";
}

internal sealed record MigrationPlan(
    string ProjectDirectory,
    MigrationEdge Edge,
    IReadOnlyList<PlannedFileEdit> FileEdits,
    IReadOnlyList<MigrationEntry> Entries)
{
    public MigrationSummary Summary => MigrationSummary.From(Entries, applied: false);
}

internal sealed record PlannedFileEdit(
    string FullPath,
    string RelativePath,
    string OriginalText,
    string UpdatedText,
    string OriginalHash,
    IReadOnlyList<MigrationEntry> Entries);

internal sealed record MigrationEntry(
    string DiagnosticId,
    string Kind,
    string Path,
    string What,
    string Expected,
    string Got,
    string Fix,
    string DocsLink,
    string? Diff);

internal sealed record MigrationResult(bool Applied, IReadOnlyList<MigrationEntry> Entries, MigrationSummary Summary)
{
    public static MigrationResult FromPlan(MigrationPlan plan, bool applied)
        => new(applied, plan.Entries, MigrationSummary.From(plan.Entries, applied));
}

internal sealed record MigrationSummary(
    int Changed,
    int Unchanged,
    int Skipped,
    int Failed,
    int ManualOnly,
    int Conflicts)
{
    public static MigrationSummary From(IReadOnlyList<MigrationEntry> entries, bool applied)
    {
        int changed = entries.Count(x => x.Kind == "safe-fix");
        int unchanged = entries.Count(x => x.Kind == "unchanged");
        return new(
            changed,
            unchanged,
            entries.Count(x => x.Kind == "skipped"),
            entries.Count(x => x.Kind == "failed"),
            entries.Count(x => x.Kind == "manual-only"),
            entries.Count(x => x.Kind == "conflict"));
    }
}

internal static class MigrationPlanner
{
    private const string ObsoleteApi = "AddFrontComposerDebugOverlay";
    private const string ReplacementApi = "AddFrontComposerDevMode";

    public static MigrationPlan Plan(string projectPath, MigrationEdge edge)
    {
        string projectFullPath = Path.GetFullPath(projectPath);
        string projectDirectory = Path.GetDirectoryName(projectFullPath)!;
        HashSet<string> submoduleRoots = SubmoduleBoundaryReader.Read(projectDirectory);
        List<MigrationEntry> entries = [];
        List<PlannedFileEdit> fileEdits = [];

        foreach (string path in Directory.EnumerateFiles(projectDirectory, "*.cs", SearchOption.AllDirectories).Order(StringComparer.Ordinal)) {
            string relative = PathUtilities.ToProjectRelative(projectDirectory, path);
            if (!WriteSafetyPolicy.IsAllowed(projectDirectory, path, submoduleRoots)) {
                string text = File.ReadAllText(path);
                if (text.Contains(ObsoleteApi, StringComparison.Ordinal) || text.Contains("HFCM", StringComparison.Ordinal)) {
                    entries.Add(new MigrationEntry(
                        "HFCM0000",
                        "skipped",
                        relative,
                        "Excluded path was not scanned or written.",
                        "Only explicit project source documents outside generated, bin, obj, package cache, and submodule paths are eligible.",
                        "Excluded path.",
                        "Move source into the selected project or apply the migration manually.",
                        edge.DocsLink,
                        null));
                }

                continue;
            }

            string original = File.ReadAllText(path);
            List<MigrationEntry> fileEntries = [];
            if (original.Contains("HFCM9002", StringComparison.Ordinal)) {
                fileEntries.Add(new MigrationEntry(
                    "HFCM9002",
                    "manual-only",
                    relative,
                    "Manual migration note was found for a customization-sensitive FrontComposer API.",
                    "Developer reviews customization semantics before changing behavior.",
                    "Source marker HFCM9002 requires judgment.",
                    "Review the migration guide and update the affected call site manually.",
                    edge.DocsLink,
                    null));
            }

            if (!original.Contains(ObsoleteApi, StringComparison.Ordinal)) {
                entries.AddRange(fileEntries);
                continue;
            }

            string updated = original.Replace(ObsoleteApi, ReplacementApi, StringComparison.Ordinal);
            string diff = UnifiedDiff.Create(relative, original, updated);
            MigrationEntry safeFix = new(
                "HFCM9001",
                "safe-fix",
                relative,
                "Obsolete development overlay registration API was found.",
                ReplacementApi + " is used for Story 6-5 dev-mode overlay registration.",
                ObsoleteApi + " call.",
                "Replace " + ObsoleteApi + " with " + ReplacementApi + ".",
                edge.DocsLink,
                diff);
            fileEntries.Add(safeFix);
            entries.AddRange(fileEntries);
            fileEdits.Add(new PlannedFileEdit(path, relative, original, updated, Hash(original), fileEntries.Where(x => x.Kind == "safe-fix").ToArray()));
        }

        if (!entries.Any(x => x.Kind == "safe-fix")) {
            entries.Add(new MigrationEntry(
                "HFCM0001",
                "unchanged",
                Path.GetFileName(projectFullPath),
                "No fixable FrontComposer migration diagnostics were found.",
                "Only allowlisted HFC migration diagnostics are changed.",
                "No matching diagnostics.",
                "No source changes are required for this migration edge.",
                edge.DocsLink,
                null));
        }

        return new MigrationPlan(projectDirectory, edge, fileEdits, entries);
    }

    public static string Hash(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}

internal static class MigrationApplier
{
    public static async Task<MigrationResult> ApplyAsync(MigrationPlan plan, CancellationToken cancellationToken)
    {
        List<MigrationEntry> entries = [.. plan.Entries];
        foreach (PlannedFileEdit edit in plan.FileEdits.OrderBy(x => x.RelativePath, StringComparer.Ordinal)) {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(PathUtilities.Canonical(edit.FullPath), PathUtilities.Canonical(Path.Combine(plan.ProjectDirectory, edit.RelativePath)), StringComparison.OrdinalIgnoreCase)) {
                entries.Add(Failed(edit, plan.Edge, "Resolved write target changed between planning and apply."));
                continue;
            }

            string current = await File.ReadAllTextAsync(edit.FullPath, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(MigrationPlanner.Hash(current), edit.OriginalHash, StringComparison.Ordinal)) {
                entries.Add(Failed(edit, plan.Edge, "Source content changed between planning and apply."));
                continue;
            }

            await File.WriteAllTextAsync(edit.FullPath, edit.UpdatedText, cancellationToken).ConfigureAwait(false);
        }

        return new MigrationResult(true, entries, MigrationSummary.From(entries, applied: true));
    }

    private static MigrationEntry Failed(PlannedFileEdit edit, MigrationEdge edge, string reason)
        => new(
            "HFCM0004",
            "failed",
            edit.RelativePath,
            reason,
            "Plan path and source hash remain stable until write.",
            "Plan drift detected.",
            "Re-run migration after reviewing source changes.",
            edge.DocsLink,
            null);
}

internal static class WriteSafetyPolicy
{
    public static bool IsAllowed(string projectDirectory, string path, HashSet<string> submoduleRoots)
    {
        string relative = PathUtilities.ToProjectRelative(projectDirectory, path);
        if (relative == "[redacted-path]" || PathUtilities.HasExcludedSegment(projectDirectory, path)) {
            return false;
        }

        string fullPath = Path.GetFullPath(path);
        return !submoduleRoots.Any(root => fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase));
    }
}

internal static class SubmoduleBoundaryReader
{
    public static HashSet<string> Read(string projectDirectory)
    {
        HashSet<string> roots = new(StringComparer.OrdinalIgnoreCase);
        string gitmodules = Path.Combine(projectDirectory, ".gitmodules");
        if (!File.Exists(gitmodules)) {
            return roots;
        }

        foreach (string line in File.ReadLines(gitmodules)) {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("path", StringComparison.Ordinal)) {
                continue;
            }

            int equals = trimmed.IndexOf('=', StringComparison.Ordinal);
            if (equals < 0) {
                continue;
            }

            string relative = trimmed[(equals + 1)..].Trim();
            if (relative.Length > 0) {
                roots.Add(Path.GetFullPath(relative, projectDirectory));
            }
        }

        return roots;
    }
}

internal static class UnifiedDiff
{
    public static string Create(string relativePath, string original, string updated)
    {
        StringBuilder builder = new();
        _ = builder.AppendLine("--- a/" + relativePath);
        _ = builder.AppendLine("+++ b/" + relativePath);
        _ = builder.AppendLine("@@");
        _ = builder.AppendLine("-" + OutputSanitizer.Sanitize(original.Trim(), 400));
        _ = builder.AppendLine("+" + OutputSanitizer.Sanitize(updated.Trim(), 400));
        return builder.ToString();
    }
}

internal static class MigrationJson
{
    public static object From(MigrationResult result)
        => new {
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
                    diff = OutputSanitizer.Sanitize(x.Diff, 2_000),
                })
                .ToArray(),
        };
}
