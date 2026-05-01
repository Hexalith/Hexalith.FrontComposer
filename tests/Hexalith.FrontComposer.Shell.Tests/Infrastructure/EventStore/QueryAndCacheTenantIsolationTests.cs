using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.State.ETagCache;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class QueryAndCacheTenantIsolationTests {
    [Fact]
    public async Task QueryAsync_BlankTenant_UsesAuthenticatedTenantOnWire() {
        RecordingHandler handler = new();
        CountingCache cache = new();
        EventStoreQueryClient sut = NewClient(handler, cache, "tenant-a", "user-a");

        _ = await sut.QueryAsync<Row>(
            Request(tenantId: " "),
            TestContext.Current.CancellationToken);

        handler.SendCount.ShouldBe(1);
        handler.Body.ShouldContain("\"tenant\":\"tenant-a\"");
    }

    [Fact]
    public async Task QueryAsync_TenantMismatch_BlocksBeforeCacheTokenAndHttpSend() {
        int tokenCalls = 0;
        RecordingHandler handler = new();
        CountingCache cache = new();
        EventStoreQueryClient sut = NewClient(handler, cache, "tenant-a", "user-a", () => tokenCalls++);

        TenantContextException ex = await Should.ThrowAsync<TenantContextException>(
            async () => await sut.QueryAsync<Row>(
                Request(tenantId: "tenant-b", cacheDiscriminator: ETagCacheDiscriminator.ForProjectionPage("Orders.Row", 0, 25)),
                TestContext.Current.CancellationToken).ConfigureAwait(true));

        ex.FailureCategory.ShouldBe(TenantContextFailureCategory.TenantMismatch);
        cache.BuildKeyCount.ShouldBe(0);
        cache.GetCount.ShouldBe(0);
        cache.SetCount.ShouldBe(0);
        tokenCalls.ShouldBe(0);
        handler.SendCount.ShouldBe(0);
    }

    [Fact]
    public void TryBuildKey_SameUserDifferentTenantAndCaseVariants_AreDistinct() {
        CountingCache cache = new();
        string discriminator = ETagCacheDiscriminator.ForProjectionPage("Orders.Row", 0, 25)!;

        cache.TryBuildKey("tenant-a", "user-x", discriminator, out string tenantA).ShouldBeTrue();
        cache.TryBuildKey("tenant-b", "user-x", discriminator, out string tenantB).ShouldBeTrue();
        cache.TryBuildKey("Tenant-A", "user-x", discriminator, out string tenantUpper).ShouldBeTrue();

        tenantA.ShouldNotBe(tenantB);
        tenantA.ShouldNotBe(tenantUpper);
    }

    private static QueryRequest Request(string? tenantId, string? cacheDiscriminator = null)
        => new(
            ProjectionType: "Orders.Row",
            TenantId: tenantId,
            Domain: "orders",
            AggregateId: "orders",
            QueryType: "Orders.Row",
            CacheDiscriminator: cacheDiscriminator);

    private static EventStoreQueryClient NewClient(
        HttpMessageHandler handler,
        IETagCache cache,
        string tenant,
        string user,
        Action? onToken = null) {
        EventStoreOptions options = new() {
            BaseAddress = new Uri("https://eventstore.test"),
            AccessTokenProvider = _ => {
                onToken?.Invoke();
                return ValueTask.FromResult<string?>("token");
            },
        };

        return new EventStoreQueryClient(
            new SingleClientFactory(handler),
            MsOptions.Create(options),
            new TestUserContextAccessor(tenant, user),
            EventStoreTestSupport.CreateClassifier(),
            cache,
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance,
            MsOptions.Create(new FcShellOptions()));
    }

    private sealed record Row(string Id);

    private sealed class TestUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
    }

    private sealed class RecordingHandler : HttpMessageHandler {
        public int SendCount { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            SendCount++;
            Body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new HttpResponseMessage(HttpStatusCode.OK) {
                Content = new StringContent("""{"payload":[{"id":"row-1"}]}""", Encoding.UTF8, "application/json"),
            };
        }
    }

    private sealed class CountingCache : IETagCache {
        public int BuildKeyCount { get; private set; }
        public int GetCount { get; private set; }
        public int SetCount { get; private set; }

        public bool TryBuildKey(string? tenantId, string? userId, string? discriminator, out string key) {
            BuildKeyCount++;
            key = string.Empty;
            if (string.IsNullOrWhiteSpace(tenantId)
                || string.IsNullOrWhiteSpace(userId)
                || string.IsNullOrWhiteSpace(discriminator)
                || tenantId.Contains(':', StringComparison.Ordinal)
                || userId.Contains(':', StringComparison.Ordinal)
                || !ETagCacheDiscriminator.IsAllowlisted(discriminator)) {
                return false;
            }

            key = $"{tenantId}:{userId}:etag:{discriminator}";
            return true;
        }

        public Task<ETagCacheEntry?> TryGetAsync(string key, int expectedPayloadVersion, CancellationToken cancellationToken = default) {
            GetCount++;
            return Task.FromResult<ETagCacheEntry?>(null);
        }

        public Task SetAsync(string key, ETagCacheEntry entry, CancellationToken cancellationToken = default) {
            SetCount++;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByProjectionTypeAsync(
            string tenantId,
            string userId,
            string projectionType,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
