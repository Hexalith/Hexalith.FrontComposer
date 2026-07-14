namespace Hexalith.FrontComposer.Mcp.Skills;

/// <summary>
/// Aggregate corpus manifest derived at runtime from the parsed snapshot (P-43, DN-8). Carries a
/// stable schema version so Story 8-6 can fingerprint the aggregate without 8-5 owning persistence
/// of the manifest as a standalone artifact. The aggregate is exposed as the
/// <c>frontcomposer://skills/manifest</c> MCP resource.
/// </summary>
public sealed record SkillCorpusAggregateManifest(
    string ManifestSchemaVersion,
    string CorpusVersion,
    IReadOnlyList<SkillCorpusManifestEntry> Resources);
