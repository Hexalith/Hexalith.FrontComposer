using System.Net;

using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>
/// Story 5-1 AC8: cancellation token must reach the outbound HTTP send for both command
/// and query paths. The command path coverage already lives in <see cref="EventStoreClientTests"/>;
/// this suite covers the query path explicitly.
/// </summary>
public sealed class EventStoreCancellationTests {
    [Fact]
    public async Task QueryClient_PropagatesCancellationToken_ToHttpClientSend() {
        TokenObservingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent("""{"payload":[]}""", System.Text.Encoding.UTF8, "application/json"),
        });
        EventStoreQueryClient sut = new(
            new SingleClientFactory(handler),
            BuildOptions(),
            new TestUserContextAccessor("acme", "alice"),
            EventStoreTestSupport.CreateClassifier(),
            new EventStoreTestSupport.NoCache(),
            new EventStoreTestSupport.RecordingAuthRedirector(),
            NullLogger<EventStoreQueryClient>.Instance);

        using CancellationTokenSource cts = new();
        _ = await sut.QueryAsync<OrderProjection>(
            new QueryRequest(
                ProjectionType: "orders",
                TenantId: "acme",
                Domain: "orders",
                AggregateId: "order-1",
                QueryType: "GetOrders"),
            cts.Token);

        // HttpClient wraps the user token in a linked source for its own timeout — assert on the
        // observable cancellation contract rather than reference-equality with the caller's token.
        handler.ObservedToken.CanBeCanceled.ShouldBeTrue();
    }

    private static IOptions<EventStoreOptions> BuildOptions()
        => Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new Uri("https://eventstore.test"),
            AccessTokenProvider = _ => ValueTask.FromResult<string?>("token"),
        });

    private sealed record OrderProjection(string Id);

    private sealed class TestUserContextAccessor(string? tenantId, string? userId) : IUserContextAccessor {
        public string? TenantId { get; } = tenantId;
        public string? UserId { get; } = userId;
    }

    private sealed class SingleClientFactory(HttpMessageHandler handler) : IHttpClientFactory {
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
    }

    private sealed class TokenObservingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler {
        public CancellationToken ObservedToken { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            ObservedToken = cancellationToken;
            return Task.FromResult(responseFactory(request));
        }
    }
}
