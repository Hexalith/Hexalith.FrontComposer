using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.FrontComposer.Shell.State.ETagCache;

/// <summary>
/// Story 5-2 T2 — bounded opportunistic ETag cache seam layered above
/// <c>IStorageService</c>. Read paths are non-throwing: every error degrades to a
/// diagnostic miss so the user-visible operation always falls through to the network.
/// </summary>
/// <remarks>
/// <para>
/// All keys go through <see cref="StorageKeys.BuildKey(string,string,string,string)"/> with
/// the <c>"etag"</c> feature segment and a framework-allowlisted discriminator (see
/// <see cref="ETagCacheDiscriminator"/>). Calls supplying null / blank / colon-containing
/// tenant or user identifiers, or non-allowlisted discriminators, must be skipped by the
/// caller via <see cref="TryBuildKey"/> returning <see langword="false"/>.
/// </para>
/// <para>
/// Writes are fire-and-forget (delegate to <c>IStorageService.SetAsync</c>); reads are
/// awaited. Story 3-1's <c>FrontComposerShell.FlushAsync</c> drains pending writes via
/// <c>IStorageService.FlushAsync</c>; Story 5-2 deliberately does not add a second
/// <c>beforeunload</c> hook.
/// </para>
/// </remarks>
public interface IETagCache {
    /// <summary>
    /// Attempts to build the canonical storage key for the supplied tenant / user /
    /// discriminator triple. Returns <see langword="false"/> when any input fails the
    /// fail-closed allowlist (Story 5-2 D3) — callers must then perform an uncached query.
    /// </summary>
    bool TryBuildKey(string? tenantId, string? userId, string? discriminator, out string key);

    /// <summary>
    /// Attempts to read a cached entry. Returns <see langword="null"/> on cache miss, on
    /// storage / serialisation failure, or when the entry's
    /// <see cref="ETagCacheEntry.FormatVersion"/> / <see cref="ETagCacheEntry.PayloadVersion"/>
    /// is incompatible with the producer's expectations. Failures are logged and the entry
    /// is best-effort removed.
    /// </summary>
    /// <param name="key">The canonical storage key (built via <see cref="TryBuildKey"/>).</param>
    /// <param name="expectedPayloadVersion">The minimum payload version the caller can read; older entries are diagnostic misses.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<ETagCacheEntry?> TryGetAsync(
        string key,
        int expectedPayloadVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes the entry fire-and-forget. Quota / serialisation / write failures are
    /// surfaced as diagnostic logs only; the user operation is unaffected. The cache
    /// applies LRU eviction in-process before enqueuing the storage write.
    /// </summary>
    Task SetAsync(string key, ETagCacheEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Best-effort remove. Used by <see cref="TryGetAsync"/> when an incompatible entry is
    /// detected and by callers handling protocol drift (304 + missing cache).
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Story 5-4 DN2 — best-effort projection-family invalidation. Removes every entry whose
    /// stored discriminator (<see cref="ETagCacheEntry.Discriminator"/>) belongs to the supplied
    /// projection type. Implementations must not throw on storage failure; failures degrade to
    /// a diagnostic log so the user-visible operation never fails because of cache cleanup.
    /// </summary>
    /// <remarks>
    /// <para>
    /// AC7 requires "all ETag cache entries for the affected projection type/discriminator family"
    /// to be invalidated after an incompatible cache entry or 200 OK payload. The default
    /// implementation iterates the in-memory LRU map and matches stored discriminators against
    /// the projection type for both lane shapes built by <see cref="ETagCacheDiscriminator"/>:
    /// <c>projection-page:{TypeFqn}:s{Skip}-t{Take}</c> and <c>action-queue-count:{TypeFqn}</c>.
    /// </para>
    /// <para>
    /// A 304 Not Modified must never trigger this path (Story 5-4 D17).
    /// </para>
    /// </remarks>
    /// <param name="projectionType">Fully-qualified projection type name; entries whose stored discriminator references this type are removed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveByProjectionTypeAsync(string projectionType, CancellationToken cancellationToken = default);
}
