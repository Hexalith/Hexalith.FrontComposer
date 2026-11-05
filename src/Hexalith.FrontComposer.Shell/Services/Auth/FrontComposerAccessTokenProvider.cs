using System.Security.Claims;

using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

public sealed class FrontComposerAccessTokenProvider(
    IHttpContextAccessor httpContextAccessor,
    CircuitServicesAccessor circuitServicesAccessor,
    FrontComposerUserTokenStore tokenStore,
    IOptions<FrontComposerAuthenticationOptions> options,
    ILogger<FrontComposerAccessTokenProvider> logger) {
    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        FrontComposerAuthenticationOptions current = options.Value;

        if (current.SelectedProviderKind == FrontComposerAuthenticationProviderKind.GitHubOAuth
            && current.TokenRelay.HostAccessTokenProvider is null
            && !current.TokenRelay.AllowGitHubOAuthTokenRelay) {
            FrontComposerSecurityLog.GitHubTokenExchangeRequired(logger);
            throw new FrontComposerAuthenticationException(
                FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired,
                $"{FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired}: GitHub OAuth requires a brokered EventStore bearer token provider.");
        }

        try {
            string? token = current.TokenRelay.HostAccessTokenProvider is not null
                ? await current.TokenRelay.HostAccessTokenProvider(cancellationToken).ConfigureAwait(false)
                : await ReadHttpContextOrCircuitTokenAsync(current).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token)) {
                // P6 — log HFC2013 on the empty-token branch so the diagnostic is observable.
                FrontComposerSecurityLog.AccessTokenMissing(
                    logger,
                    BoundedProviderKind(current.SelectedProviderKind));
                throw new FrontComposerAuthenticationException(
                    FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
                    $"{FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed}: Access token acquisition returned no token.");
            }

            return token;
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (FrontComposerAuthenticationException) {
            throw;
        }
        // P6 — narrow exception filter. Catching only the HTTP/IO/auth exceptions adopters can
        // realistically throw from a token provider; let SO/AccessViolation/ThreadAbort/OOM
        // unwind. Pass `ex` as inner exception so operators retain the root cause.
        catch (Exception ex) when (ex is HttpRequestException
                                   or TaskCanceledException
                                   or TimeoutException
                                   or InvalidOperationException
                                   or AuthenticationFailureException) {
            FrontComposerSecurityLog.AccessTokenAcquisitionFailed(
                logger,
                ex.GetType().FullName ?? "Exception");
            throw new FrontComposerAuthenticationException(
                FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
                $"{FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed}: Access token acquisition failed.",
                ex);
        }
    }

    internal async ValueTask<Func<CancellationToken, ValueTask<string?>>?> CaptureCurrentUserAccessTokenProviderAsync(
        CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        FrontComposerAuthenticationOptions current = options.Value;
        if (current.TokenRelay.HostAccessTokenProvider is not null) {
            return current.TokenRelay.HostAccessTokenProvider;
        }

        AuthenticationStateProvider? capturedState = circuitServicesAccessor.Services?.GetService<AuthenticationStateProvider>();
        ClaimsPrincipal? principal = await FrontComposerTokenSession.ReadPrincipalAsync(httpContextAccessor, circuitServicesAccessor).ConfigureAwait(false);
        FrontComposerTokenSession? session = FrontComposerTokenSession.Resolve(principal, current);
        // Always return a bound delegate. A null capture lets reconnect fall back to ambient identity.
        return async ct =>
        {
            ct.ThrowIfCancellationRequested();
            ClaimsPrincipal? user = capturedState is not null
                ? (await capturedState.GetAuthenticationStateAsync().ConfigureAwait(false)).User
                : principal;
            return session is not null
                && session == FrontComposerTokenSession.Resolve(user, current)
                && tokenStore.TryGet(session, out string accessToken) ? accessToken : null;
        };
    }

    private async Task<string?> ReadHttpContextOrCircuitTokenAsync(FrontComposerAuthenticationOptions current)
    {
        ClaimsPrincipal? principal = await FrontComposerTokenSession.ReadPrincipalAsync(httpContextAccessor, circuitServicesAccessor).ConfigureAwait(false);
        FrontComposerTokenSession? session = FrontComposerTokenSession.Resolve(principal, current);
        if (session is not null)
        {
            return tokenStore.TryGet(session, out string token) ? token : null;
        }

        // Server circuits must never recover a revoked, expired or unbound token from cookie properties.
        if (current.TokenRelay.CircuitTokenSourceEnabled || circuitServicesAccessor.Services is not null
            || principal?.HasClaim(c => c.Type == FrontComposerTokenSession.ClaimType) == true)
        {
            return null;
        }

        if (httpContextAccessor.HttpContext is not { } context)
        {
            return null;
        }

        AuthenticateResult result = await context.AuthenticateAsync(current.TokenRelay.AuthenticationScheme).ConfigureAwait(false);
        if (!result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true
            || (result.Properties?.GetTokenValue("expires_at") is { } expiresAt
                && (!DateTimeOffset.TryParse(expiresAt, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal, out DateTimeOffset expiry)
                    || expiry <= tokenStore.UtcNow)))
        {
            return null;
        }

        return result.Properties?.GetTokenValue(current.TokenRelay.AccessTokenName);
    }

    private static string BoundedProviderKind(FrontComposerAuthenticationProviderKind providerKind)
        => providerKind switch {
            FrontComposerAuthenticationProviderKind.None => nameof(FrontComposerAuthenticationProviderKind.None),
            FrontComposerAuthenticationProviderKind.OpenIdConnect => nameof(FrontComposerAuthenticationProviderKind.OpenIdConnect),
            FrontComposerAuthenticationProviderKind.Saml2 => nameof(FrontComposerAuthenticationProviderKind.Saml2),
            FrontComposerAuthenticationProviderKind.GitHubOAuth => nameof(FrontComposerAuthenticationProviderKind.GitHubOAuth),
            FrontComposerAuthenticationProviderKind.CustomBrokered => nameof(FrontComposerAuthenticationProviderKind.CustomBrokered),
            _ => "Unknown",
        };
}
