namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillBenchmarkOfflineScorer {
    /// <summary>
    /// AC9 / T6: documented one-shot pass-rate target for the v1 prompt set. Story 10-6 owns
    /// the live signed gate that enforces this target in CI; 8-5 records it as a constant so
    /// downstream tooling can compute pass-rate against the same number.
    /// </summary>
    public const double OneShotPassTarget = 0.80;

    // P-14: deterministic priority order — the scorer reports the most security-relevant
    // diagnostic when multiple categories appear in the same generation, rather than relying on
    // diagnostic insertion order (which would reorder under future parallelization).
    private static readonly GeneratedCodeFailureCategory[] PriorityOrder = [
        GeneratedCodeFailureCategory.TenantSpoofing,
        GeneratedCodeFailureCategory.GeneratedFileEdit,
        GeneratedCodeFailureCategory.PackageBoundary,
        GeneratedCodeFailureCategory.Compile,
        GeneratedCodeFailureCategory.InvalidAttribute,
        GeneratedCodeFailureCategory.MissingRegistration,
        GeneratedCodeFailureCategory.ValidationShape,
        GeneratedCodeFailureCategory.TestScaffold,
        GeneratedCodeFailureCategory.SourceToolsManifest,
        GeneratedCodeFailureCategory.Unknown,
    ];

    public static SkillBenchmarkScore Score(SkillBenchmarkPrompt prompt, IEnumerable<GeneratedCodeFile> generatedFiles) {
        ArgumentNullException.ThrowIfNull(prompt);
        GeneratedCodeValidationResult result = GeneratedBoundedContextValidator.Validate(generatedFiles);
        if (result.IsValid) {
            return new SkillBenchmarkScore(true, GeneratedCodeFailureCategory.None);
        }

        HashSet<GeneratedCodeFailureCategory> categories = [.. result.Diagnostics.Select(d => d.Category)];
        foreach (GeneratedCodeFailureCategory category in PriorityOrder) {
            if (categories.Contains(category)) {
                return new SkillBenchmarkScore(false, category);
            }
        }

        return new SkillBenchmarkScore(false, GeneratedCodeFailureCategory.Unknown);
    }

    public static double OneShotPassRate(IEnumerable<SkillBenchmarkScore> scores) {
        ArgumentNullException.ThrowIfNull(scores);

        SkillBenchmarkScore[] all = [.. scores];
        if (all.Length == 0) {
            return 0d;
        }

        int passed = all.Count(s => s.Passed);
        return (double)passed / all.Length;
    }
}
