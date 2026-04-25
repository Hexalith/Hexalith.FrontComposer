using System.Collections.Generic;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 D14 — bounded plain-text projection of an RFC 7807
/// <c>application/problem+json</c> payload. Built by the framework classifier so UI
/// emitters never see the raw HTTP body.
/// </summary>
/// <remarks>
/// All string fields are server-derived but rendered as plain text only — never inserted
/// via <c>MarkupString</c> or <c>InnerHtml</c>. The validation map carries field-keyed error
/// lists; consumers map keys through their own allowlists (per-command property names) and
/// degrade to <see cref="GlobalErrors"/> for unrecognised paths.
/// </remarks>
/// <param name="Title">Optional ProblemDetails <c>title</c> (plain text).</param>
/// <param name="Detail">Optional ProblemDetails <c>detail</c> (plain text).</param>
/// <param name="Status">Optional ProblemDetails <c>status</c> code.</param>
/// <param name="EntityLabel">Optional plain-text entity label extracted from extension members.</param>
/// <param name="ValidationErrors">Field-keyed error message lists; never null. Empty when absent.</param>
/// <param name="GlobalErrors">Form-level error messages without a field association; never null.</param>
public sealed record ProblemDetailsPayload(
    string? Title,
    string? Detail,
    int? Status,
    string? EntityLabel,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ValidationErrors,
    IReadOnlyList<string> GlobalErrors) {
    /// <summary>An empty payload, used when the server returned no parseable body.</summary>
    public static ProblemDetailsPayload Empty { get; } = new(
        Title: null,
        Detail: null,
        Status: null,
        EntityLabel: null,
        ValidationErrors: new Dictionary<string, IReadOnlyList<string>>(System.StringComparer.Ordinal),
        GlobalErrors: System.Array.Empty<string>());
}
