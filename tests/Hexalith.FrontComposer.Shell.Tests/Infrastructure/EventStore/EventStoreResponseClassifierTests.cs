using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.Extensions.Logging.Abstractions;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>
/// Story 5-2 T6 / AC1 / AC2 — full response matrix for the centralized classifier. Each fact
/// pins one row in the command/query response taxonomy so generated forms, projection page
/// loaders, and badge readers cannot drift on raw HTTP status parsing.
/// </summary>
public class EventStoreResponseClassifierTests {
    private readonly EventStoreResponseClassifier _classifier = new(NullLogger<EventStoreResponseClassifier>.Instance);

    // -----------------------
    // Command response matrix
    // -----------------------

    [Fact]
    public async Task Command_202Accepted_PreservesCorrelationLocationAndRetryAfter() {
        using HttpResponseMessage response = new(HttpStatusCode.Accepted) {
            Content = new StringContent("""{"correlationId":"corr-1"}""", Encoding.UTF8, "application/json"),
        };
        response.Headers.Location = new System.Uri("https://eventstore.test/api/v1/commands/status/corr-1");
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(System.TimeSpan.FromSeconds(2));
        response.Headers.TryAddWithoutValidation("X-Correlation-ID", "corr-1");

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        classification.IsAccepted.ShouldBeTrue();
        classification.CorrelationId.ShouldBe("corr-1");
        classification.Location.ShouldNotBeNull();
        classification.RetryAfter.ShouldBe(System.TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task Command_400BadRequest_ProducesCommandValidationException_WithProblemDetails() {
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest) {
            Content = new StringContent(
                """{"title":"Validation failed","detail":"see fields","errors":{"Quantity":["must be > 0"],"Items":"single"}}""",
                Encoding.UTF8,
                "application/problem+json"),
        };

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        classification.IsAccepted.ShouldBeFalse();
        CommandValidationException ex = classification.Failure.ShouldBeOfType<CommandValidationException>();
        ex.Problem.Title.ShouldBe("Validation failed");
        ex.Problem.ValidationErrors["Quantity"][0].ShouldBe("must be > 0");
        ex.Problem.ValidationErrors["Items"][0].ShouldBe("single");
    }

    [Fact]
    public async Task Command_401Unauthorized_ProducesAuthRedirectRequiredException() {
        using HttpResponseMessage response = new(HttpStatusCode.Unauthorized);

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        _ = classification.Failure.ShouldBeOfType<AuthRedirectRequiredException>();
    }

    [Fact]
    public async Task Command_403Forbidden_ProducesCommandWarningException() {
        using HttpResponseMessage response = new(HttpStatusCode.Forbidden) {
            Content = new StringContent("""{"title":"forbidden","detail":"missing scope"}""", Encoding.UTF8, "application/problem+json"),
        };

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        CommandWarningException ex = classification.Failure.ShouldBeOfType<CommandWarningException>();
        ex.Kind.ShouldBe(CommandWarningKind.Forbidden);
        ex.Problem.Title.ShouldBe("forbidden");
    }

    [Fact]
    public async Task Command_404NotFound_ProducesCommandWarningException() {
        using HttpResponseMessage response = new(HttpStatusCode.NotFound) {
            Content = new StringContent("""{"title":"not found"}""", Encoding.UTF8, "application/problem+json"),
        };

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        CommandWarningException ex = classification.Failure.ShouldBeOfType<CommandWarningException>();
        ex.Kind.ShouldBe(CommandWarningKind.NotFound);
    }

    [Fact]
    public async Task Command_409Conflict_PreservesCommandRejectedExceptionContract() {
        using HttpResponseMessage response = new(HttpStatusCode.Conflict) {
            Content = new StringContent(
                """{"title":"order locked","detail":"please retry"}""",
                Encoding.UTF8,
                "application/problem+json"),
        };

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        CommandRejectedException ex = classification.Failure.ShouldBeOfType<CommandRejectedException>();
        ex.Message.ShouldBe("order locked");
        ex.Resolution.ShouldBe("please retry");
    }

    [Fact]
    public async Task Command_429RateLimited_ProducesCommandWarningException_WithRetryAfter() {
        using HttpResponseMessage response = new((HttpStatusCode)429) {
            Content = new StringContent("""{"title":"rate limited"}""", Encoding.UTF8, "application/problem+json"),
        };
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(System.TimeSpan.FromSeconds(15));

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        CommandWarningException ex = classification.Failure.ShouldBeOfType<CommandWarningException>();
        ex.Kind.ShouldBe(CommandWarningKind.RateLimited);
        ex.RetryAfter.ShouldBe(System.TimeSpan.FromSeconds(15));
    }

    // -----------------------
    // Query response matrix
    // -----------------------

    [Fact]
    public async Task Query_200Ok_PreservesETag() {
        using HttpResponseMessage response = new(HttpStatusCode.OK) {
            Content = new StringContent("""{"payload":[]}""", Encoding.UTF8, "application/json"),
        };
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"");

        EventStoreQueryClassification classification = await _classifier.ClassifyQueryAsync(response, TestContext.Current.CancellationToken);

        classification.Outcome.ShouldBe(QueryClassificationOutcome.Ok);
        classification.ETag.ShouldBe("\"v1\"");
    }

    [Fact]
    public async Task Query_304NotModified_PreservesETag() {
        using HttpResponseMessage response = new(HttpStatusCode.NotModified);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"");

        EventStoreQueryClassification classification = await _classifier.ClassifyQueryAsync(response, TestContext.Current.CancellationToken);

        classification.Outcome.ShouldBe(QueryClassificationOutcome.NotModified);
        classification.ETag.ShouldBe("\"v1\"");
    }

    [Fact]
    public async Task Query_304NotModified_PreservesWeakETagPrefix() {
        using HttpResponseMessage response = new(HttpStatusCode.NotModified);
        response.Headers.ETag = new System.Net.Http.Headers.EntityTagHeaderValue("\"v1\"", isWeak: true);

        EventStoreQueryClassification classification = await _classifier.ClassifyQueryAsync(response, TestContext.Current.CancellationToken);

        classification.ETag.ShouldBe("W/\"v1\"");
    }

    [Fact]
    public async Task Query_401Unauthorized_ProducesAuthRedirectRequiredException() {
        using HttpResponseMessage response = new(HttpStatusCode.Unauthorized);

        EventStoreQueryClassification classification = await _classifier.ClassifyQueryAsync(response, TestContext.Current.CancellationToken);

        _ = classification.Failure.ShouldBeOfType<AuthRedirectRequiredException>();
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, QueryFailureKind.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, QueryFailureKind.NotFound)]
    [InlineData((HttpStatusCode)429, QueryFailureKind.RateLimited)]
    public async Task Query_WarningStatuses_ProduceQueryFailureException(HttpStatusCode status, QueryFailureKind expected) {
        using HttpResponseMessage response = new(status) {
            Content = new StringContent("""{"title":"warn"}""", Encoding.UTF8, "application/problem+json"),
        };

        EventStoreQueryClassification classification = await _classifier.ClassifyQueryAsync(response, TestContext.Current.CancellationToken);

        QueryFailureException ex = classification.Failure.ShouldBeOfType<QueryFailureException>();
        ex.Kind.ShouldBe(expected);
    }

    [Fact]
    public async Task Query_5xx_ProducesGenericHttpRequestException() {
        using HttpResponseMessage response = new(HttpStatusCode.ServiceUnavailable);

        EventStoreQueryClassification classification = await _classifier.ClassifyQueryAsync(response, TestContext.Current.CancellationToken);

        _ = classification.Failure.ShouldBeOfType<HttpRequestException>();
    }

    [Fact]
    public async Task Command_HostileProblemDetailsBody_DoesNotThrow_AndCarriesEmptyPayload() {
        // D14 — malformed JSON body must degrade to ProblemDetailsPayload.Empty rather than
        // bubble JsonException through the classifier. Render path is plain text only.
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest) {
            Content = new StringContent("not-json <script>alert(1)</script>", Encoding.UTF8, "application/problem+json"),
        };

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        CommandValidationException ex = classification.Failure.ShouldBeOfType<CommandValidationException>();
        ex.Problem.Title.ShouldBeNull();
        ex.Problem.ValidationErrors.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Command_OversizedProblemDetailsBody_DegradesToEmptyPayload() {
        string oversized = "{\"title\":\"" + new string('x', 70_000) + "\"}";
        using HttpResponseMessage response = new(HttpStatusCode.BadRequest) {
            Content = new StringContent(oversized, Encoding.UTF8, "application/problem+json"),
        };

        EventStoreCommandClassification classification = await _classifier.ClassifyCommandAsync(response, TestContext.Current.CancellationToken);

        CommandValidationException ex = classification.Failure.ShouldBeOfType<CommandValidationException>();
        ex.Problem.Title.ShouldBeNull();
    }

    [Fact]
    public async Task Command_401Unauthorized_PropagatesCallerCancellationDuringBodyDrain() {
        using CancellationTokenSource cts = new();
        await cts.CancelAsync();
        using HttpResponseMessage response = new(HttpStatusCode.Unauthorized) {
            Content = new StringContent("ignored", Encoding.UTF8, "text/plain"),
        };

        _ = await Should.ThrowAsync<OperationCanceledException>(
            async () => await _classifier.ClassifyCommandAsync(response, cts.Token).ConfigureAwait(true))
            .ConfigureAwait(true);
    }
}
