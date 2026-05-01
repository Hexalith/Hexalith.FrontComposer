using System.Collections.Generic;
using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.ETagCache;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>
/// Story 5-2 T4 / AC1 / AC4 — EventStoreQueryClient + ETag cache integration. Covers the
/// 200-write, 304-reuse, 304-protocol-drift retry, 304-without-cache-discriminator pass-through,
/// and warning-class exception propagation.
/// </summary>
public class EventStoreQueryCacheIntegrationTests {
    private const string Tenant = "acme";
    private const string User = "alice";
    private const string Domain = "orders";
    private const string ProjectionType = "Counter.Domain.OrderProjection";

    private readonly InMemoryStorageService _storage = new();
    private readonly IETagCache _cache;

    public EventStoreQueryCacheIntegrationTests() {
        _cache = new ETagCacheService(
            _storage,
            new TestOptionsMonitor(new FcShellOptions { MaxETagCacheEntries = 50 }),
            TimeProvider.System,
            NullLogger<ETagCacheService>.Instance);
    }

    [Fact]
    public async Task QueryAsync_200_WritesCacheEntryAndReturnsItems() {
        ScriptedHandler handler = new();
        handler.Script.Add(_ => Ok("[{\"id\":\"order-1\"}]", "\"v1\""));
        EventStoreQueryClient sut = NewClient(handler);

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(
            BuildRequest(cacheDiscriminator: ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)),
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeFalse();
        result.Items.Single().Id.ShouldBe("order-1");
        result.ETag.ShouldBe("\"v1\"");

        ETagCacheEntry? cached = await _cache.TryGetAsync(BuildKey(0), 1, TestContext.Current.CancellationToken);
        cached.ShouldNotBeNull();
        cached!.ETag.ShouldBe("\"v1\"");
    }

    [Theory]
    [InlineData("legacy-filter", null, null)]
    [InlineData(null, "needle", null)]
    [InlineData(null, null, "Name")]
    public async Task QueryAsync_FilterSearchOrSort_DisablesFrameworkCacheEvenWithDiscriminator(
        string? legacyFilter,
        string? searchQuery,
        string? sortColumn) {
        ScriptedHandler handler = new();
        handler.Script.Add(_ => Ok("[{\"id\":\"order-1\"}]", "\"v1\""));
        EventStoreQueryClient sut = NewClient(handler);
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;

        _ = await sut.QueryAsync<OrderProjection>(
            BuildRequest(
                cacheDiscriminator: discriminator,
                filter: legacyFilter,
                searchQuery: searchQuery,
                sortColumn: sortColumn),
            TestContext.Current.CancellationToken);

        ETagCacheEntry? cached = await _cache.TryGetAsync(BuildKey(0), 1, TestContext.Current.CancellationToken);
        cached.ShouldBeNull();
    }

    [Fact]
    public async Task QueryAsync_304WithCacheHit_ReusesCachedItems_AndKeepsIsNotModifiedTrue() {
        ScriptedHandler handler = new();
        handler.Script.Add(req => {
            // First call: 200 + ETag + payload — populates cache.
            req.Headers.Contains("If-None-Match").ShouldBeFalse();
            return Ok("[{\"id\":\"order-1\"}]", "\"v1\"");
        });
        handler.Script.Add(req => {
            // Second call: validator must be sent + server returns 304.
            req.Headers.GetValues("If-None-Match").ShouldContain("\"v1\"");
            return new HttpResponseMessage(HttpStatusCode.NotModified) {
                Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"") },
            };
        });

        EventStoreQueryClient sut = NewClient(handler);
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;

        _ = await sut.QueryAsync<OrderProjection>(BuildRequest(cacheDiscriminator: discriminator), TestContext.Current.CancellationToken);
        QueryResult<OrderProjection> reuse = await sut.QueryAsync<OrderProjection>(BuildRequest(cacheDiscriminator: discriminator), TestContext.Current.CancellationToken);

        reuse.IsNotModified.ShouldBeTrue();
        reuse.Items.Single().Id.ShouldBe("order-1");
    }

    [Fact]
    public async Task QueryAsync_304WithoutCacheDiscriminator_ReturnsNotModified_WithEmptyItems() {
        // Caller uses their own ETag without opting into framework cache integration: the
        // explicit no-change signal must propagate so the caller's own cache can react.
        ScriptedHandler handler = new();
        handler.Script.Add(_ => new HttpResponseMessage(HttpStatusCode.NotModified) {
            Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"") },
        });
        EventStoreQueryClient sut = NewClient(handler);

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(
            BuildRequest(eTag: "\"v1\""),
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeTrue();
        result.Items.ShouldBeEmpty();
        result.ETag.ShouldBe("\"v1\"");
    }

    [Fact]
    public async Task QueryAsync_304WithoutMatchingCachedEntry_RetriesUncached_AndAcceptsFresh200() {
        // D10 — cache discriminator was provided but no readable entry exists. The client retries
        // once without If-None-Match; if that returns 200 it succeeds and refreshes the cache.
        ScriptedHandler handler = new();
        handler.Script.Add(_ => new HttpResponseMessage(HttpStatusCode.NotModified) {
            Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"") },
        });
        handler.Script.Add(req => {
            req.Headers.Contains("If-None-Match").ShouldBeFalse();
            return Ok("[{\"id\":\"order-2\"}]", "\"v2\"");
        });

        EventStoreQueryClient sut = NewClient(handler);
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(
            BuildRequest(cacheDiscriminator: discriminator),
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeFalse();
        result.Items.Single().Id.ShouldBe("order-2");
        handler.RequestCount.ShouldBe(2);
    }

    [Fact]
    public async Task QueryAsync_304WithoutCacheTwiceInARow_FailsLoudly() {
        // D10 — protocol drift: server insists on 304 even on the uncached retry. Surface the
        // failure so reducers preserve currently visible UI state.
        ScriptedHandler handler = new();
        handler.Script.Add(_ => new HttpResponseMessage(HttpStatusCode.NotModified) {
            Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"") },
        });
        handler.Script.Add(_ => new HttpResponseMessage(HttpStatusCode.NotModified) {
            Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"") },
        });
        EventStoreQueryClient sut = NewClient(handler);
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;

        _ = await Should.ThrowAsync<HttpRequestException>(
            async () => await sut.QueryAsync<OrderProjection>(
                BuildRequest(cacheDiscriminator: discriminator),
                TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
    }

    [Fact]
    public async Task QueryAsync_401_ThrowsAuthRedirectRequiredException() {
        ScriptedHandler handler = new();
        handler.Script.Add(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));
        EventStoreTestSupport.RecordingAuthRedirector redirector = new();
        EventStoreQueryClient sut = NewClient(handler, redirector);

        _ = await Should.ThrowAsync<AuthRedirectRequiredException>(
            async () => await sut.QueryAsync<OrderProjection>(BuildRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);
        redirector.CallCount.ShouldBe(1);
    }

    [Fact]
    public async Task QueryAsync_429_ThrowsQueryFailureExceptionWithRetryAfter() {
        ScriptedHandler handler = new();
        handler.Script.Add(_ => {
            HttpResponseMessage response = new((HttpStatusCode)429) {
                Content = new StringContent("""{"title":"slow down"}""", Encoding.UTF8, "application/problem+json"),
            };
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(System.TimeSpan.FromSeconds(5));
            return response;
        });
        EventStoreQueryClient sut = NewClient(handler);

        QueryFailureException ex = await Should.ThrowAsync<QueryFailureException>(
            async () => await sut.QueryAsync<OrderProjection>(BuildRequest(), TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);

        ex.Kind.ShouldBe(QueryFailureKind.RateLimited);
        ex.RetryAfter.ShouldBe(System.TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task QueryAsync_Incompatible200Payload_ThrowsSchemaMismatch() {
        ScriptedHandler handler = new();
        handler.Script.Add(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("{not-json", Encoding.UTF8, "application/json"),
        });
        EventStoreQueryClient sut = NewClient(handler);

        ProjectionSchemaMismatchException ex = await Should.ThrowAsync<ProjectionSchemaMismatchException>(
            async () => await sut.QueryAsync<OrderProjection>(
                BuildRequest(cacheDiscriminator: ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)),
                TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);

        ex.ProjectionType.ShouldBe(ProjectionType);
        ETagCacheEntry? cached = await _cache.TryGetAsync(BuildKey(0), 1, TestContext.Current.CancellationToken);
        cached.ShouldBeNull();
    }

    [Fact]
    public async Task QueryAsync_304WithIncompatibleCachedPayload_DoesNotInvalidateCache() {
        string key = BuildKey(0);
        await _storage.SetAsync(
            key,
            new ETagCacheEntry(
                ETag: "\"v1\"",
                Payload: "{not-json",
                CachedAtUtcTicks: 1,
                LastAccessedUtcTicks: 1,
                FormatVersion: ETagCacheEntry.CurrentFormatVersion,
                PayloadVersion: 1,
                Discriminator: ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!),
            TestContext.Current.CancellationToken);
        ScriptedHandler handler = new();
        handler.Script.Add(_ => new HttpResponseMessage(HttpStatusCode.NotModified) {
            Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"") },
        });
        EventStoreQueryClient sut = NewClient(handler);

        _ = await Should.ThrowAsync<ProjectionSchemaMismatchException>(
            async () => await sut.QueryAsync<OrderProjection>(
                BuildRequest(cacheDiscriminator: ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)),
                TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);

        ETagCacheEntry? stored = await _storage.GetAsync<ETagCacheEntry>(key, TestContext.Current.CancellationToken);
        stored.ShouldNotBeNull();
        stored!.Payload.ShouldBe("{not-json");
    }

    private static HttpResponseMessage Ok(string payloadArrayJson, string etag) {
        HttpResponseMessage response = new(HttpStatusCode.OK) {
            Content = new StringContent("{\"payload\":" + payloadArrayJson + "}", Encoding.UTF8, "application/json"),
        };
        response.Headers.ETag = System.Net.Http.Headers.EntityTagHeaderValue.Parse(etag);
        return response;
    }

    [Fact]
    public async Task QueryAsync_InvalidPayloadVersion_DisablesFrameworkCache() {
        ScriptedHandler handler = new();
        handler.Script.Add(_ => Ok("[{\"id\":\"order-1\"}]", "\"v1\""));
        EventStoreQueryClient sut = NewClient(handler);

        _ = await sut.QueryAsync<OrderProjection>(
            BuildRequest(
                cacheDiscriminator: ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25),
                cachePayloadVersion: 0),
            TestContext.Current.CancellationToken);

        ETagCacheEntry? cached = await _cache.TryGetAsync(BuildKey(0), 1, TestContext.Current.CancellationToken);
        cached.ShouldBeNull();
    }

    [Fact]
    public async Task QueryAsync_WeakETag_PreservesWeakValidatorInCacheAndIfNoneMatch() {
        ScriptedHandler handler = new();
        handler.Script.Add(_ => Ok("[{\"id\":\"order-1\"}]", "W/\"v1\""));
        handler.Script.Add(req => {
            req.Headers.GetValues("If-None-Match").ShouldContain("W/\"v1\"");
            return new HttpResponseMessage(HttpStatusCode.NotModified) {
                Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"", isWeak: true) },
            };
        });

        EventStoreQueryClient sut = NewClient(handler);
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;

        _ = await sut.QueryAsync<OrderProjection>(BuildRequest(cacheDiscriminator: discriminator), TestContext.Current.CancellationToken);
        QueryResult<OrderProjection> reuse = await sut.QueryAsync<OrderProjection>(BuildRequest(cacheDiscriminator: discriminator), TestContext.Current.CancellationToken);

        reuse.ETag.ShouldBe("W/\"v1\"");
    }

    [Fact]
    public async Task RemoveByProjectionTypeAsync_WithTenantUserScope_DoesNotRemoveOtherTenantEntries() {
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;
        string tenantAKey = $"{Tenant}:{User}:etag:{discriminator}";
        string tenantBKey = $"tenant-b:{User}:etag:{discriminator}";
        ETagCacheEntry entry = new(
            ETag: "\"v1\"",
            Payload: """{"payload":[{"id":"order-1"}]}""",
            CachedAtUtcTicks: 1,
            LastAccessedUtcTicks: 1,
            FormatVersion: ETagCacheEntry.CurrentFormatVersion,
            PayloadVersion: 1,
            Discriminator: discriminator);

        await _cache.SetAsync(tenantAKey, entry, TestContext.Current.CancellationToken);
        await _cache.SetAsync(tenantBKey, entry with { ETag = "\"v2\"" }, TestContext.Current.CancellationToken);

        await _cache.RemoveByProjectionTypeAsync(Tenant, User, ProjectionType, TestContext.Current.CancellationToken);

        (await _storage.GetAsync<ETagCacheEntry>(tenantAKey, TestContext.Current.CancellationToken)).ShouldBeNull();
        ETagCacheEntry? tenantB = await _storage.GetAsync<ETagCacheEntry>(tenantBKey, TestContext.Current.CancellationToken);
        tenantB.ShouldNotBeNull();
        tenantB!.ETag.ShouldBe("\"v2\"");
    }

    private static QueryRequest BuildRequest(
        string? cacheDiscriminator = null,
        string? eTag = null,
        string? filter = null,
        string? searchQuery = null,
        string? sortColumn = null,
        int cachePayloadVersion = 1) {
#pragma warning disable CS0618 // Legacy Filter participates in Story 5-2 cache-safety tests.
        return new QueryRequest(
            ProjectionType: ProjectionType,
            TenantId: Tenant,
            Filter: filter,
            Domain: Domain,
            AggregateId: "order-1",
            QueryType: ProjectionType,
            ETag: eTag,
            SearchQuery: searchQuery,
            SortColumn: sortColumn,
            CacheDiscriminator: cacheDiscriminator,
            CachePayloadVersion: cachePayloadVersion);
#pragma warning restore CS0618
    }

    private static string BuildKey(int skip)
        => $"{Tenant}:{User}:etag:projection-page:{ProjectionType}:s{skip}-t25";

    private EventStoreQueryClient NewClient(
        HttpMessageHandler handler,
        EventStoreTestSupport.RecordingAuthRedirector? redirector = null) => new(
        new SingleClientFactory(handler),
        Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new System.Uri("https://eventstore.test"),
            AccessTokenProvider = _ => System.Threading.Tasks.ValueTask.FromResult<string?>("token"),
        }),
        new TestUserContextAccessor(Tenant, User),
        EventStoreTestSupport.CreateClassifier(),
        _cache,
        redirector ?? new EventStoreTestSupport.RecordingAuthRedirector(),
        NullLogger<EventStoreQueryClient>.Instance);

    private sealed record OrderProjection(string Id);

    private sealed class TestUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new System.Uri("https://eventstore.test") };
    }

    private sealed class ScriptedHandler : HttpMessageHandler {
        public List<System.Func<HttpRequestMessage, HttpResponseMessage>> Script { get; } = new();
        public int RequestCount { get; private set; }

        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken) {
            int index = RequestCount++;
            if (index >= Script.Count) {
                throw new System.InvalidOperationException("ScriptedHandler ran out of canned responses.");
            }

            return System.Threading.Tasks.Task.FromResult(Script[index](request));
        }
    }

    private sealed class TestOptionsMonitor(FcShellOptions value) : IOptionsMonitor<FcShellOptions> {
        public FcShellOptions CurrentValue => value;
        public FcShellOptions Get(string? name) => value;
        public IDisposable? OnChange(System.Action<FcShellOptions, string?> listener) => null;
    }
}
