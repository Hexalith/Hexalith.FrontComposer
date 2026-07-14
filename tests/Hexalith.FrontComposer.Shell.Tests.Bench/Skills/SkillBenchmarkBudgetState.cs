namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkBudgetState(
    decimal MonthlyCap,
    decimal Consumed,
    DateTimeOffset ExpiresAt,
    bool ProviderCostMetadataAvailable,
    bool RetryStormDetected);
