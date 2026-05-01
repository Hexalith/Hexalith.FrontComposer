using System.Net;

using Hexalith.FrontComposer.Contracts.Attributes;
using Hexalith.FrontComposer.Contracts;
using Hexalith.FrontComposer.Contracts.Communication;
using Hexalith.FrontComposer.Contracts.Lifecycle;
using Hexalith.FrontComposer.Contracts.Rendering;
using Hexalith.FrontComposer.Shell.Infrastructure.EventStore;
using Hexalith.FrontComposer.Shell.Infrastructure.Tenancy;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MsOptions = Microsoft.Extensions.Options.Options;

using Shouldly;

using Xunit;

namespace Hexalith.FrontComposer.Shell.Tests.Infrastructure.EventStore;

public sealed class CommandTenantIsolationTests {
    [Fact]
    public async Task DispatchAsync_TenantMismatch_BlocksBeforeSerializationTokenAndHttpSend() {
        int tokenCalls = 0;
        RecordingHandler handler = new();
        EventStoreOptions eventStore = new() {
            BaseAddress = new Uri("https://eventstore.test"),
            AccessTokenProvider = _ => {
                tokenCalls++;
                return ValueTask.FromResult<string?>("token");
            },
        };
        EventStoreCommandClient sut = new(
            new SingleClientFactory(handler),
            MsOptions.Create(eventStore),
            new FixedUlidFactory(),
            new TestUserContextAccessor("tenant-a", "user-a"),
            EventStoreTestSupport.CreateClassifier(),
            NullLogger<EventStoreCommandClient>.Instance,
            MsOptions.Create(new FcShellOptions()));

        TenantContextException ex = await Should.ThrowAsync<TenantContextException>(
            async () => await sut.DispatchAsync(new ExplodingCommand { TenantId = "tenant-b" }, TestContext.Current.CancellationToken)
                .ConfigureAwait(true));

        ex.FailureCategory.ShouldBe(TenantContextFailureCategory.TenantMismatch);
        tokenCalls.ShouldBe(0);
        handler.SendCount.ShouldBe(0);
    }

    [BoundedContext("Orders")]
    private sealed class ExplodingCommand {
        public string TenantId { get; set; } = "tenant-a";
        public string AggregateId { get; set; } = "order-1";
        private readonly string _secret = "payload-secret";
        public string Payload => throw new InvalidOperationException(_secret);
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
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("https://eventstore.test") };
    }

    private sealed class RecordingHandler : HttpMessageHandler {
        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            SendCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted));
        }
    }
}
