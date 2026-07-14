using System.Text.RegularExpressions;

namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillCorpusReleaseGuard {
    private static readonly Regex MigrationOwnerPattern = new(
        @"^Story\s+\d+-\d+(?:[A-Za-z])?\b",
        RegexOptions.CultureInvariant | RegexOptions.NonBacktracking | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(500));

    /// <summary>
    /// Validates that every supplied resource carries a migrationOwner that resembles a story
    /// reference (e.g. "Story 9-5"). P-35: empty/whitespace and informal placeholders such as
    /// "TBD" or "unknown" are rejected so the guardrail cannot be silenced by a hand-wave.
    /// </summary>
    public static SkillCorpusValidationResult ValidateBreakingChangesRequireMigration(IEnumerable<SkillCorpusResource> changedResources) {
        ArgumentNullException.ThrowIfNull(changedResources);

        List<SkillCorpusDiagnostic> diagnostics = [];
        foreach (SkillCorpusResource resource in changedResources) {
            ValidateMigrationOwner(resource, diagnostics);
        }

        return new SkillCorpusValidationResult(diagnostics);
    }

    /// <summary>
    /// P-41 / DN-5: compare the current snapshot against a baseline snapshot. Resources whose
    /// public API references differ between baseline and current are treated as breaking changes
    /// and require a migration owner; resources present only in the current snapshot are treated
    /// as additive and skipped. Baseline-not-supplied is a no-op so a release pipeline that has
    /// not yet wired the baseline does not block the build, but the absence is reported so the
    /// gap is visible.
    /// </summary>
    public static SkillCorpusValidationResult ValidateAgainstBaseline(
        SkillCorpusSnapshot current,
        ISkillCorpusBaselineProvider baselineProvider) {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(baselineProvider);

        SkillCorpusSnapshot? baseline = baselineProvider.GetBaseline();
        if (baseline is null) {
            // Stub mode: nothing to compare. Surface this as a benign info diagnostic so the
            // release pipeline knows it is running without a baseline.
            return new SkillCorpusValidationResult([
                new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.BaselineMismatch,
                    SkillCorpusAggregateManifestBuilder.ManifestResourceUri,
                    "No skill corpus baseline configured — skipping breaking-change comparison.")]);
        }

        var baselineByUri = baseline.Resources.ToDictionary(r => r.ResourceUri, StringComparer.Ordinal);
        List<SkillCorpusDiagnostic> diagnostics = [];

        foreach (SkillCorpusResource currentResource in current.Resources) {
            if (!baselineByUri.TryGetValue(currentResource.ResourceUri, out SkillCorpusResource? baselineResource)) {
                continue;
            }

            bool publicApiDrift = !currentResource.PublicApiReferences.OrderBy(v => v, StringComparer.Ordinal)
                .SequenceEqual(baselineResource.PublicApiReferences.OrderBy(v => v, StringComparer.Ordinal), StringComparer.Ordinal);

            bool versionChanged = !string.Equals(currentResource.Version, baselineResource.Version, StringComparison.Ordinal);

            if ((publicApiDrift || versionChanged) && !MigrationOwnerLooksValid(currentResource.MigrationOwner)) {
                diagnostics.Add(new SkillCorpusDiagnostic(
                    SkillCorpusDiagnosticCategory.MigrationGuideMissing,
                    currentResource.SourceDoc,
                    $"Skill resource '{currentResource.ResourceUri}' has changed public API references or version vs baseline; migrationOwner must reference an owning story (e.g. 'Story 9-5')."));
            }
        }

        return new SkillCorpusValidationResult(diagnostics);
    }

    private static void ValidateMigrationOwner(SkillCorpusResource resource, List<SkillCorpusDiagnostic> diagnostics) {
        if (!MigrationOwnerLooksValid(resource.MigrationOwner)) {
            diagnostics.Add(new SkillCorpusDiagnostic(
                SkillCorpusDiagnosticCategory.MigrationGuideMissing,
                resource.SourceDoc,
                "Breaking skill corpus changes require migrationOwner metadata referencing an owning story (e.g. 'Story 9-5')."));
        }
    }

    private static bool MigrationOwnerLooksValid(string? value)
        => !string.IsNullOrWhiteSpace(value) && MigrationOwnerPattern.IsMatch(value);
}
