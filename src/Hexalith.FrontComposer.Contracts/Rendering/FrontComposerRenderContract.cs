using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Contracts.Rendering;

public enum RenderSurfaceKind {
    WebBlazor,
    McpMarkdown,
    SkillResourceMarkdown,
    Future,
}

public enum RenderCapability {
    ProjectionTable,
    ProjectionStatusOverview,
    ProjectionTimeline,
    EmptyState,
    BoundedMarkdown,
    SanitizedInertText,
}

public sealed record RenderBounds(
    int MaxRows,
    int MaxColumns,
    int MaxCharacters,
    int MaxFieldCharacters);

public sealed record FrontComposerRenderContract(
    string ContractId,
    string ContractSchemaVersion,
    RenderSurfaceKind Surface,
    string OutputContentType,
    IReadOnlyList<RenderCapability> Capabilities,
    SchemaFingerprint Fingerprint,
    RenderBounds Bounds,
    IReadOnlyDictionary<string, string>? Metadata = null);
