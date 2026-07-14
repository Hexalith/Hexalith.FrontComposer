namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkGateResult(
    SkillBenchmarkGateStatus Status,
    int PromptCount,
    int PassedCount,
    int InvalidEvidenceCount,
    double PassRate,
    double Threshold,
    IReadOnlyList<string> Diagnostics);
