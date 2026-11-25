using System.Diagnostics;
using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Contracts.Storage;
using Hexalith.FrontComposer.Contracts.Telemetry;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.State.ETagCache;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>
/// Story 5-2 T4 / AC1 / AC4 — EventStoreQueryClient + ETag cache integration. Covers the
/// 200-write, 304-reuse, 304-protocol-drift retry, 304-without-cache-discriminator pass-through,
/// and warning-class exception propagation.
/// </summary>
public class EventStoreQueryCacheIntegrationTests : IDisposable {
    private const string Tenant = "acme";
    private const string User = "alice";
    private const string Domain = "orders";
    private const string ProjectionType = "Counter.Domain.OrderProjection";

    private readonly InMemoryStorageService _storage = new();
    private readonly ETagCacheService _cache;

    public EventStoreQueryCacheIntegrationTests() => _cache = new ETagCacheService(
            _storage,
            new TestOptionsMonitor(new FcShellOptions { MaxETagCacheEntries = 50 }),
            TimeProvider.System,
            NullLogger<ETagCacheService>.Instance);

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
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
    [InlineData(false)]
    [InlineData(true)]
    public async Task QueryAsync_DetachedCacheWriteFails_RecordsSafeChildActivity(bool defaultCache) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        TaskCompletionSource pendingWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<Activity> stoppedWrite = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using Activity trace = new Activity(nameof(QueryAsync_DetachedCacheWriteFails_RecordsSafeChildActivity))
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();
        ActivityTraceId traceId = trace.TraceId;
        Activity? query = null;
        using ActivityListener listener = new() {
            ShouldListenTo = static source => source.Name == FrontComposerActivitySource.Name,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity => {
                if (activity.TraceId != traceId) {
                    return;
                }

                if (activity.OperationName == FrontComposerTelemetry.QueryExecuteOperation) {
                    query = activity;
                }
                else if (activity.OperationName == FrontComposerTelemetry.QueryCacheWriteOperation) {
                    _ = stoppedWrite.TrySetResult(activity);
                }
            },
        };
        ActivitySource.AddActivityListener(listener);

        IStorageService storage = Substitute.For<IStorageService>();
        storage.GetKeysAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<string>>([]));
        storage.SetAsync(Arg.Any<string>(), Arg.Any<ETagCacheEntry>(), Arg.Any<CancellationToken>())
            .Returns(pendingWrite.Task);
        using ETagCacheService builtInCache = new(
            storage, new TestOptionsMonitor(new FcShellOptions { MaxETagCacheEntries = 50 }),
            TimeProvider.System, NullLogger<ETagCacheService>.Instance);

        IETagCache customCache = Substitute.For<IETagCache>();
        customCache.TryBuildKey(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), out Arg.Any<string>())
            .Returns(call => {
                call[3] = "private-cache-key";
                return true;
            });
        customCache.SetAsync(Arg.Any<string>(), Arg.Any<ETagCacheEntry>(), Arg.Any<CancellationToken>())
            .Returns(pendingWrite.Task);

        ScriptedHandler handler = new();
        handler.Script.Add(_ => Ok("[{\"id\":\"order-1\"}]", "\"v1\""));
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;
        EventStoreQueryClient client = NewClient(handler, cache: defaultCache ? builtInCache : customCache);

        try {
            QueryResult<OrderProjection> result = await client.QueryAsync<OrderProjection>(
                BuildRequest(cacheDiscriminator: discriminator), ct).WaitAsync(TimeSpan.FromSeconds(5), ct);
            result.Items.Single().Id.ShouldBe("order-1");
            query.ShouldNotBeNull();
            query.GetTagItem(FrontComposerTelemetry.OutcomeTag).ShouldBe("ok");
            query.GetTagItem(FrontComposerTelemetry.FailureCategoryTag).ShouldBeNull();
            stoppedWrite.Task.IsCompleted.ShouldBeFalse();
        }
        finally {
            pendingWrite.SetException(new InvalidOperationException("private storage error"));
        }

        Activity write = await stoppedWrite.Task.WaitAsync(TimeSpan.FromSeconds(5), ct);
        write.ParentSpanId.ShouldBe(query!.SpanId);
        write.Status.ShouldBe(ActivityStatusCode.Error);
        write.StatusDescription.ShouldBe(nameof(InvalidOperationException));
        write.GetTagItem(FrontComposerTelemetry.OutcomeTag).ShouldBe("failed");
        write.GetTagItem(FrontComposerTelemetry.FailureCategoryTag).ShouldBe(nameof(InvalidOperationException));
        query.GetTagItem(FrontComposerTelemetry.FailureCategoryTag).ShouldBeNull();
        write.TagObjects.Select(tag => tag.Value?.ToString()).ShouldNotContain(value =>
            value != null && (value.Contains("private", StringComparison.Ordinal) || value.Contains("order-1", StringComparison.Ordinal)));
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task QueryAsync_CanonicalColumnOrStatusFilters_DisableFrameworkCacheEvenWithDiscriminator(bool columnFilter)
    {
        ScriptedHandler handler = new();
        handler.Script.Add(_ => Ok("[{\"id\":\"order-1\"}]", "\"v1\""));
        EventStoreQueryClient sut = NewClient(handler);
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;
        ProjectionQuery criteria = columnFilter
            ? new ProjectionQuery(ProjectionType, ColumnFilters: new Dictionary<string, string>(StringComparer.Ordinal) { ["Status"] = "Open" })
            : new ProjectionQuery(ProjectionType, StatusFilters: ["Open"]);
        QueryRequest request = QueryRequest.Create(
            criteria,
            Tenant,
            Domain: Domain,
            AggregateId: "order-1",
            QueryType: ProjectionType,
            CacheDiscriminator: discriminator);

        _ = await sut.QueryAsync<OrderProjection>(request, TestContext.Current.CancellationToken);

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

    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, false)]
    public async Task QueryAsync304WithIncompatibleCachedPayloadRetriesOnceWithoutValidators(bool validResponse, bool returnETag) {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;
        ETagCacheEntry stale = new("\"v1\"", "{not-json", 1, 1, ETagCacheEntry.CurrentFormatVersion, 1, discriminator);
        string key = BuildKey(0);
        await _cache.SetAsync(key, stale, ct);
        string otherKey = BuildKey(25);
        await _cache.SetAsync(otherKey, stale with {
            Discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 25, 25)!,
        }, ct);
        _cache.TryBuildKey("other-tenant", User, discriminator, out string otherTenantKey).ShouldBeTrue();
        await _cache.SetAsync(otherTenantKey, stale, ct);

        ScriptedHandler handler = new();
        handler.Script.Add(req => {
            req.Headers.GetValues("If-None-Match").ShouldContain(stale.ETag);
            return new HttpResponseMessage(HttpStatusCode.NotModified);
        });
        handler.Script.Add(req => {
            req.Headers.Contains("If-None-Match").ShouldBeFalse();
            HttpResponseMessage response = Ok(validResponse ? "[{\"id\":\"fresh\"}]" : "{not-json", "\"v2\"");
            if (!returnETag) {
                response.Headers.ETag = null;
            }

            return response;
        });
        EventStoreQueryClient sut = NewClient(handler);
        QueryRequest request = BuildRequest(cacheDiscriminator: discriminator, eTag: "\"caller-validator\"");

        if (validResponse) {
            QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(request, ct);
            result.IsNotModified.ShouldBeFalse();
            result.Items.Single().Id.ShouldBe("fresh");
            ETagCacheEntry? cached = await _cache.TryGetAsync(key, 1, ct);
            (cached?.ETag).ShouldBe(returnETag ? "\"v2\"" : null);
            (await _cache.TryGetAsync(otherKey, 1, ct)).ShouldNotBeNull();
        }
        else {
            _ = await Should.ThrowAsync<ProjectionSchemaMismatchException>(() => sut.QueryAsync<OrderProjection>(request, ct));
            (await _cache.TryGetAsync(key, 1, ct)).ShouldBeNull();
        }

        handler.RequestCount.ShouldBe(2);
        (await _cache.TryGetAsync(otherTenantKey, 1, ct)).ShouldNotBeNull();
    }

    [Fact]
    public async Task QueryAsyncUnreadable304AndFailedEvictionRefetchesOnlyOnce() {
        CancellationToken ct = TestContext.Current.CancellationToken;
        string key = BuildKey(0);
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;
        ETagCacheEntry stale = new("\"v1\"", "{not-json", 1, 1, ETagCacheEntry.CurrentFormatVersion, 1, discriminator);
        IETagCache cache = Substitute.For<IETagCache>();
        cache.TryBuildKey(Tenant, User, discriminator, out Arg.Any<string>()).Returns(call => {
            call[3] = key;
            return true;
        });
        cache.TryGetAsync(key, 1, ct).Returns(stale);
        cache.RemoveAsync(key, ct).Returns(Task.FromException(new IOException("Storage unavailable.")));
        ScriptedHandler handler = new();
        handler.Script.Add(_ => new HttpResponseMessage(HttpStatusCode.NotModified));
        handler.Script.Add(req => {
            req.Headers.Contains("If-None-Match").ShouldBeFalse();
            return new HttpResponseMessage(HttpStatusCode.NotModified);
        });

        HttpRequestException failure = await Should.ThrowAsync<HttpRequestException>(
            () => NewClient(handler, cache: cache).QueryAsync<OrderProjection>(BuildRequest(cacheDiscriminator: discriminator), ct));
        failure.StatusCode.ShouldBe(HttpStatusCode.NotModified);
        handler.RequestCount.ShouldBe(2);
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
    public async Task QueryAsync_InvalidCachedETag_EvictsEntryAndDoesNotSendValidator() {
        string discriminator = ETagCacheDiscriminator.ForProjectionPage(ProjectionType, 0, 25)!;
        string key = BuildKey(0);
        await _storage.SetAsync(
            key,
            new ETagCacheEntry(
                ETag: "\"v1\"\r\nInjected: value",
                Payload: """{"payload":[{"id":"order-1"}]}""",
                CachedAtUtcTicks: 1,
                LastAccessedUtcTicks: 1,
                FormatVersion: ETagCacheEntry.CurrentFormatVersion,
                PayloadVersion: 1,
                Discriminator: discriminator),
            TestContext.Current.CancellationToken);

        ScriptedHandler handler = new();
        handler.Script.Add(req => {
            // Story 11.7 code review P-9 — `If-None-Match` absence is the primary check, but
            // we also assert that no other request header smuggled the injected fragments
            // (`\r\n` or `Injected:`). A defense-in-depth assertion catches a future refactor
            // that might copy the bad ETag into a custom header before discovering the
            // injection char.
            req.Headers.Contains("If-None-Match").ShouldBeFalse();
            foreach (KeyValuePair<string, IEnumerable<string>> header in req.Headers) {
                foreach (string value in header.Value) {
                    value.ShouldNotContain("\r", Case.Sensitive, $"{header.Key} carried CR");
                    value.ShouldNotContain("\n", Case.Sensitive, $"{header.Key} carried LF");
                    value.ShouldNotContain("Injected:", Case.Sensitive, $"{header.Key} carried injected fragment");
                }
            }

            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("""{"payload":[{"id":"order-2"}]}""", Encoding.UTF8, "application/json"),
            };
        });
        EventStoreQueryClient sut = NewClient(handler);

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(
            BuildRequest(cacheDiscriminator: discriminator),
            TestContext.Current.CancellationToken);

        result.Items.Single().Id.ShouldBe("order-2");
        (await _storage.GetAsync<ETagCacheEntry>(key, TestContext.Current.CancellationToken)).ShouldBeNull();
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

#pragma warning disable HFC0001 // This helper deliberately exercises v1.12 Filter forwarding.
    private static QueryRequest BuildRequest(
        string? cacheDiscriminator = null,
        string? eTag = null,
        string? filter = null,
        string? searchQuery = null,
        string? sortColumn = null,
        int cachePayloadVersion = 1) => new(
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
#pragma warning restore HFC0001

    private static string BuildKey(int skip)
        => $"{Tenant}:{User}:etag:projection-page:{ProjectionType}:s{skip}-t25";

    private EventStoreQueryClient NewClient(
        HttpMessageHandler handler,
        EventStoreTestSupport.RecordingAuthRedirector? redirector = null,
        IETagCache? cache = null) => new(
        new SingleClientFactory(handler),
        Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new System.Uri("https://eventstore.test"),
            AccessTokenProvider = _ => System.Threading.Tasks.ValueTask.FromResult<string?>("token"),
        }),
        new TestUserContextAccessor(Tenant, User),
        EventStoreTestSupport.CreateClassifier(),
        cache ?? _cache,
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
        public List<System.Func<HttpRequestMessage, HttpResponseMessage>> Script { get; } = [];
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
