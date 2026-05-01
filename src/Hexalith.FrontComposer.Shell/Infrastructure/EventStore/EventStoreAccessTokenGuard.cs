using Hexalith.FrontComposer.Contracts.Diagnostics;
using Hexalith.FrontComposer.Shell.Services.Auth;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal static class EventStoreAccessTokenGuard {
    /// <summary>
    /// Returns the next bearer token. When `requireAccessToken` is true and the provider returns
    /// null/whitespace, throws `FrontComposerAuthenticationException(HFC2013)`. When the flag is
    /// false, normalizes whitespace tokens to null instead of returning the whitespace value.
    /// </summary>
    public static async ValueTask<string?> GetRequiredTokenAsync(
        Func<CancellationToken, ValueTask<string?>> provider,
        bool requireAccessToken,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(provider);
        string? token = await provider(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token)) {
            if (requireAccessToken) {
                throw new FrontComposerAuthenticationException(
                    FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed,
                    $"{FcDiagnosticIds.HFC2013_AuthenticationTokenRelayFailed}: EventStore access token provider returned an empty token.");
            }

            return null;
        }

        return token;
    }
}
