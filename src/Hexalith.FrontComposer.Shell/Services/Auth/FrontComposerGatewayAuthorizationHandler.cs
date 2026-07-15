using System.Net.Http.Headers;
using System.Security.Claims;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Attaches the signed-in user's access token as a bearer header on outbound gateway requests.
/// Resolves the current principal from <see cref="IHttpContextAccessor"/> during server-side render
/// and from the circuit's <see cref="AuthenticationStateProvider"/> (via
/// <see cref="CircuitServicesAccessor"/>) during interactive circuit activity. Anonymous requests are
/// left untouched, so the gateway returns 401 and the UI surfaces the sign-in state.
/// </summary>
public sealed class FrontComposerGatewayAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    FrontComposerUserTokenStore tokenStore) : DelegatingHandler {
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null) {
            string? userId = await ResolveUserIdAsync().ConfigureAwait(false);
            if (userId is not null && tokenStore.TryGet(userId, out string token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> ResolveUserIdAsync() {
        ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true) {
            if (circuitServicesAccessor.Services?.GetService(typeof(AuthenticationStateProvider)) is AuthenticationStateProvider provider) {
                AuthenticationState state = await provider.GetAuthenticationStateAsync().ConfigureAwait(false);
                user = state.User;
            }
        }

        return user?.Identity?.IsAuthenticated == true
            ? user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;
    }
}
