using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Services.Feedback;

/// <summary>
/// Story 5-2 T5 — payload published through <see cref="ICommandFeedbackPublisher"/> for each
/// 403 / 404 / 429 response. All copy is plain text per D14.
/// </summary>
/// <param name="Kind">The warning kind.</param>
/// <param name="Title">Optional ProblemDetails title.</param>
/// <param name="Detail">Optional ProblemDetails detail.</param>
/// <param name="EntityLabel">Optional plain-text entity label.</param>
/// <param name="RetryAfter">Optional retry hint (seconds) — only meaningful for <see cref="CommandWarningKind.RateLimited"/>.</param>
public sealed record CommandFeedbackWarning(
    CommandWarningKind Kind,
    string? Title,
    string? Detail,
    string? EntityLabel,
    TimeSpan? RetryAfter);
