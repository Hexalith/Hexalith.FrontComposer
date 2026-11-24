namespace Hexalith.FrontComposer.Mcp.Skills;

/// <summary>Describes an issue found while loading or validating a skill corpus.</summary>
/// <param name="Category">The type of issue.</param>
/// <param name="Source">The diagnostic origin. It can contain an operator-supplied local path and must be redacted before exposure to tenants or agents.</param>
/// <param name="Message">A description for corpus operators. Redact any local paths before sharing it with tenants or agents.</param>
/// <param name="Section">The affected section, when available.</param>
public sealed record SkillCorpusDiagnostic(
    SkillCorpusDiagnosticCategory Category,
    string Source,
    string Message,
    string? Section = null) {
    /// <summary>Gets whether this diagnostic is advisory and allows validation to succeed.</summary>
    public bool IsInformational { get; init; }
}
