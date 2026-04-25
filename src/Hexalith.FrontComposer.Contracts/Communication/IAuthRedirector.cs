using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.FrontComposer.Contracts.Communication;

/// <summary>
/// Story 5-2 D8 — framework-owned auth-redirect seam invoked when EventStore returns
/// HTTP 401 Unauthorized. Adopters register a real implementation that bridges their
/// authentication stack (e.g. a Blazor <c>NavigationManager.NavigateTo</c> to
/// <c>/authentication/login</c>, or a SAML / OIDC challenge endpoint).
/// </summary>
/// <remarks>
/// <para>
/// The default Shell registration is <c>NoOpAuthRedirector</c>, which throws an
/// <see cref="System.InvalidOperationException"/>. The framework refuses to silently swallow
/// 401s because that would let an authenticated-looking UI accumulate edits the server has
/// already declined to accept.
/// </para>
/// <para>
/// Implementations MUST NOT mutate the ETag cache, lifecycle state, or generated-form
/// validation state — those are explicitly out of scope for the redirect path.
/// </para>
/// </remarks>
public interface IAuthRedirector {
    /// <summary>
    /// Triggers the host's authentication redirect (typically a server challenge or
    /// client-side <c>NavigationManager.NavigateTo</c>).
    /// </summary>
    /// <param name="returnUrl">Optional return URL the host may use to round-trip the user back to the originating page.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous redirect.</returns>
    Task RedirectAsync(string? returnUrl = null, CancellationToken cancellationToken = default);
}
