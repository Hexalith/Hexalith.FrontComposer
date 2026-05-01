namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

internal static class EventStoreAccessTokenGuard {
    public static async ValueTask<string?> GetRequiredTokenAsync(
        Func<CancellationToken, ValueTask<string?>> provider,
        bool requireAccessToken,
        CancellationToken cancellationToken) {
        string? token = await provider(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token) && requireAccessToken) {
            throw new InvalidOperationException("EventStore access token provider returned an empty token.");
        }

        return token;
    }
}
