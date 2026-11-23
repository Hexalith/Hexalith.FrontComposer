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
    /// <summary>Gets the corpus hash recorded by the benchmark run.</summary>
    public string CorpusHash { get; init; } = string.Empty;

    /// <summary>Gets the redaction policy version applied to this result.</summary>
    public string RedactionPolicyVersion { get; init; } = string.Empty;

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
