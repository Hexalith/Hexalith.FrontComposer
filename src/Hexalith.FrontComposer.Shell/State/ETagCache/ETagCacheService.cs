using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Storage;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hexalith.FrontComposer.Shell.State.ETagCache;

/// <summary>
/// Story 5-2 T2 — default <see cref="IETagCache"/> implementation. Persists
/// <see cref="ETagCacheEntry"/> values through <see cref="IStorageService"/> with framework
/// allowlist enforcement on the discriminator and global per-cache LRU eviction by
/// <see cref="FcShellOptions.MaxETagCacheEntries"/>.
/// </summary>
/// <remarks>
/// <para>
/// Read paths are non-throwing per AC3: storage failures, serialisation failures, and entries
/// with mismatched <see cref="ETagCacheEntry.FormatVersion"/> /
/// <see cref="ETagCacheEntry.PayloadVersion"/> are logged and degrade to a cache miss while
/// best-effort removing the offending entry.
/// </para>
/// <para>
/// Write paths are fire-and-forget: <see cref="IStorageService.SetAsync"/> already enqueues
/// to a drain channel. Story 3-1's <c>FrontComposerShell.FlushAsync</c> drains pending
/// writes via <see cref="IStorageService.FlushAsync"/>; Story 5-2 adds no second unload hook.
/// </para>
/// <para>
/// LRU bookkeeping uses an in-memory <see cref="ConcurrentDictionary{TKey,TValue}"/> of last
/// access ticks per known key. Eviction picks the oldest entry by the in-memory map; the
/// underlying storage write is enqueued through the same fire-and-forget channel so the
/// browser localStorage view stays in sync.
/// </para>
/// </remarks>
public sealed class ETagCacheService : IETagCache {
    private readonly IStorageService _storage;
    private readonly IOptionsMonitor<FcShellOptions> _options;
    private readonly TimeProvider _time;
    private readonly ILogger<ETagCacheService> _logger;
    private readonly ConcurrentDictionary<string, long> _lru = new(System.StringComparer.Ordinal);
    private int _lruSeeded;

    /// <summary>
    /// Initializes a new instance of the <see cref="ETagCacheService"/> class.
    /// </summary>
    public ETagCacheService(
        IStorageService storage,
        IOptionsMonitor<FcShellOptions> options,
        TimeProvider time,
        ILogger<ETagCacheService> logger) {
        ArgumentNullException.ThrowIfNull(storage);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(time);
        ArgumentNullException.ThrowIfNull(logger);
        _storage = storage;
        _options = options;
        _time = time;
        _logger = logger;
    }

    /// <summary>Gets the number of entries currently tracked in the in-memory LRU map.</summary>
    internal int TrackedKeyCount => _lru.Count;

    /// <inheritdoc />
    public bool TryBuildKey(string? tenantId, string? userId, string? discriminator, out string key) {
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(tenantId)
            || string.IsNullOrWhiteSpace(userId)
            || string.IsNullOrWhiteSpace(discriminator)) {
            return false;
        }

        if (tenantId!.Contains(':', System.StringComparison.Ordinal)
            || userId!.Contains(':', System.StringComparison.Ordinal)) {
            return false;
        }

        if (!ETagCacheDiscriminator.IsAllowlisted(discriminator)) {
            return false;
        }

        if (_options.CurrentValue.MaxETagCacheEntries <= 0) {
            return false;
        }

        try {
            key = StorageKeys.BuildKey(tenantId, userId, "etag", discriminator!);
            return true;
        }
        catch (System.ArgumentException) {
            // Defence-in-depth — StorageKeys also enforces the colon guard.
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<ETagCacheEntry?> TryGetAsync(
        string key,
        int expectedPayloadVersion,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(key);

        if (_options.CurrentValue.MaxETagCacheEntries <= 0) {
            return null;
        }

        ETagCacheEntry? entry;
        try {
            entry = await _storage.GetAsync<ETagCacheEntry>(key, cancellationToken).ConfigureAwait(false);
        }
        catch (System.OperationCanceledException) {
            throw;
        }
        catch (System.Exception ex) {
            _logger.LogWarning(
                ex,
                "ETagCacheService: storage read failed for redacted key {KeyHash} — degrading to cache miss.",
                RedactKey(key));
            return null;
        }

        if (entry is null) {
            _ = _lru.TryRemove(key, out _);
            return null;
        }

        if (entry.FormatVersion != ETagCacheEntry.CurrentFormatVersion
            || entry.PayloadVersion < expectedPayloadVersion
            || string.IsNullOrWhiteSpace(entry.ETag)
            || string.IsNullOrEmpty(entry.Payload)) {
            _logger.LogInformation(
                "ETagCacheService: incompatible cache entry for redacted key {KeyHash} (FormatVersion={FormatVersion}, PayloadVersion={PayloadVersion}, ExpectedPayloadVersion={ExpectedPayloadVersion}) — diagnostic miss.",
                RedactKey(key),
                entry.FormatVersion,
                entry.PayloadVersion,
                expectedPayloadVersion);
            try {
                await _storage.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            }
            catch (System.OperationCanceledException) {
                throw;
            }
            catch (System.Exception ex) {
                _logger.LogWarning(
                    ex,
                    "ETagCacheService: failed to remove incompatible entry for redacted key {KeyHash} — best-effort cleanup.",
                    RedactKey(key));
            }

            _ = _lru.TryRemove(key, out _);
            return null;
        }

        _lru[key] = _time.GetUtcNow().UtcTicks;
        return entry;
    }

    /// <inheritdoc />
    public async Task SetAsync(string key, ETagCacheEntry entry, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(entry);

        int cap = _options.CurrentValue.MaxETagCacheEntries;
        if (cap <= 0) {
            return;
        }

        long nowTicks = _time.GetUtcNow().UtcTicks;
        ETagCacheEntry stamped = entry with {
            CachedAtUtcTicks = entry.CachedAtUtcTicks <= 0 ? nowTicks : entry.CachedAtUtcTicks,
            LastAccessedUtcTicks = nowTicks,
            FormatVersion = entry.FormatVersion <= 0 ? ETagCacheEntry.CurrentFormatVersion : entry.FormatVersion,
        };

        await EnsurePersistedLruSeededAsync(cancellationToken).ConfigureAwait(false);
        _lru[key] = nowTicks;
        await EvictIfOverCapAsync(cap, cancellationToken).ConfigureAwait(false);

        try {
            await _storage.SetAsync(key, stamped, cancellationToken).ConfigureAwait(false);
        }
        catch (System.OperationCanceledException) {
            throw;
        }
        catch (System.Exception ex) {
            _logger.LogWarning(
                ex,
                "ETagCacheService: storage write failed for redacted key {KeyHash} — entry not persisted.",
                RedactKey(key));
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrEmpty(key);
        _ = _lru.TryRemove(key, out _);
        try {
            await _storage.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        }
        catch (System.OperationCanceledException) {
            throw;
        }
        catch (System.Exception ex) {
            _logger.LogWarning(
                ex,
                "ETagCacheService: storage remove failed for redacted key {KeyHash} — best-effort cleanup.",
                RedactKey(key));
        }
    }

    private async Task EnsurePersistedLruSeededAsync(CancellationToken cancellationToken) {
        if (Interlocked.Exchange(ref _lruSeeded, 1) != 0) {
            return;
        }

        IReadOnlyList<string> keys;
        try {
            keys = await _storage.GetKeysAsync(string.Empty, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) {
            throw;
        }
        catch (Exception ex) {
            _logger.LogWarning(ex, "ETagCacheService: failed to enumerate persisted keys for cache LRU seeding.");
            return;
        }

        foreach (string key in keys) {
            if (!key.Contains(":etag:", StringComparison.Ordinal)) {
                continue;
            }

            try {
                ETagCacheEntry? entry = await _storage.GetAsync<ETagCacheEntry>(key, cancellationToken).ConfigureAwait(false);
                if (entry is not null) {
                    long ticks = entry.LastAccessedUtcTicks > 0
                        ? entry.LastAccessedUtcTicks
                        : entry.CachedAtUtcTicks;
                    _lru[key] = ticks > 0 ? ticks : _time.GetUtcNow().UtcTicks;
                }
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception ex) {
                _logger.LogWarning(
                    ex,
                    "ETagCacheService: failed to seed LRU timestamp for redacted key {KeyHash}.",
                    RedactKey(key));
            }
        }
    }

    private async Task EvictIfOverCapAsync(int cap, CancellationToken cancellationToken) {
        while (_lru.Count > cap) {
            string? oldestKey = null;
            long oldestTicks = long.MaxValue;
            foreach (KeyValuePair<string, long> kvp in _lru) {
                if (kvp.Value < oldestTicks) {
                    oldestTicks = kvp.Value;
                    oldestKey = kvp.Key;
                }
            }

            if (oldestKey is null || !_lru.TryRemove(oldestKey, out _)) {
                return;
            }

            try {
                await _storage.RemoveAsync(oldestKey, cancellationToken).ConfigureAwait(false);
            }
            catch (System.OperationCanceledException) {
                throw;
            }
            catch (System.Exception ex) {
                _logger.LogWarning(
                    ex,
                    "ETagCacheService: LRU eviction storage remove failed for redacted key {KeyHash}.",
                    RedactKey(oldestKey));
            }
        }
    }

    private static string RedactKey(string key) {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hash, 0, 8);
    }
}
