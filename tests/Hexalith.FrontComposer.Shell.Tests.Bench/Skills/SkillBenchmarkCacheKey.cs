namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkCacheKey(string Value) {
    /// <summary>
    /// P-4: cache key derives from a canonical pipe-delimited string covering every contract
    /// input including the prompt's full <see cref="SkillBenchmarkPrompt.ExpectedShape"/>. Anonymous
    /// JSON serialization is avoided because it depends on reflection metadata (an AOT/trim hazard)
    /// and silently omits fields that are not declared in the anonymous type.
    /// </summary>
    public static SkillBenchmarkCacheKey Create(
        SkillBenchmarkPrompt prompt,
        string frameworkVersion,
        string corpusVersion,
        SkillBenchmarkModelConfig config,
        string scorerVersion,
        string validatorVersion,
        string redactionPolicyVersion) {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(config);

        string canonical = string.Join(
            "",
            "frontcomposer.skill-benchmark.cache.v1",
            prompt.Id,
            prompt.Text,
            string.Join("|", prompt.ExpectedShape.OrderBy(v => v, StringComparer.Ordinal)),
            frameworkVersion,
            corpusVersion,
            config.ConfigHash(),
            scorerVersion,
            validatorVersion,
            redactionPolicyVersion);
        return new SkillBenchmarkCacheKey(SkillCorpusParser.Sha256Hex(canonical));
    }
}
