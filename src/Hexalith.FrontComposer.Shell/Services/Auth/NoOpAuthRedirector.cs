using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts.Communication;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Story 5-2 D8 — default <see cref="IAuthRedirector"/> that throws
/// <see cref="System.InvalidOperationException"/>. The framework refuses to silently swallow
/// HTTP 401 responses because that would let an authenticated-looking UI accumulate edits
/// that the server has already declined to accept. Adopters MUST register a real redirector
/// (via <c>services.Replace</c>) before EventStore-backed commands or queries can reach
/// production.
/// </summary>
public sealed class NoOpAuthRedirector : IAuthRedirector {
    /// <inheritdoc />
    public Task RedirectAsync(string? returnUrl = null, CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        throw new System.InvalidOperationException(
            "EventStore returned HTTP 401 Unauthorized but no IAuthRedirector implementation is registered. "
            + "Register an adopter-supplied redirector (e.g. one that calls NavigationManager.NavigateTo) "
            + "via services.Replace<IAuthRedirector, ...>().");
    }
}
