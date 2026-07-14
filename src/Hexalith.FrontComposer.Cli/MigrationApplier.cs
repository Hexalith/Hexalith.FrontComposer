using System.Text;

namespace Hexalith.FrontComposer.Cli;

internal static class MigrationApplier {
    public static async Task<MigrationResult> ApplyAsync(MigrationPlan plan, CancellationToken cancellationToken) {
        List<MigrationEntry> entries = [.. plan.Entries.Where(x => x.Kind != "safe-fix")];
        List<MigrationEntry> changed = [];
        HashSet<string> submoduleSnapshot = SubmoduleBoundaryReader.Read(plan.ProjectDirectory);
        bool cancelled = false;
        foreach (PlannedFileEdit edit in plan.FileEdits.OrderBy(x => x.RelativePath, StringComparer.Ordinal)) {
            try {
                cancellationToken.ThrowIfCancellationRequested();
                string canonicalNow = PathUtilities.Canonical(edit.FullPath);
                if (!string.Equals(canonicalNow, edit.CanonicalPath, PathUtilities.PathComparison)
                    || !WriteSafetyPolicy.IsAllowed(plan.ProjectDirectory, edit.FullPath, submoduleSnapshot)) {
                    entries.Add(Failed(edit, plan.Edge, "Resolved write target changed or became unsafe between planning and apply."));
                    continue;
                }

                SourceFileContent current;
                try {
                    current = await SourceFile.ReadAsync(edit.FullPath, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.DecoderFallbackException) {
                    entries.Add(Failed(edit, plan.Edge, "Source file could not be re-read during apply."));
                    continue;
                }

                if (!string.Equals(MigrationPlanner.Hash(current.Text), edit.OriginalHash, StringComparison.Ordinal)) {
                    entries.Add(Failed(edit, plan.Edge, "Source content changed between planning and apply."));
                    continue;
                }

                await SourceFile.WriteAsync(edit.FullPath, edit.UpdatedText, edit.OriginalContent.Encoding, cancellationToken).ConfigureAwait(false);
                changed.AddRange(edit.Entries);
            }
            catch (OperationCanceledException) {
                cancelled = true;
                entries.Add(Failed(edit, plan.Edge, "Migration apply was cancelled after writing " + changed.Count + " file(s)."));
                break;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                entries.Add(Failed(edit, plan.Edge, "Source file could not be written."));
            }
        }

        List<MigrationEntry> final = [.. entries, .. changed];
        final = [.. final.OrderBy(x => x.Path, StringComparer.Ordinal).ThenBy(x => x.DiagnosticId, StringComparer.Ordinal).ThenBy(x => x.Kind, StringComparer.Ordinal)];
        // `Applied` reports whether apply ran to completion AND every planned write succeeded.
        // Cancellation OR any per-file Failed entry flips it to false so callers cannot mistake
        // a partial-write run for a clean apply.
        bool appliedClean = !cancelled && !final.Any(x => x.Kind == "failed");
        return new MigrationResult(appliedClean, final, MigrationSummary.From(final));
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
