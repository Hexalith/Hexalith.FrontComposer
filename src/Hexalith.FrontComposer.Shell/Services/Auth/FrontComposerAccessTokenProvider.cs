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

        string? httpToken = null;
        ClaimsPrincipal? principal = null;
        if (httpContextAccessor.HttpContext is { } context) {
            httpToken = await ReadHttpContextTokenAsync(context, current).ConfigureAwait(false);
            principal = context.User;
        }

        principal ??= await ReadCircuitPrincipalAsync().ConfigureAwait(false);
        string? userId = ResolveUserId(principal, current);
        if (userId is not null) {
            return ct => {
                ct.ThrowIfCancellationRequested();
                return ValueTask.FromResult(tokenStore.TryGet(userId, out string accessToken) ? accessToken : null);
            };
        }

        return string.IsNullOrWhiteSpace(httpToken)
            ? null
            : ct => {
                ct.ThrowIfCancellationRequested();
                return ValueTask.FromResult<string?>(httpToken);
            };
    }

    private async Task<string?> ReadHttpContextOrCircuitTokenAsync(FrontComposerAuthenticationOptions current) {
        ClaimsPrincipal? principal = null;
        if (httpContextAccessor.HttpContext is { } context) {
            string? httpToken = await ReadHttpContextTokenAsync(context, current).ConfigureAwait(false);
            if (!string.IsNullOrWhiteSpace(httpToken)) {
                return httpToken;
            }

            principal = context.User;
        }

        principal ??= await ReadCircuitPrincipalAsync().ConfigureAwait(false);
        string? userId = ResolveUserId(principal, current);
        return userId is not null && tokenStore.TryGet(userId, out string circuitToken)
            ? circuitToken
            : null;
    }

    private static async Task<string?> ReadHttpContextTokenAsync(
        HttpContext context,
        FrontComposerAuthenticationOptions current)
        => string.IsNullOrWhiteSpace(current.TokenRelay.AuthenticationScheme)
            ? await context.GetTokenAsync(current.TokenRelay.AccessTokenName).ConfigureAwait(false)
            : await context.GetTokenAsync(current.TokenRelay.AuthenticationScheme, current.TokenRelay.AccessTokenName).ConfigureAwait(false);

    private async Task<ClaimsPrincipal?> ReadCircuitPrincipalAsync() {
        if (circuitServicesAccessor.Services?.GetService<AuthenticationStateProvider>() is not { } provider) {
            return null;
        }

        AuthenticationState state = await provider.GetAuthenticationStateAsync().ConfigureAwait(false);
        return state.User;
    }

    private static string? ResolveUserId(ClaimsPrincipal? user, FrontComposerAuthenticationOptions current) {
        if (user?.Identity?.IsAuthenticated != true) {
            return null;
        }

        string? stable = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(stable)) {
            return stable;
        }

        foreach (string claimType in current.UserClaimTypes) {
            string? value = user.FindFirstValue(claimType);
            if (!string.IsNullOrWhiteSpace(value)) {
                return value;
            }
        }

        return null;
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
