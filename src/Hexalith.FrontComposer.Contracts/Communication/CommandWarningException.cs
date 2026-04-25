using System;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 D6 — thrown by <see cref="ICommandService"/> when the server returns 403, 404,
/// or 429. Generated forms route this through a framework-owned warning MessageBar separate
/// from <see cref="FcLifecycleWrapper"/> rejection / acknowledgement state so the lifecycle
/// model is not overloaded with every HTTP warning class.
/// </summary>
/// <remarks>
/// All copy carried by this exception is plain text per Story 5-2 D14. Render via
/// localized templates that take <see cref="ProblemDetailsPayload.Title"/> /
/// <see cref="ProblemDetailsPayload.Detail"/> / <see cref="ProblemDetailsPayload.EntityLabel"/>
/// and never via <c>MarkupString</c>.
/// </remarks>
public class CommandWarningException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandWarningException"/> class.
    /// </summary>
    /// <param name="kind">The warning kind (Forbidden / NotFound / RateLimited).</param>
    /// <param name="problem">Parsed ProblemDetails (plain-text body).</param>
    /// <param name="retryAfter">Optional retry hint; only meaningful for <see cref="CommandWarningKind.RateLimited"/>.</param>
    public CommandWarningException(
        CommandWarningKind kind,
        ProblemDetailsPayload problem,
        TimeSpan? retryAfter = null)
        : base(problem?.Title ?? kind.ToString()) {
        Kind = kind;
        Problem = problem ?? throw new ArgumentNullException(nameof(problem));
        RetryAfter = retryAfter;
    }

    /// <summary>Gets the warning kind.</summary>
    public CommandWarningKind Kind { get; }

    /// <summary>Gets the parsed ProblemDetails payload (plain text only).</summary>
    public ProblemDetailsPayload Problem { get; }

    /// <summary>Gets the optional retry hint (HTTP <c>Retry-After</c>).</summary>
    public TimeSpan? RetryAfter { get; }
}
