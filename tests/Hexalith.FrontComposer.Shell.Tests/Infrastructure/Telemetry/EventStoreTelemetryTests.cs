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
                "01HVTESTULID",
                StringComparison.Ordinal));
        activity.GetTagItem(FrontComposerTelemetry.CommandTypeTag).ShouldBe(typeof(ShipOrderCommand).FullName);
        activity.GetTagItem(FrontComposerTelemetry.MessageIdTag).ShouldBe("01HVTESTULID");
        activity.GetTagItem(FrontComposerTelemetry.CorrelationIdTag).ShouldBe("corr-1");
        activity.GetTagItem(FrontComposerTelemetry.TenantMarkerTag).ShouldBe("present");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain(token);
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("tenant-secret");
        activity.Tags.Select(static tag => tag.Value).ShouldNotContain("user-secret");
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
        public string NewUlid() => "01HVTESTULID";
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
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));
    }

    private sealed class ActivityCapture : IDisposable {
        private readonly ActivityListener _listener;
        private readonly List<Activity> _activities = [];

        private ActivityCapture() {
            _listener = new ActivityListener {
                ShouldListenTo = source => source.Name == Hexalith.FrontComposer.Contracts.Telemetry.FrontComposerActivitySource.Name,
                Sample = static (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                ActivityStopped = activity => _activities.Add(activity),
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public static ActivityCapture Start() => new();

        public Activity Single(string operationName, Func<Activity, bool> predicate)
            => _activities.Single(activity => activity.OperationName == operationName && predicate(activity));

        public void Dispose() => _listener.Dispose();
    }
}
