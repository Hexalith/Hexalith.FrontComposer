using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class EventStoreClientTests {
    [Fact]
    public async Task CommandClient_PostsCamelCaseAcceptedRequest_ToDefaultCommandPath() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted) {
            Content = new StringContent("""{"correlationId":"corr-1"}""", Encoding.UTF8, "application/json"),
            Headers = {
                Location = new Uri("https://eventstore.test/api/v1/commands/status/corr-1"),
                RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1)),
            },
        });
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

        CommandResult result = await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken);

        result.MessageId.ShouldBe("01HVTESTULID");
        result.Status.ShouldBe("Accepted");
        result.CorrelationId.ShouldBe("corr-1");
        result.RetryAfter.ShouldBe(TimeSpan.FromSeconds(1));
        handler.Requests.Single().Method.ShouldBe(HttpMethod.Post);
        handler.Requests.Single().RequestUri!.PathAndQuery.ShouldBe("/api/v1/commands");
        handler.Requests.Single().Headers.Authorization!.Scheme.ShouldBe("Bearer");
        handler.ObservedToken.CanBeCanceled.ShouldBeTrue();
        string body = handler.Bodies.Single();
        using JsonDocument document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("messageId").GetString().ShouldBe("01HVTESTULID");
        document.RootElement.GetProperty("tenant").GetString().ShouldBe("acme");
        document.RootElement.GetProperty("domain").GetString().ShouldBe("orders");
        document.RootElement.GetProperty("aggregateId").GetString().ShouldBe("order-1");
        document.RootElement.GetProperty("payload").GetProperty("quantity").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task CommandClient_EmptyRequiredTokenFailsBeforeSend() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        EventStoreOptions options = Options().Value;
        options.AccessTokenProvider = _ => ValueTask.FromResult<string?>(null);
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommandClient_HonorsConfiguredCommandPath() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted) {
            Content = new StringContent("""{"correlationId":"corr-1"}""", Encoding.UTF8, "application/json"),
        });
        EventStoreOptions options = Options().Value;
        options.CommandEndpointPath = "/custom/commands";
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

        _ = await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken);

        handler.Requests.Single().RequestUri!.PathAndQuery.ShouldBe("/custom/commands");
    }

    [Fact]
    public async Task CommandClient_RejectsOversizedBody_BeforeSend() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        EventStoreOptions options = Options().Value;
        options.MaxRequestBytes = 1;
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task QueryClient_SendsEtags_AndReturnsNotModifiedOn304() {
        RecordingHandler handler = new(request => {
            request.Headers.GetValues("If-None-Match").ShouldBe(["\"etag-1\"", "\"etag-2\""]);
            return new HttpResponseMessage(HttpStatusCode.NotModified) {
                Headers = { ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"etag-2\"") },
            };
        });
        EventStoreQueryClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            new EventStoreTestSupport.NoCache(),
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(
            new QueryRequest(
                ProjectionType: "orders",
                TenantId: "acme",
                Domain: "orders",
                AggregateId: "order-1",
                QueryType: "GetOrders",
                ETags: ["\"etag-1\"", "\"etag-2\""]),
            TestContext.Current.CancellationToken);

        result.IsNotModified.ShouldBeTrue();
        result.ETag.ShouldBe("\"etag-2\"");
        handler.Requests.Single().RequestUri!.PathAndQuery.ShouldBe("/api/v1/queries");
    }

    [Fact]
    public async Task QueryClient_HonorsConfiguredQueryPath() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("""{"payload":[{"id":"order-1"}]}""", Encoding.UTF8, "application/json"),
        });
        EventStoreOptions options = Options().Value;
        options.QueryEndpointPath = "/custom/queries";
        EventStoreQueryClient sut = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            new EventStoreTestSupport.NoCache(),
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);

        QueryResult<OrderProjection> result = await sut.QueryAsync<OrderProjection>(
            new QueryRequest(
                ProjectionType: "orders",
                TenantId: "acme",
                Domain: "orders",
                AggregateId: "order-1",
                QueryType: "GetOrders"),
            TestContext.Current.CancellationToken);

        result.Items.Single().Id.ShouldBe("order-1");
        handler.Requests.Single().RequestUri!.PathAndQuery.ShouldBe("/custom/queries");
    }

    [Fact]
    public async Task QueryClient_RejectsMoreThanTenEtags_BeforeSend() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        EventStoreQueryClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            new EventStoreTestSupport.NoCache(),
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);

        QueryRequest request = new(
            ProjectionType: "orders",
            TenantId: "acme",
            Domain: "orders",
            AggregateId: "order-1",
            QueryType: "GetOrders",
            ETags: Enumerable.Range(0, 11).Select(i => $"\"etag-{i}\"").ToArray());

        _ = await Should.ThrowAsync<ArgumentException>(
            async () => await sut.QueryAsync<OrderProjection>(request, TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public void RequestContent_AcceptsExactByteLimit_AndRejectsOneByteOver() {
        var request = new { value = new string('a', 16) };
        int exactBytes = JsonSerializer.SerializeToUtf8Bytes(request, EventStoreRequestContent.JsonOptions).Length;

        using ByteArrayContent content = EventStoreRequestContent.Create(request, exactBytes);

        content.Headers.ContentType!.MediaType.ShouldBe("application/json");
        _ = Should.Throw<InvalidOperationException>(() => EventStoreRequestContent.Create(request, exactBytes - 1));
    }

    [Fact]
    public async Task CommandClient_RejectsTenantMismatch_BeforeSend() {
        // DN1: command body declares one tenant; authenticated user belongs to another.
        // Authenticated tenant must always win — mismatched override is a privilege-escalation hazard.
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

        ShipOrderCommand command = new() { TenantId = "victim" };
        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.DispatchAsync(command, TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommandClient_FailsClosed_WhenAuthenticatedTenantIsMissing() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new FixedUlidFactory(),
            new TestUserContextAccessor(tenantId: null, userId: "alice"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task CommandClient_PreservesAuthenticatedTenantCasing_VerbatimOnTheWire() {
        // DN2: tenant comes from auth stack and must round-trip exactly — no slug normalization.
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted) {
            Content = new StringContent("""{"correlationId":"corr-1"}""", Encoding.UTF8, "application/json"),
        });
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new FixedUlidFactory(),
            new TestUserContextAccessor("Acme_Corp", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

        _ = await sut.DispatchAsync(new ShipOrderCommand { TenantId = "Acme_Corp" }, TestContext.Current.CancellationToken);

        using JsonDocument document = JsonDocument.Parse(handler.Bodies.Single());
        document.RootElement.GetProperty("tenant").GetString().ShouldBe("Acme_Corp");
    }

    [Fact]
    public async Task CommandClient_FallsBackToRetryAfterDate_WhenDeltaIsAbsent() {
        // P5: server may send Retry-After as an HTTP-date instead of seconds-delta.
        DateTimeOffset future = DateTimeOffset.UtcNow.AddSeconds(30);
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted) {
            Headers = {
                RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(future),
            },
        });
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance);

        CommandResult result = await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken);

        result.RetryAfter.ShouldNotBeNull();
        result.RetryAfter!.Value.TotalSeconds.ShouldBeInRange(20, 35);
    }

    [Fact]
    public async Task QueryClient_RejectsEtagsContainingControlCharacters_BeforeSend() {
        // P4: TryAddWithoutValidation skips CRLF detection — must guard explicitly.
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK));
        EventStoreQueryClient sut = new(
            new SingleClientFactory(handler),
            Options(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            new EventStoreTestSupport.NoCache(),
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);

        QueryRequest request = new(
            ProjectionType: "orders",
            TenantId: "acme",
            Domain: "orders",
            AggregateId: "order-1",
            QueryType: "GetOrders",
            ETags: ["\"etag-1\"\r\nInjected-Header: value"]);

        _ = await Should.ThrowAsync<ArgumentException>(
            async () => await sut.QueryAsync<OrderProjection>(request, TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
        handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public void EventStoreValidation_InterpolatesConfiguredMaxCount_InErrorMessage() {
        // P6: hardcoded "10" misled operators who lowered MaxETagCount.
        ArgumentException ex = Should.Throw<ArgumentException>(
            () => EventStoreValidation.ValidateETagCount(["\"a\"", "\"b\"", "\"c\""], maxCount: 2));
        ex.Message.ShouldContain("At most 2");
    }

    private static IOptions<EventStoreOptions> Options()
        => Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new Uri("https://eventstore.test"),
            AccessTokenProvider = _ => ValueTask.FromResult<string?>("token"),
        });

    [BoundedContext("Orders")]
    private sealed class ShipOrderCommand {
        public string TenantId { get; set; } = "acme";
        public string AggregateId { get; set; } = "order-1";
        public int Quantity { get; set; } = 3;
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
        public List<HttpRequestMessage> Requests { get; } = [];
        public List<string> Bodies { get; } = [];
        public CancellationToken ObservedToken { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            ObservedToken = cancellationToken;
            Requests.Add(request);
            if (request.Content is not null) {
                Bodies.Add(await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
            }

            return responseFactory(request);
        }
    }
}
