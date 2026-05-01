using System.Diagnostics;
using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.Telemetry;

[Trait("Category", "Governance")]
public sealed class EventStoreTelemetryTests {
    [Fact]
    public async Task CommandDispatch_EmitsSanitizedActivity() {
        const string token = "secret-token-do-not-log";
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted) {
            Content = new StringContent("""{"correlationId":"corr-1"}""", Encoding.UTF8, "application/json"),
        });
        EventStoreOptions options = Options().Value;
        options.AccessTokenProvider = _ => ValueTask.FromResult<string?>(token);
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new FixedUlidFactory(),
            new TestUserContextAccessor("tenant-secret", "user-secret"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);
        using ActivityCapture capture = ActivityCapture.Start();

        _ = await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken);

        Activity activity = capture.Single(
            FrontComposerTelemetry.CommandDispatchOperation,
            activity => string.Equals(
                activity.GetTagItem(FrontComposerTelemetry.MessageIdTag) as string,
                "01HVTELEMETRY01",
                StringComparison.Ordinal));
        activity.GetTagItem(FrontComposerTelemetry.CommandTypeTag).ShouldBe(typeof(ShipOrderCommand).FullName);
        activity.GetTagItem(FrontComposerTelemetry.MessageIdTag).ShouldBe("01HVTELEMETRY01");
        activity.GetTagItem(FrontComposerTelemetry.CorrelationIdTag).ShouldBe("corr-1");
        activity.GetTagItem(FrontComposerTelemetry.TenantMarkerTag).ShouldBe("present");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain(token);
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("tenant-secret");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("user-secret");

        // F20 — assert the token was actually transmitted in the Authorization header so the
        // negative tag-redaction assertion above is non-vacuous (we proved it CAN leak; we then
        // proved it does NOT leak into span tags). Without this positive proof the prior
        // assertion would pass even if no token were ever sent.
        handler.LastRequest.ShouldNotBeNull();
        handler.LastRequest!.Headers.Authorization.ShouldNotBeNull();
        handler.LastRequest.Headers.Authorization!.Parameter.ShouldBe(token);
    }

    [Fact]
    public async Task QueryNotModifiedTwiceWithoutCache_TagsProtocolDriftRetry() {
        // F39 — the protocol_drift_retry cache outcome is otherwise covered only by an
        // operational log line; surface it through ActivityListener so dashboards see a
        // distinct outcome value when EventStore returns 304 twice without a matching cache.
        int requestCount = 0;
        RecordingHandler handler = new(_ => {
            requestCount++;
            return new HttpResponseMessage(HttpStatusCode.NotModified);
        });
        EventStoreQueryClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new TestUserContextAccessor("tenant-secret", "user-secret"),
            EventStoreTestSupport.CreateClassifier(),
            new EmptyKeyedCache(),
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);
        using ActivityCapture capture = ActivityCapture.Start();

        QueryRequest request = new(
            ProjectionType: "orders",
            TenantId: "tenant-secret",
            Domain: "orders",
            AggregateId: "order-1",
            QueryType: "GetOrders",
            CacheDiscriminator: "discriminator-1",
            CachePayloadVersion: 1);

        // The first call sends If-None-Match with a non-null cache key but null entry
        // → outer activity tags protocol_drift_retry, recurses; recursion observes 304
        // again with allowProtocolDriftRetry=false and throws HttpRequestException.
        _ = await Should.ThrowAsync<HttpRequestException>(() =>
            sut.QueryAsync<OrderProjection>(request, TestContext.Current.CancellationToken));

        requestCount.ShouldBe(2);
        Activity outerActivity = capture.AllOf(FrontComposerTelemetry.QueryExecuteOperation)
            .Single(a => string.Equals(
                a.GetTagItem(FrontComposerTelemetry.OutcomeTag) as string,
                "protocol_drift_retry",
                StringComparison.Ordinal));
        outerActivity.GetTagItem(FrontComposerTelemetry.OutcomeTag).ShouldBe("protocol_drift_retry");
    }

    /// <summary>
    /// Cache fixture that returns true for TryBuildKey (so the QueryClient enters the
    /// cache-integration code path) but null for TryGetAsync (so the protocol-drift retry
    /// branch is reachable).
    /// </summary>
    private sealed class EmptyKeyedCache : Hexalith.FrontComposer.Shell.State.ETagCache.IETagCache {
        public bool TryBuildKey(string? tenantId, string? userId, string? discriminator, out string key) {
            key = $"k|{tenantId}|{userId}|{discriminator}";
            return true;
        }

        public Task<Hexalith.FrontComposer.Shell.State.ETagCache.ETagCacheEntry?> TryGetAsync(string key, int expectedPayloadVersion, CancellationToken cancellationToken = default)
            => Task.FromResult<Hexalith.FrontComposer.Shell.State.ETagCache.ETagCacheEntry?>(null);

        public Task SetAsync(string key, Hexalith.FrontComposer.Shell.State.ETagCache.ETagCacheEntry entry, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task RemoveByProjectionTypeAsync(
            string tenantId,
            string userId,
            string projectionType,
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task QueryNotModified_EmitsNoChangeActivityWithoutRawEtag() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.NotModified) {
            Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"raw-etag-secret\"") },
        });
        EventStoreQueryClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new TestUserContextAccessor("tenant-secret", "user-secret"),
            EventStoreTestSupport.CreateClassifier(),
            new EventStoreTestSupport.NoCache(),
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);
        using ActivityCapture capture = ActivityCapture.Start();

        _ = await sut.QueryAsync<OrderProjection>(
            new QueryRequest(
                ProjectionType: "orders",
                TenantId: "tenant-secret",
                Domain: "orders",
                AggregateId: "order-1",
                QueryType: "GetOrders"),
            TestContext.Current.CancellationToken);

        Activity activity = capture.Single(
            FrontComposerTelemetry.QueryExecuteOperation,
            activity => string.Equals(
                activity.GetTagItem(FrontComposerTelemetry.QueryTypeTag) as string,
                "GetOrders",
                StringComparison.Ordinal));
        activity.GetTagItem(FrontComposerTelemetry.ProjectionTypeTag).ShouldBe("orders");
        activity.GetTagItem(FrontComposerTelemetry.QueryTypeTag).ShouldBe("GetOrders");
        activity.GetTagItem(FrontComposerTelemetry.OutcomeTag).ShouldBe("not_modified");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("\"raw-etag-secret\"");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("tenant-secret");
    }

    private static IOptions<EventStoreOptions> Options()
        => Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new Uri("https://eventstore.test"),
            AccessTokenProvider = _ => ValueTask.FromResult<string?>("token"),
        });

    [BoundedContext("Orders")]
    private sealed class ShipOrderCommand {
        public string TenantId { get; set; } = "tenant-secret";
        public string AggregateId { get; set; } = "order-1";
    }

    private sealed record OrderProjection(string Id);

    private sealed class FixedUlidFactory : IUlidFactory {
        public string NewUlid() => "01HVTELEMETRY01";
    }

    private sealed class TestUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class ActivityCapture : IDisposable {
        private readonly ActivityListener _listener;
        private readonly List<Activity> _activities = [];
        private readonly object _sync = new();

        private ActivityCapture() {
            _listener = new ActivityListener {
                ShouldListenTo = source => source.Name == Hexalith.FrontComposer.Contracts.Telemetry.FrontComposerActivitySource.Name,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => {
                    lock (_sync) {
                        _activities.Add(activity);
                    }
                },
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public static ActivityCapture Start() => new();

        public Activity Single(string operationName, Func<Activity, bool> predicate) {
            Activity[] snapshot;
            lock (_sync) {
                snapshot = [.. _activities];
            }

            return snapshot.Single(activity => activity.OperationName == operationName && predicate(activity));
        }

        public IEnumerable<Activity> AllOf(string operationName) {
            Activity[] snapshot;
            lock (_sync) {
                snapshot = [.. _activities];
            }

            return snapshot.Where(activity => activity.OperationName == operationName);
        }

        public void Dispose() => _listener.Dispose();
    }
}
