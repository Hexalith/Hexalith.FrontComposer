namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkBaselineArtifact(
    double InitialPassRate,
    string CorpusHash,
    string ScorerVersion,
    string ValidatorVersion,
    string RedactionPolicyVersion,
    string ProviderConfigHash,
    string CommitSha,
    string ApproverMarker,
    string SanitizedSummaryHash,
    DateTimeOffset CapturedAt);
