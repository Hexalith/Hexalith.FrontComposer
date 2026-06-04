namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Plain-text command rejection metadata surfaced to generated command forms.
/// </summary>
/// <param name="ErrorCode">Stable rejection code, or a framework fallback when absent.</param>
/// <param name="ReasonCategory">Domain/category grouping for the rejection reason.</param>
/// <param name="SuggestedAction">Operator-facing recovery guidance.</param>
/// <param name="DocsCode">Documentation lookup code associated with the rejection.</param>
public sealed record CommandRejectionDetails(
    string ErrorCode,
    string ReasonCategory,
    string SuggestedAction,
    string DocsCode) {
    /// <summary>Fallback error code used when the server does not provide one.</summary>
    public const string UnknownErrorCode = "COMMAND_REJECTED";

    /// <summary>Fallback reason category used when the server does not provide one.</summary>
    public const string UnknownReasonCategory = "Domain";

    /// <summary>Fallback operator guidance used when neither typed guidance nor resolution copy is present.</summary>
    public const string UnknownSuggestedAction = "Review your input and try again.";

    /// <summary>Fallback documentation code used when the server does not provide one.</summary>
    public const string UnknownDocsCode = "not-available";

    /// <summary>
    /// Builds a details value with non-null plain-text fallback fields.
    /// </summary>
    /// <param name="errorCode">Optional stable rejection code.</param>
    /// <param name="reasonCategory">Optional rejection category.</param>
    /// <param name="suggestedAction">Optional recovery guidance.</param>
    /// <param name="docsCode">Optional documentation code.</param>
    /// <param name="fallbackSuggestedAction">Fallback recovery guidance, usually the legacy resolution string.</param>
    /// <returns>A rejection details value with no null fields.</returns>
    public static CommandRejectionDetails FromOptional(
        string? errorCode,
        string? reasonCategory,
        string? suggestedAction,
        string? docsCode,
        string? fallbackSuggestedAction = null)
        => new(
            Normalize(errorCode, UnknownErrorCode),
            Normalize(reasonCategory, UnknownReasonCategory),
            Normalize(suggestedAction, Normalize(fallbackSuggestedAction, UnknownSuggestedAction)),
            Normalize(docsCode, UnknownDocsCode));

    private static string Normalize(string? value, string fallback)
        => string.IsNullOrWhiteSpace(value) ? fallback : value!;
}
