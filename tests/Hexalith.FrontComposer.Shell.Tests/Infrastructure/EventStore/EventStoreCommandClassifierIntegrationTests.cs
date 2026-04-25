using System.Net;
using System.Text;

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

/// <summary>
/// Story 5-2 T6 / AC2 — exercise the full command-side response taxonomy through
/// EventStoreCommandClient (which now uses the classifier). Each fact pins one of the typed
/// exceptions generated forms react to.
/// </summary>
public class EventStoreCommandClassifierIntegrationTests {
    [Fact]
    public async Task DispatchAsync_400_Throws_CommandValidationException() {
        EventStoreCommandClient sut = NewClient(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) {
            Content = new StringContent(
                """{"title":"Validation failed","errors":{"Quantity":["must be > 0"]}}""",
                Encoding.UTF8,
                "application/problem+json"),
        });

        CommandValidationException ex = await Should.ThrowAsync<CommandValidationException>(
            async () => await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);

        ex.Problem.ValidationErrors["Quantity"][0].ShouldBe("must be > 0");
    }

    [Fact]
    public async Task DispatchAsync_401_Throws_AuthRedirectRequiredException() {
        EventStoreCommandClient sut = NewClient(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        _ = await Should.ThrowAsync<AuthRedirectRequiredException>(
            async () => await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, CommandWarningKind.Forbidden)]
    [InlineData(HttpStatusCode.NotFound, CommandWarningKind.NotFound)]
    [InlineData((HttpStatusCode)429, CommandWarningKind.RateLimited)]
    public async Task DispatchAsync_WarningStatuses_Throw_CommandWarningException(HttpStatusCode status, CommandWarningKind expected) {
        EventStoreCommandClient sut = NewClient(_ => new HttpResponseMessage(status) {
            Content = new StringContent("""{"title":"warn"}""", Encoding.UTF8, "application/problem+json"),
        });

        CommandWarningException ex = await Should.ThrowAsync<CommandWarningException>(
            async () => await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);

        ex.Kind.ShouldBe(expected);
    }

    [Fact]
    public async Task DispatchAsync_409_PreservesCommandRejectedExceptionContract() {
        EventStoreCommandClient sut = NewClient(_ => new HttpResponseMessage(HttpStatusCode.Conflict) {
            Content = new StringContent(
                """{"title":"Order locked","detail":"Wait for the previous edit"}""",
                Encoding.UTF8,
                "application/problem+json"),
        });

        CommandRejectedException ex = await Should.ThrowAsync<CommandRejectedException>(
            async () => await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true))
            .ConfigureAwait(true);

        ex.Message.ShouldBe("Order locked");
        ex.Resolution.ShouldBe("Wait for the previous edit");
    }

    [Fact]
    public async Task DispatchAsync_202AcceptedWithoutBody_ReturnsAcceptedResult() {
        EventStoreCommandClient sut = NewClient(_ => new HttpResponseMessage(HttpStatusCode.Accepted));

        CommandResult result = await sut.DispatchAsync(new ShipOrderCommand(), TestContext.Current.CancellationToken);

        result.MessageId.ShouldBe("01HVTESTULID");
        result.Status.ShouldBe("Accepted");
        result.CorrelationId.ShouldBeNull();
    }

    private static EventStoreCommandClient NewClient(System.Func<HttpRequestMessage, HttpResponseMessage> responseFactory) => new(
        new SingleClientFactory(new ResponseHandler(responseFactory)),
        Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new System.Uri("https://eventstore.test"),
            AccessTokenProvider = _ => System.Threading.Tasks.ValueTask.FromResult<string?>("token"),
        }),
        new FixedUlidFactory(),
        new TestUserContextAccessor("acme", "alice"),
        EventStoreTestSupport.CreateClassifier(),
        NullLogger<EventStoreCommandClient>.Instance);

    [BoundedContext("Orders")]
    private sealed class ShipOrderCommand {
        public string TenantId { get; set; } = "acme";
        public string AggregateId { get; set; } = "order-1";
        public int Quantity { get; set; } = 3;
    }

    private sealed class FixedUlidFactory : IUlidFactory {
        public string NewUlid() => "01HVTESTULID";
    }

    private sealed class TestUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new System.Uri("https://eventstore.test") };
    }

    private sealed class ResponseHandler(System.Func<HttpRequestMessage, HttpResponseMessage> factory) : HttpMessageHandler {
        protected override System.Threading.Tasks.Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
            => System.Threading.Tasks.Task.FromResult(factory(request));
    }
}
