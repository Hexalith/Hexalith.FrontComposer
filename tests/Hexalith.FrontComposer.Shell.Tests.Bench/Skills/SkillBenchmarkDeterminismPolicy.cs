namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillBenchmarkDeterminismPolicy {
    public static SkillBenchmarkProviderRequest CreateRequest(
        SkillBenchmarkModelConfig desiredConfig,
        SkillBenchmarkProviderCapabilities capabilities) {
        ArgumentNullException.ThrowIfNull(desiredConfig);
        ArgumentNullException.ThrowIfNull(capabilities);

        List<string> unsupported = [];
        int? seed = desiredConfig.Seed;
        if (seed.HasValue && !capabilities.SupportsSeed) {
            seed = null;
            unsupported.Add("seed-unsupported");
        }

        if (!capabilities.SupportsFingerprint) {
            unsupported.Add("fingerprint-unsupported");
        }

        return new SkillBenchmarkProviderRequest(
            desiredConfig with {
                Temperature = 0d,
                Seed = seed,
            },
            SeedSent: seed.HasValue,
            FingerprintExpected: capabilities.SupportsFingerprint,
            UnsupportedCapabilities: unsupported);
    }
}
