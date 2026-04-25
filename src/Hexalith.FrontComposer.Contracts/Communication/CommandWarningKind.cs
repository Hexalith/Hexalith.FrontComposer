namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 D6 — warning-class command failures rendered through a framework-owned
/// MessageBar separate from <c>FcLifecycleWrapper</c> and the domain-rejection bar.
/// </summary>
public enum CommandWarningKind {
    /// <summary>
    /// HTTP 403 Forbidden — the server understood the command but the user lacks the
    /// required permission. Render as a warning banner that names the missing permission
    /// scope when the server payload carries it.
    /// </summary>
    Forbidden,

    /// <summary>
    /// HTTP 404 Not Found — the target entity no longer exists or was never visible.
    /// Inline warning with a navigation hint when one is available.
    /// </summary>
    NotFound,

    /// <summary>
    /// HTTP 429 Too Many Requests — caller is rate-limited. Render the warning with
    /// retry guidance from <see cref="CommandWarningException.RetryAfter"/> when present.
    /// </summary>
    RateLimited,
}
