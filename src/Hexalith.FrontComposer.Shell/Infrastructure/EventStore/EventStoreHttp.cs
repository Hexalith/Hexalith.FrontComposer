using System.Net.Http.Headers;
using System.Text;

using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;

using Microsoft.Extensions.Logging;

namespace Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

/// <summary>
/// Shared HTTP plumbing for the EventStore command, query, and pending-command status clients.
/// Centralises the bearer-token authorization and bounded response-body reading so the auth
/// and byte-bounding safety discipline cannot drift between callers (Story 3.5 review: the
/// status query previously copied both helpers verbatim from <see cref="EventStoreQueryClient"/>).
/// </summary>
internal static class EventStoreHttp {
    /// <summary>
    /// Applies the configured EventStore bearer token to <paramref name="request"/>, honouring
    /// <see cref="EventStoreOptions.RequireAccessToken"/>. Sends no <c>Authorization</c> header
    /// when no provider is configured (or it returns an empty token) and the token is optional.
    /// </summary>
    public static async Task ApplyAuthorizationAsync(
        HttpRequestMessage request,
        EventStoreOptions options,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        if (options.AccessTokenProvider is null) {
            if (options.RequireAccessToken) {
                throw new InvalidOperationException("EventStore access token provider is required.");
            }

            return;
        }

        string? token = await options.AccessTokenProvider(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(token)) {
            if (options.RequireAccessToken) {
                throw new InvalidOperationException("EventStore access token provider returned an empty token.");
            }

            return;
        }

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Reads an EventStore response body into a string, refusing to buffer more than
    /// <paramref name="maxResponseBytes"/> so a hostile or misbehaving server cannot exhaust
    /// process memory. Falls back to UTF-8 (logging once) when the server declares an
    /// unrecognised charset.
    /// </summary>
    public static async Task<string> ReadBoundedResponseBodyAsync(
        HttpContent content,
        int maxResponseBytes,
        ILogger logger,
        CancellationToken cancellationToken) {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(logger);

        if (maxResponseBytes <= 0) {
            throw new InvalidOperationException("EventStore MaxResponseBytes must be positive.");
        }

        if (content.Headers.ContentLength is { } contentLength && contentLength > maxResponseBytes) {
            throw new InvalidOperationException("EventStore response exceeded MaxResponseBytes.");
        }

        using Stream stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using MemoryStream bounded = new();
        byte[] buffer = new byte[8192];
        while (true) {
            // Cap each read at "remaining budget + 1" so a single ReadAsync cannot pull
            // MaxResponseBytes + 8 KB - 1 bytes off the wire before the cap fires.
            int remaining = maxResponseBytes - (int)bounded.Length + 1;
            int requestSize = Math.Min(buffer.Length, remaining);
            int read = await stream.ReadAsync(buffer.AsMemory(0, requestSize), cancellationToken).ConfigureAwait(false);
            if (read == 0) {
                break;
            }

            if (bounded.Length + read > maxResponseBytes) {
                throw new InvalidOperationException("EventStore response exceeded MaxResponseBytes.");
            }

            bounded.Write(buffer, 0, read);
        }

        Encoding encoding = Encoding.UTF8;
        string? charset = content.Headers.ContentType?.CharSet;
        if (!string.IsNullOrWhiteSpace(charset)) {
            try {
                encoding = Encoding.GetEncoding(charset);
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException) {
                // Explicit signal that an unrecognised charset was rewritten to UTF-8 so operators
                // can correlate downstream mojibake with a server lying about its Content-Type.
                FrontComposerLog.QueryResponseCharsetFallback(logger, ex.GetType().Name);
                encoding = Encoding.UTF8;
            }
        }

        // Avoid bounded.ToArray() which doubles peak memory. GetBuffer() returns the publicly
        // visible underlying array (MemoryStream default ctor enables this).
        return encoding.GetString(bounded.GetBuffer(), 0, (int)bounded.Length);
    }
}
