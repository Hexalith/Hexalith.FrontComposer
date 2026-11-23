namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillCorpusDiagnostic(
    SkillCorpusDiagnosticCategory Category,
    string Source,
    string Message,
    string? Section = null) {
    /// <summary>Gets whether this diagnostic is advisory and allows validation to succeed.</summary>
    public bool IsInformational { get; init; }
}
