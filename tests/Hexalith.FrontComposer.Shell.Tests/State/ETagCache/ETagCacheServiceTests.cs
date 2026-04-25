using System.Threading;
using System.Threading.Tasks;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.State.ETagCache;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.State.ETagCache;

/// <summary>
/// Story 5-2 T2 / T3 — fail-closed key construction, LRU eviction, format / payload version
/// diagnostic miss, and storage-failure-as-miss for the bounded ETag cache.
/// </summary>
public class ETagCacheServiceTests {
    [Fact]
    public void TryBuildKey_AcceptsValidTriple() {
        ETagCacheService cache = NewCache(out _);

        bool ok = cache.TryBuildKey("acme", "alice", "projection-page:Foo:s0-t25", out string key);

        ok.ShouldBeTrue();
        key.ShouldBe("acme:alice:etag:projection-page:Foo:s0-t25");
    }

    [Theory]
    [InlineData(null, "alice", "projection-page:Foo:s0-t25")]
    [InlineData("", "alice", "projection-page:Foo:s0-t25")]
    [InlineData("acme", null, "projection-page:Foo:s0-t25")]
    [InlineData("acme", "alice", null)]
    [InlineData("acme", "alice", "user-input:hostile")]
    [InlineData("ac:me", "alice", "projection-page:Foo:s0-t25")]
    [InlineData("acme", "ali:ce", "projection-page:Foo:s0-t25")]
    public void TryBuildKey_RejectsInvalidIdentitiesOrDiscriminators(string? tenantId, string? userId, string? discriminator) {
        ETagCacheService cache = NewCache(out _);

        cache.TryBuildKey(tenantId, userId, discriminator, out _).ShouldBeFalse();
    }

    [Fact]
    public void TryBuildKey_ReturnsFalseWhenCacheDisabledByOption() {
        ETagCacheService cache = NewCache(out _, maxEntries: 0);

        cache.TryBuildKey("acme", "alice", "projection-page:Foo:s0-t25", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task SetThenGet_RoundTripsEntryAndUpdatesLru() {
        ETagCacheService cache = NewCache(out _);
        const string key = "acme:alice:etag:projection-page:Foo:s0-t25";

        await cache.SetAsync(key, NewEntry(eTag: "\"v1\""), CancellationToken.None);
        ETagCacheEntry? loaded = await cache.TryGetAsync(key, expectedPayloadVersion: 1, CancellationToken.None);

        loaded.ShouldNotBeNull();
        loaded!.ETag.ShouldBe("\"v1\"");
        cache.TrackedKeyCount.ShouldBe(1);
    }

    [Fact]
    public async Task TryGet_TreatsMismatchedFormatVersionAsDiagnosticMiss() {
        ETagCacheService cache = NewCache(out InMemoryStorageService storage);
        const string key = "acme:alice:etag:projection-page:Foo:s0-t25";
        ETagCacheEntry forwardVersion = NewEntry(eTag: "\"v1\"") with { FormatVersion = ETagCacheEntry.CurrentFormatVersion + 5 };

        // Bypass SetAsync (which re-stamps FormatVersion) by writing directly through the storage.
        await storage.SetAsync(key, forwardVersion, CancellationToken.None);

        ETagCacheEntry? loaded = await cache.TryGetAsync(key, expectedPayloadVersion: 1, CancellationToken.None);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task TryGet_TreatsLowerPayloadVersionAsDiagnosticMiss() {
        ETagCacheService cache = NewCache(out _);
        const string key = "acme:alice:etag:projection-page:Foo:s0-t25";
        await cache.SetAsync(key, NewEntry(eTag: "\"v1\"") with { PayloadVersion = 1 }, CancellationToken.None);

        ETagCacheEntry? loaded = await cache.TryGetAsync(key, expectedPayloadVersion: 5, CancellationToken.None);

        loaded.ShouldBeNull();
    }

    [Fact]
    public async Task SetAsync_EvictsOldestWhenOverCap() {
        ETagCacheService cache = NewCache(out _, maxEntries: 2);

        await cache.SetAsync("acme:alice:etag:projection-page:Foo:s0-t25", NewEntry(eTag: "\"a\""), CancellationToken.None);
        await cache.SetAsync("acme:alice:etag:projection-page:Foo:s25-t25", NewEntry(eTag: "\"b\""), CancellationToken.None);
        await cache.SetAsync("acme:alice:etag:projection-page:Foo:s50-t25", NewEntry(eTag: "\"c\""), CancellationToken.None);

        cache.TrackedKeyCount.ShouldBe(2);
    }

    [Fact]
    public async Task SetAsync_SeedsPersistedEntriesBeforeEnforcingCap() {
        InMemoryStorageService storage = new();
        await storage.SetAsync(
            "acme:alice:etag:projection-page:Foo:s0-t25",
            NewEntry(eTag: "\"a\"") with { CachedAtUtcTicks = 1, LastAccessedUtcTicks = 1 },
            CancellationToken.None);
        await storage.SetAsync(
            "acme:alice:etag:projection-page:Foo:s25-t25",
            NewEntry(eTag: "\"b\"") with { CachedAtUtcTicks = 2, LastAccessedUtcTicks = 2 },
            CancellationToken.None);

        ETagCacheService cache = new(
            storage,
            new TestOptionsMonitor(new FcShellOptions { MaxETagCacheEntries = 2 }),
            TimeProvider.System,
            NullLogger<ETagCacheService>.Instance);

        await cache.SetAsync(
            "acme:alice:etag:projection-page:Foo:s50-t25",
            NewEntry(eTag: "\"c\""),
            CancellationToken.None);

        cache.TrackedKeyCount.ShouldBe(2);
        ETagCacheEntry? oldest = await storage.GetAsync<ETagCacheEntry>(
            "acme:alice:etag:projection-page:Foo:s0-t25",
            CancellationToken.None);
        oldest.ShouldBeNull();
    }

    [Fact]
    public void TryBuildKey_RejectsMalformedPrefixedDiscriminator() {
        ETagCacheService cache = NewCache(out _);

        cache.TryBuildKey("acme", "alice", "projection-page:Foo/Bar:s0-t25", out _).ShouldBeFalse();
    }

    [Fact]
    public async Task TryGet_ReturnsNullOnStorageReadFailure() {
        ThrowingStorage throwing = new();
        ETagCacheService cache = new(
            throwing,
            new TestOptionsMonitor(new FcShellOptions { MaxETagCacheEntries = 200 }),
            TimeProvider.System,
            NullLogger<ETagCacheService>.Instance);

        ETagCacheEntry? loaded = await cache.TryGetAsync("acme:alice:etag:projection-page:Foo:s0-t25", 1, CancellationToken.None);

        loaded.ShouldBeNull();
    }

    private static ETagCacheService NewCache(out InMemoryStorageService storage, int maxEntries = 200) {
        storage = new InMemoryStorageService();
        TestOptionsMonitor monitor = new(new FcShellOptions { MaxETagCacheEntries = maxEntries });
        return new ETagCacheService(storage, monitor, TimeProvider.System, NullLogger<ETagCacheService>.Instance);
    }

    private static ETagCacheEntry NewEntry(string eTag) => new(
        ETag: eTag,
        Payload: "{\"payload\":[]}",
        CachedAtUtcTicks: 1,
        LastAccessedUtcTicks: 1,
        FormatVersion: ETagCacheEntry.CurrentFormatVersion,
        PayloadVersion: 1,
        Discriminator: "projection-page:Foo:s0-t25");

    private sealed class TestOptionsMonitor(FcShellOptions value) : IOptionsMonitor<FcShellOptions> {
        public FcShellOptions CurrentValue => value;
        public FcShellOptions Get(string? name) => value;
        public IDisposable? OnChange(System.Action<FcShellOptions, string?> listener) => null;
    }

    private sealed class ThrowingStorage : IStorageService {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            => throw new System.InvalidOperationException("storage unavailable");

        public Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<System.Collections.Generic.IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken cancellationToken = default)
            => Task.FromResult<System.Collections.Generic.IReadOnlyList<string>>(System.Array.Empty<string>());

        public Task FlushAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
