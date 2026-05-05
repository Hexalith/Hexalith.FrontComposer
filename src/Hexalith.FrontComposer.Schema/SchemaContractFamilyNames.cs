using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Schema;

public static class SchemaContractFamilyNames {
    public static string Canonical(SchemaContractFamily family)
        => family switch {
            SchemaContractFamily.CommandTool => "command-tool",
            SchemaContractFamily.ProjectionResource => "projection-resource",
            SchemaContractFamily.LifecycleResult => "lifecycle-result",
            SchemaContractFamily.MarkdownRendererContract => "markdown-renderer-contract",
            SchemaContractFamily.SkillCorpusManifest => "skill-corpus-manifest",
            SchemaContractFamily.SkillCorpusResource => "skill-corpus-resource",
            SchemaContractFamily.AggregateMcpManifest => "aggregate-mcp-manifest",
            _ => throw new ArgumentOutOfRangeException(nameof(family), family, "Unknown schema contract family."),
        };
}
