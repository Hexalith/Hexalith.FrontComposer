using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Mcp.Skills;

public sealed record SkillCorpusResource(
    string Id,
    string Title,
    string Version,
    string Audience,
    bool Docfx,
    bool McpResource,
    string ResourceUri,
    int Order,
    string SourceDoc,
    bool Narrative,
    bool References,
    string? MigrationOwner,
    string Markdown,
    IReadOnlyList<string> PublicApiReferences,
    IReadOnlyList<string> SamplePaths,
    string? OwningStory = null,
    SchemaFingerprint? Fingerprint = null) {
    public string ContentType => McpResource ? "text/markdown" : "text/plain";
}
