using System.Text.Json.Serialization;

namespace Hexalith.FrontComposer.Shell.State.ETagCache;

/// <summary>
/// Story 5-2 D13 — persisted ETag cache entry. Carries framework-owned format and projection
/// payload compatibility metadata so a runtime that cannot read the entry treats it as a
/// diagnostic miss (and an uncached retry) rather than as silent stale state.
/// </summary>
/// <remarks>
/// <para>
/// <b>FormatVersion</b> bumps when the cache entry layout itself changes; current value is
/// <see cref="CurrentFormatVersion"/>. <b>PayloadVersion</b> is set by the producer to the
/// projection-payload contract version it understands; readers compare against
/// <see cref="ETagCacheService"/>'s expected payload version per discriminator.
/// </para>
/// <para>
/// <b>Payload</b> is the raw JSON UTF-8 string returned by the server. Re-serialising on
/// retrieval is intentional — the entry must round-trip identically, and re-encoding via
/// reflection-based <c>System.Text.Json</c> would lose adopter-specific extension members.
/// </para>
/// </remarks>
/// <param name="ETag">The server-supplied ETag validator. Never null or whitespace.</param>
/// <param name="Payload">The raw JSON payload returned with the original 200 OK response.</param>
/// <param name="CachedAtUtcTicks">UTC ticks when the entry was first written.</param>
/// <param name="LastAccessedUtcTicks">UTC ticks of the most recent successful read; updated by the cache service on hit.</param>
/// <param name="FormatVersion">Cache entry layout version (bump when shape changes).</param>
/// <param name="PayloadVersion">Projection payload contract version recognised by the producer.</param>
/// <param name="Discriminator">The framework-allowlisted discriminator — diagnostic only.</param>
public sealed record ETagCacheEntry(
    [property: JsonPropertyName("etag")] string ETag,
    [property: JsonPropertyName("payload")] string Payload,
    [property: JsonPropertyName("cachedAt")] long CachedAtUtcTicks,
    [property: JsonPropertyName("lastAccessed")] long LastAccessedUtcTicks,
    [property: JsonPropertyName("formatVersion")] int FormatVersion,
    [property: JsonPropertyName("payloadVersion")] int PayloadVersion,
    [property: JsonPropertyName("discriminator")] string Discriminator) {
    /// <summary>
    /// Current cache entry layout version. Bump when the persisted shape changes so older
    /// entries are treated as diagnostic misses.
    /// </summary>
    public const int CurrentFormatVersion = 1;
}
