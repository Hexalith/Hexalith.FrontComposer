namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkResult(
    string PromptId,
    string FrameworkVersion,
    string CorpusVersion,
    string ModelId,
    string ProviderConfigHash,
    string ScorerVersion,
    string ValidatorVersion,
    bool CompileSucceeded,
    bool ValidatorSucceeded,
    GeneratedCodeFailureCategory FailureCategory,
    SkillBenchmarkRedactionStatus RedactionStatus,
    string GeneratedArtifactToken,
    IReadOnlyList<string> SanitizedDiagnostics) {
    public string ProviderId { get; init; } = string.Empty;

    public double Temperature { get; init; }

    public int? Seed { get; init; }

    public int TimeoutSeconds { get; init; }

    public int RetryCount { get; init; }

    public bool SeedSupported { get; init; }

    public bool FingerprintSupported { get; init; }

    public string? ProviderFingerprint { get; init; }

    public string CacheKey { get; init; } = string.Empty;

    public string SanitizedArtifactToken { get; init; } = string.Empty;

    public SkillBenchmarkEvidenceStatus EvidenceStatus { get; init; } = SkillBenchmarkEvidenceStatus.Valid;
}
