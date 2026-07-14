namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillBenchmarkBudgetPolicy {
    public static SkillBenchmarkBudgetStatus Evaluate(SkillBenchmarkBudgetState? state, DateTimeOffset now) {
        if (state is null
            || state.MonthlyCap <= 0
            || state.Consumed < 0
            || state.ExpiresAt <= now
            || !state.ProviderCostMetadataAvailable
            || state.RetryStormDetected) {
            return SkillBenchmarkBudgetStatus.BudgetUnknown;
        }

        return state.Consumed >= state.MonthlyCap
            ? SkillBenchmarkBudgetStatus.BudgetExhausted
            : SkillBenchmarkBudgetStatus.Available;
    }
}
