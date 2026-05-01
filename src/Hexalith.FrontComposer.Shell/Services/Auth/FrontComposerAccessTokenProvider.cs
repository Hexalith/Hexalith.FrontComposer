using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Options;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.Services.Auth;

public sealed class FrontComposerAccessTokenProvider(
    IHttpContextAccessor httpContextAccessor,
    IOptions<FrontComposerAuthenticationOptions> options,
    ILogger<FrontComposerAccessTokenProvider> logger) {
    public async ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) {
        cancellationToken.ThrowIfCancellationRequested();
        FrontComposerAuthenticationOptions current = options.Value;

        if (current.SelectedProviderKind == FrontComposerAuthenticationProviderKind.GitHubOAuth
            && current.TokenRelay.HostAccessTokenProvider is null
            && !current.TokenRelay.AllowGitHubOAuthTokenRelay) {
            logger.LogWarning(
                "{DiagnosticId}: GitHub OAuth sign-in cannot be relayed as EventStore bearer token without a broker.",
                FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired);
            throw new FrontComposerAuthenticationException(
                FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired,
                $"{FcDiagnosticIds.HFC2014_GitHubTokenExchangeRequired}: GitHub OAuth requires a brokered EventStore bearer token provider.");
        }

        try {
            string? token = current.TokenRelay.HostAccessTokenProvider is not null
                ? await current.TokenRelay.HostAccessTokenProvider(cancellationToken).ConfigureAwait(false)
                : await ReadHttpContextTokenAsync(current).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(token)) {
                // P6 — log HFC2013 on the empty-token branch so the diagnostic is observable.
                logger.LogWarning(
                    "{DiagnosticId}: Access token acquisition returned no token. ProviderKind={ProviderKind}.",
                    FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
                    current.SelectedProviderKind.ToString());
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
            logger.LogWarning(
                "{DiagnosticId}: Access token acquisition failed. FailureCategory={FailureCategory}.",
                FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
                ex.GetType().Name);
            throw new FrontComposerAuthenticationException(
                FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
                $"{FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed}: Access token acquisition failed.",
                ex);
        }
    }

    private async Task<string?> ReadHttpContextTokenAsync(FrontComposerAuthenticationOptions current) {
        HttpContext context = httpContextAccessor.HttpContext
            ?? throw new FrontComposerAuthenticationException(
                FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
                $"{FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed}: HTTP context is unavailable for token relay.");
        return string.IsNullOrWhiteSpace(current.TokenRelay.AuthenticationScheme)
            ? await context.GetTokenAsync(current.TokenRelay.AccessTokenName).ConfigureAwait(false)
            : await context.GetTokenAsync(current.TokenRelay.AuthenticationScheme, current.TokenRelay.AccessTokenName).ConfigureAwait(false);
    }
}
