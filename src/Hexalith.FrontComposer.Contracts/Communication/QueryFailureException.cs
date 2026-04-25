using System;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 AC1 — thrown by <see cref="IQueryService"/> implementations when EventStore
/// returns 403 / 404 / 429 (warning class) or any other non-200 / non-304 / non-401 status.
/// Distinct from <see cref="HttpRequestException"/> so consumers (DataGrid load effect,
/// badge readers) can react with the preserve-currently-visible policy without stringly
/// typed status parsing.
/// </summary>
/// <remarks>
/// HTTP 401 is signalled separately via <see cref="AuthRedirectRequiredException"/> so the
/// auth-redirect seam stays the single explicit decision path for unauthenticated state.
/// </remarks>
public class QueryFailureException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="QueryFailureException"/> class.
    /// </summary>
    /// <param name="kind">The query failure kind.</param>
    /// <param name="problem">Parsed ProblemDetails (plain text only) — pass <see cref="ProblemDetailsPayload.Empty"/> when no body was returned.</param>
    /// <param name="retryAfter">Optional retry hint; only meaningful for <see cref="QueryFailureKind.RateLimited"/>.</param>
    public QueryFailureException(
        QueryFailureKind kind,
        ProblemDetailsPayload problem,
        TimeSpan? retryAfter = null)
        : base(problem?.Title ?? kind.ToString()) {
        Kind = kind;
        Problem = problem ?? throw new ArgumentNullException(nameof(problem));
        RetryAfter = retryAfter;
    }

    /// <summary>Gets the failure kind.</summary>
    public QueryFailureKind Kind { get; }

    /// <summary>Gets the parsed ProblemDetails payload.</summary>
    public ProblemDetailsPayload Problem { get; }

    /// <summary>Gets the retry hint (HTTP <c>Retry-After</c>) when present.</summary>
    public TimeSpan? RetryAfter { get; }
}
