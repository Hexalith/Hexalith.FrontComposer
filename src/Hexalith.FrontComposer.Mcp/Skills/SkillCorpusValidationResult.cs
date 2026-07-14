namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillCorpusValidationResult(IReadOnlyList<SkillCorpusDiagnostic> Diagnostics) {
    public bool IsValid => Diagnostics.Count == 0;
}
