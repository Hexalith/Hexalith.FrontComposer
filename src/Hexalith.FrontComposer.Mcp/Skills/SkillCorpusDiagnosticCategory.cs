namespace Hexalith.FrontComposer.Mcp.Skills;

public enum SkillCorpusDiagnosticCategory {
    MissingFrontMatter,
    InvalidFrontMatter,
    InvalidSectionMarker,
    DuplicateResource,
    MissingPublicApiReference,
    MissingSamplePath,
    UnsafeContent,
    MigrationGuideMissing,
    BrokenSnippet,
    BaselineMismatch,
}
