using System.Globalization;

namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkModelConfig(
    string ProviderId,
    string ModelId,
    double Temperature,
    int? Seed,
    int TimeoutSeconds,
    int RetryCount) {
    /// <summary>
    /// P-26: deterministic hash of the config used both for cache-key derivation and for
    /// <see cref="SkillBenchmarkResult.ProviderConfigHash"/> provenance. Producers MUST derive
    /// the persisted hash from this method rather than supplying ad-hoc values; the CanPersist
    /// check verifies the field length looks like a SHA-256 digest.
    /// </summary>
    public string ConfigHash() {
        if (double.IsNaN(Temperature) || double.IsInfinity(Temperature)) {
            throw new InvalidOperationException("SkillBenchmarkModelConfig.Temperature must be a finite double.");
        }

        string canonical = string.Join(
            "|",
            ProviderId,
            ModelId,
            Temperature.ToString("R", CultureInfo.InvariantCulture),
            Seed?.ToString(CultureInfo.InvariantCulture) ?? "<null>",
            TimeoutSeconds.ToString(CultureInfo.InvariantCulture),
            RetryCount.ToString(CultureInfo.InvariantCulture));
        return SkillCorpusParser.Sha256Hex(canonical);
    }
}
