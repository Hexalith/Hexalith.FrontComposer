namespace Hexalith.FrontComposer.Mcp.Skills;

/// <summary>
/// Result of <see cref="FrontComposerSkillResourceProvider.Read(string, CancellationToken)"/>.
/// P-13: failure shapes return a stable opaque category token rather than the raw enum name so
/// that wire-level callers cannot rely on internal naming and so that hidden-equivalent failure
/// surfaces (per Story 8-4a DN-2) remain indistinguishable.
/// </summary>
public sealed record SkillResourceReadResult {
    private SkillResourceReadResult(
        bool isSuccess,
        FrontComposerMcpFailureCategory category,
        string contentType,
        string markdown) {
        IsSuccess = isSuccess;
        Category = category;
        ContentType = contentType;
        Markdown = markdown;
    }

    public bool IsSuccess { get; }

    public FrontComposerMcpFailureCategory Category { get; }

    public string ContentType { get; }

    public string Markdown { get; }

    public static SkillResourceReadResult Success(string markdown)
        => new(true, FrontComposerMcpFailureCategory.None, "text/markdown", markdown);

    public static SkillResourceReadResult Failure(FrontComposerMcpFailureCategory category)
        => new(false, category, "text/plain", FailurePublicToken(category));

    private static string FailurePublicToken(FrontComposerMcpFailureCategory category)
        => category switch {
            FrontComposerMcpFailureCategory.Canceled => "canceled",
            FrontComposerMcpFailureCategory.MalformedRequest => "malformed_request",
            FrontComposerMcpFailureCategory.SkillResourceTooLarge => "response_too_large",
            // Hidden-equivalent surface for unknown / authorization / tenant / policy failures.
            _ => "unknown_resource",
        };
}
