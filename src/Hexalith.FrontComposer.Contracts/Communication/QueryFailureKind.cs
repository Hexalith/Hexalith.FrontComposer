namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 AC1 — query-side failure taxonomy. Distinct from
/// <see cref="CommandWarningKind"/> so consumers (DataGrid, badge readers) can react with
/// the right preserve-vs-clear policy on 304/401/403/404/429.
/// </summary>
public enum QueryFailureKind {
    /// <summary>
    /// HTTP 401 Unauthorized — caller credentials missing or expired. Triggers the
    /// <see cref="IAuthRedirector"/> seam; cached payloads MUST NOT be used as a
    /// speculative fallback for an auth failure.
    /// </summary>
    Unauthorized,

    /// <summary>HTTP 403 Forbidden — render warning banner; preserve currently visible data.</summary>
    Forbidden,

    /// <summary>HTTP 404 Not Found — render warning banner; preserve currently visible data.</summary>
    NotFound,

    /// <summary>HTTP 429 Too Many Requests — preserve currently visible data; surface retry-after.</summary>
    RateLimited,
}
