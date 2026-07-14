namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillBenchmarkBaselinePolicy {
    public static SkillBenchmarkBaselineWriteDecision DecideWrite(
        bool trustedContext,
        bool approvedMarkerPresent,
        SkillBenchmarkGateResult candidate) {
        ArgumentNullException.ThrowIfNull(candidate);

        return trustedContext
            && approvedMarkerPresent
            && candidate.Status == SkillBenchmarkGateStatus.Passed
                ? SkillBenchmarkBaselineWriteDecision.WriteApprovedBaseline
                : SkillBenchmarkBaselineWriteDecision.CandidateEvidenceOnly;
    }
}
