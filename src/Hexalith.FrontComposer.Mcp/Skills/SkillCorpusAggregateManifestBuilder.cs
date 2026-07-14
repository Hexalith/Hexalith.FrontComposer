using System.Text;

namespace Hexalith.FrontComposer.Mcp.Skills;

public static class SkillCorpusAggregateManifestBuilder {
    public const string ManifestSchemaVersion = "frontcomposer.skill-corpus.manifest.v1";
    public const string ManifestResourceUri = "frontcomposer://skills/manifest";

    public static SkillCorpusAggregateManifest Build(SkillCorpusSnapshot snapshot) {
        ArgumentNullException.ThrowIfNull(snapshot);

        IReadOnlyList<SkillCorpusManifestEntry> entries = [.. snapshot.Resources
            .OrderBy(r => r.Order)
            .ThenBy(r => r.ResourceUri, StringComparer.Ordinal)
            .Select(r => new SkillCorpusManifestEntry(
                r.Id,
                r.ResourceUri,
                r.SourceDoc,
                r.Version,
                r.OwningStory,
                r.MigrationOwner,
                r.PublicApiReferences,
                r.SamplePaths))];

        string corpusVersion = snapshot.Resources.Count == 0
            ? "0.0.0"
            : snapshot.Resources.Max(r => r.Version) ?? "0.0.0";

        return new SkillCorpusAggregateManifest(ManifestSchemaVersion, corpusVersion, entries);
    }

    public static string Render(SkillCorpusAggregateManifest manifest) {
        ArgumentNullException.ThrowIfNull(manifest);

        StringBuilder sb = new();
        _ = sb.AppendLine("# FrontComposer Skill Corpus Manifest");
        _ = sb.AppendLine();
        _ = sb.AppendLine($"- manifestSchemaVersion: `{manifest.ManifestSchemaVersion}`");
        _ = sb.AppendLine($"- corpusVersion: `{manifest.CorpusVersion}`");
        _ = sb.AppendLine($"- resourceCount: `{manifest.Resources.Count}`");
        _ = sb.AppendLine();
        _ = sb.AppendLine("## Resources");
        _ = sb.AppendLine();
        foreach (SkillCorpusManifestEntry entry in manifest.Resources) {
            _ = sb.AppendLine($"### `{entry.ResourceUri}`");
            _ = sb.AppendLine();
            _ = sb.AppendLine($"- id: `{entry.Id}`");
            _ = sb.AppendLine($"- sourceDoc: `{entry.SourceDoc}`");
            _ = sb.AppendLine($"- version: `{entry.Version}`");
            if (!string.IsNullOrWhiteSpace(entry.OwningStory)) {
                _ = sb.AppendLine($"- owningStory: `{entry.OwningStory}`");
            }

            if (!string.IsNullOrWhiteSpace(entry.MigrationOwner)) {
                _ = sb.AppendLine($"- migrationOwner: `{entry.MigrationOwner}`");
            }

            if (entry.PublicApiReferences.Count > 0) {
                _ = sb.AppendLine($"- publicApiReferences: {string.Join(", ", entry.PublicApiReferences.Select(v => $"`{v}`"))}");
            }

            if (entry.SamplePaths.Count > 0) {
                _ = sb.AppendLine($"- samplePaths: {string.Join(", ", entry.SamplePaths.Select(v => $"`{v}`"))}");
            }

            _ = sb.AppendLine();
        }

        return sb.ToString();
    }
}
