namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillCorpusSnapshot(
    IReadOnlyList<SkillCorpusResource> Resources,
    IReadOnlyList<SkillCorpusDiagnostic> Diagnostics);
