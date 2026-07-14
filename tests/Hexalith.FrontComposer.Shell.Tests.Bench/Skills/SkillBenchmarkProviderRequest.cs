namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkProviderRequest(
    SkillBenchmarkModelConfig Config,
    bool SeedSent,
    bool FingerprintExpected,
    IReadOnlyList<string> UnsupportedCapabilities);
