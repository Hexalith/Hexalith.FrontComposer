namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillCorpusManifestEntry(
    string Id,
    string ResourceUri,
    string SourceDoc,
    string Version,
    string? OwningStory,
    string? MigrationOwner,
    IReadOnlyList<string> PublicApiReferences,
    IReadOnlyList<string> SamplePaths);
