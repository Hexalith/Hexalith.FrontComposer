using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Telemetry;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;
using Hexalith.FrontComposer.Shell.Options;
using Hexalith.FrontComposer.Shell.Services.Auth;
using Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

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
        using var capture = ActivityCapture.Start();

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
    public async Task CommandDispatchTenantMismatchEmitsRedactedBlockedActivity() {
        RecordingHandler handler = new(_ => throw new InvalidOperationException("No request expected."));
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new FixedUlidFactory(),
            new TestUserContextAccessor("authenticated-tenant-secret", "caller-secret"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);
        using var capture = ActivityCapture.Start();

        TenantContextException failure = await Should.ThrowAsync<TenantContextException>(() =>
            sut.DispatchAsync(new ShipOrderCommand { TenantId = "requested-tenant-secret" },
                TestContext.Current.CancellationToken));

        Activity activity = capture.Single(
            FrontComposerTelemetry.CommandDispatchOperation,
            span => Equals(span.GetTagItem(FrontComposerTelemetry.CorrelationIdTag), failure.CorrelationId));
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(FrontComposerTelemetry.OutcomeTag).ShouldBe("blocked");
        activity.GetTagItem(FrontComposerTelemetry.FailureCategoryTag).ShouldBe(nameof(TenantContextFailureCategory.TenantMismatch));
        activity.GetTagItem(FrontComposerTelemetry.TenantMarkerTag).ShouldBe("absent");
        activity.GetTagItem(FrontComposerTelemetry.HttpStatusCodeTag).ShouldBeNull();
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("authenticated-tenant-secret");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("requested-tenant-secret");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("caller-secret");
        handler.LastRequest.ShouldBeNull();
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
        using var capture = ActivityCapture.Start();

        QueryRequest request = QueryRequest.Create(
            Criteria: new ProjectionQuery("telemetry-protocol-drift-orders"),
            TenantId: "tenant-secret",
            Domain: "orders",
            AggregateId: "order-1",
            QueryType: "TelemetryProtocolDriftRetryProbe",
            CacheDiscriminator: "discriminator-1",
            CachePayloadVersion: 1);

        // The first call sends If-None-Match with a non-null cache key but null entry
        // → outer activity tags protocol_drift_retry, recurses; recursion observes 304
        // again with allowProtocolDriftRetry=false and throws HttpRequestException.
        _ = await Should.ThrowAsync<HttpRequestException>(() =>
            sut.QueryAsync<OrderProjection>(request, TestContext.Current.CancellationToken));

        requestCount.ShouldBe(2);
        Activity outerActivity = capture.AllOf(FrontComposerTelemetry.QueryExecuteOperation)
            .Single(a =>
                string.Equals(
                    a.GetTagItem(FrontComposerTelemetry.ProjectionTypeTag) as string,
                    "telemetry-protocol-drift-orders",
                    StringComparison.Ordinal)
                && string.Equals(
                    a.GetTagItem(FrontComposerTelemetry.QueryTypeTag) as string,
                    "TelemetryProtocolDriftRetryProbe",
                    StringComparison.Ordinal)
                && string.Equals(
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
        using var capture = ActivityCapture.Start();

        _ = await sut.QueryAsync<OrderProjection>(
            QueryRequest.Create(
                Criteria: new ProjectionQuery("telemetry-raw-etag-orders"),
                TenantId: "tenant-secret",
                Domain: "orders",
                AggregateId: "order-1",
                QueryType: "TelemetryRawEtagProbe"),
            TestContext.Current.CancellationToken);

        Activity activity = capture.Single(
            FrontComposerTelemetry.QueryExecuteOperation,
            activity => string.Equals(
                    activity.GetTagItem(FrontComposerTelemetry.QueryTypeTag) as string,
                    "TelemetryRawEtagProbe",
                    StringComparison.Ordinal)
                && string.Equals(
                    activity.GetTagItem(FrontComposerTelemetry.OutcomeTag) as string,
                    "not_modified",
                    StringComparison.Ordinal));
        activity.GetTagItem(FrontComposerTelemetry.ProjectionTypeTag).ShouldBe("telemetry-raw-etag-orders");
        activity.GetTagItem(FrontComposerTelemetry.QueryTypeTag).ShouldBe("TelemetryRawEtagProbe");
        activity.GetTagItem(FrontComposerTelemetry.OutcomeTag).ShouldBe("not_modified");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("\"raw-etag-secret\"");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("tenant-secret");
    }


    [Fact]
    public async Task TokenAcquisitionFailureKeepsProviderDetailOutOfDiagnostics() {
        const string secret = "secret-from-access-token-provider";
        CapturingLogger<FrontComposerAccessTokenProvider> tokenLogger = new();
        FrontComposerAuthenticationOptions authentication = new();
        authentication.TokenRelay.HostAccessTokenProvider = _ => throw new InvalidOperationException(secret);
        FrontComposerAccessTokenProvider tokenProvider = new(
            new HttpContextAccessor(), new CircuitServicesAccessor(), new FrontComposerUserTokenStore(),
            Microsoft.Extensions.Options.Options.Create(authentication), tokenLogger);
        EventStoreOptions options = Options().Value;
        options.AccessTokenProvider = tokenProvider.GetAccessTokenAsync;
        RecordingHandler handler = new(_ => throw new InvalidOperationException("No request expected."));
        CapturingLogger<EventStoreCommandClient> logger = new();
        EventStoreCommandClient client = new(
            new SingleClientFactory(handler), Microsoft.Extensions.Options.Options.Create(options),
            new FixedUlidFactory(), new TestUserContextAccessor("tenant-secret", "user-secret"),
            EventStoreTestSupport.CreateClassifier(), logger);
        using var parent = new Activity(nameof(TokenAcquisitionFailureKeepsProviderDetailOutOfDiagnostics)).SetIdFormat(ActivityIdFormat.W3C).Start();
        using var capture = ActivityCapture.Start();

        var error = await Should.ThrowAsync<FrontComposerAuthenticationException>(() =>
            client.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken));

        error.InnerException!.Message.ShouldBe(secret);
        handler.LastRequest.ShouldBeNull();
        CapturedLogEntry warning = TelemetryRedactionAssertions.AssertLog(
            tokenLogger.Entries, 5666, secret, "tenant-secret", "user-secret");
        warning.State["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
        TelemetryRedactionAssertions.AssertLogs(logger.Entries, secret, "tenant-secret", "user-secret");
        Activity activity = TelemetryRedactionAssertions.AssertActivity(capture, parent.TraceId,
            FrontComposerTelemetry.CommandDispatchOperation, secret, "tenant-secret", "user-secret");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(FrontComposerTelemetry.FailureCategoryTag).ShouldBe(nameof(FrontComposerAuthenticationException));
        activity.GetTagItem(FrontComposerTelemetry.OutcomeTag).ShouldBe("failed");
    }

    [Fact]
    public async Task MalformedQueryJsonKeepsResponseDetailOutOfDiagnostics() {
        const string secret = "secret-response-value";
        // The JSON parser includes the unexpected first character in its error.
        const string body = "!" + secret;
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
        CapturingLogger<EventStoreQueryClient> logger = new();
        EventStoreQueryClient client = new(
            new SingleClientFactory(handler), Options(), new TestUserContextAccessor("tenant-secret", "user-secret"),
            EventStoreTestSupport.CreateClassifier(), new EventStoreTestSupport.NoCache(),
            new EventStoreTestSupport.RecordingAuthRedirector(), logger);
        using var parent = new Activity(nameof(MalformedQueryJsonKeepsResponseDetailOutOfDiagnostics)).SetIdFormat(ActivityIdFormat.W3C).Start();
        using var capture = ActivityCapture.Start();

        var error = await Should.ThrowAsync<ProjectionSchemaMismatchException>(() =>
            client.QueryAsync<OrderProjection>(
                QueryRequest.Create(Criteria: new ProjectionQuery("orders"), TenantId: "tenant-secret", Domain: "orders",
                    AggregateId: "order-1", QueryType: "ReadOrders"),
                TestContext.Current.CancellationToken));

        error.InnerException.ShouldBeAssignableTo<JsonException>();
        error.InnerException!.Message.ShouldContain("'!'");
        CapturedLogEntry warning = TelemetryRedactionAssertions.AssertLog(
            logger.Entries, 5611, secret, "'!'", "tenant-secret", "user-secret");
        warning.State["FailureCategory"].ShouldBe(error.InnerException!.GetType().Name);
        Activity activity = TelemetryRedactionAssertions.AssertActivity(capture, parent.TraceId,
            FrontComposerTelemetry.QueryExecuteOperation, secret, "'!'", "tenant-secret", "user-secret");
        activity.Status.ShouldBe(ActivityStatusCode.Error);
        activity.GetTagItem(FrontComposerTelemetry.FailureCategoryTag).ShouldBe(error.InnerException!.GetType().Name);
    }

    [Fact]
    public async Task LifecycleObserverFailureKeepsExceptionDetailOutOfDiagnostics() {
        const string secret = "secret-from-lifecycle-observer";
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted) {
            Content = new StringContent("""{"correlationId":"corr-1"}""", Encoding.UTF8, "application/json"),
        });
        CapturingLogger<EventStoreCommandClient> logger = new();
        EventStoreCommandClient client = new(
            new SingleClientFactory(handler), Options(), new FixedUlidFactory(),
            new TestUserContextAccessor("tenant-secret", "user-secret"), EventStoreTestSupport.CreateClassifier(), logger);
        using var parent = new Activity(nameof(LifecycleObserverFailureKeepsExceptionDetailOutOfDiagnostics)).SetIdFormat(ActivityIdFormat.W3C).Start();
        using var capture = ActivityCapture.Start();

        CommandResult result = await client.DispatchAsync(new ShipOrderCommand(), (_, _) => throw new InvalidOperationException(secret),
            TestContext.Current.CancellationToken);

        result.Status.ShouldBe(CommandResultStatus.Accepted);
        CapturedLogEntry warning = TelemetryRedactionAssertions.AssertLog(
            logger.Entries, 5854, secret, result.MessageId, "tenant-secret", "user-secret");
        warning.State["ExceptionType"].ShouldBe(typeof(InvalidOperationException).FullName);
        warning.State["MessageIdDigest"].ShouldBeOfType<string>().ShouldNotBeNullOrWhiteSpace();
        Activity activity = TelemetryRedactionAssertions.AssertActivity(capture, parent.TraceId,
            FrontComposerTelemetry.CommandDispatchOperation, secret, "tenant-secret", "user-secret");
        activity.GetTagItem(FrontComposerTelemetry.OutcomeTag).ShouldBe("accepted");
        activity.GetTagItem(FrontComposerTelemetry.FailureCategoryTag).ShouldBeNull();
    }

    [Fact]
    public async Task CommandDispatchPreservesMaximumSupportedCorrelationInActivity() {
        string correlationId = new string('C', 127) + "Z";
        RecordingHandler handler = new(_ => {
            HttpResponseMessage response = new(HttpStatusCode.Accepted);
            response.Headers.Add("X-Correlation-ID", correlationId);
            return response;
        });
        EventStoreCommandClient client = new(
            new SingleClientFactory(handler), Options(), new FixedUlidFactory(),
            new TestUserContextAccessor("tenant-secret", "user-secret"), EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);
        using var parent = new Activity(nameof(CommandDispatchPreservesMaximumSupportedCorrelationInActivity)).SetIdFormat(ActivityIdFormat.W3C).Start();
        using var capture = ActivityCapture.Start();

        CommandResult result = await client.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken);

        result.CorrelationId.ShouldBe(correlationId);
        Activity activity = capture.Single(FrontComposerTelemetry.CommandDispatchOperation, span => span.TraceId == parent.TraceId);
        activity.GetTagItem(FrontComposerTelemetry.CorrelationIdTag).ShouldBe(correlationId);
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
}
