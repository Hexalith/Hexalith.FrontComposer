using System;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 D8 — thrown by <see cref="ICommandService"/> / <see cref="IQueryService"/>
/// implementations when the server returns HTTP 401 Unauthorized. The request that triggered
/// this exception is abandoned: no cache mutation, no lifecycle rejection, no validation
/// pollution, and no automatic retry. Generated forms invoke <see cref="IAuthRedirector"/>
/// instead of mapping the failure to a warning MessageBar.
/// </summary>
public class AuthRedirectRequiredException : Exception {
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthRedirectRequiredException"/> class.
    /// </summary>
    /// <param name="reason">Optional plain-text reason string; never rendered via <c>MarkupString</c>.</param>
    public AuthRedirectRequiredException(string? reason = null)
        : base(reason ?? "EventStore returned 401 Unauthorized — authentication redirect required.") {
    }
}
