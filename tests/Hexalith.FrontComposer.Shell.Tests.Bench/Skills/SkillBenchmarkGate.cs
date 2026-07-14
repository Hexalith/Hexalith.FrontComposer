namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillBenchmarkGate {
    public static SkillBenchmarkGateResult Evaluate(
        SkillBenchmarkPromptSet promptSet,
        IReadOnlyList<SkillBenchmarkResult> results,
        SkillBenchmarkBaselineArtifact? approvedBaseline) {
        ArgumentNullException.ThrowIfNull(promptSet);
        ArgumentNullException.ThrowIfNull(results);

        List<string> diagnostics = [];
        if (promptSet.Prompts.Count != 20) {
            diagnostics.Add("prompt-set-must-contain-exactly-20-prompts");
        }

        string[] expectedIds = [.. promptSet.Prompts.Select(p => p.Id).Order(StringComparer.Ordinal)];
        string[] actualIds = [.. results.Select(r => r.PromptId).Order(StringComparer.Ordinal)];
        if (!expectedIds.SequenceEqual(actualIds, StringComparer.Ordinal)) {
            diagnostics.Add("result-prompt-ids-must-match-v1-corpus");
        }

        int invalid = results.Count(r => r.EvidenceStatus is not SkillBenchmarkEvidenceStatus.Valid and not SkillBenchmarkEvidenceStatus.LegitimateMiss);
        if (invalid > 0) {
            diagnostics.Add("invalid-evidence-present");
        }

        if (results.Any(r => !SkillBenchmarkArtifactWriter.HasProviderMetadata(r))) {
            diagnostics.Add("provider-metadata-required");
        }

        int passed = results.Count(r => r.EvidenceStatus == SkillBenchmarkEvidenceStatus.Valid && r.CompileSucceeded && r.ValidatorSucceeded);
        double passRate = results.Count == 0 ? 0d : (double)passed / results.Count;
        double threshold = Math.Max(SkillBenchmarkOfflineScorer.OneShotPassTarget, approvedBaseline?.InitialPassRate ?? SkillBenchmarkOfflineScorer.OneShotPassTarget);

        if (diagnostics.Count > 0) {
            return new SkillBenchmarkGateResult(
                SkillBenchmarkGateStatus.InvalidEvidence,
                results.Count,
                passed,
                invalid,
                passRate,
                threshold,
                diagnostics);
        }

        if (approvedBaseline is null) {
            return new SkillBenchmarkGateResult(
                SkillBenchmarkGateStatus.CandidateOnly,
                results.Count,
                passed,
                invalid,
                passRate,
                threshold,
                ["baseline-capture-marker-required"]);
        }

        bool passedGate = passRate >= threshold;
        return new SkillBenchmarkGateResult(
            passedGate ? SkillBenchmarkGateStatus.Passed : SkillBenchmarkGateStatus.Failed,
            results.Count,
            passed,
            invalid,
            passRate,
            threshold,
            passedGate ? [] : ["one-shot-pass-rate-below-approved-threshold"]);
    }
}
