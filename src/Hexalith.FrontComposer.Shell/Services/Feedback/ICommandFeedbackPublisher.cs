using System;

using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Services.Feedback;

/// <summary>
/// Story 5-2 D6 / T5 — framework-owned warning channel that lets generated forms (and any
/// other Shell consumer) surface 403 / 404 / 429 responses through a MessageBar without
/// overloading <c>FcLifecycleWrapper</c>'s lifecycle states.
/// </summary>
/// <remarks>
/// Implementations are scoped per circuit / per user. Subscribers detach by disposing the
/// returned <see cref="IDisposable"/> token. Warning publication MUST NOT mutate the ETag
/// cache, lifecycle state, or validation state — those are explicitly out of scope per D6.
/// </remarks>
public interface ICommandFeedbackPublisher {
    /// <summary>Publishes a warning notification to all current subscribers.</summary>
    void PublishWarning(CommandFeedbackWarning warning);

    /// <summary>Subscribes to warning notifications. Dispose the returned token to unsubscribe.</summary>
    IDisposable Subscribe(Action<CommandFeedbackWarning> handler);
}

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
