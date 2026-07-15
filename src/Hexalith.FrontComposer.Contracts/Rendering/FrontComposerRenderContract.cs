using Hexalith.FrontComposer.Contracts.Schema;

namespace Hexalith.FrontComposer.Contracts.Rendering;

/// <summary>
/// Identifies the output surface targeted by a FrontComposer rendering contract.
/// </summary>
public enum RenderSurfaceKind {
    /// <summary>Interactive Blazor web output.</summary>
    WebBlazor,

    /// <summary>Markdown output exposed through an MCP projection resource.</summary>
    McpMarkdown,

    /// <summary>Markdown output exposed through an agent skill resource.</summary>
    SkillResourceMarkdown,

    /// <summary>A reserved surface for forward-compatible renderers.</summary>
    Future,
}

/// <summary>
/// Identifies a rendering capability supported by a FrontComposer surface contract.
/// </summary>
public enum RenderCapability {
    /// <summary>Renders projection rows and columns as a table.</summary>
    ProjectionTable,

    /// <summary>Renders a projection as a status overview.</summary>
    ProjectionStatusOverview,

    /// <summary>Renders a projection as a chronological timeline.</summary>
    ProjectionTimeline,

    /// <summary>Renders an explicit empty state when no projection data is available.</summary>
    EmptyState,

    /// <summary>Produces Markdown constrained by the declared render bounds.</summary>
    BoundedMarkdown,

    /// <summary>Sanitizes untrusted values into inert text before rendering.</summary>
    SanitizedInertText,
}

/// <summary>
/// Defines quantitative output limits for a rendering contract.
/// </summary>
/// <param name="MaxRows">The maximum number of rows included in one rendered result.</param>
/// <param name="MaxColumns">The maximum number of columns included in one rendered result.</param>
/// <param name="MaxCharacters">The maximum number of characters included in the complete rendered result.</param>
/// <param name="MaxFieldCharacters">The maximum number of characters included for one rendered field.</param>
public sealed record RenderBounds(
    int MaxRows,
    int MaxColumns,
    int MaxCharacters,
    int MaxFieldCharacters);

/// <summary>
/// Describes the versioned capabilities, schema identity, and output limits of one FrontComposer renderer.
/// </summary>
/// <param name="ContractId">The stable identifier of the rendering contract.</param>
/// <param name="ContractSchemaVersion">The schema version used to interpret the rendering contract.</param>
/// <param name="Surface">The output surface targeted by the renderer.</param>
/// <param name="OutputContentType">The media type produced by the renderer.</param>
/// <param name="Capabilities">The rendering capabilities supported by the surface.</param>
/// <param name="Fingerprint">The structural fingerprint of the rendered contract schema.</param>
/// <param name="Bounds">The quantitative limits applied to rendered output.</param>
/// <param name="Metadata">Optional renderer-specific metadata keyed by stable names.</param>
public sealed record FrontComposerRenderContract(
    string ContractId,
    string ContractSchemaVersion,
    RenderSurfaceKind Surface,
    string OutputContentType,
    IReadOnlyList<RenderCapability> Capabilities,
    SchemaFingerprint Fingerprint,
    RenderBounds Bounds,
    IReadOnlyDictionary<string, string>? Metadata = null);
