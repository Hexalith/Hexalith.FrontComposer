using System.Net;
using System.Text;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

/// <summary>
/// Story 5-1 testing standard: prove the diagnostic surface never leaks bearer tokens, raw
/// payload values, or PII-bearing routing values on validation, HTTP failure, and SignalR
/// join-failure paths (AC7 redaction guarantee).
/// </summary>
public sealed class EventStoreDiagnosticsTests {
    private const string SecretToken = "secret-token-do-not-log";
    private const string SecretPayloadValue = "secret-payload-do-not-log";
    private const string PiiUserId = "alice-pii-do-not-log";

    [Fact]
    public async Task CommandClient_OnNon202Response_DoesNotLogTokenOrPayloadOrPii() {
        CapturingLogger<EventStoreCommandClient> logger = new();
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.BadRequest) {
            Content = new StringContent($$"""{"detail":"{{SecretPayloadValue}}"}""", Encoding.UTF8, "application/problem+json"),
        });
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            BuildOptions(),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", PiiUserId),
            logger);

        _ = await Should.ThrowAsync<HttpRequestException>(
            async () => await sut.DispatchAsync(new SecretCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        AssertNoSensitiveLeak(logger);
    }

    [Fact]
    public async Task CommandClient_OnTokenAcquisitionFailure_DoesNotLeakSecretMaterial() {
        CapturingLogger<EventStoreCommandClient> logger = new();
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted));
        EventStoreOptions options = BuildOptions().Value;
        options.AccessTokenProvider = _ => throw new InvalidOperationException("oauth boom");
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            Microsoft.Extensions.Options.Options.Create(options),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", PiiUserId),
            logger);

        _ = await Should.ThrowAsync<InvalidOperationException>(
            async () => await sut.DispatchAsync(new SecretCommand(), TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        AssertNoSensitiveLeak(logger);
    }

    [Fact]
    public async Task QueryClient_OnNon200Response_DoesNotLogTokenOrPii() {
        CapturingLogger<EventStoreQueryClient> logger = new();
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.PreconditionFailed) {
            Content = new StringContent($$"""{"detail":"{{SecretPayloadValue}}"}""", Encoding.UTF8, "application/problem+json"),
        });
        EventStoreQueryClient sut = new(
            new SingleClientFactory(handler),
            BuildOptions(),
            new TestUserContextAccessor("acme", PiiUserId),
            logger);

        _ = await Should.ThrowAsync<HttpRequestException>(
            async () => await sut.QueryAsync<OrderProjection>(
                new QueryRequest(
                    ProjectionType: "orders",
                    TenantId: "acme",
                    Domain: "orders",
                    AggregateId: "order-1",
                    QueryType: "GetOrders"),
                TestContext.Current.CancellationToken).ConfigureAwait(true)).ConfigureAwait(true);

        AssertNoSensitiveLeak(logger);
    }

    [Fact]
    public async Task CommandClient_OnUnparseableBody_LogsStructuredWarning_WithoutLeakingPayload() {
        // P8: silent fallback to header when JSON parse fails — must surface the contract drift,
        // but only via redacted diagnostics (no body text echoed).
        CapturingLogger<EventStoreCommandClient> logger = new();
        RecordingHandler handler = new(_ => new HttpResponseMessage(HttpStatusCode.Accepted) {
            Content = new StringContent($"not-json {SecretPayloadValue}", Encoding.UTF8, "text/plain"),
        });
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            BuildOptions(),
            new FixedUlidFactory(),
            new TestUserContextAccessor("acme", PiiUserId),
            logger);

        _ = await sut.DispatchAsync(new SecretCommand(), TestContext.Current.CancellationToken);

        logger.Entries.ShouldContain(e => e.Level == LogLevel.Warning && e.Message.Contains("could not be parsed"));
        AssertNoSensitiveLeak(logger);
    }

    private static IOptions<EventStoreOptions> BuildOptions()
        => Microsoft.Extensions.Options.Options.Create(new EventStoreOptions {
            BaseAddress = new Uri("https://eventstore.test"),
            AccessTokenProvider = _ => ValueTask.FromResult<string?>(SecretToken),
        });

    private static void AssertNoSensitiveLeak<T>(CapturingLogger<T> logger) {
        foreach (CapturingLogger<T>.Entry entry in logger.Entries) {
            entry.Message.ShouldNotContain(SecretToken);
            entry.Message.ShouldNotContain(SecretPayloadValue);
            entry.Message.ShouldNotContain(PiiUserId);
        }
    }

    [BoundedContext("Orders")]
    private sealed class SecretCommand {
        public string AggregateId { get; set; } = "order-1";
        public string SecretField { get; set; } = SecretPayloadValue;
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

    internal sealed class CapturingLogger<T> : ILogger<T> {
        public List<Entry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) {
            Entries.Add(new Entry(logLevel, formatter(state, exception)));
        }

        public sealed record Entry(LogLevel Level, string Message);
    }
}
