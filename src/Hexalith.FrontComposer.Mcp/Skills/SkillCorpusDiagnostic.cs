namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillCorpusDiagnostic(
    SkillCorpusDiagnosticCategory Category,
    string Source,
    string Message,
    string? Section = null);
