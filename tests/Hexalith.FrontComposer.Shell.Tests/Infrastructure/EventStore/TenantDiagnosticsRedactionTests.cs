using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.State.ETagCache;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

using NSubstitute;

using Shouldly;

using MsOptions = Microsoft.Extensions.Options.Options;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class TenantDiagnosticsRedactionTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DelayedQueryWithLostIdentityOmitsSensitiveValuesAsync(bool useFilter) {
        const string Tenant = "tenant-redaction-sentinel";
        const string User = "user-redaction-sentinel";
        const string Bearer = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiJzZW50aW5lbCJ9.fake-signature";
        const string Etag = "etag-redaction-sentinel";
        const string Payload = "row-redaction-sentinel";
        const string Filter = "filter-redaction-sentinel";
        const string CacheKey = "cache-key-redaction-sentinel";

        string? currentTenant = Tenant;
        IUserContextAccessor identity = Substitute.For<IUserContextAccessor>();
        identity.TenantId.Returns(_ => currentTenant);
        identity.UserId.Returns(User);
        CapturingLogger<EventStoreQueryClient> logger = new();
        CapturingLogger<EventStoreResponseClassifier> classifierLogger = new();
        IETagCache cache = Substitute.For<IETagCache>();
        cache.TryBuildKey(Tenant, User, Arg.Any<string>(), out Arg.Any<string>()).Returns(call => {
            call[3] = CacheKey;
            return true;
        });

        using QueryHandler handler = new(async (message, token) => {
            message.Headers.Authorization.ShouldNotBeNull().Parameter.ShouldBe(Bearer);
            message.Headers.GetValues("If-None-Match").ShouldContain($"\"{Etag}\"");
            if (useFilter) {
                string body = await message.Content.ShouldNotBeNull().ReadAsStringAsync(token).ConfigureAwait(false);
                body.ShouldContain(Filter);
            }

            currentTenant = null;
            HttpResponseMessage response = new(HttpStatusCode.OK) {
                Content = new StringContent($$"""{"payload":["{{Payload}}"]}""", Encoding.UTF8, "application/json"),
            };
            response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue($"\"{Etag}\"");
            return response;
        });
        using HttpClient http = new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
        IHttpClientFactory clients = Substitute.For<IHttpClientFactory>();
        clients.CreateClient(Arg.Any<string>()).Returns(http);
        EventStoreQueryClient query = new(
            clients,
            MsOptions.Create(new EventStoreOptions { AccessTokenProvider = _ => ValueTask.FromResult<string?>(Bearer) }),
            identity,
            new EventStoreResponseClassifier(classifierLogger),
            cache,
            new EventStoreTestSupport.RecordingAuthRedirector(),
            logger);

        QueryRequest request = QueryRequest.Create(
            new ProjectionQuery("Orders.Row", ColumnFilters: useFilter ? new Dictionary<string, string> { ["Name"] = Filter } : null),
            TenantId: Tenant,
            ETag: $"\"{Etag}\"",
            Domain: "orders",
            AggregateId: "orders",
            QueryType: "Orders.Row",
            CacheDiscriminator: ETagCacheDiscriminator.ForProjectionPage("Orders.Row", 0, 25));
        TenantContextException failure = await Should.ThrowAsync<TenantContextException>(
            () => query.QueryAsync<string>(request, TestContext.Current.CancellationToken));

        failure.FailureCategory.ShouldBe(TenantContextFailureCategory.TenantMissing);
        if (!useFilter) {
            _ = cache.Received(1).TryGetAsync(CacheKey, request.CachePayloadVersion, Arg.Any<CancellationToken>());
        }
        logger.Entries.ShouldContain(entry => entry.Message.Contains("HFC2015", StringComparison.Ordinal)
            && entry.Message.Contains("query-delayed-effect", StringComparison.Ordinal));

        string emitted = string.Join("\n", logger.Entries.Concat(classifierLogger.Entries).SelectMany(entry =>
            entry.State.Select(field => field.ToString()).Append(entry.Message).Append(entry.Exception?.ToString())))
            + failure;
        foreach (string sentinel in new[] { Tenant, User, Bearer, Etag, Payload, Filter, CacheKey }) {
            emitted.ShouldNotContain(sentinel);
        }
    }

    private sealed class QueryHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => send(request, cancellationToken);
    }
}
