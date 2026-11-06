using System.Net.Http.Headers;
using System.Security.Claims;

using Hexalith.FrontComposer.Shell.Options;

using Microsoft.Extensions.Options;

using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

/// <summary>
/// Attaches the signed-in user's access token as a bearer header on outbound gateway requests.
/// Resolves the active circuit's <see cref="AuthenticationStateProvider"/> (via
/// <see cref="CircuitServicesAccessor"/>), falling back to <see cref="IHttpContextAccessor"/> during
/// server-side rendering. Anonymous requests are
/// left untouched, so the gateway returns 401 and the UI surfaces the sign-in state.
/// </summary>
public sealed class FrontComposerGatewayAuthorizationHandler(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    FrontComposerUserTokenStore tokenStore,
    IOptions<FrontComposerAuthenticationOptions> options) : DelegatingHandler {
    /// <summary>
    /// Preserves the published constructor using default authentication options. Defaults contain no
    /// Tenant claim aliases, so token relay requires the configured four-argument constructor.
    /// </summary>
    public FrontComposerGatewayAuthorizationHandler(
        IHttpContextAccessor httpContextAccessor,
        CircuitServicesAccessor circuitServicesAccessor,
        FrontComposerUserTokenStore tokenStore)
        : this(httpContextAccessor, circuitServicesAccessor, tokenStore,
            Microsoft.Extensions.Options.Options.Create(new FrontComposerAuthenticationOptions()))
    {
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.Authorization is null) {
            ClaimsPrincipal? principal = await FrontComposerTokenSession.ReadPrincipalAsync(httpContextAccessor, circuitServicesAccessor).ConfigureAwait(false);
            FrontComposerTokenSession? session = FrontComposerTokenSession.Resolve(principal, options.Value);
            if (session is not null && tokenStore.TryGet(session, out string token)) {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

}
