namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillBenchmarkCachePolicy {
    public static bool CanReuse(SkillBenchmarkCacheKey expected, SkillBenchmarkCacheKey actual) {
        ArgumentNullException.ThrowIfNull(expected);
        ArgumentNullException.ThrowIfNull(actual);

        return string.Equals(expected.Value, actual.Value, StringComparison.Ordinal);
    }

    public static string CacheMissReason(SkillBenchmarkCacheKey expected, SkillBenchmarkCacheKey actual)
        => CanReuse(expected, actual) ? string.Empty : "contract-input-changed";
}
