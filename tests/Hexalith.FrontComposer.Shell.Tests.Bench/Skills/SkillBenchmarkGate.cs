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
        if (expectedIds.Distinct(StringComparer.Ordinal).Count() != expectedIds.Length) {
            diagnostics.Add("prompt-ids-must-be-unique");
        }

        if (!expectedIds.SequenceEqual(actualIds, StringComparer.Ordinal)) {
            diagnostics.Add("result-prompt-ids-must-match-v1-corpus");
        }

        string? corpusHash = approvedBaseline is null ? null : promptSet.ComputeCorpusHash();
        int invalid = 0;
        int passed = 0;
        foreach (SkillBenchmarkResult result in results) {
            string? reason = SkillBenchmarkArtifactWriter.GetPersistenceDiagnostic(result);
            if (reason == SkillBenchmarkArtifactWriter.MissingProviderMetadataDiagnostic) {
                reason = "provider-metadata-required";
            }

            if (reason is null && approvedBaseline is not null
                && (!string.Equals(result.CorpusHash, corpusHash, StringComparison.Ordinal) || !MatchesBaseline(result, approvedBaseline))) {
                reason = "benchmark-baseline-contract-mismatch";
            }

            if (reason is not null || !HasConsistentOutcome(result)) {
                invalid++;
                if (reason is not null && !diagnostics.Contains(reason)) {
                    diagnostics.Add(reason);
                }
            }
            else if (result.EvidenceStatus == SkillBenchmarkEvidenceStatus.Valid) {
                passed++;
            }
        }

        if (invalid > 0) {
            diagnostics.Add("invalid-evidence-present");
        }

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

    private static bool HasConsistentOutcome(SkillBenchmarkResult result) {
        bool succeeded = result.CompileSucceeded && result.ValidatorSucceeded;
        return result.EvidenceStatus switch {
            SkillBenchmarkEvidenceStatus.Valid => succeeded && result.FailureCategory == GeneratedCodeFailureCategory.None,
            SkillBenchmarkEvidenceStatus.LegitimateMiss => !succeeded
                && result.FailureCategory != GeneratedCodeFailureCategory.None
                && Enum.IsDefined(result.FailureCategory),
            _ => false,
        };
    }

    private static bool MatchesBaseline(SkillBenchmarkResult result, SkillBenchmarkBaselineArtifact baseline)
        => !string.IsNullOrWhiteSpace(result.RedactionPolicyVersion)
            && string.Equals(result.CorpusHash, baseline.CorpusHash, StringComparison.Ordinal)
            && string.Equals(result.ScorerVersion, baseline.ScorerVersion, StringComparison.Ordinal)
            && string.Equals(result.ValidatorVersion, baseline.ValidatorVersion, StringComparison.Ordinal)
            && string.Equals(result.RedactionPolicyVersion, baseline.RedactionPolicyVersion, StringComparison.Ordinal)
            && string.Equals(result.ProviderConfigHash, baseline.ProviderConfigHash, StringComparison.Ordinal);
}
