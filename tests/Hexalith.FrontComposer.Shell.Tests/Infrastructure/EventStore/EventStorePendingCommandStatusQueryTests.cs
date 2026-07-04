using System.Net;
using System.Text;
using System.Text.Json;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.State.PendingCommands;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class EventStorePendingCommandStatusQueryTests {
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01CPZ3NDEKTSV4RRFFQ69G5FAV";

    public static TheoryData<string, int, PendingCommandTerminalOutcome?> StatusCases => new() {
        { "Received", 0, null },
        { "Processing", 1, null },
        { "EventsStored", 2, null },
        { "EventsPublished", 3, null },
        { "Completed", 4, PendingCommandTerminalOutcome.Confirmed },
        { "Rejected", 5, PendingCommandTerminalOutcome.Rejected },
        { "PublishFailed", 6, PendingCommandTerminalOutcome.NeedsReview },
        { "TimedOut", 7, PendingCommandTerminalOutcome.NeedsReview },
    };

    [Theory]
    [MemberData(nameof(StatusCases))]
    public async Task QueryAsync_MapsEventStoreStatus_ToPendingObservation(
        string status,
        int statusCode,
        PendingCommandTerminalOutcome? expectedOutcome) {
        RecordingHandler handler = new(_ => JsonResponse(status, statusCode, retryAfter: statusCode < 4));
        EventStorePendingCommandStatusQuery sut = CreateSut(handler);

        PendingCommandOutcomeObservation? observation = await sut.QueryAsync(
            Pending(),
            TestContext.Current.CancellationToken);

        if (expectedOutcome is null) {
            observation.ShouldBeNull();
        }
        else {
            observation.ShouldNotBeNull();
            observation.Source.ShouldBe(PendingCommandOutcomeSource.IdempotencyStatusQuery);
            observation.Outcome.ShouldBe(expectedOutcome.Value);
            observation.MessageId.ShouldBe(MessageId);
        }

        handler.Requests.Single().Method.ShouldBe(HttpMethod.Get);
        handler.Requests.Single().RequestUri!.PathAndQuery.ShouldBe($"/api/v1/commands/status/{MessageId}");
        handler.Requests.Single().Headers.Authorization!.Scheme.ShouldBe("Bearer");
        handler.Requests.Single().Headers.Authorization!.Parameter.ShouldBe("token");
    }

    [Fact]
    public async Task QueryAsync_RejectedStatus_BoundsPlainTextRejectionMetadata() {
        string oversized = new('x', 700);
        RecordingHandler handler = new(_ => JsonResponse(
            "Rejected",
            5,
            rejectionEventType: $"OrderRejected\r\n{oversized}",
            failureReason: $"Business rule failed\r\n{oversized}"));
        EventStorePendingCommandStatusQuery sut = CreateSut(handler);

        PendingCommandOutcomeObservation? observation = await sut.QueryAsync(
            Pending(),
            TestContext.Current.CancellationToken);

        observation.ShouldNotBeNull();
        observation.Outcome.ShouldBe(PendingCommandTerminalOutcome.Rejected);
        observation.RejectionTitle.ShouldNotBeNull();
        observation.RejectionTitle!.Length.ShouldBeLessThanOrEqualTo(512);
        observation.RejectionTitle.ShouldNotContain('\r');
        observation.RejectionTitle.ShouldNotContain('\n');
        observation.RejectionDetail.ShouldNotBeNull();
        observation.RejectionDetail!.Length.ShouldBeLessThanOrEqualTo(512);
        observation.RejectionDetail.ShouldNotContain('\r');
        observation.RejectionDetail.ShouldNotContain('\n');
    }

    [Fact]
    public async Task QueryAsync_CompletedStatus_DoesNotPromoteAggregateIdToRowIdentityMetadata() {
        RecordingHandler handler = new(_ => JsonResponse("Completed", 4));
        EventStorePendingCommandStatusQuery sut = CreateSut(handler);

        PendingCommandOutcomeObservation? observation = await sut.QueryAsync(
            Pending(),
            TestContext.Current.CancellationToken);

        observation.ShouldNotBeNull();
        observation.MessageId.ShouldBe(MessageId);
        observation.ProjectionTypeName.ShouldBeNull();
        observation.LaneKey.ShouldBeNull();
        observation.EntityKey.ShouldBeNull();
        observation.ExpectedStatusSlot.ShouldBeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest, typeof(HttpRequestException))]
    [InlineData(HttpStatusCode.Unauthorized, typeof(AuthRedirectRequiredException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(QueryFailureException))]
    [InlineData(HttpStatusCode.NotFound, typeof(QueryFailureException))]
    [InlineData((HttpStatusCode)429, typeof(QueryFailureException))]
    [InlineData(HttpStatusCode.InternalServerError, typeof(HttpRequestException))]
    public async Task QueryAsync_NonOkStatus_ThrowsNonMutatingFailure(
        HttpStatusCode statusCode,
        Type exceptionType) {
        ArgumentNullException.ThrowIfNull(exceptionType);

        RecordingHandler handler = new(_ => new HttpResponseMessage(statusCode) {
            Content = new StringContent("""{"title":"failure"}""", Encoding.UTF8, "application/problem+json"),
        });
        EventStorePendingCommandStatusQuery sut = CreateSut(handler);

        Exception ex = await Should.ThrowAsync<Exception>(
            async () => await sut.QueryAsync(Pending(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        exceptionType.IsAssignableFrom(ex.GetType()).ShouldBeTrue();
    }

    [Theory]
    [InlineData("""{"correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","status":"Bogus","statusCode":99,"timestamp":"2026-06-04T00:00:00Z"}""")]
    [InlineData("""{"correlationId":"01ARZ3NDEKTSV4RRFFQ69G5FAV","status":"Completed","statusCode":5,"timestamp":"2026-06-04T00:00:00Z"}""")]
    [InlineData("""{""")]
    public async Task QueryAsync_MalformedOrUnknownPayload_ThrowsNonMutatingFailure(string body) {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        });
        EventStorePendingCommandStatusQuery sut = CreateSut(handler);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.QueryAsync(Pending(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);
    }

    [Fact]
    public async Task QueryAsync_OversizedPayload_ThrowsNonMutatingFailure() {
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(new string('x', EventStoreOptions.MinAllowedResponseBytes + 1), Encoding.UTF8, "application/json"),
        });
        EventStorePendingCommandStatusQuery sut = CreateSut(handler, maxResponseBytes: EventStoreOptions.MinAllowedResponseBytes);

        InvalidOperationException ex = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.QueryAsync(Pending(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        ex.Message.ShouldContain("MaxResponseBytes");
    }

    [Fact]
    public async Task QueryAsync_PropagatesCancellation() {
        using CancellationTokenSource cts = new();
        RecordingHandler handler = new(_ => throw new OperationCanceledException(cts.Token));
        EventStorePendingCommandStatusQuery sut = CreateSut(handler);
        await cts.CancelAsync();

        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await sut.QueryAsync(Pending(), cts.Token).ConfigureAwait(true)).ConfigureAwait(true);
    }

    private static EventStorePendingCommandStatusQuery CreateSut(HttpMessageHandler handler, int? maxResponseBytes = null)
        => new(
            new SingleClientFactory(handler),
            Options(maxResponseBytes),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStorePendingCommandStatusQuery>.Instance);

    private static IOptions<EventStoreOptions> Options(int? maxResponseBytes)
        => Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new Uri("https://eventstore.test"),
            AccessTokenProvider = _ => ValueTask.FromResult<string?>("token"),
            MaxResponseBytes = maxResponseBytes ?? EventStoreOptions.DefaultMaxResponseBytes,
        });

    private static PendingCommandEntry Pending()
        => new(
            CorrelationId,
            MessageId,
            "Counter.Increment",
            "Counter.Count",
            "counter-counts",
            "counter-1",
            "Approved",
            "Draft",
            new DateTimeOffset(2026, 6, 4, 12, 0, 0, TimeSpan.Zero),
            PendingCommandStatus.Pending);

    private static HttpResponseMessage JsonResponse(
        string status,
        int statusCode,
        bool retryAfter = false,
        string? rejectionEventType = null,
        string? failureReason = null) {
        HttpResponseMessage response = new(HttpStatusCode.OK) {
            Content = new StringContent(
                $$"""
                {
                  "correlationId": "{{MessageId}}",
                  "status": "{{status}}",
                  "statusCode": {{statusCode}},
                  "timestamp": "2026-06-04T00:00:00Z",
                  "aggregateId": "counter-1",
                  "eventCount": 1,
                  "rejectionEventType": {{JsonString(rejectionEventType)}},
                  "failureReason": {{JsonString(failureReason)}},
                  "timeoutDuration": "PT30S"
                }
                """,
                Encoding.UTF8,
                "application/json"),
        };
        if (retryAfter) {
            response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromSeconds(1));
        }

        return response;
    }

    private static string JsonString(string? value)
        => value is null
            ? "null"
            : JsonSerializer.Serialize(value);

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
    }
}
