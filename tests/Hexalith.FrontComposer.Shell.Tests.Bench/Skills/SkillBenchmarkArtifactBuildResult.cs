namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillBenchmarkArtifactBuildResult(
    bool CanPersist,
    IReadOnlyList<string> Diagnostics,
    string? ArtifactJson);
